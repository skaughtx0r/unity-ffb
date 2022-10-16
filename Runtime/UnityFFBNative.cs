using System;
using System.Runtime.InteropServices;
using UnityEngine;
using System.Linq;

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
        [DllImport(FFBDLL)] public static extern int    UpdateEffectGain(string guidInstance, EffectsType effectType, float gainPercent);
        [DllImport(FFBDLL)] public static extern int    GetDeviceState(string guidInstance, out FlatJoyState2 DeviceStateObj);
        [DllImport(FFBDLL)] public static extern int    UpdateConstantForce(string guidInstance, int magnitude, int[] directions);
        [DllImport(FFBDLL)] public static extern int    UpdateSpring(string guidInstance, DICondition[] conditions);
        [DllImport(FFBDLL)] public static extern int    SetAutoCenter(string guidInstance, bool autoCenter);
        [DllImport(FFBDLL)] public static extern void   StartAllFFBEffects();
        [DllImport(FFBDLL)] public static extern void   StopAllFFBEffects();
        [DllImport(FFBDLL)] public static extern void   StopDirectInput();
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



    //public static class FFBManager {

    //    private static bool _isInitialized = false;
    //    public static bool isInitialized { 
    //        get => _isInitialized; // { return _isInitialized; }
    //        set{ Debug.Log("[UnityFFB]Can't set isInitialized!"); }
    //    }
    //    private static bool _ConstantForceEnabled = false;
    //    private static bool _SpringForceEnabled   = false;
    //    private static bool _DamperForceEnabled   = false;
    //    private static bool _FrictionForceEnabled = false;
    //    private static bool _InertiaForceEnabled  = false;

    //    private static DeviceInfo[] _devices = new DeviceInfo[0];
    //    private static DeviceInfo? _activeDevice = null;
    //    private static DeviceAxisInfo[] _axes = new DeviceAxisInfo[0];
    //    private static DICondition[] _springConditions = new DICondition[0];
    //    private static DICondition[] _damperConditions = new DICondition[0];
    //    private static DICondition[] _frictionConditions = new DICondition[0];
    //    private static DICondition[] _inertiaConditions = new DICondition[0];
    //    private static int[] _axisDirections = new int[0];
    //    private static FlatJoyState2 _activeDeviceState;
    //    private static int _axesCount = 0;

    //    public static DeviceInfo[] devices { 
    //        get => _devices;
    //        set{ Debug.Log("[UnityFFB]Can't set devices!"); }
    //    }
    //    public static FlatJoyState2 state {
    //        get => _activeDeviceState;
    //        set{ Debug.Log("[UnityFFB]Can't set activeDeviceState!"); }
    //    }
    //    public static int axesCount{
    //        get => _axesCount; // No Need to match device GUID and return in devices array
    //        set{ Debug.Log("[UnityFFB]Can't set axesCount!"); }
    //    }

    //    public static event EventHandler OnDeviceStateChange = delegate {}; // Handle for events when the device input has changed, e.g. Axis moved or button pressed



    //    /// <summary>
    //    /// Initialize Master FFB
    //    /// Returns True if already Initialized or actually starts DirectInput Capture
    //    /// </summary>

    //    public static bool Initialize(){
    //    #if UNITY_STANDALONE_WIN
    //        if (_isInitialized) { return _isInitialized; }

    //        if (Native.StartDirectInput() != 0) { _isInitialized = false; }
    //        _isInitialized = true;
    //        EnumerateDirectInputDevices();
    //        SelectDevice(_devices[0]); // Select to first device

    //        Debug.Log($"[UnityFFB] Initialized! {_devices.Count()} Devices");
    //        return _isInitialized;
    //    #endif
    //    }

    //    private static void EnumerateDirectInputDevices(){
    //        int deviceCount = 0;
    //        IntPtr ptrDevices = Native.EnumerateDevices(ref deviceCount);
    //        // Debug.Log($"[UnityFFBz] Device count: {deviceCount}");
    //        if (deviceCount > 0) {
    //            _devices = new DeviceInfo[deviceCount];

    //            int deviceSize = Marshal.SizeOf(typeof(DeviceInfo));
    //            for (int i = 0; i < deviceCount; i++) {
    //                IntPtr pCurrent = ptrDevices + i * deviceSize;
    //                _devices[i] = Marshal.PtrToStructure<DeviceInfo>(pCurrent);
    //            }
    //            // foreach (DeviceInfo device in devices) {
    //            //     string ffbAxis = UnityEngine.JsonUtility.ToJson(device, true);
    //            //     // Debug.Log(ffbAxis);
    //            // }
    //        }

    //    }

    //    public static void DisableForceFeedback(){
    //    #if UNITY_STANDALONE_WIN
    //        // Native.StopDirectInput();
    //        Native.StopAllFFBEffects();
    //        // _isInitialized = false;
    //        // _ConstantForceEnabled = false;
    //        // _SpringForceEnabled = false;
    //        // _DamperForceEnabled = false;


    //        //if(_ConstantForceEnabled) { Native.RemoveFFBEffect(EffectsType.ConstantForce); }
    //        //if(_SpringForceEnabled)   { Native.RemoveFFBEffect(EffectsType.Spring); }
    //        //if(_DamperForceEnabled)   { Native.RemoveFFBEffect(EffectsType.Damper); }

    //        // _devices = new DeviceInfo[0];
    //        // _activeDevice = null;
    //        // _axes = new DeviceAxisInfo[0];
    //        // _springConditions = new DICondition[0];
    //    #endif
    //    }

    //    public static bool CreateDevice(DeviceInfo device)
    //    {
    //        int hresult = Native.CreateDevice(device.guidInstance);
    //        if (hresult != 0) { Debug.LogError($"[UnityFFB] CreateFFBDevice Failed: 0x{hresult.ToString("x")} {WinErrors.GetSystemMessage(hresult)}"); return false; }

    //        return true;
    //    }

    //    public static bool SelectDevice(DeviceInfo Device){
    //        if(_activeDevice != null){ _activeDevice = null; } // Unacquire

    //        int hresult = Native.CreateDevice(Device.guidInstance);
    //        if (hresult != 0) { Debug.LogError($"[UnityFFB] CreateFFBDevice Failed: 0x{hresult.ToString("x")} {WinErrors.GetSystemMessage(hresult)}"); return false; }

    //        IntPtr ptrAxes = Native.EnumerateFFBAxes(ref _axesCount); // Returns the first axis and _axesCount is how many axis the device has
    //        if (_axesCount > 0) {
    //            _axes               = new DeviceAxisInfo[_axesCount]; // Size the _axis array to fit for this device
    //            _axisDirections     = new int           [_axesCount]; // ^^
    //            _springConditions   = new DICondition   [_axesCount]; // ^^
    //            _damperConditions   = new DICondition   [_axesCount]; // ^^

    //            int axisSize = Marshal.SizeOf(typeof(DeviceAxisInfo));
    //            for (int i = 0; i < _axesCount; i++) {
    //                IntPtr pCurrent = ptrAxes + i * axisSize;
    //                _axes[i]             = Marshal.PtrToStructure<DeviceAxisInfo>(pCurrent); // Fill with data from the device
    //                _axisDirections[i]   = 0; // Default each Axis Direction to 0
    //                _springConditions[i] = new DICondition(); // For each axis create an effect
    //                _damperConditions[i] = new DICondition(); // For each axis create an effect
    //            }
    //        }

    //        _activeDevice = Device;
    //        return true;
    //        // Debug.Log($"[UnityFFB] Axis count: {_axes.Length}");
    //        // foreach (DeviceAxisInfo axis in _axes) {
    //        //     string ffbAxis = UnityEngine.JsonUtility.ToJson(axis, true);
    //        //     Debug.Log(ffbAxis);
    //        // }
    //    }

    //    // public static void SelectDevice(string deviceGuid){ // Sets the device as the primary controller device, Accepts Device.guidInstance
    //    //     // SHOULD unacquire existing devices here
    //    //     int hresult = Native.CreateFFBDevice(deviceGuid);
    //    //     if (hresult != 0) { Debug.LogError($"[UnityFFB] CreateFFBDevice Failed: 0x{hresult.ToString("x")} {WinErrors.GetSystemMessage(hresult)}");return false; }

    //    //     // _activeDevice = _devices[0]; This is bullshit rn

    //    //     // if (disableAutoCenter) {
    //    //     //     hresult = Native.SetAutoCenter(false);
    //    //     //     if (hresult != 0) {
    //    //     //         Debug.LogError($"[UnityFFB] SetAutoCenter Failed: 0x{hresult.ToString("x")} {WinErrors.GetSystemMessage(hresult)}");
    //    //     //     }
    //    //     // }

    //    //     IntPtr ptrAxes = Native.EnumerateFFBAxes(ref _axesCount); // Return the axis and how many it returned
    //    //     if (_axesCount > 0) {
    //    //         _axes               = new DeviceAxisInfo[_axesCount]; // Size the _axis array to fit for this device
    //    //         _axisDirections     = new int           [_axesCount]; // ^^
    //    //         _springConditions   = new DICondition   [_axesCount]; // ^^
    //    //         _damperConditions   = new DICondition   [_axesCount]; // ^^

    //    //         int axisSize = Marshal.SizeOf(typeof(DeviceAxisInfo));
    //    //         for (int i = 0; i < _axesCount; i++) {
    //    //             IntPtr pCurrent = ptrAxes + i * axisSize;
    //    //             _axes[i]             = Marshal.PtrToStructure<DeviceAxisInfo>(pCurrent); // Fill with data from the device
    //    //             _axisDirections[i]   = 0; // Default each Axis Direction to 0
    //    //             _springConditions[i] = new DICondition(); // For each axis create an effect
    //    //             _damperConditions[i] = new DICondition(); // For each axis create an effect
    //    //         }
    //    //     }
    //    //     // Debug.Log($"[UnityFFB] Axis count: {_axes.Length}");
    //    //     // foreach (DeviceAxisInfo axis in _axes) {
    //    //     //     string ffbAxis = UnityEngine.JsonUtility.ToJson(axis, true);
    //    //     //     // Debug.Log(ffbAxis);
    //    //     // }
    //    // }

    //    public static bool EnableConstantForce(){
    //        if(_ConstantForceEnabled){ return true; } // Already Enabled 

    //        int hresult = Native.AddFFBEffect(EffectsType.ConstantForce); // Enable the Constant Force Effect
    //        if (hresult != 0) { Debug.LogError($"[UnityFFB] EnableConstantForce Failed: 0x{hresult.ToString("x")} {WinErrors.GetSystemMessage(hresult)}");return false; }

    //        hresult = Native.UpdateConstantForce(0, _axisDirections); // Set Constant Force to 0
    //        if (hresult != 0) { Debug.LogError($"[UnityFFB] UpdateConstantForce Failed: 0x{hresult.ToString("x")} {WinErrors.GetSystemMessage(hresult)}"); }

    //        _ConstantForceEnabled = true;
    //        return _ConstantForceEnabled;
    //    }

    //    public static bool ConstantForce(int magnitude){
    //        if(!_ConstantForceEnabled){ return false; }// Check if ConstantForce enabled
    //        int hresult = Native.UpdateConstantForce(magnitude, _axisDirections); // Apply the force
    //        if (hresult != 0) { Debug.LogError($"[UnityFFB] UpdateConstantForce Failed: 0x{hresult.ToString("x")} {WinErrors.GetSystemMessage(hresult)}");return false; }
    //        return true; // Didn't fail so it worked
    //    }

    //    public static bool SetConstantForceGain(float gainPercent){ // Range 0 through 10,000 (https://docs.microsoft.com/en-us/previous-versions/windows/desktop/ee416616(v=vs.85))
    //        if(!_ConstantForceEnabled){ return false; }// Check if ConstantForce enabled
    //        int hresult = Native.UpdateEffectGain(EffectsType.ConstantForce, gainPercent);
    //        if (hresult != 0) { Debug.LogError($"[UnityFFB] UpdateEffectGain Failed: 0x{hresult.ToString("x")} {WinErrors.GetSystemMessage(hresult)}");return false; }
    //        return true; // Didn't fail so it worked
    //    }

    //    public static void PollDevice(){
    //        DIJOYSTATE2 DeviceState = new DIJOYSTATE2(); // Store the raw state of the device
    //        int hresult = Native.GetDeviceState(ref DeviceState); // Fetch the device state
    //        if(hresult!=0){ Debug.LogError($"[UnityFFB] GetDeviceState : 0x{hresult.ToString("x")} {WinErrors.GetSystemMessage(hresult)}\n[UnityFFB] Perhaps the device has not been attached/acquired"); }

    //        FlatJoyState2 state = Utilities.FlattenDIJOYSTATE2(DeviceState); // Flatten the state for comparison
    //        if( !state.Equals(_activeDeviceState) ){ // Some input has changed
    //            _activeDeviceState = state;
    //            OnDeviceStateChange(null, EventArgs.Empty); // Bubble up event
    //        }
    //    }


    // }
}
