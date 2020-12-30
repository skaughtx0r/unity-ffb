# Unity Force Feedback

This is a native plugin for Unity3D that interfaces with DirectInput to
enable sending Force Feedback Effects to devices.

The primary goal of this plugin is to get Force Feedback effects working
with various steering wheels (Logitech, Fanatec, etc).

Current limitations:

Only Constant Force is working, it should work with any number of Axes (up to 6).
Can currently only create 1 Effect of each type per device.
I think it's possible to create an effect per axis, however it currently
assigns all the device's axes to the same effect.

For the constant force, both the magnitude and array of directions (for each
of the device's axes) can be passed from C#.

I started adding code to enable spring condition effect, but it's not working.

Currently only supports Windows 64bit.
