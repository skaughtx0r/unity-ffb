#include "pch.h"
#include "util.h"
#include "di-device.h"

DIDevice::DIDevice(LPDIRECTINPUT8 pDI, GUID deviceGuid, DeviceInfo* deviceInfo) {
   this->pDevice = NULL;
   this->deviceGuid = deviceGuid;
   this->pDI = pDI;
   this->deviceInfo = deviceInfo;
}

HRESULT DIDevice::CreateDevice()
{
   if (pDevice) {
      DestroyDevice();
   }

   HRESULT hr = pDI->CreateDevice(deviceGuid, &pDevice, NULL);

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
}

DeviceAxisInfo* DIDevice::EnumerateFFBAxes(int &axisCount)
{
   if (pDevice == NULL) {
      return NULL;
   }

   _axisCount = 0;
   ClearDeviceAxes();
   pDevice->EnumObjects(_cbEnumFFBAxes, (void*)this, DIDFT_AXIS);

   if (vDeviceAxes.size() > 0)
   {
      axisCount = (int)vDeviceAxes.size();
      return &vDeviceAxes[0];
   }
   else {
      axisCount = 0;
   }
   return NULL;
}

BOOL CALLBACK DIDevice::_cbEnumFFBAxes(const DIDEVICEOBJECTINSTANCE* pdidoi, void* pContext)
{
   if ((pdidoi->dwFlags & DIDOI_FFACTUATOR) != 0)
   {
      DIDevice* me = (DIDevice*)pContext;
      me->_axisCount++;

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
   if (pDevice == NULL)
   {
      return E_FAIL;
   }

   if (mEffects.find(effectType) != mEffects.end())
   {
      // You cannot add an effect that is already added.
      return E_ABORT;
   }

   int axisCount = (int)vDeviceAxes.size();
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
         pEffect->Start(1, 0);
      }
   }

   return hr;
}

HRESULT DIDevice::RemoveFFBEffect(Effects::Type effectType)
{
   HRESULT hr = E_FAIL;

   if (mEffects.find(effectType) != mEffects.end())
   {
      LPDIRECTINPUTEFFECT pEffect = mEffects[effectType];

      pEffect->Stop();
      pEffect->Release();
      mEffects.erase(effectType);
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

      DICONSTANTFORCE constantForce;

      int axisCount = (int)vDeviceAxes.size();

      constantForce.lMagnitude = magnitude;

      DIEFFECT effect = mDIEFFECTs[Effects::Type::ConstantForce];
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
HRESULT DIDevice::UpdateSpring(DICONDITION* conditions)
{
   HRESULT hr = E_FAIL;

   if (mEffects.find(Effects::Type::Spring) != mEffects.end())
   {
      LPDIRECTINPUTEFFECT pEffect = mEffects[Effects::Type::Spring];

      int axisCount = (int)vDeviceAxes.size();

      DIEFFECT effect = mDIEFFECTs[Effects::Type::Spring];
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

      hr = pDevice->SetProperty(DIPROP_AUTOCENTER, &dipdw.diph);
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
}