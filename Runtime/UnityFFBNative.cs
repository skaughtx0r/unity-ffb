using System;
using System.Runtime.InteropServices;
using UnityEngine;
using System.Linq;
using System.Threading;

namespace UnityFFB {
    public class Native {
        
        #if UNITY_STANDALONE_WIN
        private const string FFBDLL = "UnityFFB";
        [DllImport(FFBDLL)] public static extern int    StartDirectInput();
        [DllImport(FFBDLL)] public static extern IntPtr EnumerateDevices(ref int deviceCount);
        [DllImport(FFBDLL)] public static extern IntPtr EnumerateFFBAxes(string guidInstance, ref int axisCount);
        [DllImport(FFBDLL)] public static extern int    CreateDevice(string guidInstance);
        [DllImport(FFBDLL)] public static extern void   DestroyDevice(string guidInstance);
        [DllImport(FFBDLL)] public static extern int    AddFFBEffect(string guidInstance, EffectsType effectType);
        [DllImport(FFBDLL)] public static extern int    RemoveFFBEffect(string guidInstance, EffectsType effectType);
        [DllImport(FFBDLL)] public static extern void   RemoveAllFFBEffects(string guidInstance);
        [DllImport(FFBDLL)] public static extern int    UpdateEffectGain(string guidInstance, EffectsType effectType, float gainPercent);
        [DllImport(FFBDLL)] public static extern int    GetDeviceState(string guidInstance, out FlatJoyState2 DeviceStateObj);
        [DllImport(FFBDLL)] public static extern int    UpdateConstantForce(string guidInstance, int magnitude, int[] directions);
        [DllImport(FFBDLL)] public static extern int    UpdateSpring(string guidInstance, DICondition[] conditions);
        [DllImport(FFBDLL)] public static extern int    SetAutoCenter(string guidInstance, bool autoCenter);
        [DllImport(FFBDLL)] public static extern void   StartAllFFBEffects();
        [DllImport(FFBDLL)] public static extern void   StopAllFFBEffects();
        [DllImport(FFBDLL)] public static extern void   StopDirectInput();

        [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void DeviceChangedCallback();
        [DllImport(FFBDLL)] public static extern void RegisterDeviceChangedCallback([MarshalAs(UnmanagedType.FunctionPtr)] DeviceChangedCallback onDeviceChange);
        [DllImport(FFBDLL)] public static extern void UnregisterDeviceChangedCallback();
#endif

    }

    public static class DirectInputManager
    {
        private static bool _isInitialized = false;
        public static bool isInitialized
        {
            get => _isInitialized;
            set { Debug.Log("[UnityFFB] Can't set isInitialized!"); }
        }

        private static DeviceInfo[] _devices = new DeviceInfo[0];

        public static DeviceInfo[] devices
        {
            get => _devices;
            set { Debug.Log("[UnityFFB] Can't set devices!"); }
        }

        public static bool Initialize()
        {
#if UNITY_STANDALONE_WIN
            if (_isInitialized) { return _isInitialized; }

            if (Native.StartDirectInput() != 0) { _isInitialized = false; }

            _isInitialized = true;
            EnumerateDirectInputDevices();

            Debug.Log($"[UnityFFB] Initialized! {_devices.Count()} Devices");
            return _isInitialized;
#endif
        }

        public static void DeInitialize()
        {
            if (_isInitialized)
            {
                _isInitialized = false;
                _devices = new DeviceInfo[0];
                Native.StopDirectInput();
            }
        }

        private static void EnumerateDirectInputDevices()
        {
            int deviceCount = 0;
            IntPtr ptrDevices = Native.EnumerateDevices(ref deviceCount);
            if (deviceCount > 0)
            {
                _devices = new DeviceInfo[deviceCount];

                int deviceSize = Marshal.SizeOf(typeof(DeviceInfo));
                for (int i = 0; i < deviceCount; i++)
                {
                    IntPtr pCurrent = ptrDevices + i * deviceSize;
                    _devices[i] = Marshal.PtrToStructure<DeviceInfo>(pCurrent);
                }
            }
        }

        public static bool CreateDevice(DeviceInfo device)
        {
            int hr = Native.CreateDevice(device.guidInstance);
            if (hr != 0)
            {
                Debug.LogError($"[UnityFFB] CreateFFBDevice Failed: 0x{hr.ToString("x")} {WinErrors.GetSystemMessage(hr)}");
                return false;
            }

            return true;
        }
    }
}
