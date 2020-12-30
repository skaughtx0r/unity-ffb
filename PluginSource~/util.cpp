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