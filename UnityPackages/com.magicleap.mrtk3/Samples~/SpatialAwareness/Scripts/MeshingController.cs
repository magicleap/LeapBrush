// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) (2018-2022) Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

using UnityEngine;
using UnityEngine.XR.MagicLeap;
using MixedReality.Toolkit;
using Unity.XR.CoreUtils;
using UnityEngine.XR;
using UnityEngine.XR.Management;

namespace MagicLeap.MRTK.Samples.SpatialAwareness
{
    public class MeshingController : MonoBehaviour
    {
        public enum RenderMode
        {
            None,
            Wireframe,
            Colored,
            //PointCloud, NOTE: removing PointCloud atm - not requiring URP
            Occlusion
        }
        [SerializeField, Tooltip("The MeshingSubsystemComponent.")]
        private MeshingSubsystemComponent meshingSubsystemComponent = null;

        [SerializeField, Tooltip("The material to apply for occlusion.")]
        private Material occlusionMaterial = null;

        [SerializeField, Tooltip("The material to apply for wireframe rendering.")]
        private Material wireframeMaterial = null;

        [SerializeField, Tooltip("The material to apply for colored rendering.")]
        private Material coloredMaterial = null;

        //[SerializeField, Tooltip("The material to apply for point cloud rendering.")]
        //private Material pointCloudMaterial = null;

        [SerializeField, Tooltip("The Render Mode")]
        private RenderMode renderMode = RenderMode.Wireframe;

        [SerializeField, Space, Tooltip("Flag specifying if mesh extents are bounded.")]
        private bool bounded = false;

        [SerializeField, Space, Tooltip("Size of the bounds extents when bounded setting is enabled.")]
        private Vector3 boundedExtentsSize = new Vector3(2.0f, 2.0f, 2.0f);

        [SerializeField, Space, Tooltip("Size of the bounds extents when bounded setting is disabled.")]
        private Vector3 boundlessExtentsSize = new Vector3(10.0f, 10.0f, 10.0f);

        [SerializeField, Space, Tooltip("Mesh boundary will follow the user on a position change")]
        private bool follow = false;

        private XROrigin xrOrigin;
        private XRInputSubsystem inputSubsystem;

        /// <summary>
        /// Initialize
        /// </summary>
        void Awake()
        {
            // Validate all required game objects.
            if (meshingSubsystemComponent == null)
            {
                Debug.LogError("Error: MeshingVisualizer.meshingSubsystemComponent is not set, disabling script!");
                enabled = false;
                return;
            }
            if (occlusionMaterial == null)
            {
                Debug.LogError("Error: MeshingVisualizer.occlusionMaterial is not set, disabling script!");
                enabled = false;
                return;
            }
            if (wireframeMaterial == null)
            {
                Debug.LogError("Error: MeshingVisualizer.wireframeMaterial is not set, disabling script!");
                enabled = false;
                return;
            }
            if (coloredMaterial == null)
            {
                Debug.LogError("Error: MeshingVisualizer.coloredMaterial is not set, disabling script!");
                enabled = false;
                return;
            }

            xrOrigin = PlayspaceUtilities.XROrigin;
            inputSubsystem = XRGeneralSettings.Instance?.Manager?.activeLoader?.GetLoadedSubsystem<XRInputSubsystem>();
        }

        /// <summary>
        /// Register callbacks, and initialize renderer, position, bounds.
        /// </summary>
        void Start()
        {
            SetRenderer(renderMode, true);
            inputSubsystem.trackingOriginUpdated += OnTrackingOriginChanged;
            meshingSubsystemComponent.meshAdded += HandleOnMeshReady;
            meshingSubsystemComponent.meshUpdated += HandleOnMeshReady;
            meshingSubsystemComponent.gameObject.transform.position = xrOrigin.CameraFloorOffsetObject.transform.position;
            UpdateBounds();
        }

        /// <summary>
        /// Unregister callbacks.
        /// </summary>
        void OnDestroy()
        {
            meshingSubsystemComponent.meshAdded -= HandleOnMeshReady;
            meshingSubsystemComponent.meshUpdated -= HandleOnMeshReady;
            inputSubsystem.trackingOriginUpdated -= OnTrackingOriginChanged;
        }

        void Update()
        {
            if (follow)
            {
                // We want meshing to move with the headpose if a boundary was set, not just stay at origin
                meshingSubsystemComponent.gameObject.transform.position = xrOrigin.CameraFloorOffsetObject.transform.position;
            }
            if ((bounded && meshingSubsystemComponent.gameObject.transform.localScale != boundedExtentsSize) ||
                (!bounded && meshingSubsystemComponent.gameObject.transform.localScale != boundlessExtentsSize))
            {
                UpdateBounds();
            }

        }

        /// <summary>
        /// Set the renderer
        /// </summary>
        /// <param name="mode">The render mode that should be used on the material.</param>
        public void SetRenderer(RenderMode mode, bool init = false)
        {
            if (init || renderMode != mode)
            {
                // Set the render mode.
                renderMode = mode;

                // Clear existing meshes to process the new mesh type.
                switch (renderMode)
                {
                    case RenderMode.Wireframe:
                    case RenderMode.Colored:
                    case RenderMode.Occlusion:
                        {
                            meshingSubsystemComponent.requestedMeshType = MeshingSubsystemComponent.MeshType.Triangles;

                            break;
                        }
                        /*case RenderMode.PointCloud:
                            {
                                meshingSubsystemComponent.requestedMeshType = MeshingSubsystemComponent.MeshType.PointCloud;

                                break;
                            }*/
                }

                meshingSubsystemComponent.DestroyAllMeshes();
                meshingSubsystemComponent.RefreshAllMeshes();
            }
        }

        /// <summary>
        /// Updates the currently selected render material on the MeshRenderer.
        /// </summary>
        /// <param name="meshRenderer">The MeshRenderer that should be updated.</param>
        private void UpdateRenderer(MeshRenderer meshRenderer)
        {
            if (meshRenderer != null)
            {
                // Toggle the GameObject(s) and set the correct material based on the current RenderMode.
                if (renderMode == RenderMode.None)
                {
                    meshRenderer.enabled = false;
                }
                /*else if (renderMode == RenderMode.PointCloud)
                {
                    meshRenderer.enabled = true;
                    meshRenderer.material = pointCloudMaterial;
                }*/
                else if (renderMode == RenderMode.Wireframe)
                {
                    meshRenderer.enabled = true;
                    meshRenderer.material = wireframeMaterial;
                }
                else if (renderMode == RenderMode.Colored)
                {
                    meshRenderer.enabled = true;
                    meshRenderer.material = coloredMaterial;
                }
                else if (renderMode == RenderMode.Occlusion)
                {
                    meshRenderer.enabled = true;
                    meshRenderer.material = occlusionMaterial;
                }
            }
        }

        private void UpdateBounds()
        {
            meshingSubsystemComponent.gameObject.transform.localScale = bounded ? boundedExtentsSize : boundlessExtentsSize;
        }

        /// <summary>
        /// Handle in charge of refreshing all meshes if a new session occurs
        /// </summary>
        /// <param name="inputSubsystem"> The inputSubsystem that invoked this event. </param>
        private void OnTrackingOriginChanged(XRInputSubsystem inputSubsystem)
        {
            meshingSubsystemComponent.DestroyAllMeshes();
            meshingSubsystemComponent.RefreshAllMeshes();
        }

#if UNITY_2019_3_OR_NEWER
        /// <summary>
        /// Handles the MeshReady event, which tracks and assigns the correct mesh renderer materials.
        /// </summary>
        /// <param name="meshId">Id of the mesh that got added / updated.</param>
        private void HandleOnMeshReady(MeshId meshId)
        {
            if (meshingSubsystemComponent.meshIdToGameObjectMap.ContainsKey(meshId))
            {
                UpdateRenderer(meshingSubsystemComponent.meshIdToGameObjectMap[meshId].GetComponent<MeshRenderer>());
            }
        }
#else
        /// <summary>
        /// Handles the MeshReady event, which tracks and assigns the correct mesh renderer materials.
        /// </summary>
        /// <param name="meshId">Id of the mesh that got added / updated.</param>
        private void HandleOnMeshReady(TrackableId meshId)
        {
            if (meshingSubsystemComponent.meshIdToGameObjectMap.ContainsKey(meshId))
            {
                UpdateRenderer(meshingSubsystemComponent.meshIdToGameObjectMap[meshId].GetComponent<MeshRenderer>());
            }
        }
#endif
    }
}