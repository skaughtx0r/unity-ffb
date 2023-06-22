// unity-ffb.cpp : Defines the exported functions for the DLL.
//

#include "pch.h"
#include "framework.h"
#include "dbt.h"
#include "unity-ffb.h"
#include "util.h"
#include "di-device.h"
// #include <fstream>

std::map<std::string, DIDevice*> g_mDeviceInstances;
std::vector<DeviceInfo> g_vDeviceInstances;

DeviceChangedCallback g_fnDeviceChangedCallback = NULL;
HHOOK g_deviceChangedHook = NULL;

/**
 * This initializes the DirectInput 8 interface.
 *
 * Once this is initialized, we can then enumerate devices
 * and select/create a Force Feedback device.
 */
HRESULT StartDirectInput()
{
   if (g_pDI != NULL)
   {
      return S_OK;
   }

   if (g_deviceChangedHook != NULL) {
      UnhookWindowsHookEx(g_deviceChangedHook);
      g_deviceChangedHook = NULL;
   }

   HMODULE module = GetModuleHandleW(NULL);
   DWORD threadID = GetCurrentThreadId();
   g_deviceChangedHook = SetWindowsHookExW(WH_CALLWNDPROC, (HOOKPROC)&_cbDeviceChanged, module, threadID);

   return DirectInput8Create(
      GetModuleHandle(NULL),
      DIRECTINPUT_VERSION,
      IID_IDirectInput8,
      (void**)&g_pDI,
      NULL
   );
}

void RegisterDeviceChangedCallback(DeviceChangedCallback fnCallback)
{
   g_fnDeviceChangedCallback = fnCallback;
}

void UnregisterDeviceChangedCallback() {
   g_fnDeviceChangedCallback = NULL;
}

void FireDeviceChangedCallback() {
   if (g_fnDeviceChangedCallback != NULL) {
      g_fnDeviceChangedCallback();
   }
}

LRESULT _cbDeviceChanged(int code, WPARAM wParam, LPARAM lParam)
{
   // invalid code skip
   if (code < 0) return CallNextHookEx(NULL, code, wParam, lParam);

   // check if device was added/removed
   PCWPSTRUCT pMsg = PCWPSTRUCT(lParam);
   if (pMsg->message == WM_DEVICECHANGE)
   {
      switch (pMsg->wParam)
      {
      case DBT_DEVICEARRIVAL:
         FireDeviceChangedCallback();
         break;

      case DBT_DEVICEREMOVECOMPLETE:
         FireDeviceChangedCallback();
         break;
      }
   }

   // continue as normal
   return CallNextHookEx(NULL, code, wParam, lParam);
}

/**
 * Returns an array of DeviceInfo's that has some basic information
 * about each force feedback device. To create a device, pass its
 * guidInstance to the CreateDevice function.
 */
DeviceInfo* EnumerateDevices(int &deviceCount)
{
   HRESULT hr = E_FAIL;
   if (g_pDI == NULL)
   {
      return NULL;
   }
   ClearDeviceInstances();

   // First fetch all devices
   hr = g_pDI->EnumDevices(
      DI8DEVCLASS_ALL,
      _cbEnumDevices,
      NULL,
      DIEDFL_ATTACHEDONLY
   );

   hr = g_pDI->EnumDevices(
      DI8DEVCLASS_GAMECTRL,
      _cbEnumFFBDevices,
      NULL,
      DIEDFL_ATTACHEDONLY | DIEDFL_FORCEFEEDBACK
   );

   std::vector<std::string> devicesToRemove;

   // Remove Unplugged devices
   for (auto& device : g_mDeviceInstances) {
      bool found = false;
      for (int i = 0; i < g_vDeviceInstances.size(); i++) {
         if (device.first == std::string(g_vDeviceInstances[i].guidInstance)) {
            found = true;
            break;
         }
      }
      if (!found) {
         devicesToRemove.push_back(device.first);
      }
   }

   // Purge the devices to be removed
   for (auto& guid : devicesToRemove) {
      g_mDeviceInstances[guid]->DestroyDevice();
      g_mDeviceInstances.erase(guid);
   }

   ClearDeviceInstances();
   // std::ofstream log_file("unityffb.log", std::ios_base::out | std::ios_base::app);
   // log_file << std::endl << "Final Direct Input Devices:" << std::endl;
   for (auto& device : g_mDeviceInstances) {
      // log_file << device.second->deviceInfo.instanceName << ": " << device.second->deviceInfo.guidInstance << std::endl;
      g_vDeviceInstances.push_back(device.second->deviceInfo);
   }

   if (g_vDeviceInstances.size() > 0)
   {
      deviceCount = (int)g_vDeviceInstances.size();
      return &g_vDeviceInstances[0];
   }
   else {
      deviceCount = 0;
   }
   return NULL;
}

BOOL CALLBACK _cbEnumDevices(const DIDEVICEINSTANCE* pInst, void* pContext)
{
   LONG deviceType = GET_DIDEVICE_TYPE(pInst->dwDevType);
   LONG deviceSubType = GET_DIDEVICE_SUBTYPE(pInst->dwDevType);
   if (!(
      deviceType == DI8DEVTYPE_JOYSTICK ||
      deviceType == DI8DEVTYPE_GAMEPAD ||
      deviceType == DI8DEVTYPE_DRIVING ||
      deviceType == DI8DEVTYPE_FLIGHT ||
      deviceType == DI8DEVTYPE_1STPERSON ||
      deviceType == DI8DEVTYPE_DEVICECTRL ||
      deviceType == DI8DEVTYPE_SUPPLEMENTAL
      )) {
      return DIENUM_CONTINUE;
   }

   DeviceInfo di = { 0 };

   OLECHAR* guidInstance;
   StringFromCLSID(pInst->guidInstance, &guidInstance);
   OLECHAR* guidProduct;
   StringFromCLSID(pInst->guidProduct, &guidProduct);

   std::string strGuidInstance = utf16ToUTF8(guidInstance);
   std::string strGuidProduct = utf16ToUTF8(guidProduct);
   std::string strInstanceName = utf16ToUTF8(pInst->tszInstanceName);
   std::string strProductName = utf16ToUTF8(pInst->tszProductName);

   HRESULT hr;
   LPDIRECTINPUTDEVICE8 dvce = nullptr;
   if (FAILED(hr = g_pDI->CreateDevice(pInst->guidInstance, &dvce, NULL))) { return true; } // L"CreateDevice failed! 0x%08x", hr

   DIPROPDWORD vidpid;
   vidpid.diph.dwSize = sizeof(DIPROPDWORD);
   vidpid.diph.dwHeaderSize = sizeof(DIPROPHEADER);
   vidpid.diph.dwObj = 0;
   vidpid.diph.dwHow = DIPH_DEVICE;
   if (FAILED(hr = dvce->GetProperty(DIPROP_VIDPID, &vidpid.diph))) { dvce->Release(); return true; } // L"GetProperty failed! Failed to get symbolic path for device 0x%08x", hr
   dvce->Release();

   di.vendorId = LOWORD(vidpid.dwData);
   di.productId = HIWORD(vidpid.dwData);

   di.guidInstance = new char[strGuidInstance.length() + 1];
   di.guidProduct = new char[strGuidProduct.length() + 1];
   di.instanceName = new char[strInstanceName.length() + 1];
   di.productName = new char[strProductName.length() + 1];

   strcpy_s(di.guidInstance, strGuidInstance.length() + 1, strGuidInstance.c_str());
   strcpy_s(di.guidProduct, strGuidProduct.length() + 1, strGuidProduct.c_str());
   di.deviceType = pInst->dwDevType;
   strcpy_s(di.instanceName, strInstanceName.length() + 1, strInstanceName.c_str());
   strcpy_s(di.productName, strProductName.length() + 1, strProductName.c_str());

   g_vDeviceInstances.push_back(di);

   if (IsDuplicateDevice(pInst)) {
      return DIENUM_CONTINUE;
   }

   if (g_mDeviceInstances.find(strGuidInstance) != g_mDeviceInstances.end()) {
      return DIENUM_CONTINUE;
   }

   DIDevice* device = new DIDevice(g_pDI, pInst->guidInstance, di);
   g_mDeviceInstances[strGuidInstance] = device;

   return DIENUM_CONTINUE;
}

/**
 * Called once for each enumerated force feedback device. Each found device is pushed
 * to an array that will be returned.
 */
BOOL CALLBACK _cbEnumFFBDevices(const DIDEVICEINSTANCE* pInst, void* pContext)
{
   OLECHAR* guidInstance;
   StringFromCLSID(pInst->guidInstance, &guidInstance);
   std::string strGuidInstance = utf16ToUTF8(guidInstance);

   if (g_mDeviceInstances.find(strGuidInstance) != g_mDeviceInstances.end()) {
      g_mDeviceInstances[strGuidInstance]->deviceInfo.hasFFB = true;
   }

   return DIENUM_CONTINUE;
}

/**
 * Create a force feedback device. The guid of the device you want to create must
 * be passed in. The guid can be obtained by looking at the array of enumerated
 * devices.
 */
HRESULT CreateDevice(LPCSTR guidInstance)
{
   std::string strInstance = std::string(guidInstance);
   if (g_mDeviceInstances.find(strInstance) != g_mDeviceInstances.end()) {
      return g_mDeviceInstances[strInstance]->CreateDevice();
   }
   return E_FAIL;
}

HRESULT Acquire(LPCSTR guidInstance)
{
   std::string strInstance = std::string(guidInstance);
   if (g_mDeviceInstances.find(strInstance) != g_mDeviceInstances.end()) {
      return g_mDeviceInstances[strInstance]->Acquire();
   }
   return E_FAIL;
}

HRESULT Unacquire(LPCSTR guidInstance)
{
   std::string strInstance = std::string(guidInstance);
   if (g_mDeviceInstances.find(strInstance) != g_mDeviceInstances.end()) {
      return g_mDeviceInstances[strInstance]->Unacquire();
   }
   return E_FAIL;
}

HRESULT GetDeviceState(LPCSTR guidInstance, FlatJoyState2& state)
{
   std::string strInstance = std::string(guidInstance);
   if (g_mDeviceInstances.find(strInstance) != g_mDeviceInstances.end()) {
      return g_mDeviceInstances[strInstance]->GetDeviceState(state);
   }
   return E_FAIL;
}

/**
 * This function will return info about Force Feedback Axes associate with
 * the currently selected device. For a steering wheel, there's typically
 * only 1 axis. This function
 */
DeviceAxisInfo* EnumerateFFBAxes(LPCSTR guidInstance, int &axisCount)
{
   std::string strInstance = std::string(guidInstance);
   if (g_mDeviceInstances.find(strInstance) != g_mDeviceInstances.end()) {
      return g_mDeviceInstances[strInstance]->EnumerateFFBAxes(axisCount);
   }

   return NULL;
}

/**
 * Add a Force Feedback Effect to the current device.
 * Currently only supports ConstantForce and Spring.
 * Only one of each effect can be added at a time.
 * Only supports up to 2 axes max.
 */
HRESULT AddFFBEffect(LPCSTR guidInstance, Effects::Type effectType)
{
   std::string strInstance = std::string(guidInstance);
   if (g_mDeviceInstances.find(strInstance) != g_mDeviceInstances.end()) {
      return g_mDeviceInstances[strInstance]->AddFFBEffect(effectType);
   }

   return E_FAIL;
}

/**
 * Remove a force feedback effect by type.
 */
HRESULT RemoveFFBEffect(LPCSTR guidInstance, Effects::Type effectType)
{
   std::string strInstance = std::string(guidInstance);
   if (g_mDeviceInstances.find(strInstance) != g_mDeviceInstances.end()) {
      return g_mDeviceInstances[strInstance]->RemoveFFBEffect(effectType);
   }

   return E_FAIL;
}

void RemoveAllFFBEffects(LPCSTR guidInstance)
{
   std::string strInstance = std::string(guidInstance);
   if (g_mDeviceInstances.find(strInstance) != g_mDeviceInstances.end()) {
       g_mDeviceInstances[strInstance]->DestroyEffects();
   }
}

/**
 * This will start all force feedback effects.
 */
void StartAllFFBEffects(LPCSTR guidInstance)
{
   std::string strInstance = std::string(guidInstance);
   if (g_mDeviceInstances.find(strInstance) != g_mDeviceInstances.end()) {
      g_mDeviceInstances[strInstance]->StartAllFFBEffects();
   }
}

/**
 * This will stop all force feedback effects.
 */
void StopAllFFBEffects(LPCSTR guidInstance)
{
   std::string strInstance = std::string(guidInstance);
   if (g_mDeviceInstances.find(strInstance) != g_mDeviceInstances.end()) {
      g_mDeviceInstances[strInstance]->StopAllFFBEffects();
   }
}

/**
 * Update the gain for the specified effect.
 *
 * Takes gainPercent value between 0 - 1 and multiplies with
 * DI_FFNOMINALMAX (10000)
 */
HRESULT UpdateEffectGain(LPCSTR guidInstance, Effects::Type effectType, float gainPercent)
{
   std::string strInstance = std::string(guidInstance);
   if (g_mDeviceInstances.find(strInstance) != g_mDeviceInstances.end()) {
      return g_mDeviceInstances[strInstance]->UpdateEffectGain(effectType, gainPercent);
   }

   return E_FAIL;
}

/**
 * Update the Constant Force Effect.
 *
 * Magnitude is the magnitude of the force on all axes.
 * Directions is an array of directions for each axis on the device.
 * The size of the array must match the number of axes on the device.
 */
HRESULT UpdateConstantForce(LPCSTR guidInstance, LONG magnitude, LONG* directions)
{
   std::string strInstance = std::string(guidInstance);
   if (g_mDeviceInstances.find(strInstance) != g_mDeviceInstances.end()) {
      return g_mDeviceInstances[strInstance]->UpdateConstantForce(magnitude, directions);
   }

   return E_FAIL;
}

/**
 * Updates the spring effect. You must pass an array of conditions that's
 * size matches the number of axes on the device.
 */
HRESULT UpdateSpring(LPCSTR guidInstance, DICONDITION* conditions)
{
   std::string strInstance = std::string(guidInstance);
   if (g_mDeviceInstances.find(strInstance) != g_mDeviceInstances.end()) {
      return g_mDeviceInstances[strInstance]->UpdateSpring(conditions);
   }

   return E_FAIL;
}

/**
 * Toggle the auto centering spring for the device.
 */
HRESULT SetAutoCenter(LPCSTR guidInstance, bool autoCenter)
{
   std::string strInstance = std::string(guidInstance);
   if (g_mDeviceInstances.find(strInstance) != g_mDeviceInstances.end()) {
      return g_mDeviceInstances[strInstance]->SetAutoCenter(autoCenter);
   }

   return E_FAIL;
}

/**
 * Clean up the Force Feedback device and any effects.
 */
void DestroyDevice(LPCSTR guidInstance)
{
   std::string strInstance = std::string(guidInstance);
   if (g_mDeviceInstances.find(strInstance) != g_mDeviceInstances.end()) {
      g_mDeviceInstances[strInstance]->DestroyDevice();
   }
}

/**
 * Clean-up DirectInput, device and any effects.
 */
void FreeDirectInput()
{
   if (g_pDI) {
      g_pDI->Release();
      g_pDI = NULL;
   }
}

/**
 * Clear the global vector of enumerated force feedback devices.
 */
void ClearDeviceInstances()
{
   g_vDeviceInstances.clear();
}

/**
 * This will stop the DirectInput Force Feedback and
 * clean up all memory and references to devices and effects.
 */
void StopDirectInput()
{
   if (g_deviceChangedHook != NULL) {
      UnhookWindowsHookEx(g_deviceChangedHook);
      g_deviceChangedHook = NULL;
   }
   for (auto& device : g_mDeviceInstances) {
      device.second->DestroyDevice();
   }
   g_mDeviceInstances.clear();
   ClearDeviceInstances();
   FreeDirectInput();
}

bool IsDuplicateDevice(const DIDEVICEINSTANCE *pInst) {
   HRESULT hr;
   LPDIRECTINPUTDEVICE8 DIDevice = nullptr;
   if (FAILED(hr = g_pDI->CreateDevice(pInst->guidInstance, &DIDevice, NULL))) { return true; } // L"CreateDevice failed! 0x%08x", hr

   DIPROPGUIDANDPATH GUIDPath;
   GUIDPath.diph.dwSize = sizeof(DIPROPGUIDANDPATH);
   GUIDPath.diph.dwHeaderSize = sizeof(DIPROPHEADER);
   GUIDPath.diph.dwObj = 0;
   GUIDPath.diph.dwHow = DIPH_DEVICE;
   if (FAILED(hr = DIDevice->GetProperty(DIPROP_GUIDANDPATH, &GUIDPath.diph))) { DIDevice->Release(); return true; } // L"GetProperty failed! Failed to get symbolic path for device 0x%08x", hr
   DIDevice->Release();

   std::string strInstanceName = utf16ToUTF8(pInst->tszInstanceName);
   std::string hidPath = utf16ToUTF8(GUIDPath.wszPath);
   OLECHAR* guidInstance;
   StringFromCLSID(pInst->guidInstance, &guidInstance);
   std::string strGuidInstance = utf16ToUTF8(guidInstance);

   // std::ofstream log_file("unityffb.log", std::ios_base::out | std::ios_base::app);
   // log_file << strInstanceName << ": " << strGuidInstance << ": " << hidPath << std::endl;

   // col01 is primary device, col02 is secondary, so col02 is the duplicate.
   if (wcsstr(GUIDPath.wszPath, L"&col02") != 0) {
      return true;
   }
   else {
      return false;
   }
}