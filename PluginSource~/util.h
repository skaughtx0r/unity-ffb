#pragma once
#include "pch.h"

#define SAFE_DELETE(p)  { if(p) { delete (p);     (p)=NULL; } }
#define SAFE_DELETE_ARRAY(p)  { if(p) { delete[] (p);     (p)=NULL; } }

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

void FlattenDIJOYSTATE2(DIJOYSTATE2& deviceState, FlatJoyState2& state);

std::function<void()> Debounce(const std::function<void()>&f, int period);

void LogMessage(const char* format, ...);
void SetLogDirectory(LPCSTR path);

// Deep-copy / free the four heap strings inside a DeviceInfo. Every DeviceInfo
// held in a container (g_vDeviceInstances, DIDevice::deviceInfo) owns its own
// strings; these helpers keep that invariant so frees can't dangle a sibling copy.
DeviceInfo DeepCopyDeviceInfo(const DeviceInfo& src);
void FreeDeviceInfoStrings(DeviceInfo& di);