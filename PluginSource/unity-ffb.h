#include "pch.h"

#ifdef UNITYFFB_EXPORTS
#define UNITYFFB_API __declspec(dllexport)
#else
#define UNITYFFB_API __declspec(dllimport)
#endif

#define SAFE_DELETE(p)  { if(p) { delete (p);     (p)=NULL; } }
#define SAFE_RELEASE(p) { if(p) { (p)->Release(); (p)=NULL; } }

LPDIRECTINPUT8          g_pDI = NULL;
LPDIRECTINPUTDEVICE8    g_pDevice = NULL;
std::vector<LPDIRECTINPUTEFFECT> g_pEffects;
BOOL                    g_bActive = TRUE;
DWORD                   g_dwNumForceFeedbackAxis = 0;

BOOL CALLBACK _cbEnumFFBDevices(const DIDEVICEINSTANCE* pInst, VOID* pContext);

VOID ClearDeviceInstances();

extern "C"
{
   struct DeviceInfo {
      LPSTR guidInstance;
      LPSTR guidProduct;
      DWORD deviceType;
      LPSTR instanceName;
      LPSTR productName;
   };

   struct DeviceInfoList {
      DeviceInfo entries[16];
   };

   UNITYFFB_API HRESULT InitDirectInput();
   UNITYFFB_API DeviceInfo* EnumerateFFBDevices(int &deviceCount);
   UNITYFFB_API HRESULT CreateFFBDevice(LPCSTR guidInstance);
   UNITYFFB_API int AddFFBEffect();
   //UNITYFFB_API HRESULT InitForceFeedback();
   //UNITYFFB_API HRESULT SetDeviceForcesXY(INT x, INT y);
   //UNITYFFB_API HRESULT StartEffect();
   //UNITYFFB_API HRESULT StopEffect();
   //UNITYFFB_API HRESULT SetAutoCenter(bool autoCentre);
   //UNITYFFB_API HRESULT SetAxisMode(bool absolute);
   UNITYFFB_API VOID FreeFFBDevice();
   UNITYFFB_API VOID FreeDirectInput();
   UNITYFFB_API VOID Shutdown();
}
