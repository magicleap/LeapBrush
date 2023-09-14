# Changelog

## 1.0.0-pre.5
### Features

- Switched package dependencies to the latest org.mixedrealitytoolkit packages from the new Mixed Reality Toolkit Org repo, (https://github.com/MixedRealityToolkit/MixedRealityToolkit-Unity), where future updates to MRTK3 will come.  To use this version of the com.magicleap.mrtk3 package, be sure to update your project's MRTK3 packages to latest.
- Switched the hand ray back to the stock MRTK hand ray.

### Bugfixes
- Fixed an issue with the hand mesh not scaling well, and causing undesirable finger pose matching, during certain hand poses on the ML 2 platform.

### Known Issues / Limitations

- Other MagicLeap 2 apis that overlap with MRTK3 are not yet
  configured or hooked up.

## 1.0.0-pre.4
### Features

- Added two samples:
  - Eye Tracking Example - showcasing eye tracking on the ML2 platform.  For best results, run the Custom Fit application to calibrate eye tracking.
  - Spatial Awareness Example - showcasing scene reconstruction/meshing of your environment with several visual options.
  - Be sure to use the **Runtime Permission Configuration** to request eye tracking and spatial mapping permissions for the samples to work.
- Added a General Settings area to the **Edit** > **Project Settings** > **MRTK3** > **Magic Leap Settings** project settings.
  - Provides an option to `Observe OS Setting for Enabling Hand Navigation` to enable/disable Hand interactions within MRTK3 based on the OS setting.  This setting defaults to off so hand interactions are available for MRTK3 by default.

### Bugfixes
- Fixed an issue with near interactions and the MagicLeap Controller when the controller was added dynamically to the rig at runtime (via Runtime Rig Config).
- Fixed the auxiliary devices causing an error when playing in Editor when the ML Application Simulator was not active.
- Fixed a compile issue with an API change in `HandsSubsystem` within the core MRTK package. 

### Known Issues / Limitations

- Other MagicLeap 2 apis that overlap with MRTK3 are not yet
  configured or hooked up.

## 1.0.0-pre.3
### Features

- Added the Runtime Permission Configuration to make permission management easier on the ML2 platform.
  - Provides an option to request or start certain permissions from settings, without needing to modify any scene.
  - Also provides for instantiating a prefab at runtime to receive permission (granted, denied) callbacks for custom handling.
  - Available in (**Edit** > **Project Settings** > **MRTK3** > **Magic Leap Settings**).
- Updated compatibility to Magic Leap Unity SDK 1.8 (or later).

### Bugfixes
- Fixed an issue with the Runtime Rig Configuration possibly adding XR Controllers that already exist in the rig, causing issues.
  The name of a controller to be added must now not match any pre-existing rig controller in order to be added.

### Known Issues / Limitations

- Other MagicLeap 2 apis that overlap with MRTK3 are not yet
  configured or hooked up.

## 1.0.0-pre.2
### Features

- Support for Eye Tracking (Gaze).
- Added the Runtime MRTK XR Rig Configuration to make the default rig compatible with ML 2 input at runtime.
  - Provides an option that no longer requires modification of the scene by having had to swap in the ML rig variant.
  - Available in (**Edit** > **Project Settings** > **MRTK3** > **Magic Leap Settings**).
- Updated compatibility to Magic Leap Unity SDK 1.6 (or later).

### Bugfixes
- Fixed the issue with the MagicLeapAuxiliaryHandDevice not cleaning up after exiting Play Mode in Editor.

### Known Issues / Limitations

- Other MagicLeap 2 apis that overlap with MRTK3 are not yet
  configured or hooked up.

## 1.0.0-pre.1
### Features

- Initial support for MRTK3 on MagicLeap 2
- Support for Hand Tracking and Controller

### Known Issues / Limitations

- Other MagicLeap 2 apis that overlap with MRTK3 are not yet
  configured or hooked up.
