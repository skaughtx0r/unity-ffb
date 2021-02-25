# Unity Force Feedback

This is a native plugin for Unity3D that interfaces with DirectInput in order
to send Force Feedback Effects to devices.

The primary goal of this plugin is to get Force Feedback effects working
with various steering wheels (Logitech, Fanatec, etc) for my hydroplane
racing sim (http://www.hydrosimracing.com).

#### Current limitations

1. Currently only supports one FFB device at a time.
2. Only supports Constant Force and Spring Condition.
3. Should support devices with up to 6 axes. I've only tested devices that
   support 1 axis though.
4. Currently only supports 1 Effect of each type per device.

#### Compatible Devices

Has only been tested with Steering Wheels.

Tested with Fanatec Forza CSR, Fanatec CSL base, and should work with any other
Fanatec wheel base.

Logitech G29 and G920 are tested and working.

#### Environment

This plugin only works on Windows 64 bit.

Has only been tested with Unity 2018.4, but should work with newer versions.

#### UPM Support

This package can be installed via Unity Package Manager.

For Unity 2018.4, you'll need to manually add the git repo to your manifest.json

```json
{
    "dependencies": {
        "com.skaughtx0r.unityffb": "https://github.com/skaughtx0r/unity-ffb.git",
    }
}
```

For Unity 2019 and greater, you can add git packages via the Package Manager
GUI. Just click the `+` and "Add package from git url...", then paste the URL:
`https://github.com/skaughtx0r/unity-ffb.git`