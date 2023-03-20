using System;
using TMPro;
using UnityEngine;

namespace MagicLeap.LeapBrush
{
    /// <summary>
    /// A rendering for a Spatial Anchor for the user's interest.
    /// </summary>
    public class AnchorView : MonoBehaviour
    {
        [SerializeField, Tooltip("Game object holding the status text")]
        private GameObject _statusLayout = null;

        [SerializeField, Tooltip("The text field for showing this anchor's status")]
        private TextMeshPro _statusText = null;

        [SerializeField, Tooltip("The root of the main anchor collider and visualizations")]
        private GameObject _colliderAndVisualization = null;

        [SerializeField, Tooltip("The root of the main anchor visualization")]
        private GameObject _visualization = null;

        [SerializeField, Tooltip("The leader line")]
        private GameObject _leaderLine = null;

        private const float MaxDistanceForCanvasDisplay = 4.0f;
        private const float LeaderLineMinLengthToDisplay = .0005f;

        private AnchorsApi.Anchor _anchorData;

        private float _statusLayoutCenterOffset;

        void Awake()
        {
            // Start the visualization's rigid body to sleep for the first frame, to allow for a
            // proper initial transform to be set. Otherwise the rigidbody jumps and results in
            // a large initial velocity.
            _visualization.GetComponent<Rigidbody>().Sleep();
        }

        void Start()
        {
            _statusLayoutCenterOffset =
                _statusLayout.transform.localPosition.magnitude;
        }

        public void Initialize(AnchorsApi.Anchor anchorData, bool shown)
        {
            _anchorData = anchorData;
            _colliderAndVisualization.SetActive(shown);
            UpdateStatusText();
        }

        void Update()
        {
            Vector3 visualizationToCamera =
                Camera.main.transform.position - _visualization.transform.position;
            _statusLayout.gameObject.SetActive(
                visualizationToCamera.sqrMagnitude < Math.Pow(MaxDistanceForCanvasDisplay, 2));
            if (_statusLayout.gameObject.activeSelf)
            {
                _statusLayout.transform.position =
                    _visualization.transform.position +
                    visualizationToCamera.normalized * _statusLayoutCenterOffset;
                _statusLayout.transform.LookAt(
                    _statusLayout.transform.position -
                    visualizationToCamera, Vector3.up);
            }

            Vector3 poseToVisualization = _visualization.transform.position - transform.position;
            _leaderLine.SetActive(poseToVisualization.sqrMagnitude
                                  >= Math.Pow(LeaderLineMinLengthToDisplay, 2));
            if (_leaderLine.activeSelf)
            {
                _leaderLine.transform.LookAt(_visualization.transform, Vector3.up);
                _leaderLine.transform.position = transform.position + poseToVisualization / 2.0f;

                Vector3 leaderScale = _leaderLine.transform.localScale;
                _leaderLine.transform.localScale = new Vector3(
                    leaderScale.x, leaderScale.y, poseToVisualization.magnitude);
            }
        }

        public string GetId()
        {
            return _anchorData.Id;
        }

        public void SetShown(bool shown)
        {
            _colliderAndVisualization.SetActive(shown);
        }

        public void UpdateData(AnchorsApi.Anchor anchorData)
        {
            AnchorsApi.Anchor oldAnchorData = _anchorData;
            _anchorData = anchorData;

            if (anchorData.Pose.position != transform.position ||
                anchorData.Pose.rotation != transform.rotation)
            {
                transform.position = anchorData.Pose.position;
                transform.rotation = anchorData.Pose.rotation;
                _visualization.transform.rotation = anchorData.Pose.rotation;
            }

            if (oldAnchorData.IsPersisted != anchorData.IsPersisted)
            {
                UpdateStatusText();
            }
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