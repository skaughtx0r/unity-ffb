#pragma once
#include "common.h"

class DIDevice
{
public:
   LPDIRECTINPUT8 pDI;
   GUID deviceGuid;
   DeviceInfo deviceInfo;
   HWND hWnd;
   DWORD _axisCount;
   std::vector<DeviceAxisInfo> vDeviceAxes;
   std::map<Effects::Type, LPDIRECTINPUTEFFECT> mEffects;
   std::map<Effects::Type, DIEFFECT> mDIEFFECTs;
   DIJOYSTATE2 joyState;

   LPDIRECTINPUTDEVICE8 pDevice;

   DIDevice(LPDIRECTINPUT8 pDI, GUID deviceGuid, const DeviceInfo& deviceInfo);

   HRESULT CreateDevice();
   void DestroyDevice();

   HRESULT GetDeviceState(FlatJoyState2& state);

   DeviceAxisInfo* EnumerateFFBAxes(int &axisCount);
   static BOOL CALLBACK _cbEnumFFBAxes(const DIDEVICEOBJECTINSTANCE* pdidoi, void* pContext);
   void ClearDeviceAxes();

   HRESULT AddFFBEffect(Effects::Type effectType);
   HRESULT RemoveFFBEffect(Effects::Type effectType);
   void StartAllFFBEffects();
   void StopAllFFBEffects();
   HRESULT UpdateEffectGain(Effects::Type effectType, float gainPercent);
   HRESULT UpdateConstantForce(LONG magnitude, LONG* directions);
   HRESULT UpdateSpring(DICONDITION* conditions);
   HRESULT SetAutoCenter(bool autoCenter);
   void DestroyEffects();

};

