# Leap Brush - Collaborative drawing

Leap Brush is Magic Leap's AR Cloud reference application that lets you draw in AR with other ML2 devices. From the paintbrush with its assortment of colors to the poly tool backed with the segmented dimmer and the ability to place 3D models, use your imagination to make your space your canvas. Also can't join in person? No problem, using the included spectator view will be able to remotely participate in the action watching your fellow participants in real time as they create their masterpieces.

## Spectator App
A spectator user observing a space being modified in realtime.

[![Spectator Application](docs/spectator_app_screenshot_small.png?raw=true "Spectator Application")](docs/spectator_app_screenshot.png)

## Magic Leap App
A Magic Leap 2 user watching another user draw in realtime.

![ML2 Application](docs/main_app_animation.gif)

## Basic Setup

* Set up an AR Cloud environment (Optional):
  * See the [AR Cloud project](https://github.com/magicleap/arcloud) and follow the setup instructions.
* Set up a Leap Brush server (Optional):
  * A single server can support many devices and is reusable across Spaces, locations, etc. See instructions below.
* Connect your ML2 device to wifi:
  * Required if using ARCloud or the Leap Brush server.
  * Navigate to `Settings > Wifi` on device, and connect to a wireless network where both ARCloud and Leap Brush Server are accessible.
* Connect your ML2 devices to your ARCloud environment if applicable:
  * NOTE: You will need to enter a license in `About -> license management`
  * Navigate to your ARCloud web page on your desktop computer. Go to Device Configuration to view the QR code.
  * On the ML2 device, go to Settings and open `Perception -> Spatial Understanding -> AR Cloud`. Scan the QR code.
* Follow the Leap Brush Application Setup instructions below.

### Network Architecture

[![Network Architecture](docs/network_architecture_small.png?raw=true "Network Architecture")](docs/network_architecture.png)

## Leap Brush Application Setup

* Prepare the desired Leap Brush artifacts (Download a release or build from source):
  * ML2 Client APK
    * `com.magicleap.leapbrush-*.apk`
      * Install with `adb install /path/to/leapbrush.apk`
  * Mac OS Spectator Application
    * `LeapBrush-Mac-*.zip`
      * Extract and run LeapBrush.app (Note: you may need to go to your Mac's `System Preferences > Security` to allow the app to open)
      * An easy workaround is to un-quarantine the download zip file before opening it:
        * `xattr -d com.apple.quarantine /path/to/LeapBrush-Mac-VERSION.app.zip`
  * Linux Spectator Application
    * `LeapBrush-Linux-*.zip`
      * Extract and run `LeapBrush.x86_64`
  * Windows Spectator Application
    * `LeapBrush-Windows-*.zip`
      * Extract and run `LeapBrush.exe`
  * Leap Brush Server
    * `leapbrush-server-mac-*.universal` (Mac)
    * `leapbrush-server-linux-*.x86_64` (Linux)
    * `leapbrush-server-windows-*.exe` (Windows)
    * Pass `--help` for usage instructions

## ML2 App Usage Instructions

* In order to take advantage of multi-user and spectator app capabilities, connect the ML2 app to a Leap Brush Server `host:port`:
  * Open the app on your device.
  * Select the "Choose Server" button in the connection screen.
  * Type in the `host:port`, e.g. `<ip-address>:8402`.
  * Note, alternatively you can push the host port to the device using adb:
    * `echo 'your_server_ip_address:8402' |tee /tmp/serverHostPort.txt`
    * `adb push /tmp/serverHostPort.txt /storage/emulated/0/Android/data/com.magicleap.leapbrush/files/serverHostPort.txt`
    * See the spectator instructions below for where to place a similar serverHostPort.txt file.
* Alternatively, for quick single-user drawing session, choose "Draw Solo" once the app launches.

* To add 3D models to the app for viewing, see [Adding 3D models to the app](#adding-custom-3d-models-to-the-app)

| Task | Instructions |
| ---- | ------------ |
| Paint with the Brush tool | Select the Brush tool, then hold the Controller's trigger or make a pinch gesture with your hands, to draw. |
| Draw with the Poly tool | Select the Poly tool, then click the Controller's trigger or use a pinch gesture with your hands, for each control point. To end a drawing, tap the menu button or click on the previous or first control points |
| Import a 3D model | First make sure the models are in the app data directory as described above. Then use the Import button and choose the model file |
| Open the Quick Settings menu | Turn the palm of your hand to face you |
| Move a 3D model | Select the laser pointer tool then point and grab the model by pulling the trigger or with a pinch gesture. You can also directly grab a 3D model with one or both hands using a grab gesture. |
| Rotate or Scale a 3D model | Using the laser pointer, tap on a 3D model to show the Bounds Control handles. Click and drag these handles to scale and rotate. |
| Delete a Brush Stroke or 3D Model | Select the Eraser tool and touch the object with it |
| Toggle Anchor, Wearable, Controller, or Origin Axis visibility | Tap the settings gear button |
| Change the Stroke or Fill colors | Select the Palette menu and use the two color pickers. The Fill options are supported for Poly brush. |
| Move brush or Eraser further or closer from you | While the Brush, Poly, or Eraser tool is selected, use up or down touchpad gestures. |

### The Brush and Poly Style Dialog

* The Brush and Poly Style Dialog lets you select a stroke color, but also a fill color which may be partially transparent or may include real darkness (Using [Segmented Dimming](https://developer-docs.magicleap.cloud/docs/guides/best-practices/dimming/dimmer-design-guidelines)).

[![Brush and Poly Style Dialog](docs/brush_and_poly_style_dialog_screenshot_small.png?raw=true "Brush and Poly Style Dialog")](docs/brush_and_poly_style_dialog_screenshot.png)

## Spectator App Instructions

### Configuration

* Leap Brush will look at the following directories to find various configuration options and GLB/GLTF models. Use the same configuration files as for ML2 (e.g. `serverHostPort.txt`)
  * Mac
    * `$HOME/Library/Application Support/Magic Leap/Leap Brush/`
  * Linux
    * `$HOME/.config/unity3d/Magic Leap/Leap Brush/`
  * Windows
    * `%HOMEPATH%\"AppData\LocalLow\Magic Leap\Leap Brush\"`

### Usage

| Task  | Instructions |       |
| ----- | ------------ | ----- |
| Rotate the camera | Hold the Right Mouse Button and Move Mouse | |
| Move the camera | W, A, S, D, Q, E | Strafe, move up/down, move forwards/back |
| Emulate the Left hand | Hold Left SHIFT + Move Mouse | See the MRTK3 Input Simulator [quick reference guide](https://learn.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/mrtk3-input/packages/input/input-simulation-quick-ref) for more details. |
| Emulate the Right hand | Hold SPACE + Move Mouse | See the MRTK3 Input Simulator [quick reference guide](https://learn.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/mrtk3-input/packages/input/input-simulation-quick-ref) for more details. |
| Toggle the menu | Tap M or Tab keys | |
| Pull the trigger, click, pinch hands, etc | Click or hold the Left Mouse Button | You can draw, grab, etc just like an ML2 device. Note, in order to use the brush tools or eraser, you'll also need to be using the left or right hand emulation (Left SHIFT and SPACE keys, see above). When using the hands, hold down the Left Mouse Button until a pinch is fully performed. |
| Join a user session | Tap the Settings gear button and then the Join User Session button. Select a user to join. The spectator will start in POV mode, following the joined user. Tap "M" to show the menu and exit follow mode. | The anchors for the selected user will be imported into the scene and your device will act like it is in the same room. |

## Adding custom 3D models to the app

Leap Brush includes a handful of sample 3D Models (available from the Import button), however you can also side-load them onto your device and for the spectator app. Once added, you'll see them show up in the Import list, and they can be added to the scene.

* Install GLB/GLTF files to the ML2 device and spectator apps:
  * Note: App must have previously been opened once in order to create the parent directories.
  
  * cd `<PATH_WHERE_YOU_EXTRACTED_FILES>`
    * Push to ML2 device:
      * `ls *.glb *.gltf|while read F; do echo "$F"; adb push "$F" /storage/emulated/0/Android/data/com.magicleap.leapbrush/files; done`
    * Push to Mac spectator app:
      * `ls *.glb *.gltf|while read F; do echo "$F"; cp "$F" "$HOME/Library/Application Support/Magic Leap/Leap Brush/"; done`
    * Push to Linux spectator app:
      * `ls *.glb *.gltf|while read F; do echo "$F"; cp "$F" "$HOME/.config/unity3d/Magic Leap/Leap Brush/"; done`
    * Push to Windows spectator app:
      * `FOR %i IN (*.glb *.gltf) DO copy %i %HOMEPATH%\"AppData\LocalLow\Magic Leap\Leap Brush\"`

## Development Setup

- `git submodule update --init --recursive`

### Run the unity client

1. Prepare a Unity environment for ML2 development.

    - https://developer-docs.magicleap.cloud/docs/guides/unity/getting-started/set-up-development-environment
    - The current Unity editor version in use is 2022.3.2f1

2. Open the directory `LeapBrush/` as an application in Unity.

3. Open Build Settings (`File > Build Settings`), and switch to the
   "Android" Platform.

    Note: Mac Mono and Linux Mono Platforms are also supported (with the amd64
    instruction set only).

4. Open the LeapBrush Scene in the Project explorer
   (`Assets/MagicLeap/LeapBrush/Scenes/LeapBrush.unity`)

5. Build the project to generate an android apk or app, or run from the editor.


### Package a build for release

- `./scripts/build.py`


### Regenerate protocol buffer apis as needed.

- `./scripts/generate_protos.py`
- Note: Run the associated api update script for the server project as well.


### Run the server

- See `server/README.md` for instructions.


## Copyright

Copyright (c) 2022-present Magic Leap, Inc. All Rights Reserved.
Use of this file is governed by the Developer Agreement, located
here: https://www.magicleap.com/software-license-agreement-ml2
