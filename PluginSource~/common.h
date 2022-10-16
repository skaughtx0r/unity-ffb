#pragma once
extern "C"
{
   struct DeviceInfo {
      DWORD deviceType;
      WORD vendorId;
      WORD productId;
      LPSTR guidInstance;
      LPSTR guidProduct;
      LPSTR instanceName;
      LPSTR productName;
      bool hasFFB;
   };

   struct DeviceAxisInfo {
      DWORD offset;
      DWORD type;
      DWORD flags;
      DWORD ffMaxForce;
      DWORD ffForceResolution;
      DWORD collectionNumber;
      DWORD designatorIndex;
      DWORD usagePage;
      DWORD usage;
      DWORD dimension;
      DWORD exponent;
      DWORD reportId;
      LPSTR guidType;
      LPSTR name;
   };

   struct Effects {
      typedef enum {
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
      } Type;
   };

   struct FlatJoyState2 {
      uint64_t buttonsA; // Buttons seperated into banks of 64-Bits to fit into 64-bit integer
      uint64_t buttonsB; // Buttons seperated into banks of 64-Bits to fit into 64-bit integer
      uint16_t lX;       // X-axis
      uint16_t lY;       // Y-axis
      uint16_t lZ;       // Z-axis
      uint16_t lU;       // U-axis
      uint16_t lV;       // V-axis
      uint16_t lRx;      // X-axis rotation
      uint16_t lRy;      // Y-axis rotation
      uint16_t lRz;      // Z-axis rotation
      uint16_t lVX;      // X-axis velocity
      uint16_t lVY;      // Y-axis velocity
      uint16_t lVZ;      // Z-axis velocity
      uint16_t lVU;      // U-axis velocity
      uint16_t lVV;      // V-axis velocity
      uint16_t lVRx;     // X-axis angular velocity
      uint16_t lVRy;     // Y-axis angular velocity
      uint16_t lVRz;     // Z-axis angular velocity
      uint16_t lAX;      // X-axis acceleration
      uint16_t lAY;      // Y-axis acceleration
      uint16_t lAZ;      // Z-axis acceleration
      uint16_t lAU;      // U-axis acceleration
      uint16_t lAV;      // V-axis acceleration
      uint16_t lARx;     // X-axis angular acceleration
      uint16_t lARy;     // Y-axis angular acceleration
      uint16_t lARz;     // Z-axis angular acceleration
      uint16_t lFX;      // X-axis force
      uint16_t lFY;      // Y-axis force
      uint16_t lFZ;      // Z-axis force
      uint16_t lFU;      // U-axis force
      uint16_t lFV;      // V-axis force
      uint16_t lFRx;     // X-axis torque
      uint16_t lFRy;     // Y-axis torque
      uint16_t lFRz;     // Z-axis torque
      uint16_t rgdwPOV; // Store each DPAD in chunks of 4 bits inside 16-bit short     
   };
}