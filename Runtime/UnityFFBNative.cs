using System;
using System.Runtime.InteropServices;
using UnityEngine;
using System.Linq;

namespace DirectInputFFB {
    public class Native {
        
        #if UNITY_STANDALONE_WIN
        private const string FFBDLL = "UnityDirectInputFFB";
        [DllImport(FFBDLL)] public static extern int    StartDirectInput();
        [DllImport(FFBDLL)] public static extern IntPtr EnumerateFFBDevices(ref int deviceCount);
        [DllImport(FFBDLL)] public static extern IntPtr EnumerateFFBAxes(ref int axisCount);
        [DllImport(FFBDLL)] public static extern int    CreateFFBDevice(string guidInstance);
        [DllImport(FFBDLL)] public static extern int    AddFFBEffect(EffectsType effectType);
        [DllImport(FFBDLL)] public static extern int    RemoveFFBEffect(EffectsType effectType);
        [DllImport(FFBDLL)] public static extern int    UpdateEffectGain(EffectsType effectType, float gainPercent);
        [DllImport(FFBDLL)] public static extern int    GetDeviceState(ref DIJOYSTATE2 DeviceStateObj);
        [DllImport(FFBDLL)] public static extern int    UpdateConstantForce(int magnitude, int[] directions);
        [DllImport(FFBDLL)] public static extern int    UpdateSpringRaw(DICondition[] conditions);
        [DllImport(FFBDLL)] public static extern int    UpdateSpring(int offset, int Coefficient, int Saturation);
        [DllImport(FFBDLL)] public static extern int    UpdateDamperRaw(DICondition[] conditions);
        [DllImport(FFBDLL)] public static extern int    UpdateDamper(int magnitude);
        [DllImport(FFBDLL)] public static extern int    UpdateFrictionRaw(DICondition[] conditions);
        [DllImport(FFBDLL)] public static extern int    UpdateFriction(int magnitude);
        [DllImport(FFBDLL)] public static extern int    UpdateInertiaRaw(DICondition[] conditions);
        [DllImport(FFBDLL)] public static extern int    UpdateInertia(int magnitude);
        [DllImport(FFBDLL)] public static extern int    SetAutoCenter(bool autoCenter);
        [DllImport(FFBDLL)] public static extern void   StartAllFFBEffects();
        [DllImport(FFBDLL)] public static extern void   StopAllFFBEffects();
        [DllImport(FFBDLL)] public static extern void   StopDirectInput();
        #endif

    }





    public static class FFBManager{

        private static bool _isInitialized = false;
        public static bool isInitialized { 
            get => _isInitialized; // { return _isInitialized; }
            set{ Debug.Log("[DirectInputFFB]Can't set isInitialized!"); }
        }
        private static bool _ConstantForceEnabled = false;
        private static bool _SpringForceEnabled   = false;
        private static bool _DamperForceEnabled   = false;
        private static bool _FrictionForceEnabled = false;
        private static bool _InertiaForceEnabled  = false;

        private static DeviceInfo[] _devices = new DeviceInfo[0];
        private static DeviceInfo? _activeDevice = null;
        private static DeviceAxisInfo[] _axes = new DeviceAxisInfo[0];
        private static DICondition[] _springConditions = new DICondition[0];
        private static DICondition[] _damperConditions = new DICondition[0];
        private static DICondition[] _frictionConditions = new DICondition[0];
        private static DICondition[] _inertiaConditions = new DICondition[0];
        private static int[] _axisDirections = new int[0];
        private static FlatJoyState2 _activeDeviceState;
        private static int _axesCount = 0;





        public static DeviceInfo[] devices { 
            get => _devices;
            set{ Debug.Log("[DirectInputFFB]Can't set devices!"); }
        }
        public static FlatJoyState2 state {
            get => _activeDeviceState;
            set{ Debug.Log("[DirectInputFFB]Can't set activeDeviceState!"); }
        }
        public static int axesCount{
            get => _axesCount; // No Need to match device GUID and return in devices array
            set{ Debug.Log("[DirectInputFFB]Can't set axesCount!"); }
        }

        public static event EventHandler OnDeviceStateChange = delegate {}; // Handle for events when the device input has changed, e.g. Axis moved or button pressed



        /// <summary>
        /// Initialize Master FFB
        /// Returns True if already Initialized or actually starts DirectInput Capture
        /// </summary>

        public static bool Initialize(){
        #if UNITY_STANDALONE_WIN
            if (_isInitialized) { return _isInitialized; }

            if (Native.StartDirectInput() != 0) { _isInitialized = false; }
            _isInitialized = true;
            EnumerateDirectInputDevices();
            SelectDevice(_devices[0]); // Select to first device

            Debug.Log($"[DirectInputFFB] Initialized! {_devices.Count()} Devices");
            return _isInitialized;
        #endif
        }

        private static void EnumerateDirectInputDevices(){
            int deviceCount = 0;
            IntPtr ptrDevices = Native.EnumerateFFBDevices(ref deviceCount);
            // Debug.Log($"[DirectInputFFBz] Device count: {deviceCount}");
            if (deviceCount > 0) {
                _devices = new DeviceInfo[deviceCount];

                int deviceSize = Marshal.SizeOf(typeof(DeviceInfo));
                for (int i = 0; i < deviceCount; i++) {
                    IntPtr pCurrent = ptrDevices + i * deviceSize;
                    _devices[i] = Marshal.PtrToStructure<DeviceInfo>(pCurrent);
                }
                // foreach (DeviceInfo device in devices) {
                //     string ffbAxis = UnityEngine.JsonUtility.ToJson(device, true);
                //     // Debug.Log(ffbAxis);
                // }
            }

        }

        public static void DisableForceFeedback(){
        #if UNITY_STANDALONE_WIN
            // Native.StopDirectInput();
            Native.StopAllFFBEffects();
            // _isInitialized = false;
            // _ConstantForceEnabled = false;
            // _SpringForceEnabled = false;
            // _DamperForceEnabled = false;


            if(_ConstantForceEnabled) { Native.RemoveFFBEffect(EffectsType.ConstantForce); }
            if(_SpringForceEnabled)   { Native.RemoveFFBEffect(EffectsType.Spring); }
            if(_DamperForceEnabled)   { Native.RemoveFFBEffect(EffectsType.Damper); }

            // _devices = new DeviceInfo[0];
            // _activeDevice = null;
            // _axes = new DeviceAxisInfo[0];
            // _springConditions = new DICondition[0];
        #endif
        }

        public static bool SelectDevice(DeviceInfo Device){
            if(_activeDevice != null){ _activeDevice = null; } // Unacquire

            int hresult = Native.CreateFFBDevice(Device.guidInstance);
            if (hresult != 0) { Debug.LogError($"[DirectInputFFB] CreateFFBDevice Failed: 0x{hresult.ToString("x")} {WinErrors.GetSystemMessage(hresult)}"); return false; }

            IntPtr ptrAxes = Native.EnumerateFFBAxes(ref _axesCount); // Returns the first axis and _axesCount is how many axis the device has
            if (_axesCount > 0) {
                _axes               = new DeviceAxisInfo[_axesCount]; // Size the _axis array to fit for this device
                _axisDirections     = new int           [_axesCount]; // ^^
                _springConditions   = new DICondition   [_axesCount]; // ^^
                _damperConditions   = new DICondition   [_axesCount]; // ^^

                int axisSize = Marshal.SizeOf(typeof(DeviceAxisInfo));
                for (int i = 0; i < _axesCount; i++) {
                    IntPtr pCurrent = ptrAxes + i * axisSize;
                    _axes[i]             = Marshal.PtrToStructure<DeviceAxisInfo>(pCurrent); // Fill with data from the device
                    _axisDirections[i]   = 0; // Default each Axis Direction to 0
                    _springConditions[i] = new DICondition(); // For each axis create an effect
                    _damperConditions[i] = new DICondition(); // For each axis create an effect
                }
            }

            _activeDevice = Device;
            return true;
            // Debug.Log($"[DirectInputFFB] Axis count: {_axes.Length}");
            // foreach (DeviceAxisInfo axis in _axes) {
            //     string ffbAxis = UnityEngine.JsonUtility.ToJson(axis, true);
            //     Debug.Log(ffbAxis);
            // }
        }

        // public static void SelectDevice(string deviceGuid){ // Sets the device as the primary controller device, Accepts Device.guidInstance
        //     // SHOULD unacquire existing devices here
        //     int hresult = Native.CreateFFBDevice(deviceGuid);
        //     if (hresult != 0) { Debug.LogError($"[DirectInputFFB] CreateFFBDevice Failed: 0x{hresult.ToString("x")} {WinErrors.GetSystemMessage(hresult)}");return false; }

        //     // _activeDevice = _devices[0]; This is bullshit rn

        //     // if (disableAutoCenter) {
        //     //     hresult = Native.SetAutoCenter(false);
        //     //     if (hresult != 0) {
        //     //         Debug.LogError($"[DirectInputFFB] SetAutoCenter Failed: 0x{hresult.ToString("x")} {WinErrors.GetSystemMessage(hresult)}");
        //     //     }
        //     // }

        //     IntPtr ptrAxes = Native.EnumerateFFBAxes(ref _axesCount); // Return the axis and how many it returned
        //     if (_axesCount > 0) {
        //         _axes               = new DeviceAxisInfo[_axesCount]; // Size the _axis array to fit for this device
        //         _axisDirections     = new int           [_axesCount]; // ^^
        //         _springConditions   = new DICondition   [_axesCount]; // ^^
        //         _damperConditions   = new DICondition   [_axesCount]; // ^^

        //         int axisSize = Marshal.SizeOf(typeof(DeviceAxisInfo));
        //         for (int i = 0; i < _axesCount; i++) {
        //             IntPtr pCurrent = ptrAxes + i * axisSize;
        //             _axes[i]             = Marshal.PtrToStructure<DeviceAxisInfo>(pCurrent); // Fill with data from the device
        //             _axisDirections[i]   = 0; // Default each Axis Direction to 0
        //             _springConditions[i] = new DICondition(); // For each axis create an effect
        //             _damperConditions[i] = new DICondition(); // For each axis create an effect
        //         }
        //     }
        //     // Debug.Log($"[DirectInputFFB] Axis count: {_axes.Length}");
        //     // foreach (DeviceAxisInfo axis in _axes) {
        //     //     string ffbAxis = UnityEngine.JsonUtility.ToJson(axis, true);
        //     //     // Debug.Log(ffbAxis);
        //     // }
        // }

        public static bool EnableConstantForce(){
            if(_ConstantForceEnabled){ return true; } // Already Enabled 
            
            int hresult = Native.AddFFBEffect(EffectsType.ConstantForce); // Enable the Constant Force Effect
            if (hresult != 0) { Debug.LogError($"[DirectInputFFB] EnableConstantForce Failed: 0x{hresult.ToString("x")} {WinErrors.GetSystemMessage(hresult)}");return false; }

            hresult = Native.UpdateConstantForce(0, _axisDirections); // Set Constant Force to 0
            if (hresult != 0) { Debug.LogError($"[DirectInputFFB] UpdateConstantForce Failed: 0x{hresult.ToString("x")} {WinErrors.GetSystemMessage(hresult)}"); }
        
            _ConstantForceEnabled = true;
            return _ConstantForceEnabled;
        }

        public static bool ConstantForce(int magnitude){
            if(!_ConstantForceEnabled){ return false; }// Check if ConstantForce enabled
            int hresult = Native.UpdateConstantForce(magnitude, _axisDirections); // Apply the force
            if (hresult != 0) { Debug.LogError($"[DirectInputFFB] UpdateConstantForce Failed: 0x{hresult.ToString("x")} {WinErrors.GetSystemMessage(hresult)}");return false; }
            return true; // Didn't fail so it worked
        }

        public static bool SetConstantForceGain(float gainPercent){ // Range 0 through 10,000 (https://docs.microsoft.com/en-us/previous-versions/windows/desktop/ee416616(v=vs.85))
            if(!_ConstantForceEnabled){ return false; }// Check if ConstantForce enabled
            int hresult = Native.UpdateEffectGain(EffectsType.ConstantForce, gainPercent);
            if (hresult != 0) { Debug.LogError($"[DirectInputFFB] UpdateEffectGain Failed: 0x{hresult.ToString("x")} {WinErrors.GetSystemMessage(hresult)}");return false; }
            return true; // Didn't fail so it worked
        }

        
        
        public static bool EnableSpringForce(){
            if(_SpringForceEnabled){ return true; } // Already Enabled 
            int hresult = Native.AddFFBEffect(EffectsType.Spring); // Try add the Spring Effect
            if (hresult != 0) { Debug.LogError($"[DirectInputFFB] EnableSpringForce Failed: 0x{hresult.ToString("x")} {WinErrors.GetSystemMessage(hresult)}");return false; }
            
            for (int i = 0; i < _springConditions.Length; i++) {
                _springConditions[i].offset = 0;
                _springConditions[i].negativeCoefficient = 0;
                _springConditions[i].positiveCoefficient = 0;
                _springConditions[i].negativeSaturation = 0;
                _springConditions[i].positiveSaturation = 0;
                _springConditions[i].deadband = 0;
            }
            hresult = Native.UpdateSpringRaw(_springConditions);
            if (hresult != 0) { Debug.LogError($"[DirectInputFFB] UpdateSpring Failed: 0x{hresult.ToString("x")} {WinErrors.GetSystemMessage(hresult)}");return false; }
            _SpringForceEnabled = true;
            return _SpringForceEnabled;
        }

        public static bool SpringForce(DICondition[] conditions){ // Useful if you want to set deadband or want a non-symmetric effect 
            if(!_SpringForceEnabled){ return false; }// Check if SpringForce enabled
            int hresult = Native.UpdateSpringRaw(conditions);
            if (hresult != 0) { Debug.LogError($"[DirectInputFFB] SpringForceRaw Failed: 0x{hresult.ToString("x")} {WinErrors.GetSystemMessage(hresult)}");return false; }
            return true; // Didn't fail so it worked
        }
        
        public static bool SpringForce(int SpringOffset, int SpringCoefficient, int SpringSaturation){
            if(!_SpringForceEnabled){ return false; }// Check if SpringForce enabled
            int hresult = Native.UpdateSpring(SpringOffset, SpringCoefficient, SpringSaturation);
            if (hresult != 0) { Debug.LogError($"[DirectInputFFB] UpdateSpring Failed: 0x{hresult.ToString("x")} {WinErrors.GetSystemMessage(hresult)}");return false; }
            return true; // Didn't fail so it worked
        }



        public static bool EnableDamperForce(){ // Adds the Damper effect to the DirectInput Device
            if(_DamperForceEnabled){ return true; } // Already Enabled 
            int hresult = Native.AddFFBEffect(EffectsType.Damper); // Try add the Damper Effect
            if (hresult != 0) { Debug.LogError($"[DirectInputFFB] EnableDamperForce Failed: 0x{hresult.ToString("x")} {WinErrors.GetSystemMessage(hresult)}");return false; }      
            
            for (int i = 0; i < _damperConditions.Length; i++) {
                _damperConditions[i].offset = 0;
                _damperConditions[i].negativeCoefficient = 0;
                _damperConditions[i].positiveCoefficient = 0;
                _damperConditions[i].negativeSaturation = 0;
                _damperConditions[i].positiveSaturation = 0;
                _damperConditions[i].deadband = 0;
            }
            hresult = Native.UpdateDamperRaw(_damperConditions);
            if (hresult != 0) { Debug.LogError($"[DirectInputFFB] UpdateDamper Failed: 0x{hresult.ToString("x")} {WinErrors.GetSystemMessage(hresult)}");return false; }
            _DamperForceEnabled = true;

            return _DamperForceEnabled;
        }

        public static bool DamperForce(DICondition[] conditions){ // Useful if you want to set deadband or want a non-symmetric effect 
            if(!_DamperForceEnabled){ return false; }// Check if DamperForce enabled
            int hresult = Native.UpdateDamperRaw(conditions);
            if (hresult != 0) { Debug.LogError($"[DirectInputFFB] DamperForceRaw Failed: 0x{hresult.ToString("x")} {WinErrors.GetSystemMessage(hresult)}");return false; }
            return true; // Didn't fail so it worked
        }

        public static bool DamperForce(int magnitude){
            if(!_DamperForceEnabled){ return false; }// Check if DamperForce enabled
            int hresult = Native.UpdateDamper(magnitude);
            if (hresult != 0) { Debug.LogError($"[DirectInputFFB] UpdateDamper Failed: 0x{hresult.ToString("x")} {WinErrors.GetSystemMessage(hresult)}");return false; }
            return true; // Didn't fail so it worked
        }



        public static bool EnableFrictionForce(){ // Adds the Friction effect to the DirectInput Device
            if(_FrictionForceEnabled){ return true; } // Already Enabled 

            int hresult = Native.AddFFBEffect(EffectsType.Friction); // Try add the Friction Effect
            if (hresult != 0) { Debug.LogError($"[DirectInputFFB] EnableFrictionForce Failed: 0x{hresult.ToString("x")} {WinErrors.GetSystemMessage(hresult)}");return false; }      

            for (int i = 0; i < _frictionConditions.Length; i++) {
                _frictionConditions[i].offset = 0;
                _frictionConditions[i].negativeCoefficient = 0;
                _frictionConditions[i].positiveCoefficient = 0;
                _frictionConditions[i].negativeSaturation = 0;
                _frictionConditions[i].positiveSaturation = 0;
                _frictionConditions[i].deadband = 0;
            }
            hresult = Native.UpdateFrictionRaw(_frictionConditions);
            if (hresult != 0) { Debug.LogError($"[DirectInputFFB] UpdateFriction Failed: 0x{hresult.ToString("x")} {WinErrors.GetSystemMessage(hresult)}");return false; }
            _FrictionForceEnabled = true;

            return _FrictionForceEnabled;
        }

        public static bool FrictionForce(DICondition[] conditions){ // Useful if you want to set deadband or want a non-symmetric effect 
            if(!_FrictionForceEnabled){ return false; }// Check if FrictionForce enabled
            int hresult = Native.UpdateFrictionRaw(conditions);
            if (hresult != 0) { Debug.LogError($"[DirectInputFFB] FrictionForceRaw Failed: 0x{hresult.ToString("x")} {WinErrors.GetSystemMessage(hresult)}");return false; }
            return true; // Didn't fail so it worked
        }

        public static bool FrictionForce(int magnitude){
            if(!_FrictionForceEnabled){ return false; } // Check if FrictionForce enabled
            int hresult = Native.UpdateFriction(magnitude);
            if (hresult != 0) { Debug.LogError($"[DirectInputFFB] FrictionForce Failed: 0x{hresult.ToString("x")} {WinErrors.GetSystemMessage(hresult)}");return false; }
            return true; // Didn't fail so it worked
        }



        public static bool EnableInertiaForce(){ // Adds the Inertia effect to the DirectInput Device
            if(_InertiaForceEnabled){ return true; } // Already Enabled 

            int hresult = Native.AddFFBEffect(EffectsType.Inertia); // Try add the Inertia Effect
            if (hresult != 0) { Debug.LogError($"[DirectInputFFB] EnableInertiaForce Failed: 0x{hresult.ToString("x")} {WinErrors.GetSystemMessage(hresult)}");return false; }      

            for (int i = 0; i < _inertiaConditions.Length; i++) {
                _inertiaConditions[i].offset = 0;
                _inertiaConditions[i].negativeCoefficient = 0;
                _inertiaConditions[i].positiveCoefficient = 0;
                _inertiaConditions[i].negativeSaturation = 0;
                _inertiaConditions[i].positiveSaturation = 0;
                _inertiaConditions[i].deadband = 0;
            }
            hresult = Native.UpdateInertiaRaw(_inertiaConditions);
            if (hresult != 0) { Debug.LogError($"[DirectInputFFB] UpdateInertia Failed: 0x{hresult.ToString("x")} {WinErrors.GetSystemMessage(hresult)}");return false; }
            _InertiaForceEnabled = true;

            return _InertiaForceEnabled;
        }

        public static bool InertiaForce(DICondition[] conditions){ // Useful if you want to set deadband or want a non-symmetric effect 
            if(!_InertiaForceEnabled){ return false; }// Check if InertiaForce enabled
            int hresult = Native.UpdateInertiaRaw(conditions);
            if (hresult != 0) { Debug.LogError($"[DirectInputFFB] InertiaForceRaw Failed: 0x{hresult.ToString("x")} {WinErrors.GetSystemMessage(hresult)}");return false; }
            return true; // Didn't fail so it worked
        }

        public static bool InertiaForce(int magnitude){
            if(!_InertiaForceEnabled){ return false; } // Check if InertiaForce enabled
            int hresult = Native.UpdateInertia(magnitude);
            if (hresult != 0) { Debug.LogError($"[DirectInputFFB] InertiaForce Failed: 0x{hresult.ToString("x")} {WinErrors.GetSystemMessage(hresult)}");return false; }
            return true; // Didn't fail so it worked
        }


        // DICondition[] CalculateSpringCondition(int Offset, int Coefficient, int Saturation){ // Transform Offset, Coefficient, Saturation to DICondition
        //     DICondition[] SpringConditions = new DICondition[FFBManager.axesCount];
        //     for (int i = 0; i < SpringConditions.Length; i++) {
        //         SpringConditions[i] = new DICondition();
        //         SpringConditions[i].offset = Offset;
        //         SpringConditions[i].positiveCoefficient = Coefficient;
        //         SpringConditions[i].negativeCoefficient = Coefficient;
        //         SpringConditions[i].positiveSaturation  = (uint)Saturation;
        //         SpringConditions[i].negativeSaturation  = (uint)Saturation;
        //         SpringConditions[i].deadband = 0;
        //         // Debug.Log($"Spring: {SpringConditions[i].positiveCoefficient}, {SpringConditions[i].negativeCoefficient}, {SpringConditions[i].positiveSaturation}, {SpringConditions[i].negativeSaturation} ");
        //     }
        //     return SpringConditions;
        // }

        // DICondition[] CalculateDamperCondition(int magnitude){ // Transform Magnitude to DICondition
        //     DICondition[] DamperConditions = new DICondition[FFBManager.axesCount];
        //     for (int i = 0; i < DamperConditions.Length; i++) {
        //         DamperConditions[i] = new DICondition();
        //         DamperConditions[i].offset = 0;
        //         DamperConditions[i].positiveCoefficient = magnitude;
        //         DamperConditions[i].negativeCoefficient = magnitude;
        //         DamperConditions[i].positiveSaturation  = (uint)0;
        //         DamperConditions[i].negativeSaturation  = (uint)0;
        //         DamperConditions[i].deadband = 0;
        //         // Debug.Log($"Damper: {magnitude} ");
        //     }
        //     return DamperConditions;
        // }

        // public static DICondition[] CalculateFrictionCondition(int Offset, int Coefficient, int Saturation){ // Transform Offset, Coefficient, Saturation to DICondition
        //     DICondition[] FrictionConditions = new DICondition[FFBManager.axesCount];
        //     for (int i = 0; i < FrictionConditions.Length; i++) {
        //         FrictionConditions[i] = new DICondition();
        //         FrictionConditions[i].offset = Offset;
        //         FrictionConditions[i].positiveCoefficient = Coefficient;
        //         FrictionConditions[i].negativeCoefficient = Coefficient;
        //         FrictionConditions[i].positiveSaturation  = (uint)Saturation;
        //         FrictionConditions[i].negativeSaturation  = (uint)Saturation;
        //         FrictionConditions[i].deadband = 0;
        //         // Debug.Log($"Friction: {FrictionConditions[i].positiveCoefficient}, {FrictionConditions[i].negativeCoefficient}, {FrictionConditions[i].positiveSaturation}, {FrictionConditions[i].negativeSaturation} ");
        //     }
        //     return FrictionConditions;
        // }

        // public static DICondition[] CalculateInertiaCondition(int Offset, int Coefficient, int Saturation){ // Transform Offset, Coefficient, Saturation to DICondition
        //     DICondition[] InertiaConditions = new DICondition[FFBManager.axesCount];
        //     for (int i = 0; i < InertiaConditions.Length; i++) {
        //         InertiaConditions[i] = new DICondition();
        //         InertiaConditions[i].offset = Offset;
        //         InertiaConditions[i].positiveCoefficient = Coefficient;
        //         InertiaConditions[i].negativeCoefficient = Coefficient;
        //         InertiaConditions[i].positiveSaturation  = (uint)Saturation;
        //         InertiaConditions[i].negativeSaturation  = (uint)Saturation;
        //         InertiaConditions[i].deadband = 0;
        //         // Debug.Log($"Inertia: {InertiaConditions[i].positiveCoefficient}, {InertiaConditions[i].negativeCoefficient}, {InertiaConditions[i].positiveSaturation}, {InertiaConditions[i].negativeSaturation} ");
        //     }
        //     return InertiaConditions;
        // }

        public static void PollDevice(){
            DirectInputFFB.DIJOYSTATE2 DeviceState = new DirectInputFFB.DIJOYSTATE2(); // Store the raw state of the device
            int hresult = DirectInputFFB.Native.GetDeviceState(ref DeviceState); // Fetch the device state
            if(hresult!=0){ Debug.LogError($"[DirectInputFFB] GetDeviceState : 0x{hresult.ToString("x")} {DirectInputFFB.WinErrors.GetSystemMessage(hresult)}\n[DirectInputFFB] Perhaps the device has not been attached/acquired"); }
            
            FlatJoyState2 state = Utilities.FlattenDIJOYSTATE2(DeviceState); // Flatten the state for comparison
            if( !state.Equals(_activeDeviceState) ){ // Some input has changed
                _activeDeviceState = state;
                OnDeviceStateChange(null, EventArgs.Empty); // Bubble up event
            }
        }


    }
}
