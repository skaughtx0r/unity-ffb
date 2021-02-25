// unity-ffb.cpp : Defines the exported functions for the DLL.
//

#include "pch.h"
#include "framework.h"
#include "unity-ffb.h"
#include "util.h"

std::vector<DeviceInfo> g_vDeviceInstances;
std::vector<DeviceAxisInfo> g_vDeviceAxes;
std::map<Effects::Type, LPDIRECTINPUTEFFECT> g_mEffects;
std::map<Effects::Type, DIEFFECT> g_mDIEFFECTs;

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
   return DirectInput8Create(
      GetModuleHandle(NULL),
      DIRECTINPUT_VERSION,
      IID_IDirectInput8,
      (void**)&g_pDI,
      NULL
   );
}

/**
 * Returns an array of DeviceInfo's that has some basic information
 * about each force feedback device. To create a device, pass its
 * guidInstance to the CreateDevice function.
 */
DeviceInfo* EnumerateFFBDevices(int &deviceCount)
{
   if (g_pDI == NULL)
   {
      return NULL;
   }
   ClearDeviceInstances();
   HRESULT hr = g_pDI->EnumDevices(
      DI8DEVCLASS_GAMECTRL,
      _cbEnumFFBDevices,
      NULL,
      DIEDFL_ATTACHEDONLY | DIEDFL_FORCEFEEDBACK
   );
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

/**
 * Called once for each enumerated force feedback device. Each found device is pushed
 * to an array that will be returned.
 */
BOOL CALLBACK _cbEnumFFBDevices(const DIDEVICEINSTANCE* pInst, void* pContext)
{
   DeviceInfo di = { 0 };

   OLECHAR* guidInstance;
   StringFromCLSID(pInst->guidInstance, &guidInstance);
   OLECHAR* guidProduct;
   StringFromCLSID(pInst->guidProduct, &guidProduct);

   std::string strGuidInstance = utf16ToUTF8(guidInstance);
   std::string strGuidProduct = utf16ToUTF8(guidProduct);
   std::string strInstanceName = utf16ToUTF8(pInst->tszInstanceName);
   std::string strProductName = utf16ToUTF8(pInst->tszProductName);

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

   return DIENUM_CONTINUE;
}

/**
 * Create a force feedback device. The guid of the device you want to create must
 * be passed in. The guid can be obtained by looking at the array of enumerated
 * devices.
 */
HRESULT CreateFFBDevice(LPCSTR guidInstance)
{
   if (g_pDevice) {
      FreeFFBDevice();
   }
   GUID deviceGuid;
   int wcharCount = MultiByteToWideChar(CP_UTF8, 0, guidInstance, -1, NULL, 0);
   WCHAR* wstrGuidInstance = new WCHAR[wcharCount];
   MultiByteToWideChar(CP_UTF8, 0, guidInstance, -1, wstrGuidInstance, wcharCount);
   CLSIDFromString(wstrGuidInstance, &deviceGuid);
   delete[] wstrGuidInstance;

   LPDIRECTINPUTDEVICE8 pDevice;

   HRESULT hr = g_pDI->CreateDevice(deviceGuid, &pDevice, NULL);

   if (FAILED(hr))
   {
      return hr;
   }

   // Not sure if this is necessary.
   if (FAILED(hr = pDevice->SetDataFormat(&c_dfDIJoystick)))
   {
      return hr;
   }

   // Find the main window associated with this process.
   HWND hWnd = FindMainWindow(GetCurrentProcessId());
   // Set the cooperative level to let DInput know how this device should
   // interact with the system and with other DInput applications.
   // Exclusive access is required in order to perform force feedback.
   if (FAILED(hr = pDevice->SetCooperativeLevel(hWnd, DISCL_EXCLUSIVE | DISCL_BACKGROUND)))
   {
      return hr;
   }

   if (FAILED(hr = pDevice->Acquire()))
   {
      return hr;
   }

   g_pDevice = pDevice;

   return S_OK;
}

/**
 * This function will return info about Force Feedback Axes associate with
 * the currently selected device. For a steering wheel, there's typically
 * only 1 axis. This function 
 */
DeviceAxisInfo* EnumerateFFBAxes(int &axisCount)
{
   // For now assume only one active FFB device at a time.
   if (g_pDevice == NULL)
   {
      return NULL;
   }

   DWORD _axisCount = 0;
   ClearDeviceAxes();
   g_pDevice->EnumObjects(_cbEnumFFBAxes, (void*)&_axisCount, DIDFT_AXIS);

   if (g_vDeviceAxes.size() > 0)
   {
      axisCount = (int)g_vDeviceAxes.size();
      return &g_vDeviceAxes[0];
   }
   else {
      axisCount = 0;
   }
   return NULL;
}

BOOL CALLBACK _cbEnumFFBAxes(const DIDEVICEOBJECTINSTANCE* pdidoi, void* pContext)
{
   if ((pdidoi->dwFlags & DIDOI_FFACTUATOR) != 0)
   {
      DWORD* axisCount = (DWORD*)pContext;
      (*axisCount)++;

      DeviceAxisInfo dai = { 0 };
      int daiSize = sizeof(DeviceAxisInfo);
      OLECHAR* guidType;
      StringFromCLSID(pdidoi->guidType, &guidType);

      std::string strGuidType = utf16ToUTF8(guidType);
      std::string strName = utf16ToUTF8(pdidoi->tszName);
      
      dai.guidType = new char[strGuidType.length() + 1];
      dai.name = new char[strName.length() + 1];

      strcpy_s(dai.guidType, strGuidType.length() + 1, strGuidType.c_str());
      strcpy_s(dai.name, strName.length() + 1, strName.c_str());

      dai.offset = pdidoi->dwOfs;
      dai.type = pdidoi->dwType;
      dai.flags = pdidoi->dwFlags;
      dai.ffMaxForce = pdidoi->dwFFMaxForce;
      dai.ffForceResolution = pdidoi->dwFFForceResolution;
      dai.collectionNumber = pdidoi->wCollectionNumber;
      dai.designatorIndex = pdidoi->wDesignatorIndex;
      dai.usagePage = pdidoi->wUsagePage;
      dai.usage = pdidoi->wUsage;
      dai.dimension = pdidoi->dwDimension;
      dai.exponent = pdidoi->wExponent;
      dai.reportId = pdidoi->wReportId;

      g_vDeviceAxes.push_back(dai);
   }

   return DIENUM_CONTINUE;
}

/**
 * Add a Force Feedback Effect to the current device.
 * Currently only supports ConstantForce and Spring.
 * Only one of each effect can be added at a time.
 * Only supports up to 2 axes max.
 */
HRESULT AddFFBEffect(Effects::Type effectType)
{
   if (g_pDevice == NULL)
   {
      return E_FAIL;
   }

   if (g_mEffects.find(effectType) != g_mEffects.end())
   {
      // You cannot add an effect that is already added.
      return E_ABORT;
   }

   int axisCount = (int)g_vDeviceAxes.size();
   if (axisCount == 0)
   {
      // Must run EnumerateAxes first.
      return E_BOUNDS;
   }
   
   DWORD* axes = new DWORD[axisCount];
   LONG* directions = new LONG[axisCount];
   
   // Populate the rgdwAxes value using data
   // from the Axis enumeration.
   // This should make it so it can support up to 6 axes.
   for (int i = 0; i < axisCount; i++)
   {
      DeviceAxisInfo axis = g_vDeviceAxes[i];

      // This is ugly due to storing GUIDs as strings for C#
      GUID axisTypeGuid;
      int wcharCount = MultiByteToWideChar(CP_UTF8, 0, axis.guidType, -1, NULL, 0);
      WCHAR* wstrGuidInstance = new WCHAR[wcharCount];
      MultiByteToWideChar(CP_UTF8, 0, axis.guidType, -1, wstrGuidInstance, wcharCount);
      CLSIDFromString(wstrGuidInstance, &axisTypeGuid);
      delete[] wstrGuidInstance;
      
      axes[i] = GuidToDIJOFS(axisTypeGuid);
      directions[i] = 0;
   }

   DIEFFECT effect = { 0 };
   effect.dwSize = sizeof(DIEFFECT);
   effect.dwFlags = DIEFF_CARTESIAN | DIEFF_OBJECTOFFSETS;
   effect.dwDuration = INFINITE;
   effect.dwSamplePeriod = 0;
   effect.dwGain = DI_FFNOMINALMAX;
   effect.dwTriggerButton = DIEB_NOTRIGGER;
   effect.dwTriggerRepeatInterval = 0;
   effect.cAxes = axisCount;
   effect.rgdwAxes = axes;
   effect.rglDirection = directions;
   effect.lpEnvelope = NULL;
   effect.dwStartDelay = 0;

   GUID guidType = {};
   ZeroMemory(&guidType, sizeof(GUID));

   DICONSTANTFORCE* constantForce = NULL;
   DICONDITION* conditions = NULL;
   if (effectType == Effects::Type::ConstantForce)
   {
      constantForce = new DICONSTANTFORCE();
      constantForce->lMagnitude = 0;
      effect.cbTypeSpecificParams = sizeof(DICONSTANTFORCE);
      effect.lpvTypeSpecificParams = constantForce;
      guidType = GUID_ConstantForce;
   }
   else if (effectType == Effects::Type::Spring)
   {
      conditions = new DICONDITION[axisCount];
      ZeroMemory(conditions, sizeof(DICONDITION) * axisCount);
      effect.cbTypeSpecificParams = sizeof(DICONDITION) * axisCount;
      effect.lpvTypeSpecificParams = conditions;
      guidType = GUID_Spring;
   }
   
   HRESULT hr = E_FAIL;
   if (guidType != GUID_NULL)
   {
      LPDIRECTINPUTEFFECT pEffect;
      hr = g_pDevice->CreateEffect(guidType, &effect, &pEffect, NULL);
      if (!FAILED(hr))
      {
         hr = S_OK;
         g_mEffects[effectType] = pEffect;
         g_mDIEFFECTs[effectType] = effect;
         pEffect->Start(1, 0);
      }
   }

   return hr;
}

/**
 * Remove a force feedback effect by type.
 */
HRESULT RemoveFFBEffect(Effects::Type effectType)
{
   HRESULT hr = E_FAIL;

   if (g_mEffects.find(effectType) != g_mEffects.end())
   {
      LPDIRECTINPUTEFFECT pEffect = g_mEffects[effectType];

      pEffect->Stop();
      pEffect->Release();
      g_mEffects.erase(effectType);
      g_mDIEFFECTs.erase(effectType);

      hr = S_OK;
   }

   return hr;
}

/**
 * This will start all force feedback effects.
 */
void StartAllFFBEffects()
{
   for (auto const& effect : g_mEffects) {
      if (effect.second != NULL) {
         effect.second->Start(1, 0);
      }
   }
}

/**
 * This will stop all force feedback effects.
 */
void StopAllFFBEffects()
{
   for (auto const& effect : g_mEffects) {
      if (effect.second != NULL) {
         effect.second->Stop();
      }
   }
}

/**
 * Update the gain for the specified effect.
 *
 * Takes gainPercent value between 0 - 1 and multiplies with
 * DI_FFNOMINALMAX (10000)
 */
HRESULT UpdateEffectGain(Effects::Type effectType, float gainPercent)
{
   HRESULT hr = E_FAIL;

   if (g_mEffects.find(effectType) != g_mEffects.end())
   {
      LPDIRECTINPUTEFFECT pEffect = g_mEffects[effectType];
      DIEFFECT effect = g_mDIEFFECTs[effectType];
      effect.dwSize = sizeof(DIEFFECT);
      effect.dwGain = (DWORD)(clamp(gainPercent, 0.0, 1.0) * DI_FFNOMINALMAX);

      hr = pEffect->SetParameters(&effect, DIEP_GAIN | DIEP_START);
   }

   return hr;
}

/**
 * Update the Constant Force Effect.
 * 
 * Magnitude is the magnitude of the force on all axes.
 * Directions is an array of directions for each axis on the device.
 * The size of the array must match the number of axes on the device.
 */
HRESULT UpdateConstantForce(LONG magnitude, LONG* directions)
{
   HRESULT hr = E_FAIL;

   if (g_mEffects.find(Effects::Type::ConstantForce) != g_mEffects.end())
   {
      LPDIRECTINPUTEFFECT pEffect = g_mEffects[Effects::Type::ConstantForce];

      DICONSTANTFORCE constantForce;

      int axisCount = (int)g_vDeviceAxes.size();

      constantForce.lMagnitude = magnitude;
      
      DIEFFECT effect = g_mDIEFFECTs[Effects::Type::ConstantForce];
      effect.cAxes = axisCount;
      for (int i = 0; i < axisCount; i++) {
         effect.rglDirection[i] = directions[i];
      }
      effect.cbTypeSpecificParams = sizeof(DICONSTANTFORCE);
      effect.lpvTypeSpecificParams = &constantForce;

      hr = pEffect->SetParameters(&effect, DIEP_DIRECTION | DIEP_TYPESPECIFICPARAMS | DIEP_START);
   }

   return hr;
}

/**
 * Updates the spring effect. You must pass an array of conditions that's
 * size matches the number of axes on the device.
 */
HRESULT UpdateSpring(DICONDITION* conditions)
{
   HRESULT hr = E_FAIL;

   if (g_mEffects.find(Effects::Type::Spring) != g_mEffects.end())
   {
      LPDIRECTINPUTEFFECT pEffect = g_mEffects[Effects::Type::Spring];

      int axisCount = (int)g_vDeviceAxes.size();

      DIEFFECT effect = g_mDIEFFECTs[Effects::Type::Spring];
      effect.cAxes = axisCount;
      effect.cbTypeSpecificParams = sizeof(DICONDITION) * axisCount;
      for (int i = 0; i < axisCount; i++) {
         ((DICONDITION*)effect.lpvTypeSpecificParams)[i].lOffset = conditions[i].lOffset;
         ((DICONDITION*)effect.lpvTypeSpecificParams)[i].lPositiveCoefficient = conditions[i].lPositiveCoefficient;
         ((DICONDITION*)effect.lpvTypeSpecificParams)[i].lNegativeCoefficient = conditions[i].lNegativeCoefficient;
         ((DICONDITION*)effect.lpvTypeSpecificParams)[i].dwPositiveSaturation = conditions[i].dwPositiveSaturation;
         ((DICONDITION*)effect.lpvTypeSpecificParams)[i].dwNegativeSaturation = conditions[i].dwNegativeSaturation;
      }

      hr = pEffect->SetParameters(&effect, DIEP_DIRECTION | DIEP_TYPESPECIFICPARAMS | DIEP_START);
   }

   return hr;
}

/**
 * Toggle the auto centering spring for the device.
 */
HRESULT SetAutoCenter(bool autoCenter)
{
   HRESULT hr = E_FAIL;

   if (g_pDevice != NULL)
   {
      DIPROPDWORD dipdw;
      dipdw.diph.dwSize = sizeof(DIPROPDWORD);
      dipdw.diph.dwHeaderSize = sizeof(DIPROPHEADER);
      dipdw.diph.dwObj = 0;
      dipdw.diph.dwHow = DIPH_DEVICE;
      dipdw.dwData = autoCenter ? DIPROPAUTOCENTER_ON : DIPROPAUTOCENTER_OFF;

      hr = g_pDevice->SetProperty(DIPROP_AUTOCENTER, &dipdw.diph);
   }

   return hr;
}

/**
 * Clean up the Force Feedback device and any effects.
 */
void FreeFFBDevice()
{     
   for (auto const& effect : g_mEffects) {
      if (effect.second != NULL) {
         effect.second->Stop();
         effect.second->Release();
      }
   }
   g_mEffects.clear();
   if (g_pDevice) {
      g_pDevice->Unacquire();
      g_pDevice->Release();
      g_pDevice = NULL;
   }
}

/**
 * Clean-up DirectInput, device and any effects.
 */
void FreeDirectInput()
{
   FreeFFBDevice();
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
   for (int i = 0; i < g_vDeviceInstances.size(); i++)
   {
      delete[] g_vDeviceInstances[i].guidInstance;
      delete[] g_vDeviceInstances[i].guidProduct;
      delete[] g_vDeviceInstances[i].instanceName;
      delete[] g_vDeviceInstances[i].productName;
   }
   g_vDeviceInstances.clear();
}

/**
 * Clear the global vector of the selected device's axes.
 */
void ClearDeviceAxes()
{
   for (int i = 0; i < g_vDeviceAxes.size(); i++)
   {
      delete[] g_vDeviceAxes[i].guidType;
      delete[] g_vDeviceAxes[i].name;
   }
   g_vDeviceAxes.clear();
}

/**
 * This will stop the DirectInput Force Feedback and
 * clean up all memory and references to devices and effects.
 */
void StopDirectInput()
{
   FreeDirectInput();
   ClearDeviceAxes();
   ClearDeviceInstances();
}