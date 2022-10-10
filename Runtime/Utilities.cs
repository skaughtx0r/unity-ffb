using System;
using System.Runtime.InteropServices;
using System.Linq;

namespace DirectInputFFB {

    public static class Utilities{
        public static FlatJoyState2 FlattenDIJOYSTATE2(DIJOYSTATE2 DeviceState){
            var state = new FlatJoyState2(); // Hold the flattend state

            // ButtonA
            for (int i = 0; i < 64; i++){ // In banks of 64, shift in the sate of each button BankA 0-63
                if(DeviceState.rgbButtons[i] == 128) // 128 = Button pressed
                    state.buttonsA |= (ulong)(1 << i); // Shift in a 1 to the button at index i
            }
            // ButtonB
            for (int i = 64; i < 128; i++){ // 2nd bank of buttons from 64-128
                if(DeviceState.rgbButtons[i] == 128) // 128 = Button pressed
                    state.buttonsB |= (ulong)(1 << i); // Shift in a 1 to the button at index i
            }

            state.lX = (ushort)DeviceState.lX; // X-axis
            state.lY = (ushort)DeviceState.lY; // Y-axis
            state.lZ = (ushort)DeviceState.lZ; // Z-axis
            // rglSlider
            state.lU = (ushort)DeviceState.rglSlider.First(); // U-axis
            state.lV = (ushort)DeviceState.rglSlider.Last(); // V-axis

            state.lRx = (ushort)DeviceState.lRx; // X-axis rotation
            state.lRy = (ushort)DeviceState.lRy; // Y-axis rotation
            state.lRz = (ushort)DeviceState.lRz; // Z-axis rotation

            state.lVX = (ushort)DeviceState.lVX; // X-axis velocity
            state.lVY = (ushort)DeviceState.lVY; // Y-axis velocity
            state.lVZ = (ushort)DeviceState.lVZ; // Z-axis velocity
            // rglVSlider
            state.lVU = (ushort)DeviceState.rglVSlider.First(); // U-axis velocity
            state.lVV = (ushort)DeviceState.rglVSlider.Last(); // V-axis velocity

            state.lVRx = (ushort)DeviceState.lVRx; // X-axis angular velocity
            state.lVRy = (ushort)DeviceState.lVRy; // Y-axis angular velocity
            state.lVRz = (ushort)DeviceState.lVRz; // Z-axis angular velocity

            state.lAX = (ushort)DeviceState.lAX; // X-axis acceleration
            state.lAY = (ushort)DeviceState.lAY; // Y-axis acceleration
            state.lAZ = (ushort)DeviceState.lAZ; // Z-axis acceleration
            // rglASlider
            state.lAU = (ushort)DeviceState.rglASlider.First(); // U-axis acceleration
            state.lAV = (ushort)DeviceState.rglASlider.Last(); // V-axis acceleration

            state.lARx = (ushort)DeviceState.lARx; // X-axis angular acceleration
            state.lARy = (ushort)DeviceState.lARy; // Y-axis angular acceleration
            state.lARz = (ushort)DeviceState.lARz; // Z-axis angular acceleration

            state.lFX = (ushort)DeviceState.lFX; // X-axis force
            state.lFY = (ushort)DeviceState.lFY; // Y-axis force
            state.lFZ = (ushort)DeviceState.lFZ; // Z-axis force
            // rglFSlider
            state.lFU = (ushort)DeviceState.rglFSlider.First(); // U-axis force
            state.lFV = (ushort)DeviceState.rglFSlider.Last(); // V-axis force
            
            state.lFRx = (ushort)DeviceState.lFRx; // X-axis torque
            state.lFRy = (ushort)DeviceState.lFRy; // Y-axis torque
            state.lFRz = (ushort)DeviceState.lFRz; // Z-axis torque

            for (int i = 0; i < 4; i++){ // In banks of 4, shift in the sate of each DPAD 0-16 bits
                switch(DeviceState.rgdwPOV[i]){
                    case 0:     state.rgdwPOV |= (byte)(1 << ((i+1)*0));break; // dpad0/up, bit = 0     shift into value at stride (i+1) * DPADButton
                    case 18000: state.rgdwPOV |= (byte)(1 << ((i+1)*1));break; // dpad0/down, bit = 1
                    case 27000: state.rgdwPOV |= (byte)(1 << ((i+1)*2));break; // dpad0/left, bit = 2
                    case 9000:  state.rgdwPOV |= (byte)(1 << ((i+1)*3));break; // dpad0/right, bit = 3
                }
            }

            return state;
        }

        public static DIJOYSTATE2State UnFlatJoyState2(FlatJoyState2 state){
            DIJOYSTATE2State DIJ2S = new DIJOYSTATE2State();
            DIJ2S.buttonsA = state.buttonsA; // Buttons seperated into banks of 64-Bits to fit into Unsigned 64-bit integer
            DIJ2S.buttonsB = state.buttonsB; // Buttons seperated into banks of 64-Bits to fit into Unsigned 64-bit integer
            DIJ2S.lX = state.lX; // X-axis
            DIJ2S.lY = state.lY; // Y-axis
            DIJ2S.lZ = state.lZ; // Z-axis
            DIJ2S.lU = state.lU; // U-axis
            DIJ2S.lV = state.lV; // V-axis
            DIJ2S.lRx = state.lRx; // X-axis rotation
            DIJ2S.lRy = state.lRy; // Y-axis rotation
            DIJ2S.lRz = state.lRz; // Z-axis rotation
            DIJ2S.lVX = state.lVX; // X-axis velocity
            DIJ2S.lVY = state.lVY; // Y-axis velocity
            DIJ2S.lVZ = state.lVZ; // Z-axis velocity
            DIJ2S.lVU = state.lVU; // U-axis velocity
            DIJ2S.lVV = state.lVV; // V-axis velocity
            DIJ2S.lVRx = state.lVRx; // X-axis angular velocity
            DIJ2S.lVRy = state.lVRy; // Y-axis angular velocity
            DIJ2S.lVRz = state.lVRz; // Z-axis angular velocity
            DIJ2S.lAX = state.lAX; // X-axis acceleration
            DIJ2S.lAY = state.lAY; // Y-axis acceleration
            DIJ2S.lAZ = state.lAZ; // Z-axis acceleration
            DIJ2S.lAU = state.lAU; // U-axis acceleration
            DIJ2S.lAV = state.lAV; // V-axis acceleration
            DIJ2S.lARx = state.lARx; // X-axis angular acceleration
            DIJ2S.lARy = state.lARy; // Y-axis angular acceleration
            DIJ2S.lARz = state.lARz; // Z-axis angular acceleration
            DIJ2S.lFX = state.lFX; // X-axis force
            DIJ2S.lFY = state.lFY; // Y-axis force
            DIJ2S.lFZ = state.lFZ; // Z-axis force
            DIJ2S.lFU = state.lFU; // U-axis force
            DIJ2S.lFV = state.lFV; // V-axis force
            DIJ2S.lFRx = state.lFRx; // X-axis torque
            DIJ2S.lFRy = state.lFRy; // Y-axis torque
            DIJ2S.lFRz = state.lFRz; // Z-axis torque
            DIJ2S.rgdwPOV = state.rgdwPOV; // Store each DPAD in chunks of 4 bits inside 16-bit short     
            return DIJ2S;
        }  
        /// <summary>
        /// Rescale value X from ranges A-B to ranges X-Y, bool to clamp values
        /// </summary>
        public static double SuperLerp(double x, double in_min, double in_max, double out_min, double out_max, bool clamp = false){
            if (clamp) x = Math.Max(in_min, Math.Min(x, in_max)); // If input above our range, clamp it
            return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
        }
    }

    /// <summary>
    /// Helper class to print out user friendly system error codes.
    /// 
    /// Taken from: https://stackoverflow.com/a/21174331/9053848
    /// 
    /// </summary>
    public static class WinErrors {
        #region definitions
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr LocalFree(IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern int FormatMessage(FormatMessageFlags dwFlags, IntPtr lpSource, uint dwMessageId, uint dwLanguageId, ref IntPtr lpBuffer, uint nSize, IntPtr Arguments);

        [Flags]
        private enum FormatMessageFlags : uint {
            FORMAT_MESSAGE_ALLOCATE_BUFFER = 0x00000100,
            FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200,
            FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000,
            FORMAT_MESSAGE_ARGUMENT_ARRAY = 0x00002000,
            FORMAT_MESSAGE_FROM_HMODULE = 0x00000800,
            FORMAT_MESSAGE_FROM_STRING = 0x00000400,
        }
        #endregion

        /// <summary>
        /// Gets a user friendly string message for a system error code
        /// </summary>
        /// <param name="errorCode">System error code</param>
        /// <returns>Error string</returns>
        public static string GetSystemMessage(int errorCode) {
            try {
                IntPtr lpMsgBuf = IntPtr.Zero;

                int dwChars = FormatMessage(
                    FormatMessageFlags.FORMAT_MESSAGE_ALLOCATE_BUFFER | FormatMessageFlags.FORMAT_MESSAGE_FROM_SYSTEM | FormatMessageFlags.FORMAT_MESSAGE_IGNORE_INSERTS,
                    IntPtr.Zero,
                    (uint)errorCode,
                    0, // Default language
                    ref lpMsgBuf,
                    0,
                    IntPtr.Zero);
                if (dwChars == 0) {
                    // Handle the error.
                    int le = Marshal.GetLastWin32Error();
                    return "Unable to get error code string from System - Error " + le.ToString();
                }

                string sRet = Marshal.PtrToStringAnsi(lpMsgBuf);

                // Free the buffer.
                lpMsgBuf = LocalFree(lpMsgBuf);
                return sRet;
            } catch (Exception e) {
                return "Unable to get error code string from System -> " + e.ToString();
            }
        }
    }

}
