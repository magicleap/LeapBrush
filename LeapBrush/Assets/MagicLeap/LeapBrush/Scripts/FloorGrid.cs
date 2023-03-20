using System;
using System.Collections;
using UnityEngine;

namespace MagicLeap
{
    /// <summary>
    /// Manager for a visualization of a floor grid that can be displayed in a spectator app.
    /// </summary>
    /// <remarks>
    /// The floor grid relocates based on loaded content to always remain below things.
    /// </remarks>
    public class FloorGrid : MonoBehaviour
    {
        [SerializeField]
        private GameObject _gridSnapCenter;

        [SerializeField]
        private MeshRenderer _gridLinesMeshRenderer;

        private const float FloorPlaneUpdateDelaySeconds = 0.25f;
        private const float FloorNotDetectedYOffset = -1.5f;
        private const float OffsetYFromFloor = .02f;
        private const float FloorAnimationLerpSpeed = 5.0f;
        private const float FloorMovementEpsilon = 0.001f;
        private const float FloorFromHeadposeMinYOffset = -0.5f;
        private const float LowestContentYOffset = -.05f;

        private float _floorYPosition = Single.MinValue;
        private float _lowestContentYPosition = Single.MinValue;
        private IEnumerator _updateFloorPlaneCoroutine;

        private const float CellSize = 2.0f;

        private static Vector3Int PositionToGridIndex(Vector3 position)
        {
            return new Vector3Int(
                Mathf.RoundToInt(position.x / CellSize),
                Mathf.RoundToInt(position.y / CellSize),
                Mathf.RoundToInt(position.z / CellSize));
        }

        private static Vector3 GetCellCenter(Vector3Int gridIndex)
        {
            return new Vector3(
                gridIndex.x * CellSize, gridIndex.y * CellSize, gridIndex.z * CellSize);
        }

        private void OnEnable()
        {
            _updateFloorPlaneCoroutine = UpdateFloorPlanePeriodically();
            StartCoroutine(_updateFloorPlaneCoroutine);
        }

        private void OnDisable()
        {
            StopCoroutine(_updateFloorPlaneCoroutine);
            _updateFloorPlaneCoroutine = null;
        }

        public void Update()
        {
            Transform cameraTransform = Camera.main.transform;

            float lowestContentOrCameraOffsetPosition = Mathf.Min(
                _lowestContentYPosition != Single.MinValue ?
                    _lowestContentYPosition + LowestContentYOffset : Single.MaxValue,
                cameraTransform.position.y + FloorFromHeadposeMinYOffset);

            // Animate the floor position towards a target either offset from headpose or based
            // on the detected floor plane. Include a minimum offset below headpose.
            float targetFloorYPosition =
                Mathf.Min(
                    _floorYPosition != Single.MinValue ? _floorYPosition + OffsetYFromFloor
                        : cameraTransform.position.y + FloorNotDetectedYOffset,
                    lowestContentOrCameraOffsetPosition);
            float animatedFloorYPosition = Mathf.Lerp(
                transform.localPosition.y, targetFloorYPosition,
                Time.deltaTime * FloorAnimationLerpSpeed);
            if (Mathf.Abs(targetFloorYPosition - animatedFloorYPosition) > FloorMovementEpsilon)
            {
                transform.localPosition = new Vector3(0, animatedFloorYPosition, 0);
            }

            _gridLinesMeshRenderer.material.SetVector(
                "_HeadPose", cameraTransform.position);

            Vector3Int cellIndex = PositionToGridIndex(cameraTransform.position);

            // Move the center of the grid lines visualization to snap to the closest grid cell
            // center.
            Vector3 gridCenterPosition = GetCellCenter(cellIndex);
            gridCenterPosition.y = 0;
            _gridSnapCenter.transform.localPosition = gridCenterPosition;
        }

        private IEnumerator UpdateFloorPlanePeriodically()
        {
            yield return new WaitForEndOfFrame();

            while (true)
            {
                yield return UpdateFloorPlane();

                // Wait before querying again for floor plane
                yield return new WaitForSeconds(FloorPlaneUpdateDelaySeconds);
            }
        }

        private IEnumerator UpdateFloorPlane()
        {
            yield break;
        }

        public void FoundContentAtPosition(Vector3 position)
        {
            if (_lowestContentYPosition == Single.MinValue || position.y < _lowestContentYPosition)
            {
                _lowestContentYPosition = position.y;
            }
        }
    }
}