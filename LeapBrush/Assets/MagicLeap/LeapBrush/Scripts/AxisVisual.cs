using System;
using UnityEngine;

namespace MagicLeap.LeapBrush
{
    public class AxisVisual : MonoBehaviour
    {
        [SerializeField, Tooltip("Game object containing the text to billboard")]
        private GameObject _annotationBillboard;

        [SerializeField, Tooltip("The root of the main anchor collider and visualizations")]
        private GameObject _visualization;

        [SerializeField, Tooltip("The root of the anchor annotation")]
        private GameObject _annotation;

        [SerializeField, Tooltip("The leader line")]
        private GameObject _leaderLine;

        private const float MaxDistanceForTextDisplay = 4.0f;
        private const float LeaderLineMinLengthToDisplay = .0005f;

        void Awake()
        {
            // Start the annotation's rigid body to sleep for the first frame, to allow for a
            // proper initial transform to be set. Otherwise the rigidbody jumps and results in
            // a large initial velocity.
            _annotation.GetComponent<Rigidbody>().Sleep();
        }

        private void Update()
        {
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
    }
}