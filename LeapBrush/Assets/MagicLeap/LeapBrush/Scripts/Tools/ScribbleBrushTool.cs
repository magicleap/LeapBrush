using System;
using UnityEngine;

namespace MagicLeap.LeapBrush
{
    /// <summary>
    /// The scribble brush tool.
    /// </summary>
    /// <remarks>
    /// The scribble brush allows the user to draw while the select input action is active in a
    /// free-form drawing mode. Any motion by the controller while drawing will be picked up as new brush
    /// pose.
    ///
    /// <para>The algorithm will only append new brush poses if the user moves the tool
    /// far enough from the previous pose.
    /// </para>
    /// </remarks>
    public class ScribbleBrushTool : BrushToolBase
    {
        /// <summary>
        /// Event fired when poses have been added to this brush.
        /// </summary>
        public event Action<ScribbleBrushTool> OnPosesAdded;

        public override BrushBase Brush => _brush;

        [SerializeField]
        private AudioSource _drawStartSound;

        [SerializeField]
        private AudioSource _drawEndSound;

        [SerializeField]
        private ScribbleBrush _brush;

        private const float BrushEndCapLength = .001f;
        private const float MinDistanceAddPose = .0025f;

        private bool _playEndSoundAfterTimeout;

        private void Awake()
        {
            ApplyDrawingTipPoses();
        }

        private void OnEnable()
        {
            base.OnEnable();
        }

        private void Update()
        {
            if (_drawing) {
                Pose lastPose = _brush.Poses[^1];
                Pose nextPose = new Pose(_brushControllerTransform.position,
                    _brushControllerTransform.rotation);
                if ((nextPose.position - lastPose.position).sqrMagnitude >= MinDistanceAddPose * MinDistanceAddPose)
                {
                    // The new pose is far enough away from the previous pose: Add a new brush pose.
                    _brush.AddPose(nextPose);

                    OnPosesAdded?.Invoke(this);
                }
            }
        }

        private void MaybeStartDrawing()
        {
            if (_isHandControlled && !IsBrushControllerInFieldOfView())
            {
                return;
            }

            _drawing = true;
            transform.SetParent(null);
            transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            Vector3 endCapStartPoint = _brushControllerTransform.TransformPoint(
                new Vector3(-BrushEndCapLength, 0, 0));

            _brush.SetPosesAndTruncate(0, new Pose[]
            {
                new(endCapStartPoint, _brushControllerTransform.rotation),
                new(_brushControllerTransform.position, _brushControllerTransform.rotation)
            }, false);

            OnPosesAdded?.Invoke(this);

            _drawStartSound.transform.position = _brush.GetStartPosition();
            _drawStartSound.Play();
        }

        private void StopDrawing()
        {
            _drawEndSound.transform.position = _brush.GetEndPosition();
            _drawEndSound.Play();

            _drawing = false;
            transform.SetParent(_brushControllerTransform);
            transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            DispatchOnDrawingCompleted();

            ApplyDrawingTipPoses();
        }

        public override void OnSelectStarted()
        {
            // Start drawing when the select input action is active.
            MaybeStartDrawing();
        }

        public override void OnSelectEnded()
        {
            if (_drawing)
            {
                // Stop drawing when the select input action is released.
                StopDrawing();
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