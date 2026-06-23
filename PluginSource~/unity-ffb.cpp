// unity-ffb.cpp : Defines the exported functions for the DLL.
//

#include "pch.h"
#include "framework.h"
#include "dbt.h"
#include "unity-ffb.h"
#include "util.h"
#include "di-device.h"
#include <set>

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

   LogMessage("[UnityFFB] ======== EnumerateDevices START ========");

   // First fetch all devices
   hr = g_pDI->EnumDevices(
      DI8DEVCLASS_ALL,
      _cbEnumDevices,
      NULL,
      DIEDFL_ATTACHEDONLY
   );
   LogMessage("[UnityFFB] Pass 1 (all devices) complete: %d device(s) in map", (int)g_mDeviceInstances.size());

   hr = g_pDI->EnumDevices(
      DI8DEVCLASS_GAMECTRL,
      _cbEnumFFBDevices,
      NULL,
      DIEDFL_ATTACHEDONLY | DIEDFL_FORCEFEEDBACK
   );
   LogMessage("[UnityFFB] Pass 2 (FFB devices) complete");

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
      LogMessage("[UnityFFB] Removing unplugged device: %s", guid.c_str());
      g_mDeviceInstances[guid]->DestroyDevice();
      g_mDeviceInstances.erase(guid);
   }

   // Collapse phantom duplicate HID collections of the same physical device.
   // Some wheel bases (first seen on Fanatec, VID 0x0eb7) enumerate a phantom
   // secondary collection (&col02) with a distinct instance GUID that duplicates
   // the wheel but maps its axes into the wrong DIJOYSTATE2 slots (one-sided
   // wheel, half-range pedals) and can't host an FFB effect. The phantom reports
   // firmwareRevision==0 while the real collection reports nonzero (the FFB caps
   // are identical on both, so they can't be used to tell them apart). Group
   // collections by physical device (HID path up to "&col") and, ONLY when a
   // device exposes more than one collection, drop the firmwareRevision==0 ones.
   //
   // This is intentionally applied to ALL manufacturers (not VID-gated): the bug
   // class is general to composite-HID wheels. Guards keep it conservative:
   //   - single-collection devices are never touched;
   //   - if no collection in a group has a nonzero revision, keep them all;
   //   - g_dedupVendorOptOut lists VIDs to exclude if a device is ever reported
   //     broken by this (e.g. one whose multiple collections are all legitimate
   //     and a real secondary collection happens to report firmwareRevision==0).
   // Every removal is logged so a regression is diagnosable straight from the log.
   {
      // VIDs to exclude from dedup. Add here if a device is reported broken by it.
      static const std::set<WORD> g_dedupVendorOptOut = {};

      std::map<std::string, std::vector<std::string>> deviceGroups; // pathBase -> guids
      for (auto& entry : g_mDeviceInstances) {
         DIDevice* d = entry.second;
         if (g_dedupVendorOptOut.count(d->deviceInfo.vendorId)) { continue; }
         if (d->hidPath.empty() || d->hidPath == "(unknown)") { continue; }
         std::string pathBase = d->hidPath.substr(0, d->hidPath.find("&col"));
         deviceGroups[pathBase].push_back(entry.first);
      }

      std::vector<std::string> duplicatesToRemove;
      for (auto& group : deviceGroups) {
         if (group.second.size() < 2) { continue; } // not duplicated; leave alone
         bool anyReal = false;
         for (auto& guid : group.second) {
            if (g_mDeviceInstances[guid]->firmwareRevision != 0) { anyReal = true; break; }
         }
         if (!anyReal) { continue; } // all zero; can't distinguish, keep them all
         for (auto& guid : group.second) {
            if (g_mDeviceInstances[guid]->firmwareRevision == 0) {
               duplicatesToRemove.push_back(guid);
            }
         }
      }

      for (auto& guid : duplicatesToRemove) {
         DIDevice* d = g_mDeviceInstances[guid];
         LogMessage("[UnityFFB] Removing duplicate collection (firmwareRevision=0): '%s' VID=0x%04x PID=0x%04x guid=%s path=%s",
            d->deviceInfo.instanceName, d->deviceInfo.vendorId, d->deviceInfo.productId,
            guid.c_str(), d->hidPath.c_str());
         d->DestroyDevice();
         g_mDeviceInstances.erase(guid);
      }
   }

   ClearDeviceInstances();
   LogMessage("[UnityFFB] Final devices:");
   for (auto& device : g_mDeviceInstances) {
      LogMessage("[UnityFFB]   '%s' guid=%s VID=0x%04x PID=0x%04x hasFFB=%d",
         device.second->deviceInfo.instanceName,
         device.second->deviceInfo.guidInstance,
         device.second->deviceInfo.vendorId,
         device.second->deviceInfo.productId,
         device.second->deviceInfo.hasFFB);
      g_vDeviceInstances.push_back(device.second->deviceInfo);
   }

   if (g_vDeviceInstances.size() > 0)
   {
      deviceCount = (int)g_vDeviceInstances.size();
      LogMessage("[UnityFFB] ======== EnumerateDevices END: %d device(s) ========", deviceCount);
      return &g_vDeviceInstances[0];
   }
   else {
      deviceCount = 0;
   }
   LogMessage("[UnityFFB] ======== EnumerateDevices END: 0 devices ========");
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
   if (FAILED(hr = g_pDI->CreateDevice(pInst->guidInstance, &dvce, NULL))) {
      LogMessage("[UnityFFB] EnumDevices: CreateDevice failed for '%s' guid=%s hr=0x%08x - skipping",
         strInstanceName.c_str(), strGuidInstance.c_str(), hr);
      return DIENUM_CONTINUE;
   }

   // Get VID/PID
   DIPROPDWORD vidpid;
   vidpid.diph.dwSize = sizeof(DIPROPDWORD);
   vidpid.diph.dwHeaderSize = sizeof(DIPROPHEADER);
   vidpid.diph.dwObj = 0;
   vidpid.diph.dwHow = DIPH_DEVICE;
   if (FAILED(hr = dvce->GetProperty(DIPROP_VIDPID, &vidpid.diph))) {
      LogMessage("[UnityFFB] EnumDevices: GetProperty VIDPID failed for '%s' guid=%s hr=0x%08x - skipping",
         strInstanceName.c_str(), strGuidInstance.c_str(), hr);
      dvce->Release();
      return DIENUM_CONTINUE;
   }

   // Get HID path for logging
   std::string hidPath = "(unknown)";
   DIPROPGUIDANDPATH guidPath;
   guidPath.diph.dwSize = sizeof(DIPROPGUIDANDPATH);
   guidPath.diph.dwHeaderSize = sizeof(DIPROPHEADER);
   guidPath.diph.dwObj = 0;
   guidPath.diph.dwHow = DIPH_DEVICE;
   if (!FAILED(dvce->GetProperty(DIPROP_GUIDANDPATH, &guidPath.diph))) {
      hidPath = utf16ToUTF8(guidPath.wszPath);
   }

   // Device capabilities. Empirically, the FFB-specific caps (DIDC_FORCEFEEDBACK,
   // dwFFSamplePeriod, dwFFMinTimeResolution) are IDENTICAL on a Fanatec base's
   // real collection and its phantom duplicate, so they can't tell them apart.
   // dwFirmwareRevision/dwHardwareRevision DO differ: the real collection reports
   // a nonzero revision, the phantom reports 0. We capture firmwareRevision here
   // and use it in EnumerateDevices to collapse duplicates. These are static caps
   // (no Acquire / effect creation), so this works even when FFB is never enabled.
   DWORD firmwareRevision = 0;
   DIDEVCAPS caps = { 0 };
   caps.dwSize = sizeof(DIDEVCAPS);
   if (!FAILED(dvce->GetCapabilities(&caps))) {
      firmwareRevision = caps.dwFirmwareRevision;
      LogMessage("[UnityFFB] EnumDevices: Caps flags=0x%08x ff=%d axes=%d buttons=%d povs=%d ffSamplePeriod=%d ffMinTimeRes=%d fwRev=%d hwRev=%d",
         caps.dwFlags, (caps.dwFlags & DIDC_FORCEFEEDBACK) != 0,
         caps.dwAxes, caps.dwButtons, caps.dwPOVs,
         caps.dwFFSamplePeriod, caps.dwFFMinTimeResolution,
         caps.dwFirmwareRevision, caps.dwHardwareRevision);
   }
   else {
      LogMessage("[UnityFFB] EnumDevices: GetCapabilities failed for guid=%s", strGuidInstance.c_str());
   }
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

   LogMessage("[UnityFFB] EnumDevices: '%s' product='%s' guid=%s VID=0x%04x PID=0x%04x type=0x%08x path=%s",
      strInstanceName.c_str(), strProductName.c_str(), strGuidInstance.c_str(),
      di.vendorId, di.productId, pInst->dwDevType, hidPath.c_str());

   g_vDeviceInstances.push_back(di);

   if (g_mDeviceInstances.find(strGuidInstance) != g_mDeviceInstances.end()) {
      LogMessage("[UnityFFB] EnumDevices: guid=%s already in map, skipping", strGuidInstance.c_str());
      return DIENUM_CONTINUE;
   }

   DIDevice* device = new DIDevice(g_pDI, pInst->guidInstance, di);
   device->firmwareRevision = firmwareRevision;
   device->hidPath = hidPath;
   g_mDeviceInstances[strGuidInstance] = device;
   LogMessage("[UnityFFB] EnumDevices: Added '%s' to device map", strInstanceName.c_str());

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
   std::string strInstanceName = utf16ToUTF8(pInst->tszInstanceName);

   LogMessage("[UnityFFB] EnumFFBDevices: '%s' guid=%s", strInstanceName.c_str(), strGuidInstance.c_str());

   if (g_mDeviceInstances.find(strGuidInstance) != g_mDeviceInstances.end()) {
      g_mDeviceInstances[strGuidInstance]->deviceInfo.hasFFB = true;
      LogMessage("[UnityFFB] EnumFFBDevices: Marked '%s' as FFB-capable", strInstanceName.c_str());
   } else {
      LogMessage("[UnityFFB] EnumFFBDevices: WARNING - guid=%s not found in device map, hasFFB NOT set", strGuidInstance.c_str());
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

/**
 * Set the directory for the log file. Call before StartDirectInput.
 * Defaults to %LOCALAPPDATA%Low\unity-ffb\ if not set.
 */
void SetLogPath(LPCSTR path)
{
   SetLogDirectory(path);
}

