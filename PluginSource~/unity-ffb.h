#include "pch.h"

#ifdef UNITYFFB_EXPORTS
#define UNITYFFB_API __declspec(dllexport)
#else
#define UNITYFFB_API __declspec(dllimport)
#endif

#define SAFE_DELETE(p)  { if(p) { delete (p);     (p)=NULL; } }

LPDIRECTINPUT8          g_pDI = NULL;
LPDIRECTINPUTDEVICE8    g_pDevice = NULL;
BOOL                    g_bActive = TRUE;
DWORD                   g_dwNumForceFeedbackAxis = 0;

BOOL CALLBACK _cbEnumFFBDevices(const DIDEVICEINSTANCE* pInst, void* pContext);
BOOL CALLBACK _cbEnumFFBAxes(const DIDEVICEOBJECTINSTANCE* pdidoi, void* pContext);

void ClearDeviceInstances();
void ClearDeviceAxes();
void FreeFFBDevice();
void FreeDirectInput();

extern "C"
{
   struct DeviceInfo {
      DWORD deviceType;
      LPSTR guidInstance;
      LPSTR guidProduct;
      LPSTR instanceName;
      LPSTR productName;
   };

   struct DeviceAxisInfo {
      DWORD offset;
      DWORD type;
      DWORD flags;
      DWORD ffMaxForce;
      DWORD ffForceResolution;
      DWORD collectionNumber;
      DWORD designatorIndex;
      DWORD usagePage;
      DWORD usage;
      DWORD dimension;
      DWORD exponent;
      DWORD reportId;
      LPSTR guidType;
      LPSTR name;
   };

   struct Effects {
      typedef enum {
         ConstantForce = 0,
         RampForce = 1,
         Square = 2,
         Sine = 3,
         Triangle = 4,
         SawtoothUp = 5,
         SawtoothDown = 6,
         Spring = 7,
         Damper = 8,
         Inertia = 9,
         Friction = 10,
         CustomForce = 11
      } Type;
   };

   UNITYFFB_API HRESULT StartDirectInput();
   UNITYFFB_API DeviceInfo* EnumerateFFBDevices(int &deviceCount);
   UNITYFFB_API HRESULT CreateFFBDevice(LPCSTR guidInstance);
   UNITYFFB_API DeviceAxisInfo* EnumerateFFBAxes(int &axisCount);
   UNITYFFB_API HRESULT AddFFBEffect(Effects::Type effectType);
   UNITYFFB_API HRESULT UpdateEffectGain(Effects::Type effectType, float gainPercent);
   UNITYFFB_API HRESULT UpdateConstantForce(LONG magnitude, LONG* directions);
   UNITYFFB_API HRESULT UpdateSpring(DICONDITION* conditions);
   UNITYFFB_API HRESULT SetAutoCenter(bool autoCenter);
   UNITYFFB_API void StartAllFFBEffects();
   UNITYFFB_API void StopAllFFBEffects();
   UNITYFFB_API void StopDirectInput();
}
