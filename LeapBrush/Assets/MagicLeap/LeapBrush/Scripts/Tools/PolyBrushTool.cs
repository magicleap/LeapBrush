using System;
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
    public class PolyBrushTool : BrushToolBase
    {
        /// <summary>
        /// Event fired when poses have been updated in this brush. The first argument is the
        /// start index in the list of poses where poses have been modified.
        /// </summary>
        public event Action<PolyBrushTool, int> OnPosesUpdated;

        public override BrushBase Brush => _brush;

        [SerializeField, Tooltip("Visualization for a snap point when the tool is close to one")]
        private PolyBrushSnapVisualization _snapVisualization;

        [SerializeField]
        private AudioSource _drawPointSound;

        [SerializeField]
        private AudioSource _drawEndSound;

        [SerializeField]
        private PolyBrush _brush;

        private const float BrushEndCapLength = .001f;
        private const float MinDistanceUpdatePose = .0005f;

        private bool _initialized;
        private bool _movedAwayFromPreviousSnap;
        private Color32 _fillColor;
        private float _fillDimmerAlpha;
        private bool _fillMaterialIsOpaque = true;

        private static readonly Vector3 SnapScale = new(.04f, .04f, .08f);

        private void Awake()
        {
            ApplyDrawingTipPoses();
        }

        private void OnEnable()
        {
            base.OnEnable();
        }

        private void OnDisable()
        {
            if (_drawing)
            {
                _brush.RemoveLastPose();
                OnPosesUpdated?.Invoke(this, _brush.Poses.Count - 1);
                StopDrawing();
            }
        }

        private void Update()
        {
            if (_drawing)
            {
                transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

                Pose lastPose = _brush.Poses[^1];
                Pose nextPose = new Pose(_brushControllerTransform.position,
                    _brushControllerTransform.rotation);
                if ((nextPose.position - lastPose.position).sqrMagnitude <
                    MinDistanceUpdatePose * MinDistanceUpdatePose)
                {
                    // The tool is too close to the last pose added still.
                    return;
                }

                if (_brush.Poses.Count > 1)
                {
                    Vector3 firstPosition = _brush.Poses[0].position;
                    Vector3 mostRecentPosition = _brush.Poses[^2].position;
                    Vector3 newPosition = nextPose.position;

                    // Calculate a matrix for snapping to an existing point.
                    // The snap space helps calculate whether a point is within an ellipsoid
                    // centered at the new pose. The ellipsoid is scaled up based on the distance
                    // from the camera.
                    Matrix4x4 cameraLocalToWorld = _camera.transform.localToWorldMatrix;
                    Vector4 snapTranslation = newPosition;
                    snapTranslation.w = 1;
                    float snapCameraDistance = Vector3.Distance(
                        _camera.transform.position, nextPose.position);
                    Matrix4x4 snapSpace = new Matrix4x4(
                        cameraLocalToWorld.GetColumn(0) * (snapCameraDistance * SnapScale.x),
                        cameraLocalToWorld.GetColumn(1) * (snapCameraDistance * SnapScale.y),
                        cameraLocalToWorld.GetColumn(2) * (snapCameraDistance * SnapScale.z),
                        snapTranslation);
                    Matrix4x4 snapSpaceInverse = snapSpace.inverse;

                    if (snapSpaceInverse.MultiplyPoint(mostRecentPosition).sqrMagnitude < 1.0f)
                    {
                        if (_movedAwayFromPreviousSnap)
                        {
                            // The new pose is close to the most recently added pose -- show the
                            // snap visualization since the user could click here to finish
                            // the current drawing.
                            _snapVisualization.transform.position = mostRecentPosition;
                            _snapVisualization.gameObject.SetActive(true);

                            nextPose.position = mostRecentPosition;
                        }
                    }
                    else if (_brush.Poses.Count > 2 &&
                             snapSpaceInverse.MultiplyPoint(firstPosition).sqrMagnitude < 1.0f)
                    {
                        if (_movedAwayFromPreviousSnap)
                        {
                            // The new pose is close to the first pose in the drawing -- show
                            // the snap visualization since the user could click here to finish
                            // the current drawing as a loop.
                            _snapVisualization.transform.position = firstPosition;
                            _snapVisualization.gameObject.SetActive(true);

                            nextPose.position = firstPosition;
                        }
                    }
                    else
                    {
                        // Hide the snap visualization since the user is not close to a snap point.
                        _snapVisualization.gameObject.SetActive(false);
                        _movedAwayFromPreviousSnap = true;
                    }
                }

                // Replace the last pose with the current tool location.
                _brush.SetLastPose(nextPose);

                OnPosesUpdated?.Invoke(this, _brush.Poses.Count - 1);
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

            _brush.SetPosesAndTruncate(0, new Pose[]
            {
                new(_brushControllerTransform.position, _brushControllerTransform.rotation)
            }, false);

            OnPosesUpdated?.Invoke(this, 0);
        }

        private void StopDrawing()
        {
            _drawing = false;
            _snapVisualization.gameObject.SetActive(false);

            if (_brush.Poses.Count > 1)
            {
                DispatchOnDrawingCompleted();
            }

            ApplyDrawingTipPoses();
        }

        public override void OnSelectStarted()
        {
        }

        /// <summary>
        /// Handle the select input action being released, and create a new control point or start
        /// drawing depending on the current state.
        /// </summary>
        public override void OnSelectEnded()
        {
            if (!IsBrushControllerInFieldOfView())
            {
                return;
            }

            if (!_drawing)
            {
                StartDrawing();
            }

            if (_brush.Poses.Count > 1)
            {
                Vector3 firstPosition = _brush.Poses[0].position;
                Vector3 mostRecentPosition = _brush.Poses[^2].position;
                Vector3 newPosition = _brush.Poses[^1].position;

                if (newPosition == mostRecentPosition)
                {
                    _brush.RemoveLastPose();
                    OnPosesUpdated?.Invoke(this, _brush.Poses.Count - 1);
                    StopDrawing();
                }
                else if (_brush.Poses.Count > 2 && newPosition == firstPosition)
                {
                    StopDrawing();
                }
            }

            if (_drawing)
            {
                _brush.AddPose(new Pose(_brushControllerTransform.position,
                    _brushControllerTransform.rotation));
                _movedAwayFromPreviousSnap = false;

                _drawPointSound.Play();
            }
            else
            {
                _drawEndSound.Play();
            }
        }

        /// <summary>
        /// Replace the current poses with a simple list which visualizes a drawing tip. This
        /// is displayed so the user knows where the drawing will start if they initiate a drawing.
        /// </summary>
        private void ApplyDrawingTipPoses()
        {
            _brush.SetPosesAndTruncate(0, new[]
            {
                new(new Vector3(-BrushEndCapLength, 0, 0), Quaternion.identity),
                Pose.identity
            }, false);
        }
    }
}