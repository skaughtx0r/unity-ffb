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

/// <summary>
/// Describes a DIJOYSTATE2 in the Unity Input System form
/// Breaks out RGLSlider-s into U and V axis
/// See https://docs.microsoft.com/en-us/previous-versions/windows/desktop/ee416628(v=vs.85)
/// </summary>
public struct DIJOYSTATE2State : IInputStateTypeInfo {
    public FourCC format => new FourCC('D', 'F', 'F', 'B');

    [InputControl(name="Button000", layout="Button", bit=0, displayName="0")]
    [InputControl(name="Button001", layout="Button", bit=1, displayName="1")]
    [InputControl(name="Button002", layout="Button", bit=2, displayName="2")]
    [InputControl(name="Button003", layout="Button", bit=3, displayName="3")]
    [InputControl(name="Button004", layout="Button", bit=4, displayName="4")]
    [InputControl(name="Button005", layout="Button", bit=5, displayName="5")]
    [InputControl(name="Button006", layout="Button", bit=6, displayName="6")]
    [InputControl(name="Button007", layout="Button", bit=7, displayName="7")]
    [InputControl(name="Button008", layout="Button", bit=8, displayName="8")]
    [InputControl(name="Button009", layout="Button", bit=9, displayName="9")]
    [InputControl(name="Button010", layout="Button", bit=10, displayName="10")]
    [InputControl(name="Button011", layout="Button", bit=11, displayName="11")]
    [InputControl(name="Button012", layout="Button", bit=12, displayName="12")]
    [InputControl(name="Button013", layout="Button", bit=13, displayName="13")]
    [InputControl(name="Button014", layout="Button", bit=14, displayName="14")]
    [InputControl(name="Button015", layout="Button", bit=15, displayName="15")]
    [InputControl(name="Button016", layout="Button", bit=16, displayName="16")]
    [InputControl(name="Button017", layout="Button", bit=17, displayName="17")]
    [InputControl(name="Button018", layout="Button", bit=18, displayName="18")]
    [InputControl(name="Button019", layout="Button", bit=19, displayName="19")]
    [InputControl(name="Button020", layout="Button", bit=20, displayName="20")]
    [InputControl(name="Button021", layout="Button", bit=21, displayName="21")]
    [InputControl(name="Button022", layout="Button", bit=22, displayName="22")]
    [InputControl(name="Button023", layout="Button", bit=23, displayName="23")]
    [InputControl(name="Button024", layout="Button", bit=24, displayName="24")]
    [InputControl(name="Button025", layout="Button", bit=25, displayName="25")]
    [InputControl(name="Button026", layout="Button", bit=26, displayName="26")]
    [InputControl(name="Button027", layout="Button", bit=27, displayName="27")]
    [InputControl(name="Button028", layout="Button", bit=28, displayName="28")]
    [InputControl(name="Button029", layout="Button", bit=29, displayName="29")]
    [InputControl(name="Button030", layout="Button", bit=30, displayName="30")]
    [InputControl(name="Button031", layout="Button", bit=31, displayName="31")]
    [InputControl(name="Button032", layout="Button", bit=32, displayName="32")]
    [InputControl(name="Button033", layout="Button", bit=33, displayName="33")]
    [InputControl(name="Button034", layout="Button", bit=34, displayName="34")]
    [InputControl(name="Button035", layout="Button", bit=35, displayName="35")]
    [InputControl(name="Button036", layout="Button", bit=36, displayName="36")]
    [InputControl(name="Button037", layout="Button", bit=37, displayName="37")]
    [InputControl(name="Button038", layout="Button", bit=38, displayName="38")]
    [InputControl(name="Button039", layout="Button", bit=39, displayName="39")]
    [InputControl(name="Button040", layout="Button", bit=40, displayName="40")]
    [InputControl(name="Button041", layout="Button", bit=41, displayName="41")]
    [InputControl(name="Button042", layout="Button", bit=42, displayName="42")]
    [InputControl(name="Button043", layout="Button", bit=43, displayName="43")]
    [InputControl(name="Button044", layout="Button", bit=44, displayName="44")]
    [InputControl(name="Button045", layout="Button", bit=45, displayName="45")]
    [InputControl(name="Button046", layout="Button", bit=46, displayName="46")]
    [InputControl(name="Button047", layout="Button", bit=47, displayName="47")]
    [InputControl(name="Button048", layout="Button", bit=48, displayName="48")]
    [InputControl(name="Button049", layout="Button", bit=49, displayName="49")]
    [InputControl(name="Button050", layout="Button", bit=50, displayName="50")]
    [InputControl(name="Button051", layout="Button", bit=51, displayName="51")]
    [InputControl(name="Button052", layout="Button", bit=52, displayName="52")]
    [InputControl(name="Button053", layout="Button", bit=53, displayName="53")]
    [InputControl(name="Button054", layout="Button", bit=54, displayName="54")]
    [InputControl(name="Button055", layout="Button", bit=55, displayName="55")]
    [InputControl(name="Button056", layout="Button", bit=56, displayName="56")]
    [InputControl(name="Button057", layout="Button", bit=57, displayName="57")]
    [InputControl(name="Button058", layout="Button", bit=58, displayName="58")]
    [InputControl(name="Button059", layout="Button", bit=59, displayName="59")]
    [InputControl(name="Button060", layout="Button", bit=60, displayName="60")]
    [InputControl(name="Button061", layout="Button", bit=61, displayName="61")]
    [InputControl(name="Button062", layout="Button", bit=62, displayName="62")]
    [InputControl(name="Button063", layout="Button", bit=63, displayName="63")]
    public ulong buttonsA; // Buttons seperated into banks of 64-Bits to fit into Unsigned 64-bit integer
    [InputControl(name="Button064", layout="Button", bit=0, displayName="64")]
    [InputControl(name="Button065", layout="Button", bit=1, displayName="65")]
    [InputControl(name="Button066", layout="Button", bit=2, displayName="66")]
    [InputControl(name="Button067", layout="Button", bit=3, displayName="67")]
    [InputControl(name="Button068", layout="Button", bit=4, displayName="68")]
    [InputControl(name="Button069", layout="Button", bit=5, displayName="69")]
    [InputControl(name="Button070", layout="Button", bit=6, displayName="70")]
    [InputControl(name="Button071", layout="Button", bit=7, displayName="71")]
    [InputControl(name="Button072", layout="Button", bit=8, displayName="72")]
    [InputControl(name="Button073", layout="Button", bit=9, displayName="73")]
    [InputControl(name="Button074", layout="Button", bit=10, displayName="74")]
    [InputControl(name="Button075", layout="Button", bit=11, displayName="75")]
    [InputControl(name="Button076", layout="Button", bit=12, displayName="76")]
    [InputControl(name="Button077", layout="Button", bit=13, displayName="77")]
    [InputControl(name="Button078", layout="Button", bit=14, displayName="78")]
    [InputControl(name="Button079", layout="Button", bit=15, displayName="79")]
    [InputControl(name="Button080", layout="Button", bit=16, displayName="80")]
    [InputControl(name="Button081", layout="Button", bit=17, displayName="81")]
    [InputControl(name="Button082", layout="Button", bit=18, displayName="82")]
    [InputControl(name="Button083", layout="Button", bit=19, displayName="83")]
    [InputControl(name="Button084", layout="Button", bit=20, displayName="84")]
    [InputControl(name="Button085", layout="Button", bit=21, displayName="85")]
    [InputControl(name="Button086", layout="Button", bit=22, displayName="86")]
    [InputControl(name="Button087", layout="Button", bit=23, displayName="87")]
    [InputControl(name="Button088", layout="Button", bit=24, displayName="88")]
    [InputControl(name="Button089", layout="Button", bit=25, displayName="89")]
    [InputControl(name="Button090", layout="Button", bit=26, displayName="90")]
    [InputControl(name="Button091", layout="Button", bit=27, displayName="91")]
    [InputControl(name="Button092", layout="Button", bit=28, displayName="92")]
    [InputControl(name="Button093", layout="Button", bit=29, displayName="93")]
    [InputControl(name="Button094", layout="Button", bit=30, displayName="94")]
    [InputControl(name="Button095", layout="Button", bit=31, displayName="95")]
    [InputControl(name="Button096", layout="Button", bit=32, displayName="96")]
    [InputControl(name="Button097", layout="Button", bit=33, displayName="97")]
    [InputControl(name="Button098", layout="Button", bit=34, displayName="98")]
    [InputControl(name="Button099", layout="Button", bit=35, displayName="99")]
    [InputControl(name="Button100", layout="Button", bit=36, displayName="100")]
    [InputControl(name="Button101", layout="Button", bit=37, displayName="101")]
    [InputControl(name="Button102", layout="Button", bit=38, displayName="102")]
    [InputControl(name="Button103", layout="Button", bit=39, displayName="103")]
    [InputControl(name="Button104", layout="Button", bit=40, displayName="104")]
    [InputControl(name="Button105", layout="Button", bit=41, displayName="105")]
    [InputControl(name="Button106", layout="Button", bit=42, displayName="106")]
    [InputControl(name="Button107", layout="Button", bit=43, displayName="107")]
    [InputControl(name="Button108", layout="Button", bit=44, displayName="108")]
    [InputControl(name="Button109", layout="Button", bit=45, displayName="109")]
    [InputControl(name="Button110", layout="Button", bit=46, displayName="110")]
    [InputControl(name="Button111", layout="Button", bit=47, displayName="111")]
    [InputControl(name="Button112", layout="Button", bit=48, displayName="112")]
    [InputControl(name="Button113", layout="Button", bit=49, displayName="113")]
    [InputControl(name="Button114", layout="Button", bit=50, displayName="114")]
    [InputControl(name="Button115", layout="Button", bit=51, displayName="115")]
    [InputControl(name="Button116", layout="Button", bit=52, displayName="116")]
    [InputControl(name="Button117", layout="Button", bit=53, displayName="117")]
    [InputControl(name="Button118", layout="Button", bit=54, displayName="118")]
    [InputControl(name="Button119", layout="Button", bit=55, displayName="119")]
    [InputControl(name="Button120", layout="Button", bit=56, displayName="120")]
    [InputControl(name="Button121", layout="Button", bit=57, displayName="121")]
    [InputControl(name="Button122", layout="Button", bit=58, displayName="122")]
    [InputControl(name="Button123", layout="Button", bit=59, displayName="123")]
    [InputControl(name="Button124", layout="Button", bit=60, displayName="124")]
    [InputControl(name="Button125", layout="Button", bit=61, displayName="125")]
    [InputControl(name="Button126", layout="Button", bit=62, displayName="126")]
    [InputControl(name="Button127", layout="Button", bit=63, displayName="127")]
    public ulong buttonsB; // Buttons seperated into banks of 64-Bits to fit into Unsigned 64-bit integer

    [InputControl(name = "X", layout = "Axis", displayName = "X")]
    public ushort lX; // X-axis
    [InputControl(name = "Y", layout = "Axis", displayName = "Y")]
    public ushort lY; // Y-axis
    [InputControl(name = "Z", layout = "Axis", displayName = "Z")]
    public ushort lZ; // Z-axis
    // rglSlider Broken out
    // // LONG rglSlider[2];
    [InputControl(name = "U", layout = "Axis", displayName = "U")]
    public ushort lU; // U-axis
    [InputControl(name = "V", layout = "Axis", displayName = "V")]
    public ushort lV; // V-axis

    [InputControl(name = "RX", layout = "Axis", displayName = "X Rotation")]
    public ushort lRx; // X-axis rotation
    [InputControl(name = "RY", layout = "Axis", displayName = "Y Rotation")]
    public ushort lRy; // Y-axis rotation
    [InputControl(name = "RZ", layout = "Axis", displayName = "Z Rotation")]
    public ushort lRz; // Z-axis rotation

    [InputControl(name = "VX", layout = "Axis", displayName = "X Velocity")]
    public ushort lVX; // X-axis velocity
    [InputControl(name = "VY", layout = "Axis", displayName = "Y Velocity")]
    public ushort lVY; // Y-axis velocity
    [InputControl(name = "VZ", layout = "Axis", displayName = "Z Velocity")]
    public ushort lVZ; // Z-axis velocity
    // rglVSlider Broken out
    // // LONG rglVSlider[2]; // Extra axis velocities
    [InputControl(name = "VU", layout = "Axis", displayName = "U Velocity")]
    public ushort lVU; // U-axis velocity
    [InputControl(name = "VV", layout = "Axis", displayName = "V Velocity")]
    public ushort lVV; // V-axis velocity

    [InputControl(name = "VRX", layout = "Axis", displayName = "X Angular Velocity")]
    public ushort lVRx; // X-axis angular velocity
    [InputControl(name = "VRY", layout = "Axis", displayName = "Y Angular Velocity")]
    public ushort lVRy; // Y-axis angular velocity
    [InputControl(name = "VRZ", layout = "Axis", displayName = "Z Angular Velocity")]
    public ushort lVRz; // Z-axis angular velocity

    [InputControl(name = "AX", layout = "Axis", displayName = "X Acceleration")]
    public ushort lAX; // X-axis acceleration
    [InputControl(name = "AY", layout = "Axis", displayName = "Y Acceleration")]
    public ushort lAY; // Y-axis acceleration
    [InputControl(name = "AZ", layout = "Axis", displayName = "Z Acceleration")]
    public ushort lAZ; // Z-axis acceleration
    // rglASlider Broken out
    // // LONG rglASlider[2]; // Extra axis accelerations
    [InputControl(name = "AU", layout = "Axis", displayName = "U Acceleration")]
    public ushort lAU; // U-axis acceleration
    [InputControl(name = "AV", layout = "Axis", displayName = "V Acceleration")]
    public ushort lAV; // V-axis acceleration

    [InputControl(name = "ARX", layout = "Axis", displayName = "X Angular Acceleration")]
    public ushort lARx; // X-axis angular acceleration
    [InputControl(name = "ARY", layout = "Axis", displayName = "Y Angular Acceleration")]
    public ushort lARy; // Y-axis angular acceleration
    [InputControl(name = "ARZ", layout = "Axis", displayName = "Z Angular Acceleration")]
    public ushort lARz; // Z-axis angular acceleration

    [InputControl(name = "AFX", layout = "Axis", displayName = "X Force")]
    public ushort lFX; // X-axis force
    [InputControl(name = "AFY", layout = "Axis", displayName = "Y Force")]
    public ushort lFY; // Y-axis force
    [InputControl(name = "AFZ", layout = "Axis", displayName = "Z Force")]
    public ushort lFZ; // Z-axis force
    // rglFSlider Broken out
    // // LONG rglFSlider[2]; // Extra axis forces
    [InputControl(name = "AFU", layout = "Axis", displayName = "U Force")]
    public ushort lFU; // U-axis force
    [InputControl(name = "AFV", layout = "Axis", displayName = "V Force")]
    public ushort lFV; // V-axis force


    [InputControl(name = "FRX", layout = "Axis", displayName = "X Torque")]
    public ushort lFRx; // X-axis torque
    [InputControl(name = "FRY", layout = "Axis", displayName = "Y Torque")]
    public ushort lFRy; // Y-axis torque
    [InputControl(name = "FRZ", layout = "Axis", displayName = "Z Torque")]
    public ushort lFRz; // Z-axis torque

    // rgdwPOV Broken out
    // DWORD rgdwPOV[4]; // 4 PoV Hats
    [InputControl(name = "dpad0", layout = "Dpad", bit = 0, sizeInBits = 4, displayName="Dpad0")]
    [InputControl(name = "dpad0/up",    bit = 0, displayName="Up")]
    [InputControl(name = "dpad0/down",  bit = 1, displayName="Down")]
    [InputControl(name = "dpad0/left",  bit = 2, displayName="Left")]
    [InputControl(name = "dpad0/right", bit = 3, displayName="Right")]
    [InputControl(name = "dpad1", layout = "Dpad", bit = 4, sizeInBits = 4, displayName="Dpad1")]
    [InputControl(name = "dpad1/up",    bit = 4, displayName="Up")]
    [InputControl(name = "dpad1/down",  bit = 5, displayName="Down")]
    [InputControl(name = "dpad1/left",  bit = 6, displayName="Left")]
    [InputControl(name = "dpad1/right", bit = 7, displayName="Right")]
    [InputControl(name = "dpad2", layout = "Dpad", bit = 0, sizeInBits = 4, displayName="Dpad2")]
    [InputControl(name = "dpad2/up",    bit = 8, displayName="Up")]
    [InputControl(name = "dpad2/down",  bit = 9, displayName="Down")]
    [InputControl(name = "dpad2/left",  bit = 10, displayName="Left")]
    [InputControl(name = "dpad2/right", bit = 11, displayName="Right")]
    [InputControl(name = "dpad3", layout = "Dpad", bit = 4, sizeInBits = 4, displayName="Dpad3")]
    [InputControl(name = "dpad3/up",    bit = 12, displayName="Up")]
    [InputControl(name = "dpad3/down",  bit = 13, displayName="Down")]
    [InputControl(name = "dpad3/left",  bit = 14, displayName="Left")]
    [InputControl(name = "dpad3/right", bit = 15, displayName="Right")]
    public short rgdwPOV; // Store each DPAD in chunks of 4 bits inside 16-bit short     
    
}

#if UNITY_EDITOR
[InitializeOnLoad]
#endif
[InputControlLayout(stateType = typeof(DIJOYSTATE2State))]
public class DirectInputDevice : InputDevice, IInputUpdateCallbackReceiver{

    #if UNITY_EDITOR
    static DirectInputDevice(){
        Initialize();
    }
    #endif

    [RuntimeInitializeOnLoadMethod]
    private static void Initialize(){
        DirectInputFFB.FFBManager.Initialize(); // Initialize FFBManager incase it's not already
        DirectInputFFB.FFBManager.OnDeviceStateChange += OnDeviceStateChange; // Register listner
        InputSystem.RegisterLayout<DirectInputDevice>(
            matches: new InputDeviceMatcher()
                .WithInterface("DirectX DirectInput"));
    }

    protected override void FinishSetup(){
        base.FinishSetup();
    }

    public static DirectInputDevice current { get; private set; }
    public override void MakeCurrent(){
        base.MakeCurrent();
        current = this;
    }

    protected override void OnRemoved(){
        base.OnRemoved();
        if (current == this)
            current = null;
    }


    #if UNITY_EDITOR
    [MenuItem("DirectInputFFB/Create Virtual Input Device")]
    private static void CreateDevice(){
        InputSystem.AddDevice(new InputDeviceDescription{
            interfaceName = "DirectX DirectInput",
            product = "DIJOYSTATE2 Device"
        });
    }

    [MenuItem("DirectInputFFB/Destroy Virtual Input Device")]
    private static void RemoveDevice(){
        var DirectInputDevice = InputSystem.devices.FirstOrDefault(x => x is DirectInputDevice);
        if (DirectInputDevice != null)
            InputSystem.RemoveDevice(DirectInputDevice);
    }

    #endif

    public void OnUpdate(){
        DirectInputFFB.FFBManager.PollDevice(); // Poll the DirectInput Device
    }

    public static void OnDeviceStateChange(object sender, EventArgs args){
        DIJOYSTATE2State state = DirectInputFFB.Utilities.UnFlatJoyState2(DirectInputFFB.FFBManager.state);
        // Check if DirectInputDevice is enabled
        if( InputSystem.devices.FirstOrDefault(x => x is DirectInputDevice) != null){
            InputSystem.QueueStateEvent( DirectInputDevice.current , state); // Only bubble Input System Event if input has changed
        }
    }
}



/// <summary>
/// Unity Input System Processor to center Axis values, like steering wheels
/// </summary>
#if UNITY_EDITOR
[InitializeOnLoad]
#endif
public class CenteringProcessor : InputProcessor<float>{
    #if UNITY_EDITOR
    static CenteringProcessor(){ Initialize(); }
    #endif

    [RuntimeInitializeOnLoadMethod]
    static void Initialize(){ InputSystem.RegisterProcessor<CenteringProcessor>(); }

    public override float Process(float value, InputControl control){
        return (value*2)-1;
    }
}


/// <summary>
/// (Value*-1)+1    Smart Invert, remaps values from 1-0 to 0-1
/// </summary>
#if UNITY_EDITOR
[InitializeOnLoad]
#endif
public class SmartInvertProcessor : InputProcessor<float>{
    #if UNITY_EDITOR
    static SmartInvertProcessor(){ Initialize(); }
    #endif

    [RuntimeInitializeOnLoadMethod]
    static void Initialize(){ InputSystem.RegisterProcessor<SmartInvertProcessor>(); }

    public override float Process(float value, InputControl control){
        return (value*-1)+1;
    }
}