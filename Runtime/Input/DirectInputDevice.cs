using System.Linq;
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityFFB
{
//#if UNITY_EDITOR
//    [InitializeOnLoad]
//#endif
    [InputControlLayout(stateType = typeof(FlatJoyState2), canRunInBackground = true)]
    public class DirectInputDevice : InputDevice, IInputUpdateCallbackReceiver
    {

        public FlatJoyState2 lastState;
        public static List<InputDeviceDescription> removedDevices = new List<InputDeviceDescription>();

        bool prevFocused = true;

        //#if UNITY_EDITOR
        //        static DirectInputDevice()
        //        {
        //            Initialize();
        //        }
        //#endif

        //        [RuntimeInitializeOnLoadMethod]
        public static void Initialize()
        {
            DirectInputManager.Initialize();
            RegisterDevices();
        }

        public static void RegisterDevices()
        {
            removedDevices.Clear();
            InputSystem.Update();

            // Remove Disconnected Direct Input Devices
            foreach (InputDevice dev in InputSystem.devices)
            {
                if (dev.description.interfaceName == "DirectInput")
                {
                    bool deviceFound = false;
                    foreach (DeviceInfo di in DirectInputManager.devices)
                    {
                        VidPid vidpid = JsonUtility.FromJson<VidPid>(dev.description.capabilities);
                        if (vidpid.vendorId == di.vendorId && vidpid.productId == di.productId && dev.description.serial == di.guidInstance)
                        {
                            deviceFound = true;
                        }
                    }
                    if (!deviceFound)
                    {
                        removedDevices.Add(dev.description);
                        InputSystem.RemoveDevice(dev);
                    }
                }
            }

            InputSystem.Update();

            // Add Input System Direct Input Devices if they are not already added.
            foreach (DeviceInfo di in DirectInputManager.devices)
            {
                bool deviceFound = false;
                InputSystem.Update();
                foreach (InputDevice dev in InputSystem.devices)
                {
                    if (dev != null)
                    {
                        if (dev.description.serial == di.guidInstance)
                        {
                            deviceFound = true;
                            break;
                        }
                    }
                }
                if (deviceFound)
                {
                    continue;
                }
                if (DirectInputManager.CreateDevice(di))
                {
                    string hasFFB = di.hasFFB ? "true" : "false";
                    Debug.Log($"Found Direct Input Device: {di.productName} - {di.instanceName} - {di.guidInstance}");
                    InputDeviceDescription devDesc = new InputDeviceDescription
                    {
                        interfaceName = "DirectInput",
                        manufacturer = di.productName,
                        product = di.productName,
                        serial = di.guidInstance,
                        capabilities = $@"{{""vendorId"":{di.vendorId},""productId"":{di.productId},""hasFFB"":{hasFFB}}}",
                    };
                    InputSystem.RegisterLayout<DirectInputDevice>($"DI::{di.productName}",
                        matches: InputDeviceMatcher.FromDeviceDescription(devDesc)
                    );
                    InputDevice ISDevice = InputSystem.AddDevice(devDesc);
                    Debug.Log($"Added Device: {di.productName} - {di.instanceName} - {di.guidInstance}");
                }
            }
        }

        public static void Deinitialize()
        {
            List<InputDevice> devicesToRemove = new List<InputDevice>();
            foreach (InputDevice dev in InputSystem.devices)
            {
                if (dev.description.interfaceName == "DirectInput")
                {
                    devicesToRemove.Add(dev);
                }
            }
            foreach (InputDevice dev in devicesToRemove)
            {
                InputSystem.RemoveDevice(dev);
            }
            foreach (InputDeviceDescription desc in removedDevices)
            {
                InputSystem.AddDevice(desc);
            }
            DirectInputManager.DeInitialize();
            InputSystem.Update();
        }

        protected override void FinishSetup()
        {
            base.FinishSetup();
        }

        public static DirectInputDevice current { get; private set; }
        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        protected override void OnRemoved()
        {
            base.OnRemoved();
            if (current == this)
                current = null;
            Debug.Log($"{this} Removed!");
        }

        protected override void OnAdded()
        {
            base.OnAdded();
            Debug.Log($"{this} Added!");
            if (this.description.interfaceName == "HID")
            {
                Debug.LogError($"Removing {this} because interfaceName == HID");
                InputSystem.RemoveDevice(this);
            }
        }

        public void OnUpdate()
        {
#if UNITY_STANDALONE_WIN
            if (Application.isFocused != prevFocused) {
                prevFocused = Application.isFocused;
                if (prevFocused) {
                    int hr = Native.Unacquire(device.description.serial);
                    if (hr != 0) {
                        Debug.LogError($"[UnityFFB] Unacquire Failed: 0x{hr.ToString("x")} {WinErrors.GetSystemMessage(hr)}");
                    }
                    hr = Native.Acquire(device.description.serial);
                    if (hr != 0) {
                        Debug.LogError($"[UnityFFB] Acquire Failed: 0x{hr.ToString("x")} {WinErrors.GetSystemMessage(hr)}");
                    }
                }
            }
            FlatJoyState2 state = new FlatJoyState2();
            int hresult = Native.GetDeviceState(device.description.serial, out state); // Poll the DirectInput Device
            if (hresult == 0)
            {
                lastState = state;
                InputSystem.QueueStateEvent(this, state);
            }
#endif
        }
    }



    /// <summary>
    /// Unity Input System Processor to center Axis values, like steering wheels
    /// </summary>
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public class CenteringProcessor : InputProcessor<float>
    {
#if UNITY_EDITOR
        static CenteringProcessor() { Initialize(); }
#endif

        [RuntimeInitializeOnLoadMethod]
        static void Initialize() { InputSystem.RegisterProcessor<CenteringProcessor>(); }

        public override float Process(float value, InputControl control)
        {
            return (value * 2) - 1;
        }
    }


    /// <summary>
    /// (Value*-1)+1    Smart Invert, remaps values from 1-0 to 0-1
    /// </summary>
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public class SmartInvertProcessor : InputProcessor<float>
    {
#if UNITY_EDITOR
        static SmartInvertProcessor() { Initialize(); }
#endif

        [RuntimeInitializeOnLoadMethod]
        static void Initialize() { InputSystem.RegisterProcessor<SmartInvertProcessor>(); }

        public override float Process(float value, InputControl control)
        {
            return (value * -1) + 1;
        }
    }
}
