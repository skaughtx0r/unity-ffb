#include "pch.h"
#include "common.h"

#ifdef UNITYFFB_EXPORTS
#define UNITYFFB_API __declspec(dllexport)
#else
#define UNITYFFB_API __declspec(dllimport)
#endif

#define SAFE_DELETE(p)  { if(p) { delete (p);     (p)=NULL; } }

LPDIRECTINPUT8          g_pDI = NULL;
BOOL                    g_bActive = TRUE;
DWORD                   g_dwNumForceFeedbackAxis = 0;

BOOL CALLBACK _cbEnumDevices(const DIDEVICEINSTANCE* pInst, void* pContext);
BOOL CALLBACK _cbEnumFFBDevices(const DIDEVICEINSTANCE* pInst, void* pContext);
LRESULT _cbDeviceChanged(int code, WPARAM wParam, LPARAM lParam);

void ClearDeviceInstances();
void FreeDirectInput();

extern "C"
{
   UNITYFFB_API HRESULT StartDirectInput();
   UNITYFFB_API DeviceInfo* EnumerateDevices(int &deviceCount);
   UNITYFFB_API HRESULT CreateDevice(LPCSTR guidInstance);
   UNITYFFB_API void DestroyDevice(LPCSTR guidInstance);
   UNITYFFB_API DeviceAxisInfo* EnumerateFFBAxes(LPCSTR guidInstance, int &axisCount);
   UNITYFFB_API HRESULT AddFFBEffect(LPCSTR guidInstance, Effects::Type effectType);
   UNITYFFB_API HRESULT UpdateEffectGain(LPCSTR guidInstance, Effects::Type effectType, float gainPercent);
   UNITYFFB_API HRESULT UpdateConstantForce(LPCSTR guidInstance, LONG magnitude, LONG* directions);
   UNITYFFB_API HRESULT UpdateSpring(LPCSTR guidInstance, DICONDITION* conditions);
   UNITYFFB_API HRESULT SetAutoCenter(LPCSTR guidInstance, bool autoCenter);
   UNITYFFB_API HRESULT GetDeviceState(LPCSTR guidInstance, FlatJoyState2& state);
   UNITYFFB_API HRESULT RemoveFFBEffect(LPCSTR guidInstance, Effects::Type effectType);
   UNITYFFB_API void RemoveAllFFBEffects(LPCSTR guidInstance);
   UNITYFFB_API void StartAllFFBEffects(LPCSTR guidInstance);
   UNITYFFB_API void StopAllFFBEffects(LPCSTR guidInstance);
   UNITYFFB_API void StopDirectInput();

   typedef void(__stdcall* DeviceChangedCallback)();
   UNITYFFB_API void RegisterDeviceChangedCallback(DeviceChangedCallback fnCallback);
   UNITYFFB_API void UnregisterDeviceChangedCallback();
}
