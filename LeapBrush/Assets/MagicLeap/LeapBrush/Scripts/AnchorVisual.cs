using System;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace MagicLeap.LeapBrush
{
    /// <summary>
    /// A rendering for a Spatial Anchor for the user's interest.
    /// </summary>
    public class AnchorVisual : MonoBehaviour
    {
        [SerializeField, Tooltip("Game object containing the text to billboard")]
        private GameObject _annotationBillboard;

        [SerializeField, Tooltip("The text field for showing this anchor's status")]
        private TextMeshPro _statusText;

        [SerializeField, Tooltip("The root of the main anchor collider and visualizations")]
        private GameObject _visualization;

        [SerializeField, Tooltip("The root of the anchor annotation")]
        private GameObject _annotation;

        [SerializeField, Tooltip("The leader line")]
        private GameObject _leaderLine;

        private const float MaxDistanceForTextDisplay = 4.0f;
        private const float LeaderLineMinLengthToDisplay = .0005f;

        private AnchorsApi.Anchor _anchorData;

        private bool _animating;
        private AnimationCurve _lerpAnimation;
        private float _animationTime;
        private Pose _lerpStartPose;

        void Awake()
        {
            // Start the annotation's rigid body to sleep for the first frame, to allow for a
            // proper initial transform to be set. Otherwise the rigidbody jumps and results in
            // a large initial velocity.
            _annotation.GetComponent<Rigidbody>().Sleep();
        }

        public void Initialize(AnchorsApi.Anchor anchorData, bool shown)
        {
            _anchorData = anchorData;
            _visualization.SetActive(shown);
            UpdateStatusText();
        }

        void Update()
        {
            if (_animating)
            {
                _animationTime += Time.deltaTime;
                float lerpRatio = _lerpAnimation.Evaluate(_animationTime);

                transform.position = Vector3.Lerp(
                    _lerpStartPose.position, _anchorData.Pose.position, lerpRatio);
                transform.rotation = Quaternion.Slerp(
                    _lerpStartPose.rotation, _anchorData.Pose.rotation, lerpRatio);

                _animating = _animationTime < _lerpAnimation[_lerpAnimation.length - 1].time;
            }

            Vector3 annotationToCamera =
                Camera.main.transform.position - _annotation.transform.position;
            _annotation.gameObject.SetActive(
                annotationToCamera.sqrMagnitude < Math.Pow(MaxDistanceForTextDisplay, 2));
            if (_annotation.gameObject.activeSelf)
            {
                _annotationBillboard.transform.LookAt(
                    _annotationBillboard.transform.position - annotationToCamera,
                    Vector3.up);

                Vector3 poseToAnnotation = _annotation.transform.position - transform.position;
                _leaderLine.SetActive(poseToAnnotation.sqrMagnitude
                                      >= Math.Pow(LeaderLineMinLengthToDisplay, 2));
                if (_leaderLine.activeSelf)
                {
                    _leaderLine.transform.LookAt(_annotation.transform, Vector3.up);
                    _leaderLine.transform.position = transform.position + poseToAnnotation / 2.0f;

                    Vector3 leaderScale = _leaderLine.transform.localScale;
                    _leaderLine.transform.localScale = new Vector3(
                        leaderScale.x, leaderScale.y, poseToAnnotation.magnitude);
                }
            }
        }

        public string GetId()
        {
            return _anchorData.Id;
        }

        public void SetShown(bool shown)
        {
            _visualization.SetActive(shown);
        }

        public void UpdateData(AnchorsApi.Anchor anchorData, bool animate,
            AnimationCurve lerpAnimation,  float maxPositionChangeToAnimate,
            float maxRotationAngleChangeToAnimate)
        {
            _lerpAnimation = lerpAnimation;

            AnchorsApi.Anchor oldAnchorData = _anchorData;
            _anchorData = anchorData;

            if (oldAnchorData.IsPersisted != anchorData.IsPersisted)
            {
                UpdateStatusText();
            }

            // Animate or jump the anchor to a new pose

            // Don't animate to the new pose if the pose has jumped more than a threshold.
            if (!animate ||
                ((anchorData.Pose.position - transform.position).sqrMagnitude >
                 Math.Pow(maxPositionChangeToAnimate, 2)) ||
                (Quaternion.Angle(anchorData.Pose.rotation, transform.rotation) >
                 maxRotationAngleChangeToAnimate))
            {
                _animating = false;
                transform.SetWorldPose(anchorData.Pose);
                return;
            }

            if (_animating)
            {
                if (anchorData.Pose.position == oldAnchorData.Pose.position &&
                    anchorData.Pose.rotation == oldAnchorData.Pose.rotation)
                {
                    // The new pose is approximately the same, ignore
                    return;
                }
            }

            // Start animating towards the new pose.
            _animating = true;
            _lerpStartPose = transform.GetWorldPose();
            _animationTime = 0;
        }

        private void UpdateStatusText()
        {
            string statusText = "";
            if (_anchorData.Id != null)
            {
                statusText = "Spatial Anchor\nId: " + _anchorData.Id +
                             (_anchorData.IsPersisted ? "; Persisted" : "") + "\n";
            }

            _statusText.text = statusText;
        }
    }
}