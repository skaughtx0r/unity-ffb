using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace UnityFFB {
    public enum EffectsType {
        ConstantForce = 0,
        RampForce = 1,
        Square = 2,
        Sine = 3,
        Triangle = 4,
        SawtoothUp = 5,
        SawtoothDown = 6,
        Spring = 7,
        Damper = 8,
        Inertia = 9,
        Friction = 10,
        CustomForce = 11
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct DeviceInfo {
        public uint deviceType;
        public ushort vendorId;
        public ushort productId;
        [MarshalAs(UnmanagedType.LPStr)]
        public string guidInstance;
        [MarshalAs(UnmanagedType.LPStr)]
        public string guidProduct;
        [MarshalAs(UnmanagedType.LPStr)]
        public string instanceName;
        [MarshalAs(UnmanagedType.LPStr)]
        public string productName;
        public bool hasFFB;
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct DeviceAxisInfo {
        public uint offset;
        public uint type;
        public uint flags;
        public uint ffMaxForce;
        public uint ffForceResolution;
        public uint collectionNumber;
        public uint designatorIndex;
        public uint usagePage;
        public uint usage;
        public uint dimension;
        public uint exponent;
        public uint reportId;
        [MarshalAs(UnmanagedType.LPStr)]
        public string guidType;
        [MarshalAs(UnmanagedType.LPStr)]
        public string name;
    };

    /// <summary>
    /// See https://docs.microsoft.com/en-us/previous-versions/windows/desktop/ee416601(v=vs.85)
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct DICondition {
        /// <summary>
        /// Offset for the condition, in the range from - 10,000 through 10,000.
        /// </summary>
        public int offset;
        /// <summary>
        /// Coefficient constant on the positive side of the offset, in the range 
        /// from - 10,000 through 10,000.
        /// </summary>
        public int positiveCoefficient;
        /// <summary>
        /// Coefficient constant on the negative side of the offset, in the range 
        /// from - 10,000 through 10,000. If the device does not support separate
        /// positive and negative coefficients, the value of lNegativeCoefficient 
        /// is ignored, and the value of lPositiveCoefficient is used as both the 
        /// positive and negative coefficients.
        /// </summary>
        public int negativeCoefficient;
        /// <summary>
        /// Maximum force output on the positive side of the offset, in the range
        /// from 0 through 10,000.
        /// 
        /// If the device does not support force saturation, the value of this
        /// member is ignored.
        /// </summary>
        public uint positiveSaturation;
        /// <summary>
        /// Maximum force output on the negative side of the offset, in the range
        /// from 0 through 10,000.
        ///
        /// If the device does not support force saturation, the value of this member
        /// is ignored.
        /// 
        /// If the device does not support separate positive and negative saturation,
        /// the value of dwNegativeSaturation is ignored, and the value of dwPositiveSaturation
        /// is used as both the positive and negative saturation.
        /// </summary>
        public uint negativeSaturation;
        /// <summary>
        /// Region around lOffset in which the condition is not active, in the range
        /// from 0 through 10,000. In other words, the condition is not active between
        /// lOffset minus lDeadBand and lOffset plus lDeadBand.
        /// </summary>
        public int deadband;
    }

    /// <summary>
    /// Data structure to extract the vendorId and productId from the HID descriptor.
    /// This is used for preventing duplicate HID and DirectInput devices.
    /// </summary>
    public struct VidPid
    {
        public ushort vendorId;
        public ushort productId;
    }

    /// <summary>
    /// Describes a Flattened JoyState2 for both the Unity Input System
    /// and can be populated directly by the Native UnityFFB plugin.
    /// See https://docs.microsoft.com/en-us/previous-versions/windows/desktop/ee416628(v=vs.85)
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 81)]
    public struct FlatJoyState2 : IInputStateTypeInfo
    {
        public FourCC format => new FourCC('U', 'F', 'F', 'B');

        [InputControl(name = "Button000", layout = "Button", bit = 0, displayName = "0")]
        [InputControl(name = "Button001", layout = "Button", bit = 1, displayName = "1")]
        [InputControl(name = "Button002", layout = "Button", bit = 2, displayName = "2")]
        [InputControl(name = "Button003", layout = "Button", bit = 3, displayName = "3")]
        [InputControl(name = "Button004", layout = "Button", bit = 4, displayName = "4")]
        [InputControl(name = "Button005", layout = "Button", bit = 5, displayName = "5")]
        [InputControl(name = "Button006", layout = "Button", bit = 6, displayName = "6")]
        [InputControl(name = "Button007", layout = "Button", bit = 7, displayName = "7")]
        [InputControl(name = "Button008", layout = "Button", bit = 8, displayName = "8")]
        [InputControl(name = "Button009", layout = "Button", bit = 9, displayName = "9")]
        [InputControl(name = "Button010", layout = "Button", bit = 10, displayName = "10")]
        [InputControl(name = "Button011", layout = "Button", bit = 11, displayName = "11")]
        [InputControl(name = "Button012", layout = "Button", bit = 12, displayName = "12")]
        [InputControl(name = "Button013", layout = "Button", bit = 13, displayName = "13")]
        [InputControl(name = "Button014", layout = "Button", bit = 14, displayName = "14")]
        [InputControl(name = "Button015", layout = "Button", bit = 15, displayName = "15")]
        [InputControl(name = "Button016", layout = "Button", bit = 16, displayName = "16")]
        [InputControl(name = "Button017", layout = "Button", bit = 17, displayName = "17")]
        [InputControl(name = "Button018", layout = "Button", bit = 18, displayName = "18")]
        [InputControl(name = "Button019", layout = "Button", bit = 19, displayName = "19")]
        [InputControl(name = "Button020", layout = "Button", bit = 20, displayName = "20")]
        [InputControl(name = "Button021", layout = "Button", bit = 21, displayName = "21")]
        [InputControl(name = "Button022", layout = "Button", bit = 22, displayName = "22")]
        [InputControl(name = "Button023", layout = "Button", bit = 23, displayName = "23")]
        [InputControl(name = "Button024", layout = "Button", bit = 24, displayName = "24")]
        [InputControl(name = "Button025", layout = "Button", bit = 25, displayName = "25")]
        [InputControl(name = "Button026", layout = "Button", bit = 26, displayName = "26")]
        [InputControl(name = "Button027", layout = "Button", bit = 27, displayName = "27")]
        [InputControl(name = "Button028", layout = "Button", bit = 28, displayName = "28")]
        [InputControl(name = "Button029", layout = "Button", bit = 29, displayName = "29")]
        [InputControl(name = "Button030", layout = "Button", bit = 30, displayName = "30")]
        [InputControl(name = "Button031", layout = "Button", bit = 31, displayName = "31")]
        [InputControl(name = "Button032", layout = "Button", bit = 32, displayName = "32")]
        [InputControl(name = "Button033", layout = "Button", bit = 33, displayName = "33")]
        [InputControl(name = "Button034", layout = "Button", bit = 34, displayName = "34")]
        [InputControl(name = "Button035", layout = "Button", bit = 35, displayName = "35")]
        [InputControl(name = "Button036", layout = "Button", bit = 36, displayName = "36")]
        [InputControl(name = "Button037", layout = "Button", bit = 37, displayName = "37")]
        [InputControl(name = "Button038", layout = "Button", bit = 38, displayName = "38")]
        [InputControl(name = "Button039", layout = "Button", bit = 39, displayName = "39")]
        [InputControl(name = "Button040", layout = "Button", bit = 40, displayName = "40")]
        [InputControl(name = "Button041", layout = "Button", bit = 41, displayName = "41")]
        [InputControl(name = "Button042", layout = "Button", bit = 42, displayName = "42")]
        [InputControl(name = "Button043", layout = "Button", bit = 43, displayName = "43")]
        [InputControl(name = "Button044", layout = "Button", bit = 44, displayName = "44")]
        [InputControl(name = "Button045", layout = "Button", bit = 45, displayName = "45")]
        [InputControl(name = "Button046", layout = "Button", bit = 46, displayName = "46")]
        [InputControl(name = "Button047", layout = "Button", bit = 47, displayName = "47")]
        [InputControl(name = "Button048", layout = "Button", bit = 48, displayName = "48")]
        [InputControl(name = "Button049", layout = "Button", bit = 49, displayName = "49")]
        [InputControl(name = "Button050", layout = "Button", bit = 50, displayName = "50")]
        [InputControl(name = "Button051", layout = "Button", bit = 51, displayName = "51")]
        [InputControl(name = "Button052", layout = "Button", bit = 52, displayName = "52")]
        [InputControl(name = "Button053", layout = "Button", bit = 53, displayName = "53")]
        [InputControl(name = "Button054", layout = "Button", bit = 54, displayName = "54")]
        [InputControl(name = "Button055", layout = "Button", bit = 55, displayName = "55")]
        [InputControl(name = "Button056", layout = "Button", bit = 56, displayName = "56")]
        [InputControl(name = "Button057", layout = "Button", bit = 57, displayName = "57")]
        [InputControl(name = "Button058", layout = "Button", bit = 58, displayName = "58")]
        [InputControl(name = "Button059", layout = "Button", bit = 59, displayName = "59")]
        [InputControl(name = "Button060", layout = "Button", bit = 60, displayName = "60")]
        [InputControl(name = "Button061", layout = "Button", bit = 61, displayName = "61")]
        [InputControl(name = "Button062", layout = "Button", bit = 62, displayName = "62")]
        [InputControl(name = "Button063", layout = "Button", bit = 63, displayName = "63")]
        [FieldOffset(0)]
        public UInt64 buttonsA; // Buttons seperated into banks of 64-Bits to fit into Unsigned 64-bit integer
        [InputControl(name = "Button064", layout = "Button", bit = 0, displayName = "64")]
        [InputControl(name = "Button065", layout = "Button", bit = 1, displayName = "65")]
        [InputControl(name = "Button066", layout = "Button", bit = 2, displayName = "66")]
        [InputControl(name = "Button067", layout = "Button", bit = 3, displayName = "67")]
        [InputControl(name = "Button068", layout = "Button", bit = 4, displayName = "68")]
        [InputControl(name = "Button069", layout = "Button", bit = 5, displayName = "69")]
        [InputControl(name = "Button070", layout = "Button", bit = 6, displayName = "70")]
        [InputControl(name = "Button071", layout = "Button", bit = 7, displayName = "71")]
        [InputControl(name = "Button072", layout = "Button", bit = 8, displayName = "72")]
        [InputControl(name = "Button073", layout = "Button", bit = 9, displayName = "73")]
        [InputControl(name = "Button074", layout = "Button", bit = 10, displayName = "74")]
        [InputControl(name = "Button075", layout = "Button", bit = 11, displayName = "75")]
        [InputControl(name = "Button076", layout = "Button", bit = 12, displayName = "76")]
        [InputControl(name = "Button077", layout = "Button", bit = 13, displayName = "77")]
        [InputControl(name = "Button078", layout = "Button", bit = 14, displayName = "78")]
        [InputControl(name = "Button079", layout = "Button", bit = 15, displayName = "79")]
        [InputControl(name = "Button080", layout = "Button", bit = 16, displayName = "80")]
        [InputControl(name = "Button081", layout = "Button", bit = 17, displayName = "81")]
        [InputControl(name = "Button082", layout = "Button", bit = 18, displayName = "82")]
        [InputControl(name = "Button083", layout = "Button", bit = 19, displayName = "83")]
        [InputControl(name = "Button084", layout = "Button", bit = 20, displayName = "84")]
        [InputControl(name = "Button085", layout = "Button", bit = 21, displayName = "85")]
        [InputControl(name = "Button086", layout = "Button", bit = 22, displayName = "86")]
        [InputControl(name = "Button087", layout = "Button", bit = 23, displayName = "87")]
        [InputControl(name = "Button088", layout = "Button", bit = 24, displayName = "88")]
        [InputControl(name = "Button089", layout = "Button", bit = 25, displayName = "89")]
        [InputControl(name = "Button090", layout = "Button", bit = 26, displayName = "90")]
        [InputControl(name = "Button091", layout = "Button", bit = 27, displayName = "91")]
        [InputControl(name = "Button092", layout = "Button", bit = 28, displayName = "92")]
        [InputControl(name = "Button093", layout = "Button", bit = 29, displayName = "93")]
        [InputControl(name = "Button094", layout = "Button", bit = 30, displayName = "94")]
        [InputControl(name = "Button095", layout = "Button", bit = 31, displayName = "95")]
        [InputControl(name = "Button096", layout = "Button", bit = 32, displayName = "96")]
        [InputControl(name = "Button097", layout = "Button", bit = 33, displayName = "97")]
        [InputControl(name = "Button098", layout = "Button", bit = 34, displayName = "98")]
        [InputControl(name = "Button099", layout = "Button", bit = 35, displayName = "99")]
        [InputControl(name = "Button100", layout = "Button", bit = 36, displayName = "100")]
        [InputControl(name = "Button101", layout = "Button", bit = 37, displayName = "101")]
        [InputControl(name = "Button102", layout = "Button", bit = 38, displayName = "102")]
        [InputControl(name = "Button103", layout = "Button", bit = 39, displayName = "103")]
        [InputControl(name = "Button104", layout = "Button", bit = 40, displayName = "104")]
        [InputControl(name = "Button105", layout = "Button", bit = 41, displayName = "105")]
        [InputControl(name = "Button106", layout = "Button", bit = 42, displayName = "106")]
        [InputControl(name = "Button107", layout = "Button", bit = 43, displayName = "107")]
        [InputControl(name = "Button108", layout = "Button", bit = 44, displayName = "108")]
        [InputControl(name = "Button109", layout = "Button", bit = 45, displayName = "109")]
        [InputControl(name = "Button110", layout = "Button", bit = 46, displayName = "110")]
        [InputControl(name = "Button111", layout = "Button", bit = 47, displayName = "111")]
        [InputControl(name = "Button112", layout = "Button", bit = 48, displayName = "112")]
        [InputControl(name = "Button113", layout = "Button", bit = 49, displayName = "113")]
        [InputControl(name = "Button114", layout = "Button", bit = 50, displayName = "114")]
        [InputControl(name = "Button115", layout = "Button", bit = 51, displayName = "115")]
        [InputControl(name = "Button116", layout = "Button", bit = 52, displayName = "116")]
        [InputControl(name = "Button117", layout = "Button", bit = 53, displayName = "117")]
        [InputControl(name = "Button118", layout = "Button", bit = 54, displayName = "118")]
        [InputControl(name = "Button119", layout = "Button", bit = 55, displayName = "119")]
        [InputControl(name = "Button120", layout = "Button", bit = 56, displayName = "120")]
        [InputControl(name = "Button121", layout = "Button", bit = 57, displayName = "121")]
        [InputControl(name = "Button122", layout = "Button", bit = 58, displayName = "122")]
        [InputControl(name = "Button123", layout = "Button", bit = 59, displayName = "123")]
        [InputControl(name = "Button124", layout = "Button", bit = 60, displayName = "124")]
        [InputControl(name = "Button125", layout = "Button", bit = 61, displayName = "125")]
        [InputControl(name = "Button126", layout = "Button", bit = 62, displayName = "126")]
        [InputControl(name = "Button127", layout = "Button", bit = 63, displayName = "127")]
        [FieldOffset(8)]
        public UInt64 buttonsB; // Buttons seperated into banks of 64-Bits to fit into Unsigned 64-bit integer

        [InputControl(name = "X", layout = "Axis", displayName = "X", parameters = "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5")]
        [FieldOffset(16)]
        public UInt16 lX; // X-axis
        [InputControl(name = "Y", layout = "Axis", displayName = "Y")]
        [FieldOffset(18)]
        public UInt16 lY; // Y-axis
        [InputControl(name = "Z", layout = "Axis", displayName = "Z")]
        [FieldOffset(20)]
        public UInt16 lZ; // Z-axis
                          // rglSlider Broken out
                          // // LONG rglSlider[2];
        [InputControl(name = "U", layout = "Axis", displayName = "U")]
        [FieldOffset(22)]
        public UInt16 lU; // U-axis
        [InputControl(name = "V", layout = "Axis", displayName = "V")]
        [FieldOffset(24)]
        public UInt16 lV; // V-axis

        [InputControl(name = "RX", layout = "Axis", displayName = "X Rotation")]
        [FieldOffset(26)]
        public UInt16 lRx; // X-axis rotation
        [InputControl(name = "RY", layout = "Axis", displayName = "Y Rotation")]
        [FieldOffset(28)]
        public UInt16 lRy; // Y-axis rotation
        [InputControl(name = "RZ", layout = "Axis", displayName = "Z Rotation")]
        [FieldOffset(30)]
        public UInt16 lRz; // Z-axis rotation

        [InputControl(name = "VX", layout = "Axis", displayName = "X Velocity")]
        [FieldOffset(32)]
        public UInt16 lVX; // X-axis velocity
        [InputControl(name = "VY", layout = "Axis", displayName = "Y Velocity")]
        [FieldOffset(34)]
        public UInt16 lVY; // Y-axis velocity
        [InputControl(name = "VZ", layout = "Axis", displayName = "Z Velocity")]
        [FieldOffset(36)]
        public UInt16 lVZ; // Z-axis velocity
                           // rglVSlider Broken out
                           // // LONG rglVSlider[2]; // Extra axis velocities
        [InputControl(name = "VU", layout = "Axis", displayName = "U Velocity")]
        [FieldOffset(38)]
        public UInt16 lVU; // U-axis velocity
        [InputControl(name = "VV", layout = "Axis", displayName = "V Velocity")]
        [FieldOffset(40)]
        public UInt16 lVV; // V-axis velocity

        [InputControl(name = "VRX", layout = "Axis", displayName = "X Angular Velocity")]
        [FieldOffset(42)]
        public UInt16 lVRx; // X-axis angular velocity
        [InputControl(name = "VRY", layout = "Axis", displayName = "Y Angular Velocity")]
        [FieldOffset(44)]
        public UInt16 lVRy; // Y-axis angular velocity
        [InputControl(name = "VRZ", layout = "Axis", displayName = "Z Angular Velocity")]
        [FieldOffset(46)]
        public UInt16 lVRz; // Z-axis angular velocity

        [InputControl(name = "AX", layout = "Axis", displayName = "X Acceleration")]
        [FieldOffset(48)]
        public UInt16 lAX; // X-axis acceleration
        [InputControl(name = "AY", layout = "Axis", displayName = "Y Acceleration")]
        [FieldOffset(50)]
        public UInt16 lAY; // Y-axis acceleration
        [InputControl(name = "AZ", layout = "Axis", displayName = "Z Acceleration")]
        [FieldOffset(52)]
        public UInt16 lAZ; // Z-axis acceleration
                           // rglASlider Broken out
                           // // LONG rglASlider[2]; // Extra axis accelerations
        [InputControl(name = "AU", layout = "Axis", displayName = "U Acceleration")]
        [FieldOffset(54)]
        public UInt16 lAU; // U-axis acceleration
        [InputControl(name = "AV", layout = "Axis", displayName = "V Acceleration")]
        [FieldOffset(56)]
        public UInt16 lAV; // V-axis acceleration

        [InputControl(name = "ARX", layout = "Axis", displayName = "X Angular Acceleration")]
        [FieldOffset(58)]
        public UInt16 lARx; // X-axis angular acceleration
        [InputControl(name = "ARY", layout = "Axis", displayName = "Y Angular Acceleration")]
        [FieldOffset(60)]
        public UInt16 lARy; // Y-axis angular acceleration
        [InputControl(name = "ARZ", layout = "Axis", displayName = "Z Angular Acceleration")]
        [FieldOffset(62)]
        public UInt16 lARz; // Z-axis angular acceleration

        [InputControl(name = "AFX", layout = "Axis", displayName = "X Force")]
        [FieldOffset(64)]
        public UInt16 lFX; // X-axis force
        [InputControl(name = "AFY", layout = "Axis", displayName = "Y Force")]
        [FieldOffset(66)]
        public UInt16 lFY; // Y-axis force
        [InputControl(name = "AFZ", layout = "Axis", displayName = "Z Force")]
        [FieldOffset(68)]
        public UInt16 lFZ; // Z-axis force
                           // rglFSlider Broken out
                           // // LONG rglFSlider[2]; // Extra axis forces
        [InputControl(name = "AFU", layout = "Axis", displayName = "U Force")]
        [FieldOffset(70)]
        public UInt16 lFU; // U-axis force
        [InputControl(name = "AFV", layout = "Axis", displayName = "V Force")]
        [FieldOffset(72)]
        public UInt16 lFV; // V-axis force


        [InputControl(name = "FRX", layout = "Axis", displayName = "X Torque")]
        [FieldOffset(74)]
        public UInt16 lFRx; // X-axis torque
        [InputControl(name = "FRY", layout = "Axis", displayName = "Y Torque")]
        [FieldOffset(76)]
        public UInt16 lFRy; // Y-axis torque
        [InputControl(name = "FRZ", layout = "Axis", displayName = "Z Torque")]
        [FieldOffset(78)]
        public UInt16 lFRz; // Z-axis torque

        // rgdwPOV Broken out
        // DWORD rgdwPOV[4]; // 4 PoV Hats
        [InputControl(name = "dpad0", layout = "Dpad", bit = 0, sizeInBits = 4, displayName = "Dpad0")]
        [InputControl(name = "dpad0/up", bit = 0, displayName = "Up")]
        [InputControl(name = "dpad0/down", bit = 1, displayName = "Down")]
        [InputControl(name = "dpad0/left", bit = 2, displayName = "Left")]
        [InputControl(name = "dpad0/right", bit = 3, displayName = "Right")]
        [InputControl(name = "dpad1", layout = "Dpad", bit = 4, sizeInBits = 4, displayName = "Dpad1")]
        [InputControl(name = "dpad1/up", bit = 4, displayName = "Up")]
        [InputControl(name = "dpad1/down", bit = 5, displayName = "Down")]
        [InputControl(name = "dpad1/left", bit = 6, displayName = "Left")]
        [InputControl(name = "dpad1/right", bit = 7, displayName = "Right")]
        [InputControl(name = "dpad2", layout = "Dpad", bit = 0, sizeInBits = 4, displayName = "Dpad2")]
        [InputControl(name = "dpad2/up", bit = 8, displayName = "Up")]
        [InputControl(name = "dpad2/down", bit = 9, displayName = "Down")]
        [InputControl(name = "dpad2/left", bit = 10, displayName = "Left")]
        [InputControl(name = "dpad2/right", bit = 11, displayName = "Right")]
        [InputControl(name = "dpad3", layout = "Dpad", bit = 4, sizeInBits = 4, displayName = "Dpad3")]
        [InputControl(name = "dpad3/up", bit = 12, displayName = "Up")]
        [InputControl(name = "dpad3/down", bit = 13, displayName = "Down")]
        [InputControl(name = "dpad3/left", bit = 14, displayName = "Left")]
        [InputControl(name = "dpad3/right", bit = 15, displayName = "Right")]
        [FieldOffset(80)]
        public UInt16 rgdwPOV; // Store each DPAD in chunks of 4 bits inside 16-bit short     
    }
}