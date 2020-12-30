using System;
using System.Runtime.InteropServices;

namespace UnityFFB
{
    public class UnityFFBNative
    {
#if UNITY_STANDALONE_WIN

        [DllImport("UNITYFFB")]
        public static extern int InitDirectInput();

        [DllImport("UNITYFFB")]
        public static extern IntPtr EnumerateFFBDevices(ref int deviceCount);

        [DllImport("UNITYFFB")]
        public static extern IntPtr EnumerateFFBAxes(ref int axisCount);

        [DllImport("UNITYFFB")]
        public static extern int CreateFFBDevice(string guidInstance);

        [DllImport("UNITYFFB")]
        public static extern int AddFFBEffect(EffectsType effectType);

        [DllImport("UNITYFFB")]
        public static extern int UpdateConstantForce(int magnitude, int[] directions);

        [DllImport("UNITYFFB")]
        public static extern int UpdateSpring(DICondition[] conditions);

        [DllImport("UNITYFFB")]
        public static extern int UpdateEffectGain(EffectsType effectType, float gainPercent);

        [DllImport("UNITYFFB")]
        public static extern void StartAllFFBEffects();

        [DllImport("UNITYFFB")]
        public static extern void StopAllFFBEffects();

        [DllImport("UNITYFFB")]
        public static extern void FreeFFBDevice();

        [DllImport("UNITYFFB")]
        public static extern void FreeDirectInput();

        [DllImport("UNITYFFB")]
        public static extern void Shutdown();
#endif
    }
}
