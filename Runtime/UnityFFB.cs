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
        /// <summary>
        /// Whether or not to automatically disable auto-centering on the device.
        /// </summary>
        public bool disableAutoCenter = true;
        /// <summary>
        /// Whether or not to automatically add a constant force effect to the device.
        /// </summary>
        public bool addConstantForce = true;
        /// <summary>
        /// Whether or not to automatically add a spring force to the device.
        /// </summary>
        public bool addSpringForce = false;

        // Constant force properties
        public int force = 0;
        public float sensitivity = 1.0f;
        public int[] axisDirections = new int[0];

        public bool ffbEnabled { get; private set; }
        public bool constantForceEnabled { get; private set; }
        public bool springForceEnabled { get; private set; }

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
                UnityFFBNative.UpdateConstantForce((int)(force * sensitivity), axisDirections);
            }
        }
#endif

        public void EnableForceFeedback()
        {
#if UNITY_STANDALONE_WIN
            if (ffbEnabled)
            {
                return;
            }

            if (UnityFFBNative.StartDirectInput() >= 0)
            {
                ffbEnabled = true;
            }
            else
            {
                ffbEnabled = false;
            }

            int deviceCount = 0;

            IntPtr ptrDevices = UnityFFBNative.EnumerateFFBDevices(ref deviceCount);

            Debug.Log($"[UnityFFB] Device count: {devices.Length}");
            if (deviceCount > 0)
            {
                devices = new DeviceInfo[deviceCount];

                int deviceSize = Marshal.SizeOf(typeof(DeviceInfo));
                for (int i = 0; i < deviceCount; i++)
                {
                    IntPtr pCurrent = ptrDevices + i * deviceSize;
                    devices[i] = Marshal.PtrToStructure<DeviceInfo>(pCurrent);
                }

                foreach (DeviceInfo device in devices)
                {
                    string ffbAxis = UnityEngine.JsonUtility.ToJson(device, true);
                    Debug.Log(ffbAxis);
                }

                if (autoSelectFirstDevice)
                {
                    SelectDevice(devices[0].guidInstance);
                }
            }
#endif
        }

        public void DisableForceFeedback()
        {
#if UNITY_STANDALONE_WIN
            UnityFFBNative.StopDirectInput();
            ffbEnabled = false;
            constantForceEnabled = false;
            devices = new DeviceInfo[0];
            activeDevice = null;
            axes = new DeviceAxisInfo[0];
            springConditions = new DICondition[0];
#endif
        }

        public void SelectDevice(string deviceGuid)
        {
#if UNITY_STANDALONE_WIN
            // For now just initialize the first FFB Device.
            int hresult = UnityFFBNative.CreateFFBDevice(deviceGuid);
            if (hresult == 0)
            {
                activeDevice = devices[0];

                if (disableAutoCenter)
                {
                    hresult = UnityFFBNative.SetAutoCenter(false);
                    if (hresult != 0)
                    {
                        Debug.LogError($"[UnityFFB] SetAutoCenter Failed: {WinErrors.GetSystemMessage(hresult)}");
                    }
                }

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

                    if (addConstantForce)
                    {
                        hresult = UnityFFBNative.AddFFBEffect(EffectsType.ConstantForce);
                        if (UnityFFBNative.AddFFBEffect(EffectsType.ConstantForce) == 0)
                        {
                            hresult = UnityFFBNative.UpdateConstantForce(0, axisDirections);
                            if (hresult != 0)
                            {
                                Debug.LogError($"[UnityFFB] UpdateConstantForce Failed: {WinErrors.GetSystemMessage(hresult)}");
                            }
                            constantForceEnabled = true;
                        }
                        else
                        {
                            Debug.LogError($"[UnityFFB] AddConstantForce Failed: {WinErrors.GetSystemMessage(hresult)}");
                        }
                    }

                    if (addSpringForce)
                    {
                        hresult = UnityFFBNative.AddFFBEffect(EffectsType.Spring);
                        if (hresult == 0)
                        {
                            for (int i = 0; i < springConditions.Length; i++)
                            {
                                springConditions[i].deadband = 0;
                                springConditions[i].offset = 0;
                                springConditions[i].negativeCoefficient = 2000;
                                springConditions[i].positiveCoefficient = 2000;
                                springConditions[i].negativeSaturation = 10000;
                                springConditions[i].positiveSaturation = 10000;
                            }
                            hresult = UnityFFBNative.UpdateSpring(springConditions);
                            Debug.LogError($"[UnityFFB] UpdateSpringForce Failed: {WinErrors.GetSystemMessage(hresult)}");
                        }
                        else
                        {
                            Debug.LogError($"[UnityFFB] AddSpringForce Failed: {WinErrors.GetSystemMessage(hresult)}");
                        }
                    }
                }
                Debug.Log($"[UnityFFB] Axis count: {axes.Length}");
                foreach (DeviceAxisInfo axis in axes)
                {
                    string ffbAxis = UnityEngine.JsonUtility.ToJson(axis, true);
                    Debug.Log(ffbAxis);
                }
            }
            else
            {
                activeDevice = null;
                Debug.LogError($"[UnityFFB] {WinErrors.GetSystemMessage(hresult)}");
            }
#endif
        }

        public void SetConstantForceGain(float gainPercent)
        {
#if UNITY_STANDALONE_WIN
            if (constantForceEnabled)
            {
                int hresult = UnityFFBNative.UpdateEffectGain(EffectsType.ConstantForce, gainPercent);
                Debug.LogError($"[UnityFFB] UpdateEffectGain Failed: {WinErrors.GetSystemMessage(hresult)}");
            }
#endif
        }

        public void StartFFBEffects()
        {
#if UNITY_STANDALONE_WIN
            UnityFFBNative.StartAllFFBEffects();
            constantForceEnabled = true;
#endif
        }

        public void StopFFBEffects()
        {
#if UNITY_STANDALONE_WIN
            UnityFFBNative.StopAllFFBEffects();
            constantForceEnabled = false;
#endif
        }

#if UNITY_STANDALONE_WIN
        public void OnApplicationQuit()
        {
            DisableForceFeedback();
        }
#endif
    }
}
