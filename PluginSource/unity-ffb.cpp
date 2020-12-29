// unity-ffb.cpp : Defines the exported functions for the DLL.
//

#include "pch.h"
#include "framework.h"
#include "unity-ffb.h"
#include "util.h"

std::vector<DeviceInfo> g_pDeviceInstances;

/**
 * Get a pointer to the DirectInput 8 interface.
 */
HRESULT InitDirectInput()
{
   if (g_pDI != nullptr)
   {
      return S_OK;
   }
   return DirectInput8Create(
      GetModuleHandle(NULL),
      DIRECTINPUT_VERSION,
      IID_IDirectInput8,
      (VOID**)&g_pDI,
      NULL
   );
}

DeviceInfo* EnumerateFFBDevices(int &deviceCount)
{
   if (g_pDI == NULL) {
      return nullptr;
   }
   ClearDeviceInstances();
   HRESULT hr = g_pDI->EnumDevices(
      DI8DEVCLASS_GAMECTRL,
      _cbEnumFFBDevices,
      NULL,
      DIEDFL_ATTACHEDONLY | DIEDFL_FORCEFEEDBACK
   );
   if (g_pDeviceInstances.size() > 0)
   {
      DeviceInfo* devices = (DeviceInfo*)CoTaskMemAlloc(sizeof(DeviceInfo) * g_pDeviceInstances.size());
      for (int i = 0; i < g_pDeviceInstances.size(); i++) {
         devices[i] = g_pDeviceInstances[i];
      }
      deviceCount = g_pDeviceInstances.size();
      return devices;
   }
   else {
      deviceCount = 0;
   }
   return nullptr;
}

/**
 * Called once for each enumerated force feedback device. Each found device is pushed
 * to an array that will be returned.
 */
BOOL CALLBACK _cbEnumFFBDevices(const DIDEVICEINSTANCE* pInst, VOID* pContext)
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

   di.guidInstance = new char[strGuidInstance.length () + 1];
   di.guidProduct = new char[strGuidProduct.length() + 1];
   di.instanceName = new char[strInstanceName.length() + 1];
   di.productName = new char[strProductName.length() + 1];
   strcpy_s(di.guidInstance, strGuidInstance.length() + 1, strGuidInstance.c_str());
   strcpy_s(di.guidProduct, strGuidProduct.length() + 1, strGuidProduct.c_str());
   di.deviceType = pInst->dwDevType;
   strcpy_s(di.instanceName, strInstanceName.length() + 1, strInstanceName.c_str());
   strcpy_s(di.productName, strProductName.length() + 1, strProductName.c_str());

   g_pDeviceInstances.push_back(di);

   return DIENUM_CONTINUE;
}

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

   // Not sure if this is necessary.
   if (FAILED(hr = g_pDevice->SetDataFormat(&c_dfDIJoystick)))
      return hr;

   // Find the main window associated with this process.
   HWND hWnd = FindMainWindow(GetCurrentProcessId());
   // Set the cooperative level to let DInput know how this device should
   // interact with the system and with other DInput applications.
   // Exclusive access is required in order to perform force feedback.
   if (FAILED(hr = g_pDevice->SetCooperativeLevel(hWnd, DISCL_EXCLUSIVE | DISCL_FOREGROUND)))
      return hr;

   if (FAILED(hr))
   {
      return E_FAIL;
   }

   g_pDevice = pDevice;

   return S_OK;
}

int AddFFBEffect()
{

}

VOID FreeFFBDevice()
{     
   for (int i = 0; i < g_pEffects.size(); i++) {
      g_pEffects[i]->Stop();
      g_pEffects[i]->Release();
      SAFE_RELEASE(g_pEffects[i]);
   }
   g_pEffects.clear();
   if (g_pDevice) {
      g_pDevice->Unacquire();
      g_pDevice->Release();
      SAFE_RELEASE(g_pDevice);
   }
}

/**
 * Clean-up DirectInput and 
 */
VOID FreeDirectInput()
{
   FreeFFBDevice();
   if (g_pDI) {
      g_pDI->Release();
      SAFE_RELEASE(g_pDI);
   }
}

VOID ClearDeviceInstances()
{
   for (int i = 0; i < g_pDeviceInstances.size(); i++) {
      delete[] g_pDeviceInstances[i].guidInstance;
      delete[] g_pDeviceInstances[i].guidProduct;
      delete[] g_pDeviceInstances[i].instanceName;
      delete[] g_pDeviceInstances[i].productName;
   }
   g_pDeviceInstances.clear();
}

VOID Shutdown()
{
   FreeDirectInput();
   ClearDeviceInstances();
}