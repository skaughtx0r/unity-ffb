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
        public FFBMode currentMode { get; private set; }

        private bool effectsEnabled = false;

        public DeviceInfo[] devices = new DeviceInfo[0];

        public DeviceInfo? activeDevice = null;

        public DeviceAxisInfo[] axes = new DeviceAxisInfo[0];
        public DICondition[] springConditions = new DICondition[0];

        protected bool nativeLibLoadFailed = false;

        void Awake()
        {
            instance = this;
            DirectInputDevice.Initialize();
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
            if (effectsEnabled && constantForceEnabled && activeDevice != null)
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

                Debug.Log($"[UnityFFB] EnableForceFeedback: {DirectInputManager.devices.Length} device(s), autoSelect={autoSelectFirstDevice}");
                foreach (var device in DirectInputManager.devices)
                {
                    Debug.Log($"[UnityFFB] EnableForceFeedback: '{device.productName}' hasFFB={device.hasFFB} guid={device.guidInstance}");
                }

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
            effectsEnabled = false;
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

                    // Set initial mode based on inspector flags
                    if (addConstantForce)
                    {
                        SetFFBMode(FFBMode.ConstantForce);
                    }
                    else if (addSpringForce)
                    {
                        SetFFBMode(FFBMode.SpringEffect);
                    }
                    else if (!disableAutoCenter)
                    {
                        SetFFBMode(FFBMode.NativeSpring);
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
                if (activeDevice != null)
                {
                    int hresult = Native.UpdateEffectGain(activeDevice.Value.guidInstance, EffectsType.ConstantForce, gainPercent);
                    Debug.LogError($"[UnityFFB] UpdateEffectGain Failed: 0x{hresult.ToString("x")} {WinErrors.GetSystemMessage(hresult)}");
                }
            }
#endif
        }

        public void StartFFBEffects()
        {
#if UNITY_STANDALONE_WIN
            if (nativeLibLoadFailed) { return; }
            try
            {
                if (activeDevice != null)
                {
                    Native.StartAllFFBEffects(activeDevice.Value.guidInstance);
                }
                effectsEnabled = true;
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
                if (activeDevice != null)
                {
                    Native.StopAllFFBEffects(activeDevice.Value.guidInstance);
                }
                effectsEnabled = false;
            }
            catch (DllNotFoundException e)
            {
                LogMissingRuntimeError();
            }
#endif
        }

        /// <summary>
        /// Switch the force feedback mode at runtime.
        /// </summary>
        public void SetFFBMode(FFBMode mode)
        {
#if UNITY_STANDALONE_WIN
            if (nativeLibLoadFailed) { return; }
            if (activeDevice == null || axes.Length == 0) { return; }

            string guid = activeDevice.Value.guidInstance;
            int hresult;

            // Tear down current mode
            if (constantForceEnabled)
            {
                Native.RemoveFFBEffect(guid, EffectsType.ConstantForce);
                constantForceEnabled = false;
            }
            if (springForceEnabled)
            {
                Native.RemoveFFBEffect(guid, EffectsType.Spring);
                springForceEnabled = false;
            }
            effectsEnabled = false;

            // Set up new mode
            switch (mode)
            {
                case FFBMode.ConstantForce:
                    Native.SetAutoCenter(guid, false);
                    hresult = Native.AddFFBEffect(guid, EffectsType.ConstantForce);
                    if (hresult == 0)
                    {
                        Native.UpdateConstantForce(guid, 0, axisDirections);
                        Native.StartAllFFBEffects(guid);
                        constantForceEnabled = true;
                        effectsEnabled = true;
                    }
                    else
                    {
                        Debug.LogError($"[UnityFFB] SetFFBMode ConstantForce: AddEffect failed 0x{hresult.ToString("x")} {WinErrors.GetSystemMessage(hresult)}");
                    }
                    break;

                case FFBMode.SpringEffect:
                    Native.SetAutoCenter(guid, false);
                    hresult = Native.AddFFBEffect(guid, EffectsType.Spring);
                    if (hresult == 0)
                    {
                        for (int i = 0; i < springConditions.Length; i++)
                        {
                            if (springConditions[i].positiveCoefficient == 0 && springConditions[i].negativeCoefficient == 0)
                            {
                                springConditions[i].deadband = 0;
                                springConditions[i].offset = 0;
                                springConditions[i].negativeCoefficient = 2000;
                                springConditions[i].positiveCoefficient = 2000;
                                springConditions[i].negativeSaturation = 10000;
                                springConditions[i].positiveSaturation = 10000;
                            }
                        }
                        Native.UpdateSpring(guid, springConditions);
                        Native.StartAllFFBEffects(guid);
                        springForceEnabled = true;
                        effectsEnabled = true;
                    }
                    else
                    {
                        Debug.LogError($"[UnityFFB] SetFFBMode SpringEffect: AddEffect failed 0x{hresult.ToString("x")} {WinErrors.GetSystemMessage(hresult)}");
                    }
                    break;

                case FFBMode.NativeSpring:
                    Native.SetAutoCenter(guid, true);
                    break;
            }

            currentMode = mode;
            Debug.Log($"[UnityFFB] FFB mode set to {mode}");
#endif
        }

        /// <summary>
        /// Update the spring force conditions at runtime. Only applies when in SpringEffect mode.
        /// </summary>
        public void UpdateSpringForce(DICondition[] conditions)
        {
#if UNITY_STANDALONE_WIN
            if (nativeLibLoadFailed) { return; }
            if (!springForceEnabled || activeDevice == null) { return; }

            springConditions = conditions;
            int hresult = Native.UpdateSpring(activeDevice.Value.guidInstance, springConditions);
            if (hresult != 0)
            {
                Debug.LogError($"[UnityFFB] UpdateSpringForce Failed: 0x{hresult.ToString("x")} {WinErrors.GetSystemMessage(hresult)}");
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
            DirectInputDevice.Deinitialize();
        }
#endif
    }
}
