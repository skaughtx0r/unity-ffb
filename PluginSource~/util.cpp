#include "pch.h"
#include "util.h"

/**
 * Helper function for converting wide strings to regular strings
 */
std::string utf16ToUTF8(const std::wstring &s)
{
   const int size = ::WideCharToMultiByte(CP_UTF8, 0, s.c_str(), -1, NULL, 0, 0, NULL);

   std::vector<char> buf(size);
   ::WideCharToMultiByte(CP_UTF8, 0, s.c_str(), -1, &buf[0], size, 0, NULL);

   return std::string(&buf[0]);
}

/**
 * Helper to find the main window handle for the given process ID.
 */
HWND FindMainWindow(unsigned long process_id)
{
   handle_data data;
   data.process_id = process_id;
   data.window_handle = 0;
   EnumWindows(_cbEnumWindows, (LPARAM)&data);
   return data.window_handle;
}

/**
 * Callback function for EnumWindows, this checks each window to see
 * if the handle is the main window for the given process ID.
 */
BOOL CALLBACK _cbEnumWindows(HWND handle, LPARAM lParam)
{
   handle_data& data = *(handle_data*)lParam;
   unsigned long process_id = 0;
   GetWindowThreadProcessId(handle, &process_id);
   if (data.process_id != process_id || !IsMainWindow(handle))
      return TRUE;
   data.window_handle = handle;
   return FALSE;
}

/**
 * Whether or not the handle is to the main window.
 */
BOOL IsMainWindow(HWND handle)
{
   return GetWindow(handle, GW_OWNER) == (HWND)0 && IsWindowVisible(handle);
}

/**
 * Converts a DirectInput Axis GUID to a DIJOYSTATE axis
 */
DWORD GuidToDIJOFS(GUID axisType)
{
   if (axisType == GUID_XAxis)
   {
      return DIJOFS_X;
   }
   else if (axisType == GUID_YAxis)
   {
      return DIJOFS_Y;
   }
   else if (axisType == GUID_ZAxis)
   {
      return DIJOFS_Z;
   }
   else if (axisType == GUID_RxAxis)
   {
      return DIJOFS_RX;
   }
   else if (axisType == GUID_RyAxis)
   {
      return DIJOFS_RY;
   }
   else if (axisType == GUID_RzAxis)
   {
      return DIJOFS_RZ;
   }

   return 0;
}

float clamp(float val, float min, float max) {
   const float outVal = val < min ? min : val;
   return outVal > max ? max : outVal;
}

void FlattenDIJOYSTATE2(DIJOYSTATE2& deviceState, FlatJoyState2& state) {
   // ButtonA
   for (int i = 0; i < 64; i++) { // In banks of 64, shift in the sate of each button BankA 0-63
      if (deviceState.rgbButtons[i] == 128) // 128 = Button pressed
         state.buttonsA |= (unsigned long long)1 << i; // Shift in a 1 to the button at index i
   }
   // ButtonB
   for (int i = 64; i < 128; i++) { // 2nd bank of buttons from 64-128
      if (deviceState.rgbButtons[i] == 128) // 128 = Button pressed
         state.buttonsB |= (unsigned long long)1 << i; // Shift in a 1 to the button at index i
   }

   state.lX = deviceState.lX; // X-axis
   state.lY = deviceState.lY; // Y-axis
   state.lZ = deviceState.lZ; // Z-axis
   // rglSlider
   state.lU = deviceState.rglSlider[0]; // U-axis
   state.lV = deviceState.rglSlider[1]; // V-axis

   state.lRx = deviceState.lRx; // X-axis rotation
   state.lRy = deviceState.lRy; // Y-axis rotation
   state.lRz = deviceState.lRz; // Z-axis rotation

   state.lVX = deviceState.lVX; // X-axis velocity
   state.lVY = deviceState.lVY; // Y-axis velocity
   state.lVZ = deviceState.lVZ; // Z-axis velocity
   // rglVSlider
   state.lVU = deviceState.rglVSlider[0]; // U-axis velocity
   state.lVV = deviceState.rglVSlider[1]; // V-axis velocity

   state.lVRx = deviceState.lVRx; // X-axis angular velocity
   state.lVRy = deviceState.lVRy; // Y-axis angular velocity
   state.lVRz = deviceState.lVRz; // Z-axis angular velocity

   state.lAX = deviceState.lAX; // X-axis acceleration
   state.lAY = deviceState.lAY; // Y-axis acceleration
   state.lAZ = deviceState.lAZ; // Z-axis acceleration
   // rglASlider
   state.lAU = deviceState.rglASlider[0]; // U-axis acceleration
   state.lAV = deviceState.rglASlider[1]; // V-axis acceleration

   state.lARx = deviceState.lARx; // X-axis angular acceleration
   state.lARy = deviceState.lARy; // Y-axis angular acceleration
   state.lARz = deviceState.lARz; // Z-axis angular acceleration

   state.lFX = deviceState.lFX; // X-axis force
   state.lFY = deviceState.lFY; // Y-axis force
   state.lFZ = deviceState.lFZ; // Z-axis force
   // rglFSlider
   state.lFU = deviceState.rglFSlider[0]; // U-axis force
   state.lFV = deviceState.rglFSlider[1]; // V-axis force

   state.lFRx = deviceState.lFRx; // X-axis torque
   state.lFRy = deviceState.lFRy; // Y-axis torque
   state.lFRz = deviceState.lFRz; // Z-axis torque

   for (int i = 0; i < 4; i++) { // In banks of 4, shift in the sate of each DPAD 0-16 bits
      switch (deviceState.rgdwPOV[i]) {
      case 0:     state.rgdwPOV |= (byte)(1 << ((i + 1) * 0)); break; // dpad[i]/up, bit = 0     shift into value at stride (i+1) * DPADButton
      case 18000: state.rgdwPOV |= (byte)(1 << ((i + 1) * 1)); break; // dpad[i]/down, bit = 1
      case 27000: state.rgdwPOV |= (byte)(1 << ((i + 1) * 2)); break; // dpad[i]/left, bit = 2
      case 9000:  state.rgdwPOV |= (byte)(1 << ((i + 1) * 3)); break; // dpad[i]/right, bit = 3
      }
   }
}