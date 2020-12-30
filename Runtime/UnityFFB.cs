using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace UnityFFB
{
    public class UnityFFB : MonoBehaviour
    {
        public static UnityFFB instance;

        /// <summary>
        /// Whether or not to enable Force Feedback when the behavior starts.
        /// </summary>
        public bool enableOnAwake = true;
        /// <summary>
        /// Whether or not to automatically select the first FFB device on start.
        /// </summary>
        public bool autoSelectFirstDevice = true;

        // Constant force properties
        public int force = 0;
        public int[] axisDirections = new int[0];

        public bool ffbEnabled { get; private set; }
        public bool constantForceEnabled { get; private set; }

        public DeviceInfo[] devices = new DeviceInfo[0];

        public DeviceInfo? activeDevice = null;

        public DeviceAxisInfo[] axes = new DeviceAxisInfo[0];
        public DICondition[] springConditions = new DICondition[0];

#if UNITY_STANDALONE_WIN

        void Awake()
        {
            instance = this;

            if (enableOnAwake)
            {
                EnableForceFeedback();
            }
        }

        private void FixedUpdate()
        {
            if (constantForceEnabled)
            {
                UnityFFBNative.UpdateConstantForce(force, axisDirections);
            }
        }

        public void EnableForceFeedback()
        {
            if (ffbEnabled)
            {
                return;
            }

            if (UnityFFBNative.InitDirectInput() >= 0)
            {
                ffbEnabled = true;
            }
            else
            {
                ffbEnabled = false;
            }

            int deviceCount = 0;

            IntPtr ptrDevices = UnityFFBNative.EnumerateFFBDevices(ref deviceCount);

            if (deviceCount > 0)
            {

                devices = new DeviceInfo[deviceCount];

                int deviceSize = Marshal.SizeOf(typeof(DeviceInfo));
                for (int i = 0; i < deviceCount; i++)
                {
                    IntPtr pCurrent = ptrDevices + i * deviceSize;
                    devices[i] = Marshal.PtrToStructure<DeviceInfo>(pCurrent);
                }

                if (autoSelectFirstDevice)
                {
                    SelectDevice(devices[0].guidInstance);
                }
            }
        }

        public void DisableForceFeedback()
        {
            UnityFFBNative.Shutdown();
            ffbEnabled = false;
            constantForceEnabled = false;
            devices = new DeviceInfo[0];
            activeDevice = null;
            axes = new DeviceAxisInfo[0];
            springConditions = new DICondition[0];
        }

        public void SelectDevice(string deviceGuid)
        {
            // For now just initialize the first FFB Device.
            if (UnityFFBNative.CreateFFBDevice(deviceGuid) == 0)
            {
                activeDevice = devices[0];
                int axisCount = 0;
                IntPtr ptrAxes = UnityFFBNative.EnumerateFFBAxes(ref axisCount);
                if (axisCount > 0)
                {
                    axes = new DeviceAxisInfo[axisCount];
                    axisDirections = new int[axisCount];
                    springConditions = new DICondition[axisCount];

                    int axisSize = Marshal.SizeOf(typeof(DeviceAxisInfo));
                    for (int i = 0; i < axisCount; i++)
                    {
                        IntPtr pCurrent = ptrAxes + i * axisSize;
                        axes[i] = Marshal.PtrToStructure<DeviceAxisInfo>(pCurrent);
                        axisDirections[i] = 0;
                        springConditions[i] = new DICondition();
                    }

                    if (UnityFFBNative.AddFFBEffect(EffectsType.ConstantForce) == 0)
                    {
                        int hresult = UnityFFBNative.UpdateConstantForce(0, axisDirections);
                        constantForceEnabled = true;
                    }
                }
            }
            else
            {
                activeDevice = null;
            }
        }

        public void SetConstantForceGain(float gainPercent)
        {
            if (constantForceEnabled)
            {
                UnityFFBNative.UpdateEffectGain(EffectsType.ConstantForce, gainPercent);
            }
        }

        public void StartFFBEffects()
        {
            UnityFFBNative.StartAllFFBEffects();
            constantForceEnabled = true;
        }

        public void StopFFBEffects()
        {
            UnityFFBNative.StopAllFFBEffects();
            constantForceEnabled = false;
        }

        public void OnApplicationQuit()
        {
            DisableForceFeedback();
        }
#endif
    }
}
