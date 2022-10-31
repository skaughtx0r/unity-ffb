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

        protected bool nativeLibLoadFailed = false;

        void Awake()
        {
            instance = this;
#if UNITY_STANDALONE_WIN
            if (enableOnAwake)
            {
                EnableForceFeedback();
            }
#endif
        }

#if UNITY_STANDALONE_WIN
        private void FixedUpdate()
        {
            if (nativeLibLoadFailed) { return; }
            if (constantForceEnabled && activeDevice != null)
            {
                Native.UpdateConstantForce(activeDevice.Value.guidInstance, (int)(force * sensitivity), axisDirections);
            }
        }
#endif

        public void EnableForceFeedback()
        {
#if UNITY_STANDALONE_WIN
            if (nativeLibLoadFailed || ffbEnabled)
            {
                return;
            }

            try
            {
                ffbEnabled = true;

                if (autoSelectFirstDevice)
                {
                    foreach (var device in DirectInputManager.devices)
                    {
                        if (device.hasFFB)
                        {
                            SelectDevice(device);
                            break;
                        }
                    }
                }
            }
            catch (DllNotFoundException e)
            {
                LogMissingRuntimeError();
            }
#endif
        }

        public void DisableForceFeedback()
        {
#if UNITY_STANDALONE_WIN
            if (nativeLibLoadFailed) { return; }
            try
            {
                if (activeDevice != null)
                {
                    Native.RemoveAllFFBEffects(activeDevice.Value.guidInstance);
                }
            }
            catch (DllNotFoundException e)
            {
                LogMissingRuntimeError();
            }
            ffbEnabled = false;
            constantForceEnabled = false;
            devices = new DeviceInfo[0];
            activeDevice = null;
            axes = new DeviceAxisInfo[0];
            springConditions = new DICondition[0];
#endif
        }

        public void SelectDevice(DeviceInfo device)
        {
#if UNITY_STANDALONE_WIN
            if (nativeLibLoadFailed) { return; }
            try
            {
                int hresult;
                activeDevice = device;

                if (disableAutoCenter)
                {
                    hresult = Native.SetAutoCenter(device.guidInstance, false);
                    if (hresult != 0)
                    {
                        Debug.LogError($"[UnityFFB] SetAutoCenter Failed: 0x{hresult.ToString("x")} {WinErrors.GetSystemMessage(hresult)}");
                    }
                }

                int axisCount = 0;
                IntPtr ptrAxes = Native.EnumerateFFBAxes(device.guidInstance, ref axisCount);
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
                        hresult = Native.AddFFBEffect(device.guidInstance, EffectsType.ConstantForce);
                        if (hresult == 0)
                        {
                            hresult = Native.UpdateConstantForce(device.guidInstance, 0, axisDirections);
                            if (hresult != 0)
                            {
                                Debug.LogError($"[UnityFFB] UpdateConstantForce Failed: 0x{hresult.ToString("x")} {WinErrors.GetSystemMessage(hresult)}");
                            }
                            constantForceEnabled = true;
                        }
                        else
                        {
                            Debug.LogError($"[UnityFFB] AddConstantForce Failed: 0x{hresult.ToString("x")} {WinErrors.GetSystemMessage(hresult)}");
                        }
                    }

                    if (addSpringForce)
                    {
                        hresult = Native.AddFFBEffect(device.guidInstance, EffectsType.Spring);
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
                            hresult = Native.UpdateSpring(device.guidInstance, springConditions);
                            Debug.LogError($"[UnityFFB] UpdateSpringForce Failed: 0x{hresult.ToString("x")} {WinErrors.GetSystemMessage(hresult)}");
                        }
                        else
                        {
                            Debug.LogError($"[UnityFFB] AddSpringForce Failed: 0x{hresult.ToString("x")} {WinErrors.GetSystemMessage(hresult)}");
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
            catch (DllNotFoundException e)
            {
                LogMissingRuntimeError();
            }
#endif
        }

        public void SetConstantForceGain(float gainPercent)
        {
#if UNITY_STANDALONE_WIN
            if (nativeLibLoadFailed) { return; }
            if (constantForceEnabled)
            {
                int hresult = Native.UpdateEffectGain(activeDevice.Value.guidInstance, EffectsType.ConstantForce, gainPercent);
                Debug.LogError($"[UnityFFB] UpdateEffectGain Failed: 0x{hresult.ToString("x")} {WinErrors.GetSystemMessage(hresult)}");
            }
#endif
        }

        public void StartFFBEffects()
        {
#if UNITY_STANDALONE_WIN
            if (nativeLibLoadFailed) { return; }
            try
            {
                Native.StartAllFFBEffects();
                constantForceEnabled = true;
            }
            catch (DllNotFoundException e)
            {
                LogMissingRuntimeError();
            }
#endif
        }

        public void StopFFBEffects()
        {
#if UNITY_STANDALONE_WIN
            if (nativeLibLoadFailed) { return; }
            try
            {
                Native.StopAllFFBEffects();
                constantForceEnabled = false;
            }
            catch (DllNotFoundException e)
            {
                LogMissingRuntimeError();
            }
#endif
        }

        void LogMissingRuntimeError()
        {
            Debug.LogError(
                "Unable to load Force Feedback plugin. Ensure that the following are installed:\n\n" +
                "DirectX End-User Runtime: https://www.microsoft.com/en-us/download/details.aspx?id=35\n" +
                "Visual C++ Redistributable: https://aka.ms/vs/17/release/vc_redist.x64.exe"
            );
            nativeLibLoadFailed = true;
        }

#if UNITY_STANDALONE_WIN
        public void OnApplicationQuit()
        {
            DisableForceFeedback();
        }
#endif
    }
}
