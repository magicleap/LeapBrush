# MRTK3 Spatial Awareness

MRTK 2.x had a custom spatial awareness system that required configuration to work on your devices platform. With MRTK3, "Spatial Awareness" functionality is now 100% covered by the built-in Unity ARFoundation APIs.
(https://github.com/microsoft/MixedRealityToolkit-Unity/issues/11400)

Scene reconstruction: ARMeshManager API
https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@5.1/api/UnityEngine.XR.ARFoundation.ARMeshManager.html

Plane Finding: ARPlaneManager Component
https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@5.1/manual/features/plane-detection.html




# Magic Leap 2 + MRTK3 Spatial Awareness

Magic Leap's Unity SDK currently requires a Meshing Subsystem Component to load and manage meshes for scene reconstruction. Because of this, the ARMeshManager is not necessary, and adds extra overhead when added to the scene. To use Meshing with your MRTK3 project, drag the Meshing prefab into your scene and ensure your permission is granted.

Permission: SPATIAL_MAPPING must be requested and granted.

NOTES:
As of 04/2023, you must check "Force Multipass" in the XR Plug-in Management settings, to use a mesh material that uses a Geometry shader.