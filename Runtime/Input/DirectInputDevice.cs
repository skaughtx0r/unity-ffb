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
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityFFB
{
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    [InputControlLayout(stateType = typeof(FlatJoyState2))]
    public class DirectInputDevice : InputDevice, IInputUpdateCallbackReceiver
    {

        FlatJoyState2 lastState;

#if UNITY_EDITOR
        static DirectInputDevice()
        {
            Initialize();
        }
#endif

        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {

            DirectInputManager.Initialize();

            InputSystem.Update();
            foreach (DeviceInfo di in DirectInputManager.devices)
            {
                bool skip = false;
                foreach (InputDevice dev in InputSystem.devices)
                {
                    if (dev.description.interfaceName == "HID")
                    {
                        VidPid vidpid = JsonUtility.FromJson<VidPid>(dev.description.capabilities);
                        if (vidpid.vendorId == di.vendorId && vidpid.productId == di.productId)
                        {
                            InputSystem.RemoveDevice(dev);
                        }
                    }
                    else if (dev.description.interfaceName == "DirectInput")
                    {
                        VidPid vidpid = JsonUtility.FromJson<VidPid>(dev.description.capabilities);
                        if (vidpid.vendorId == di.vendorId && vidpid.productId == di.productId)
                        {
                            skip = true;
                        }
                    }
                }
                if (skip)
                {
                    continue;
                }
                InputSystem.RegisterLayout<DirectInputDevice>($"DI::{di.productName}",
                    matches: new InputDeviceMatcher().WithProduct(di.productName)
                );
                if (DirectInputManager.CreateDevice(di))
                {
                    string hasFFB = di.hasFFB ? "true" : "false";
                    InputDevice ISDevice = InputSystem.AddDevice(new InputDeviceDescription
                    {
                        interfaceName = "DirectInput",
                        manufacturer = di.productName,
                        product = di.instanceName,
                        serial = di.guidInstance,
                        capabilities = $@"{{""vendorId"":{di.vendorId},""productId"":{di.productId},""hasFFB"":{hasFFB}}}",
                    });
                }
            }
        }

        public static void Deinitialize()
        {
            foreach (InputDevice dev in InputSystem.devices)
            {
                if (dev.description.interfaceName == "DirectInput")
                {
                    InputSystem.RemoveDevice(dev);
                }
                else if (dev.description.interfaceName == "HID")
                {
                    InputSystem.EnableDevice(dev);
                }
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
        }

        public void OnUpdate()
        {
            FlatJoyState2 state = new FlatJoyState2();
            int hresult = Native.GetDeviceState(device.description.serial, out state); // Poll the DirectInput Device
            if (hresult == 0)
            {
                if (!lastState.Equals(state))
                {
                    lastState = state;
                    InputSystem.QueueStateEvent(this, state);
                }
            }
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