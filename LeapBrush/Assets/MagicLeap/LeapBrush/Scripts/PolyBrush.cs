using System;
using System.Collections.Generic;
using MagicLeap.DesignToolkit.Audio;
using UnityEngine;

namespace MagicLeap.LeapBrush
{
    /// <summary>
    /// The polygon brush tool and brush stroke.
    /// </summary>
    /// <remarks>
    /// The polygon brush allows the user to precisely place a series of control points which are
    /// connected by brush strokes. The user can click on the previous or first control point
    /// to stop the drawing or create a loop. The drawing can optionally have a fill and/or
    /// segmented dimming fill value.
    ///
    /// <para>Similar to the ScribbleBrush, this algorithm creates a ribbon between the control
    /// points, however if a fill is selected, it also uses a simple triangle fan to attempt
    /// to draw a reasonable fill. This algorithm could be improved.
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshCollider))]
    [RequireComponent(typeof(GenericAudioHandler))]
    public class PolyBrush : BrushBase
    {
        /// <summary>
        /// Event fired when poses have been updated in this brush. The first argument is the
        /// start index in the list of poses where poses have been modified.
        /// </summary>
        public event Action<int> OnPosesUpdated;

        [SerializeField, Tooltip("Visualization for a snap point when the tool is close to one")]
        private PolyBrushSnapVisualization _snapVisualization;

        [SerializeField]
        private GameObject _fillGameObject;

        [SerializeField]
        private GameObject _fillDimmerGameObject;

        [SerializeField]
        private Material _opaqueFillMaterial;

        [SerializeField]
        private Material _transparentFillMaterial;

        [SerializeField]
        private SoundDefinition _drawPointSound;

        [SerializeField]
        private SoundDefinition _drawEndSound;

        private const float BrushEndCapLength = .001f;
        private const float BrushHalfWidth = .01f;
        private const float MinDistanceUpdatePose = .0005f;
        private const float SnapRadiusCameraDistanceMultiplier = .02f;

        private bool _initialized;
        private GenericAudioHandler _audioHandler;
        private bool _movedAwayFromPreviousSnap;
        private Color32 _fillColor;
        private float _fillDimmerAlpha;
        private bool _fillMaterialIsOpaque = true;
        private float _snapRadius;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private void Awake()
        {
            EnsureInitialized();
            _audioHandler = GetComponent<GenericAudioHandler>();
        }

        private void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            var meshFilter = GetComponent<MeshFilter>();

            Mesh mesh = meshFilter.mesh;
            if (mesh == null) {
                mesh = meshFilter.mesh = new Mesh() {
                    name = "Quad strip mesh"
                };
            };
            mesh.Clear();
            mesh.MarkDynamic();

            ApplyDrawingTipPoses();

            RebuildMesh(_poses);

            _initialized = true;
        }

        private void OnEnable()
        {
            RebuildMesh(_poses);
        }

        private void OnDisable()
        {
            if (_drawing)
            {
                _poses.RemoveAt(_poses.Count - 1);
                OnPosesUpdated?.Invoke(_poses.Count - 1);
                StopDrawing();
            }
        }

        private void Update()
        {
            if (_drawing)
            {
                Pose lastPose = _poses[^1];
                Pose nextPose = new Pose(_brushControllerTransform.position,
                    _brushControllerTransform.rotation);
                if ((nextPose.position - lastPose.position).sqrMagnitude <
                    MinDistanceUpdatePose * MinDistanceUpdatePose)
                {
                    // The tool is too close to the last pose added still.
                    return;
                }

                // Replace the last pose with the current tool location.
                _poses[^1] = nextPose;
                RebuildMesh(_poses);

                // Calculate a snap radius that increases based on the distance from the camera.
                _snapRadius = Vector3.Distance(Camera.main.transform.position, nextPose.position)
                              * SnapRadiusCameraDistanceMultiplier;
                _snapVisualization.TargetRadius = _snapRadius;

                if (_poses.Count > 1)
                {
                    Vector3 firstPosition = _poses[0].position;
                    Vector3 mostRecentPosition = _poses[^2].position;
                    Vector3 newPosition = _poses[^1].position;

                    if ((newPosition - mostRecentPosition).magnitude < _snapRadius)
                    {
                        if (_movedAwayFromPreviousSnap)
                        {
                            // The new pose is close to the most recently added pose -- show the
                            // snap visualization since the user could click here to finish
                            // the current drawing.
                            _snapVisualization.transform.position = mostRecentPosition;
                            _snapVisualization.gameObject.SetActive(true);
                        }
                    }
                    else if (_poses.Count > 2 &&
                             (newPosition - firstPosition).magnitude < _snapRadius)
                    {
                        if (_movedAwayFromPreviousSnap)
                        {
                            // The new pose is close to the first pose in the drawing -- show
                            // the snap visualization since the user could click here to finish
                            // the current drawing as a loop.
                            _snapVisualization.transform.position = firstPosition;
                            _snapVisualization.gameObject.SetActive(true);
                        }
                    }
                    else
                    {
                        // Hide the snap visualization since the user is not close to a snap point.
                        _snapVisualization.gameObject.SetActive(false);
                        _movedAwayFromPreviousSnap = true;
                    }
                }

                OnPosesUpdated?.Invoke(_poses.Count - 1);
            }
            else if (_brushControllerTransform != null)
            {
                // The user is not drawing currently but this is the polygon brush tool visual
                // -- move the brush to the expected transform.

                transform.SetPositionAndRotation(_brushControllerTransform.position,
                    _brushControllerTransform.rotation);
            }
        }

        private void StartDrawing()
        {
            _drawing = true;
            _movedAwayFromPreviousSnap = false;

            transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            _poses.Clear();
            _poses.Add(new Pose(_brushControllerTransform.position, _brushControllerTransform.rotation));
            RebuildMesh(_poses);

            OnPosesUpdated?.Invoke(0);
        }

        private void StopDrawing()
        {
            _drawing = false;
            _snapVisualization.gameObject.SetActive(false);

            if (_poses.Count > 1)
            {
                DispatchOnDrawingCompleted();
            }

            ApplyDrawingTipPoses();
            RebuildMesh(_poses);
        }

        public override void SetPosesAndTruncate(int startIndex, IList<Pose> poses,
            bool receivedDrawing)
        {
            EnsureInitialized();

            if (receivedDrawing && startIndex >= _poses.Count)
            {
                _audioHandler.PlaySoundSafe(_drawPointSound);
            }

            if (startIndex < _poses.Count)
            {
                _poses.RemoveRange(startIndex, _poses.Count - startIndex);
            }

            _poses.AddRange(poses);
            RebuildMesh(_poses);

            if (!_drawing && _poses.Count > 1 &&
                !(_poses.Count == 2 && _poses[0].position == _poses[1].position))
            {
                var meshCollider = GetComponent<MeshCollider>();
                meshCollider.sharedMesh = null;
                meshCollider.sharedMesh = GetComponent<MeshFilter>().mesh;
            }

            if (!_drawing && _poses.Count > 2)
            {
                var fillMeshCollider = _fillGameObject.GetComponent<MeshCollider>();
                fillMeshCollider.sharedMesh = null;
                fillMeshCollider.sharedMesh = _fillGameObject.GetComponent<MeshFilter>().mesh;
            }
        }

        public override void SetColors(Color32 strokeColor, Color32 fillColor,
            float fillDimmerAlpha)
        {
            Material strokeMaterial = GetComponent<MeshRenderer>().material;
            strokeMaterial.SetColor(BaseColorId, strokeColor);
            strokeMaterial.SetColor(EmissionColorId, (Color)(strokeColor) / 4.0f);

            _fillColor = fillColor;
            _fillDimmerAlpha = fillDimmerAlpha;

            _fillGameObject.SetActive(_fillColor != Color.clear);

            bool newFillMaterialIsOpaque = _fillColor.a == Byte.MaxValue;
            if (_fillMaterialIsOpaque != newFillMaterialIsOpaque)
            {
                _fillMaterialIsOpaque = newFillMaterialIsOpaque;
                _fillGameObject.GetComponent<MeshRenderer>().material =
                    _fillMaterialIsOpaque ? _opaqueFillMaterial : _transparentFillMaterial;
            }

            Material fillMaterial = _fillGameObject.GetComponent<MeshRenderer>().material;
            fillMaterial.SetColor(BaseColorId, fillColor);
            fillMaterial.SetColor(EmissionColorId, (Color)(fillColor) / 4.0f);

            _fillDimmerGameObject.SetActive(_fillDimmerAlpha > 0);
            _fillDimmerGameObject.GetComponent<MeshRenderer>().material
                .SetColor(BaseColorId, new Color(
                    fillDimmerAlpha, fillDimmerAlpha, fillDimmerAlpha, fillDimmerAlpha));
        }

        public override void OnTriggerButtonDown()
        {
        }

        /// <summary>
        /// Handle the trigger button being released, and create a new control point or start
        /// drawing depending on the current state.
        /// </summary>
        public override void OnTriggerButtonUp()
        {
            if (!_drawing)
            {
                StartDrawing();
            }

            if (_poses.Count > 1)
            {
                Vector3 firstPosition = _poses[0].position;
                Vector3 mostRecentPosition = _poses[^2].position;
                Vector3 newPosition = _poses[^1].position;

                if ((newPosition - mostRecentPosition).magnitude < _snapRadius)
                {
                    _poses.RemoveAt(_poses.Count - 1);
                    OnPosesUpdated?.Invoke(_poses.Count - 1);
                    StopDrawing();
                }
                else if (_poses.Count > 2 &&
                         (newPosition - firstPosition).magnitude < _snapRadius)
                {
                    _poses[^1] = _poses[0];
                    OnPosesUpdated?.Invoke(_poses.Count - 1);
                    StopDrawing();
                }
            }

            if (_drawing)
            {
                _poses.Add(new Pose(_brushControllerTransform.position,
                    _brushControllerTransform.rotation));
                _movedAwayFromPreviousSnap = false;
                RebuildMesh(_poses);

                _audioHandler.PlaySoundSafe(_drawPointSound);
            }
            else
            {
                _audioHandler.PlaySoundSafe(_drawEndSound);
            }
        }

        /// <summary>
        /// Replace the current poses with a simple list which visualizes a drawing tip. This
        /// is displayed so the user knows where the drawing will start if they initiate a drawing.
        /// </summary>
        private void ApplyDrawingTipPoses()
        {
            _poses.Clear();
            _poses.Add(new Pose(new Vector3(-BrushEndCapLength, 0, 0), Quaternion.identity));
            _poses.Add(Pose.identity);
        }

        /// <summary>
        /// Rebuild the current mesh of this brush stroke with an updated set of poses.
        /// </summary>
        /// <param name="poses">The new poses to use for the mesh.</param>
        private void RebuildMesh(IList<Pose> poses)
        {
            var mesh = GetComponent<MeshFilter>().mesh;
            mesh.Clear();

            if (poses.Count < 2)
            {
                _fillGameObject.SetActive(false);
                _fillDimmerGameObject.SetActive(false);
                return;
            }

            // Create a ribbon strip for the lines (strokes) between control points.

            var vertices = new Vector3[poses.Count * 2];
            var triangles = new int[poses.Count * 6 - 6];

            for (int poseIdx = 0; poseIdx < poses.Count; ++poseIdx)
            {
                Pose pose = poses[poseIdx];
                Matrix4x4 trs = Matrix4x4.TRS(pose.position, pose.rotation, Vector3.one);
                vertices[poseIdx * 2] = trs.MultiplyPoint3x4(new Vector3(0, BrushHalfWidth, 0));
                vertices[poseIdx * 2 + 1] = trs.MultiplyPoint3x4(new Vector3(0, -BrushHalfWidth, 0));
            }

            for (int quadIdx = 0; quadIdx < poses.Count - 1; ++quadIdx)
            {
                triangles[quadIdx * 6] = quadIdx * 2;
                triangles[quadIdx * 6 + 1] = quadIdx * 2 + 1;
                triangles[quadIdx * 6 + 2] = quadIdx * 2 + 2;
                triangles[quadIdx * 6 + 3] = quadIdx * 2 + 2;
                triangles[quadIdx * 6 + 4] = quadIdx * 2 + 1;
                triangles[quadIdx * 6 + 5] = quadIdx * 2 + 3;
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;

            mesh.RecalculateNormals();

            // Update the fill mesh if there are enough poses and colors were selected.

            _fillGameObject.SetActive(poses.Count > 2 && _fillColor != Color.clear);
            _fillDimmerGameObject.SetActive(poses.Count > 2 && _fillDimmerAlpha > 0);
            if (poses.Count > 2)
            {
                RebuildFillMesh(poses);
            }
        }

        /// <summary>
        /// Rebuild the fill and dimmer meshes with new poses.
        /// </summary>
        /// <param name="poses">The new poses that should make up the fill mesh</param>
        private void RebuildFillMesh(IList<Pose> poses)
        {
            var mesh = _fillGameObject.GetComponent<MeshFilter>().mesh;
            mesh.Clear();
            mesh.MarkDynamic();

            var vertices = new Vector3[poses.Count];
            var triangles = new int[poses.Count * 3 - 6];

            for (int poseIdx = 0; poseIdx < poses.Count; ++poseIdx)
            {
                vertices[poseIdx] = poses[poseIdx].position;
            }

            for (int triangleIdx = 0; triangleIdx < poses.Count - 2; ++triangleIdx)
            {
                triangles[triangleIdx * 3] = 0;
                triangles[triangleIdx * 3 + 1] = triangleIdx + 1;
                triangles[triangleIdx * 3 + 2] = triangleIdx + 2;
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;

            mesh.RecalculateNormals();

            _fillDimmerGameObject.GetComponent<MeshFilter>().mesh = mesh;
        }
    }
}