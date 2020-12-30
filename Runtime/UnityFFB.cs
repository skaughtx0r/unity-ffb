using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace UnityFFB
{
    public class UnityFFB : MonoBehaviour
    {
        public static UnityFFB instance;

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

        [DllImport("UNITYFFB")]
        private static extern int InitDirectInput();

        [DllImport("UNITYFFB")]
        private static extern IntPtr EnumerateFFBDevices(ref int deviceCount);

        [DllImport("UNITYFFB")]
        private static extern IntPtr EnumerateFFBAxes(ref int axisCount);

        [DllImport("UNITYFFB")]
        private static extern int CreateFFBDevice(string guidInstance);

        [DllImport("UNITYFFB")]
        private static extern int AddFFBEffect(EffectsType effectType);

        [DllImport("UNITYFFB")]
        private static extern int UpdateConstantForce(int magnitude, int[] directions);

        [DllImport("UNITYFFB")]
        private static extern int UpdateSpring(DICondition[] conditions);

        [DllImport("UNITYFFB")]
        private static extern void StartAllFFBEffects();

        [DllImport("UNITYFFB")]
        private static extern void StopAllFFBEffects();

        [DllImport("UNITYFFB")]
        private static extern void FreeFFBDevice();

        [DllImport("UNITYFFB")]
        private static extern void FreeDirectInput();

        [DllImport("UNITYFFB")]
        private static extern void Shutdown();

        void Awake()
        {
            instance = this;

            if (InitDirectInput() >= 0)
            {
                ffbEnabled = true;
            }
            else
            {
                ffbEnabled = false;
            }

            int deviceCount = 0;

            IntPtr ptrDevices = EnumerateFFBDevices(ref deviceCount);

            if (deviceCount > 0)
            {

                devices = new DeviceInfo[deviceCount];

                int deviceSize = Marshal.SizeOf(typeof(DeviceInfo));
                for (int i = 0; i < deviceCount; i++)
                {
                    IntPtr pCurrent = ptrDevices + i * deviceSize;
                    devices[i] = Marshal.PtrToStructure<DeviceInfo>(pCurrent);
                }

                // For now just initialize the first FFB Device.
                if (CreateFFBDevice(devices[0].guidInstance) == 0)
                {
                    activeDevice = devices[0];
                    int axisCount = 0;
                    IntPtr ptrAxes = EnumerateFFBAxes(ref axisCount);
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
                    }
                }
                else
                {
                    activeDevice = null;
                }
            }
        }

        private void Start()
        {
            if(AddFFBEffect(EffectsType.ConstantForce) == 0)
            {
                int hresult = UpdateConstantForce(0, axisDirections);
                constantForceEnabled = true;
            }
        }

        private void FixedUpdate()
        {
            if (constantForceEnabled) {
                UpdateConstantForce(force, axisDirections);
            }
        }

        public void StartFFBEffects()
        {
            StartAllFFBEffects();
            constantForceEnabled = true;
        }

        public void StopFFBEffects()
        {
            StopAllFFBEffects();
            constantForceEnabled = false;
        }

        public void OnApplicationQuit()
        {
            Shutdown();
        }
#endif
    }
}
