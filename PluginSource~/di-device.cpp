#include "pch.h"
#include "util.h"
#include "di-device.h"

DIDevice::DIDevice(LPDIRECTINPUT8 pDI, GUID deviceGuid, const DeviceInfo& deviceInfo) {
   this->pDevice = NULL;
   this->deviceGuid = deviceGuid;
   this->pDI = pDI;
   // Own our strings: the caller's DeviceInfo lives in g_vDeviceInstances, which is
   // cleared on every re-enumeration while this device may live on in the map.
   this->deviceInfo = DeepCopyDeviceInfo(deviceInfo);
   this->joyState = { 0 };
}

DIDevice::~DIDevice() {
   DestroyDevice();
   FreeDeviceInfoStrings(deviceInfo);
}

HRESULT DIDevice::CreateDevice()
{
   if (pDevice) {
      DestroyDevice();
   }

   LogMessage("[UnityFFB] CreateDevice: Creating device for '%s'", deviceInfo.instanceName);

   HRESULT hr = pDI->CreateDevice(deviceGuid, &pDevice, NULL);

   if (FAILED(hr))
   {
      LogMessage("[UnityFFB] CreateDevice: CreateDevice failed hr=0x%08x", hr);
      return hr;
   }

   if (FAILED(hr = pDevice->SetDataFormat(&c_dfDIJoystick2)))
   {
      LogMessage("[UnityFFB] CreateDevice: SetDataFormat failed hr=0x%08x", hr);
      return hr;
   }

   // Find the main window associated with this process.
   HWND hWnd = FindMainWindow(GetCurrentProcessId());
   LogMessage("[UnityFFB] CreateDevice: Found HWND=%p", hWnd);
   // Set the cooperative level to let DInput know how this device should
   // interact with the system and with other DInput applications.
   // Exclusive access is required in order to perform force feedback.
   if (FAILED(hr = pDevice->SetCooperativeLevel(hWnd, DISCL_EXCLUSIVE | DISCL_BACKGROUND)))
   {
      LogMessage("[UnityFFB] CreateDevice: SetCooperativeLevel (EXCLUSIVE) failed hr=0x%08x", hr);
      return hr;
   }

   if (FAILED(hr = pDevice->Acquire()))
   {
      LogMessage("[UnityFFB] CreateDevice: Acquire failed hr=0x%08x", hr);
      return hr;
   }

   LogMessage("[UnityFFB] CreateDevice: Success for '%s'", deviceInfo.instanceName);
   return S_OK;
}

void DIDevice::DestroyDevice()
{
   DestroyEffects();
   ClearDeviceAxes();
   if (pDevice) {
      pDevice->Unacquire();
      pDevice->Release();
      pDevice = NULL;
   }
   // deviceInfo strings are owned by this object and freed in the destructor;
   // DestroyDevice() must stay re-Create-able (CreateDevice can be called again).
}

HRESULT DIDevice::Unacquire()
{
   if (pDevice) {
      HRESULT hr = pDevice->Unacquire();
      LogMessage("[UnityFFB] Unacquire: hr=0x%08x for '%s'", hr, deviceInfo.instanceName);
      return hr;
   }
   LogMessage("[UnityFFB] Unacquire: pDevice is NULL for '%s'", deviceInfo.instanceName);
   return E_FAIL;
}

HRESULT DIDevice::Acquire()
{
   if (pDevice) {
      // Find the main window associated with this process.
      HWND hWnd = FindMainWindow(GetCurrentProcessId());
      // Set the cooperative level to let DInput know how this device should
      // interact with the system and with other DInput applications.
      // Exclusive access is required in order to perform force feedback.
      HRESULT hr;
      if (FAILED(hr = pDevice->SetCooperativeLevel(hWnd, DISCL_EXCLUSIVE | DISCL_BACKGROUND)))
      {
         LogMessage("[UnityFFB] Acquire: SetCooperativeLevel failed hr=0x%08x for '%s'", hr, deviceInfo.instanceName);
         return hr;
      }
      hr = pDevice->Acquire();
      LogMessage("[UnityFFB] Acquire: hr=0x%08x for '%s'", hr, deviceInfo.instanceName);
      return hr;
   }
   LogMessage("[UnityFFB] Acquire: pDevice is NULL for '%s'", deviceInfo.instanceName);
   return E_FAIL;
}

HRESULT DIDevice::GetDeviceState(FlatJoyState2& state)
{
   HRESULT hr = E_FAIL;
   if (pDevice == NULL) {
      return hr;
   }

   hr = pDevice->GetDeviceState(sizeof(DIJOYSTATE2), &joyState);
   FlattenDIJOYSTATE2(joyState, state);

   return hr;
}

DeviceAxisInfo* DIDevice::EnumerateFFBAxes(int &axisCount)
{
   if (pDevice == NULL) {
      LogMessage("[UnityFFB] EnumerateFFBAxes: pDevice is NULL for '%s'", deviceInfo.instanceName);
      return NULL;
   }

   LogMessage("[UnityFFB] EnumerateFFBAxes: Enumerating axes for '%s'", deviceInfo.instanceName);
   _axisCount = 0;
   ClearDeviceAxes();
   pDevice->EnumObjects(_cbEnumFFBAxes, (void*)this, DIDFT_AXIS);

   if (vDeviceAxes.size() > 0)
   {
      axisCount = (int)vDeviceAxes.size();
      LogMessage("[UnityFFB] EnumerateFFBAxes: Found %d FFB axis/axes", axisCount);
      return &vDeviceAxes[0];
   }
   else {
      axisCount = 0;
      LogMessage("[UnityFFB] EnumerateFFBAxes: No FFB axes found (0 axes with DIDOI_FFACTUATOR)");
   }
   return NULL;
}

BOOL CALLBACK DIDevice::_cbEnumFFBAxes(const DIDEVICEOBJECTINSTANCE* pdidoi, void* pContext)
{
   DIDevice* me = (DIDevice*)pContext;
   std::string strName = utf16ToUTF8(pdidoi->tszName);

   OLECHAR* guidTypeStr;
   StringFromCLSID(pdidoi->guidType, &guidTypeStr);
   std::string strGuidType = utf16ToUTF8(guidTypeStr);
   CoTaskMemFree(guidTypeStr);

   bool isFFBActuator = (pdidoi->dwFlags & DIDOI_FFACTUATOR) != 0;
   LogMessage("[UnityFFB] EnumAxis: '%s' type=%s offset=%d flags=0x%08x ffActuator=%d maxForce=%d",
      strName.c_str(), strGuidType.c_str(), pdidoi->dwOfs, pdidoi->dwFlags,
      isFFBActuator, pdidoi->dwFFMaxForce);

   if (isFFBActuator)
   {
      me->_axisCount++;

      DeviceAxisInfo dai = { 0 };
      int daiSize = sizeof(DeviceAxisInfo);

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

      me->vDeviceAxes.push_back(dai);
   }

   return DIENUM_CONTINUE;
}

/**
 * Clear the vector of the device's axes.
 */
void DIDevice::ClearDeviceAxes()
{
   for (int i = 0; i < vDeviceAxes.size(); i++)
   {
      delete[] vDeviceAxes[i].guidType;
      delete[] vDeviceAxes[i].name;
   }
   vDeviceAxes.clear();
}

HRESULT DIDevice::AddFFBEffect(Effects::Type effectType) {
   LogMessage("[UnityFFB] AddFFBEffect: type=%d for '%s'", effectType, deviceInfo.instanceName);

   if (pDevice == NULL)
   {
      LogMessage("[UnityFFB] AddFFBEffect: pDevice is NULL");
      return E_FAIL;
   }

   if (mEffects.find(effectType) != mEffects.end())
   {
      LogMessage("[UnityFFB] AddFFBEffect: Effect type %d already added", effectType);
      return E_ABORT;
   }

   int axisCount = (int)vDeviceAxes.size();
   if (axisCount == 0)
   {
      LogMessage("[UnityFFB] AddFFBEffect: No axes enumerated, must run EnumerateAxes first");
      return E_BOUNDS;
   }

   DWORD* axes = new DWORD[axisCount];
   LONG* directions = new LONG[axisCount];

   // Populate the rgdwAxes value using data
   // from the Axis enumeration.
   // This should make it so it can support up to 6 axes.
   for (int i = 0; i < axisCount; i++)
   {
      DeviceAxisInfo axis = vDeviceAxes[i];

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
      hr = pDevice->CreateEffect(guidType, &effect, &pEffect, NULL);
      if (!FAILED(hr))
      {
         hr = S_OK;
         mEffects[effectType] = pEffect;
         mDIEFFECTs[effectType] = effect;
         LogMessage("[UnityFFB] AddFFBEffect: Created effect type=%d with %d axes", effectType, axisCount);
      }
      else
      {
         LogMessage("[UnityFFB] AddFFBEffect: CreateEffect failed type=%d hr=0x%08x", effectType, hr);
      }
   }

   if (FAILED(hr))
   {
      // Effect not stored, so nothing owns these allocations.
      delete[] axes;
      delete[] directions;
      delete constantForce;
      delete[] conditions;
   }

   return hr;
}

// Free the arrays a stored DIEFFECT points at. ConstantForce's type-specific
// params were a scalar new, Spring's an array new — the delete form must match.
void DIDevice::FreeStoredEffectArrays(Effects::Type effectType, DIEFFECT& effect)
{
   delete[] effect.rgdwAxes;
   effect.rgdwAxes = NULL;
   delete[] effect.rglDirection;
   effect.rglDirection = NULL;
   if (effectType == Effects::Type::ConstantForce) {
      delete (DICONSTANTFORCE*)effect.lpvTypeSpecificParams;
   }
   else if (effectType == Effects::Type::Spring) {
      delete[] (DICONDITION*)effect.lpvTypeSpecificParams;
   }
   effect.lpvTypeSpecificParams = NULL;
}

HRESULT DIDevice::RemoveFFBEffect(Effects::Type effectType)
{
   HRESULT hr = E_FAIL;

   if (mEffects.find(effectType) != mEffects.end())
   {
      LogMessage("[UnityFFB] RemoveFFBEffect: type=%d for '%s'", effectType, deviceInfo.instanceName);
      LPDIRECTINPUTEFFECT pEffect = mEffects[effectType];

      pEffect->Stop();
      pEffect->Release();
      mEffects.erase(effectType);
      FreeStoredEffectArrays(effectType, mDIEFFECTs[effectType]);
      mDIEFFECTs.erase(effectType);

      hr = S_OK;
   }

   return hr;
}

/**
 * Update the gain for the specified effect.
 *
 * Takes gainPercent value between 0 - 1 and multiplies with
 * DI_FFNOMINALMAX (10000)
 */
HRESULT DIDevice::UpdateEffectGain(Effects::Type effectType, float gainPercent)
{
   HRESULT hr = E_FAIL;

   if (mEffects.find(effectType) != mEffects.end())
   {
      LPDIRECTINPUTEFFECT pEffect = mEffects[effectType];
      DIEFFECT effect = mDIEFFECTs[effectType];
      effect.dwSize = sizeof(DIEFFECT);
      effect.dwGain = (DWORD)(clamp(gainPercent, 0.0, 1.0) * DI_FFNOMINALMAX);

      // DIEP_START is load-bearing: the game's focus-regain flow (FFBSettings in
      // HydroSim) relies on a gain update restarting effects stopped by Unacquire.
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
HRESULT DIDevice::UpdateConstantForce(LONG magnitude, LONG* directions)
{
   HRESULT hr = E_FAIL;

   if (mEffects.find(Effects::Type::ConstantForce) != mEffects.end())
   {
      LPDIRECTINPUTEFFECT pEffect = mEffects[Effects::Type::ConstantForce];

      DIEFFECT effect = {};
      DICONSTANTFORCE constantForce;

      constantForce.lMagnitude = magnitude;

      effect.dwSize = sizeof(DIEFFECT);
      effect.cbTypeSpecificParams = sizeof(DICONSTANTFORCE);
      effect.lpvTypeSpecificParams = &constantForce;

      hr = pEffect->SetParameters(&effect, DIEP_TYPESPECIFICPARAMS);
   }

   return hr;
}

/**
 * Updates the spring effect. You must pass an array of conditions that's
 * size matches the number of axes on the device.
 */
HRESULT DIDevice::UpdateSpring(DICONDITION* conditions, int conditionCount)
{
   HRESULT hr = E_FAIL;

   if (mEffects.find(Effects::Type::Spring) != mEffects.end())
   {
      LPDIRECTINPUTEFFECT pEffect = mEffects[Effects::Type::Spring];

      // The stored DIEFFECT's cAxes/cbTypeSpecificParams and the arrays they
      // describe were sized when AddFFBEffect created the effect. The live axis
      // list can differ by now (device-change re-enumeration), so never size
      // writes off vDeviceAxes here — bound them by the creation-time allocation
      // and by how many conditions the caller actually passed.
      DIEFFECT effect = mDIEFFECTs[Effects::Type::Spring];
      int allocCount = (int)(effect.cbTypeSpecificParams / sizeof(DICONDITION));
      int axisCount = conditionCount < allocCount ? conditionCount : allocCount;
      if (axisCount < allocCount) {
         LogMessage("[UnityFFB] UpdateSpring: caller passed %d condition(s) for %d allocated axis/axes on '%s' - updating first %d only",
            conditionCount, allocCount, deviceInfo.instanceName, axisCount);
      }
      for (int i = 0; i < axisCount; i++) {
         ((DICONDITION*)effect.lpvTypeSpecificParams)[i].lOffset = conditions[i].lOffset;
         ((DICONDITION*)effect.lpvTypeSpecificParams)[i].lPositiveCoefficient = conditions[i].lPositiveCoefficient;
         ((DICONDITION*)effect.lpvTypeSpecificParams)[i].lNegativeCoefficient = conditions[i].lNegativeCoefficient;
         ((DICONDITION*)effect.lpvTypeSpecificParams)[i].dwPositiveSaturation = conditions[i].dwPositiveSaturation;
         ((DICONDITION*)effect.lpvTypeSpecificParams)[i].dwNegativeSaturation = conditions[i].dwNegativeSaturation;
      }

      hr = pEffect->SetParameters(&effect, DIEP_TYPESPECIFICPARAMS);
   }

   return hr;
}

/**
 * Toggle the auto centering spring for the device.
 */
HRESULT DIDevice::SetAutoCenter(bool autoCenter)
{
   HRESULT hr = E_FAIL;

   if (pDevice != NULL)
   {
      DIPROPDWORD dipdw;
      dipdw.diph.dwSize = sizeof(DIPROPDWORD);
      dipdw.diph.dwHeaderSize = sizeof(DIPROPHEADER);
      dipdw.diph.dwObj = 0;
      dipdw.diph.dwHow = DIPH_DEVICE;
      dipdw.dwData = autoCenter ? DIPROPAUTOCENTER_ON : DIPROPAUTOCENTER_OFF;

      // Must unacquire to set autocenter property, then re-acquire
      pDevice->Unacquire();
      hr = pDevice->SetProperty(DIPROP_AUTOCENTER, &dipdw.diph);
      LogMessage("[UnityFFB] SetAutoCenter: %s hr=0x%08x for '%s'",
         autoCenter ? "ON" : "OFF", hr, deviceInfo.instanceName);
      pDevice->Acquire();
   }
   else
   {
      LogMessage("[UnityFFB] SetAutoCenter: pDevice is NULL for '%s'", deviceInfo.instanceName);
   }

   return hr;
}

void DIDevice::StartAllFFBEffects()
{
   for (auto const& effect : mEffects) {
      if (effect.second != NULL) {
         effect.second->Start(1, 0);
      }
   }
}

void DIDevice::StopAllFFBEffects()
{
   for (auto const& effect : mEffects) {
      if (effect.second != NULL) {
         effect.second->Stop();
      }
   }
}

void DIDevice::DestroyEffects()
{
   for (auto const& effect : mEffects) {
      if (effect.second != NULL) {
         effect.second->Stop();
         effect.second->Release();
      }
   }
   mEffects.clear();
   for (auto& effect : mDIEFFECTs) {
      FreeStoredEffectArrays(effect.first, effect.second);
   }
   mDIEFFECTs.clear();
}
