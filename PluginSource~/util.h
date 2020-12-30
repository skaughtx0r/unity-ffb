#pragma once
#include "pch.h"

std::string utf16ToUTF8(const std::wstring &s);

struct handle_data {
   unsigned long process_id;
   HWND window_handle;
};
HWND FindMainWindow(unsigned long process_id);
BOOL CALLBACK _cbEnumWindows(HWND handle, LPARAM lParam);
BOOL IsMainWindow(HWND handle);

DWORD GuidToDIJOFS(GUID axisType);

float clamp(float val, float min, float max);