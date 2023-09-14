# Magic Leap MRTK3 Package

This package adds support for Magic Leap devices when using the
Mixed Reality Toolkit 3 (MRTK3).

## Current Status

| Feature | Status |
|--|--|
| Controller | Pre-Release |
| Hand Tracking | Pre-Release |
| Eye Tracking | Pre-Release |

### Important Note

This package is an early access version. This means features are still in active development and subject to changes or even having their implementation completely removed and done in a different way.

## Prerequisites

- Magic Leap SDK v1.2.0 (or later)
- Magic Leap Unity SDK v1.8.0 (or later)

## Getting Started

Before importing the Magic Leap MRTK3 Early Access Package, developers will need to configure their project for MRTK3. This section provides general guidance on downloading and installing the MRTK3 packages using the Mixed Reality Feature tool (Windows Only) or using the MRTK3 Dev Template Project.

### Using the MRTK Dev Template Project

#### Ready-made ML2 port of the MRTK Dev Template Project

There is a ready-made ML2 port of the MRTK Dev Template Project provided within the `mrtk3_MagicLeap2` branch in the [Magic Leap fork of the MRTK Github repository](https://github.com/magicleap/MixedRealityToolkit-Unity/tree/mrtk3_MagicLeap2).  This is the quickest and easiest way to get an MRTK3 app up and running on the Magic Leap 2.  To use the ported Dev Template project, clone the forked MRTK GitHub repo and check out the `mrtk3_MagicLeap2` branch. [Official Microsoft Guide Here] (https://learn.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/mrtk3-overview/getting-started/setting-up/setup-template)

If you work with Git using the command line, you can clone the repo while specifying the mrtk3_MagicLeap2 branch: `git clone --branch mrtk3_MagicLeap2 https://github.com/magicleap/MixedRealityToolkit-Unity`

#### Original MRTK Dev Template Project

The original MRTK Dev Template project is available by downloading the MRTK project from the [MRTK Github repository](https://github.com/MixedRealityToolkit/MixedRealityToolkit-Unity). To use the original Template project, clone MRTK from the GitHub repo and open the `MRTKDevTemplate` project under `UnityProjects` in Unity. [Official Microsoft Guide Here] (https://learn.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/mrtk3-overview/getting-started/setting-up/setup-template)

Clone the repo on the command line: `git clone  https://github.com/MixedRealityToolkit/MixedRealityToolkit-Unity.git`

Once the Github project is downloaded, follow the steps below to update the project settings to be compatible with Magic Leap 2.

##### Original Dev Template Project Setup

1. Using the Unity Hub, open the `MRTKDevTemplate` project using Unity version 2022.2.x
   1. On the **Opening Project in Non-Matching Editor Installation** popup select **Continue**.
   2. On the **Script Updating Consent** popup select **Yes, for these and other files that might be found later**.
   3. On the **Enter Safe Mode?** popup select **Ignore**.
2. Open the package manager (Window > Package Manager) and remove the following packages
   1. **Project Auditor**
   2. The following OpenXR packages do not need to be removed any longer, but
      are not currently used on the ML2 platform, so optional to keep.
      1. **Mixed Reality OpenXR Plugin**
      2. **Google ARCore XR Plugin**
      3. **OpenXR Plugin**
3. Clear any errors that appear as a result of a missing dependency from a prefab of XR provider.
   1. If Errors are still present, close the project and delete the project's Library folder and re-open unity to reimport the existing packages.
4. Download and Install the [Magic Leap Setup Tool](https://assetstore.unity.com/packages/tools/integration/magic-leap-setup-tool-194780) from the Unity Asset store.
5. Once installed, use the Project Setup window to configure your project settings. Complete all the steps in the project setup tool.

### Creating a new MRTK3 Project

This section assumes you have already configured your Unity Project for ML2. (https://developer.magicleap.cloud/learn/docs/guides/unity/getting-started/configure-unity-settings)

Download the MRTK3 dependencies using one of two methods: Using the [MixedReality Feature Tool](https://learn.microsoft.com/en-us/windows/mixed-reality/develop/unity/welcome-to-mr-feature-tool) (Windows Only), Expanded on below, or by manually downloading and importing the packages from [Github](https://github.com/MixedRealityToolkit/MixedRealityToolkit-Unity). MRTK Input and MRTK UX Components packages are required for using with Magic Leap, please review [MRTK Package Dependencies](https://learn.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/mrtk3-overview/packages/packages-overview) to know which dependencies are required for those two packages.

#### Using the Mixed Reality Feature Tool

This section provides instructions on installing the MRTK3 dependencies into an existing project using the [Mixed Reality Feature tool](https://learn.microsoft.com/en-us/windows/mixed-reality/develop/unity/welcome-to-mr-feature-tool). Note this tool is only available for Windows.

1. Open the Mixed Reality Feature tool
2. Target your Unity project.
3. At a minimum install the following packages, as they are required:

- MRTK3 / MRTK Input
- MRTK3 / MRTK UX Components

*Note: If you do not see MRTK3, you may need to select the **Show preview releases** option located at the bottom of the window.*

4. After choosing the the packages to install select **Get Features**. This will display the package dependencies.
5. Finally, press **Import** then **Approve**
6. Clear any errors that appear as a result of a missing dependency from a prefab of XR provider.

To learn more see Microsoft's [Starting from a new project](https://learn.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/mrtk3-overview/getting-started/setting-up/setup-new-project) guide.

## Importing the MRTK3 Magic Leap Package

Once the project is configured for ML2 and has the required MRTK3 packages, import the provided MRTK3 Magic Leap package into the project.

1. Open the Package Manager (**Window** > **Package Manager**) and import the `com.magicleap.mrtk3.tgz` package. Select the **＋** icon then select **Add package from tarball...**

2. Open MRTK3's Project settings (**Edit** > **Project Settings** > **MRTK3**). Then set the **Profile** to **MRTKProfile - MagicLeap**.

3. Configure the MRTK XR Rig to be compatible with Magic Leap 2 input (3 options).
   1. Use the Runtime MRTK XR Rig Configuration option in (**Edit** > **Project Settings** > **MRTK3** > **Magic Leap Settings**).
      Enabling this feature will allow the default MRTK XR Rig to work with ML2 input without needing to modify the scene.
   2. Remove and Replace the default MRTK XR Rig from the Scene with the `MRTK XR Rig - MagicLeap` prefab variant located in `Packages/com.magicleap.mrtk3/Runtime/MagicLeap/Prefabs/MRTK_Variants/`
   3. Manually configure the default MRTK XR Rig.  *See the [Configure Existing Rig Manually](#configuring-an-existing-xr-rig-manually) section below on information on how to edit the default MRTK rig instead of replacing it.*

## Magic Leap Permissions

The required Magic Leap permissions for your application must be added to the application's `AndroidManifest.xml` file.  At a minimum for MRTK3 to use hand tracking, the 
HAND_TRACKING permission should be added to the manifest.  This can be done in (**Edit** > **Project Settings** > **Magic Leap** > **Permissions**),
if MLSDK is setup, or by adding it to the manifest file manually.  Examples:

      <uses-permission android:name="com.magicleap.permission.HAND_TRACKING" />
      <uses-permission android:name="com.magicleap.permission.EYE_TRACKING" />

This package offers Runtime Permission Configuration in settings to auto request and/or start certain permission easily without needing to add prefabs or code to your scenes to do so.  Permission can be setup to be auto requested/started in (**Edit** > **Project Settings** > **MRTK3** > **Magic Leap Settings**).

For best eye tracking results, after having setup the EYE_TRACKING permission for your application, be sure to run the Custom Fit application and go through eye calibration.

Select any other permissions as needed for your application.

### Configuring an existing XR Rig manually
*(only an option, not recommended)*

This section describes how to configure MRTKs existing XR rig so instead of replacing it with the pre-configured "MRTK XR Rig - MagicLeap" variant.

1. Select the **MRTK XR Rig** in the scene. Add the **MagicLeapInputs** and **MagicLeapAuxiliaryInputs** input action assets to the **Input Action Manager**
2. Expand object so that the MRTK RightHand and MRTK LeftHand Controllers are visible (`MRTK XR Rig/ Camera Offset/`).
3. Add the  `Packages/com.magicleap.mrtk3/Runtime/MagicLeap/Prefabs/MRTK_Variants/MRTK MagicLeap Controller` to add support for MagicLeap controller input.
4. Select each of the Hand Controller objects and update the following components.
   1. Update the **Articulated Hand Controller** to use equivalent `MagicLeapAuxiliaryInputs` inputs instead of the generic MRTK bindings. For example, the left hand bindings would be the following in order:
      1. `Aux LeftHand/DevicePosition`
      2. `Aux LeftHand/DeviceRotation`
      3. `Aux LeftHand/TrackingState`
      4. `Aux LeftHand/Select`
      5. `Aux LeftHand/Select Value`
   2. Remove actions that do not have matching values. For the left hand these values would be:
       1. `MRTK LeftHand/Activate`
       2. `MRTK LeftHand/UI Press`
       3. `MRTK LeftHand/Rotate Anchor`
       4. `MRTK LeftHand/Translate Anchor`
5. Replace the input binding on each of the hand's child objects so they target the `MagicLeapAuxiliaryInputs` input actions.
   1. Select the **IndexTip PokeInteractor** then expand the Poke Pose Source/Pose Source List.
    Replace Element 1, to use Tracking State and PointerPosition/Rotation actions.
   2. Select the **Far Ray** then select the Aim Pose Source Pose / Source List. Replace Element 0, to use Tracking State and PointerPosition/PointerRotation actions.
   3. Select the **Far Ray** hen select the Device Pose Source / Pose Source List. Replace Element 0 to use Tracking State and DevicePosition/DeviceRotation actions.
   4. Select **GrabInteractor** then select the Pinch Pose Source / Pose Source List. Replace Element 1 Tracking State and PointerPosition/PointerRotation actions.
   5. Select the **GazePinchInteractor** and Replace the **Device Pose Source**   (DevicePosition/Rotation), and **Aim Pose Source** (PointerPosition/Rotation).
6. Finally, select the **Main Camera**, then add the **MagicLeap Camera** component. *(Optional)*

## FAQ

Why do I need to replace or configure the rig?
Replacing or configuring the rig is a temporary requirement to make sure the Magic Leap input bindings target the standard MRTK inputs.
