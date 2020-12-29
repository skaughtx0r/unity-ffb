#include "pch.h"
#include "util.h"

std::string utf16ToUTF8(const std::wstring &s)
{
   const int size = ::WideCharToMultiByte(CP_UTF8, 0, s.c_str(), -1, NULL, 0, 0, NULL);

   std::vector<char> buf(size);
   ::WideCharToMultiByte(CP_UTF8, 0, s.c_str(), -1, &buf[0], size, 0, NULL);

   return std::string(&buf[0]);
}

HWND FindMainWindow(unsigned long process_id)
{
   handle_data data;
   data.process_id = process_id;
   data.window_handle = 0;
   EnumWindows(_cbEnumWindows, (LPARAM)&data);
   return data.window_handle;
}

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

BOOL IsMainWindow(HWND handle)
{
   return GetWindow(handle, GW_OWNER) == (HWND)0 && IsWindowVisible(handle);
}
