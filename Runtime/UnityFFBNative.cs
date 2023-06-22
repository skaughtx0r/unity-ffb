using System;
using System.Runtime.InteropServices;
using UnityEngine;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnityFFB {
    public class Native {

        #if UNITY_STANDALONE_WIN
        private const string FFBDLL = "UnityFFB";
        [DllImport(FFBDLL)] public static extern int    StartDirectInput();
        [DllImport(FFBDLL)] public static extern IntPtr EnumerateDevices(ref int deviceCount);
        [DllImport(FFBDLL)] public static extern IntPtr EnumerateFFBAxes(string guidInstance, ref int axisCount);
        [DllImport(FFBDLL)] public static extern int    CreateDevice(string guidInstance);
        [DllImport(FFBDLL)] public static extern void   DestroyDevice(string guidInstance);
        [DllImport(FFBDLL)] public static extern int    Acquire(string guidInstance);
        [DllImport(FFBDLL)] public static extern int    Unacquire(string guidInstance);
        [DllImport(FFBDLL)] public static extern int    AddFFBEffect(string guidInstance, EffectsType effectType);
        [DllImport(FFBDLL)] public static extern int    RemoveFFBEffect(string guidInstance, EffectsType effectType);
        [DllImport(FFBDLL)] public static extern void   RemoveAllFFBEffects(string guidInstance);
        [DllImport(FFBDLL)] public static extern int    UpdateEffectGain(string guidInstance, EffectsType effectType, float gainPercent);
        [DllImport(FFBDLL)] public static extern int    GetDeviceState(string guidInstance, out FlatJoyState2 DeviceStateObj);
        [DllImport(FFBDLL)] public static extern int    UpdateConstantForce(string guidInstance, int magnitude, int[] directions);
        [DllImport(FFBDLL)] public static extern int    UpdateSpring(string guidInstance, DICondition[] conditions);
        [DllImport(FFBDLL)] public static extern int    SetAutoCenter(string guidInstance, bool autoCenter);
        [DllImport(FFBDLL)] public static extern void   StartAllFFBEffects(string guidInstance);
        [DllImport(FFBDLL)] public static extern void   StopAllFFBEffects(string guidInstance);
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

            Native.RegisterDeviceChangedCallback(OnDeviceChanged);

            _isInitialized = true;
            EnumerateDirectInputDevices();

            Debug.Log($"[UnityFFB] Initialized! {_devices.Count()} Devices");
            return _isInitialized;
#else
            _isInitialized = false;
            return false;
#endif
        }

        public static void DeInitialize()
        {
#if UNITY_STANDALONE_WIN
            if (_isInitialized)
            {
                _isInitialized = false;
                _devices = new DeviceInfo[0];
                Native.StopDirectInput();
                Native.UnregisterDeviceChangedCallback();
            }
#endif
        }

        private static void EnumerateDirectInputDevices()
        {
#if UNITY_STANDALONE_WIN
            int deviceCount = 0;
            IntPtr ptrDevices = Native.EnumerateDevices(ref deviceCount);
            _devices = new DeviceInfo[deviceCount];
            if (deviceCount > 0)
            {
                int deviceSize = Marshal.SizeOf(typeof(DeviceInfo));
                for (int i = 0; i < deviceCount; i++)
                {
                    IntPtr pCurrent = ptrDevices + i * deviceSize;
                    _devices[i] = Marshal.PtrToStructure<DeviceInfo>(pCurrent);
                }
            }
#endif
        }

        public static bool CreateDevice(DeviceInfo device)
        {
#if UNITY_STANDALONE_WIN
            int hr = Native.CreateDevice(device.guidInstance);
            if (hr != 0)
            {
                Debug.LogError($"[UnityFFB] CreateFFBDevice Failed: 0x{hr.ToString("x")} {WinErrors.GetSystemMessage(hr)}");
                return false;
            }

            return true;
#else
            return false;
#endif
        }

        private static Debouncer debounceDeviceChanged = new Debouncer(1000);

        private static void OnDeviceChanged()
        {
            debounceDeviceChanged.Debounce(() => { DetectDeviceChanges(); });
        }

        private static void DetectDeviceChanges()
        {
            EnumerateDirectInputDevices();
            DirectInputDevice.RegisterDevices();
        }

    }

    class Debouncer
    {
        private List<CancellationTokenSource> StepperCancelTokens = new List<CancellationTokenSource>();
        private int MillisecondsToWait;
        private readonly object _lockThis = new object(); // Use a locking object to prevent the debouncer to trigger again while the func is still running

        public Debouncer(int millisecondsToWait = 300)
        {
            this.MillisecondsToWait = millisecondsToWait;
        }

        public void Debounce(Action func)
        {
            CancelAllStepperTokens(); // Cancel all api requests;
            var newTokenSrc = new CancellationTokenSource();
            lock (_lockThis)
            {
                StepperCancelTokens.Add(newTokenSrc);
            }
            Task.Delay(MillisecondsToWait, newTokenSrc.Token).ContinueWith(task => // Create new request
            {
                if (!newTokenSrc.IsCancellationRequested) // if it hasn't been cancelled
                {
                    CancelAllStepperTokens(); // Cancel any that remain (there shouldn't be any)
                    StepperCancelTokens = new List<CancellationTokenSource>(); // set to new list
                    lock (_lockThis)
                    {
                        func(); // run
                    }
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void CancelAllStepperTokens()
        {
            foreach (var token in StepperCancelTokens)
            {
                if (!token.IsCancellationRequested)
                {
                    token.Cancel();
                }
            }
        }
    }
}
