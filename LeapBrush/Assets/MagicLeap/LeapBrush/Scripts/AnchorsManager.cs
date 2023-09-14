using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.MagicLeap;

namespace MagicLeap.LeapBrush
{
    /// <summary>
    /// Manager for Spatial Anchor game objects. Spatial Anchors are periodically queried and
    /// kept in sync.
    /// </summary>
    public class AnchorsManager : MonoBehaviour
    {
        /// <summary>
        /// Get the current anchor results, sorted by Id
        /// </summary>
        public AnchorsApi.Anchor[] Anchors => _anchors;

        /// <summary>
        /// Get whether the anchors in use were imported from another user.
        /// </summary>
        public bool IsUsingImportedAnchors => _isUsingImportedAnchors;

        /// <summary>
        /// Get whether the last query for anchors was successful.
        /// </summary>
        public bool LastQuerySuccessful => _lastQuerySuccessful;

        [SerializeField, Tooltip("The parent node where anchor game objects should be added.")]
        private GameObject _anchorContainer;

        [SerializeField, Tooltip("The anchor prefab.")]
        private GameObject _anchorPrefab;

        [SerializeField]
        private LeapBrushPreferences _preferences;

        [SerializeField, Tooltip("Animation curve used to lerp/slerp an anchor to the latest pose")]
        private AnimationCurve _anchorLerpAnimation;

        [SerializeField, Tooltip("The maximum positional change for an anchor to have while still" +
                                 " animating to the new position")]
        private float _maxPositionChangeToAnimate = 0.5f;

        [SerializeField, Tooltip("The maximum rotational angle change for an anchor to have while" +
                                 " still animating to the new rotation (in degrees)")]
        private float _maxRotationAngleChangeToAnimate = 15.0f;

        private const float AnchorsUpdateDelaySeconds = .1f;

        private AnchorsApi.Anchor[] _queryAnchors = Array.Empty<AnchorsApi.Anchor>();
        private AnchorsApi.Anchor[] _pendingAnchors = Array.Empty<AnchorsApi.Anchor>();
        private AnchorsApi.Anchor[] _anchors = Array.Empty<AnchorsApi.Anchor>();
        private bool _pendingIsUsingImportedAnchors;
        private bool _isUsingImportedAnchors;
        private bool _pendingLastQuerySuccessful;
        private bool _lastQuerySuccessful;

        private Dictionary<string, GameObject> _anchorGameObjectsMap = new();
        private IEnumerator _updateAnchorsCoroutine;

        /// <summary>
        /// Try to get the game object representation for an anchor.
        /// </summary>
        /// <param name="anchorId">The anchor id to find</param>
        /// <param name="gameObject">The game object that is representing this anchor.</param>
        /// <returns>True if the game object could be found, or false otherwise.</returns>
        public bool TryGetAnchorGameObject(string anchorId, out GameObject gameObject)
        {
            if (anchorId == null)
            {
                gameObject = null;
                return false;
            }
            return _anchorGameObjectsMap.TryGetValue(anchorId, out gameObject);
        }

        public void SetContentShown(bool shown)
        {
            _anchorContainer.SetActive(shown);
        }

        private void OnEnable()
        {
            _updateAnchorsCoroutine = UpdateAnchorsPeriodically();
            StartCoroutine(_updateAnchorsCoroutine);

            _preferences.ShowSpatialAnchors.OnChanged += OnShowSpatialAnchorsPreferenceChanged;
            OnShowSpatialAnchorsPreferenceChanged();
        }

        private void OnDisable()
        {
            StopCoroutine(_updateAnchorsCoroutine);

            _preferences.ShowSpatialAnchors.OnChanged -= OnShowSpatialAnchorsPreferenceChanged;
        }

        private void OnShowSpatialAnchorsPreferenceChanged()
        {
            foreach (var entry in _anchorGameObjectsMap)
            {
                entry.Value.GetComponent<AnchorVisual>().SetShown(
                    _preferences.ShowSpatialAnchors.Value);
            }
        }

        private IEnumerator UpdateAnchorsPeriodically()
        {
            Action updateAnchorsAction = UpdateAnchorsOnWorkerThread;
            YieldInstruction anchorsUpdateDelay = new WaitForSeconds(AnchorsUpdateDelaySeconds);

            while (true)
            {
                ThreadDispatcher.ScheduleWork(updateAnchorsAction);

                // Wait before querying again for localization status
                yield return anchorsUpdateDelay;
            }
        }

        private void UpdateAnchorsOnWorkerThread()
        {
            bool isUsingImportedAnchors;
            MLResult result = AnchorsApi.QueryAnchors(ref _queryAnchors,
                out isUsingImportedAnchors);
            if (result.IsOk)
            {
                bool hasValidPoses = true;
                for (var i = 0; i < _queryAnchors.Length; i++)
                {
                    AnchorsApi.Anchor anchor = _queryAnchors[i];
                    if (anchor.Pose.rotation.x == 0 && anchor.Pose.rotation.y == 0 &&
                        anchor.Pose.rotation.z == 0 && anchor.Pose.rotation.w == 0)
                    {
                        hasValidPoses = false;
                    }
                }

                if (hasValidPoses)
                {
                    Array.Sort(_queryAnchors, (a, b) =>
                        string.CompareOrdinal(a.Id, b.Id));

                    if (!_pendingLastQuerySuccessful
                        || isUsingImportedAnchors != _pendingIsUsingImportedAnchors
                        || !AnchorsApi.Anchor.ArraysEqual(_queryAnchors, _pendingAnchors))
                    {
                        _pendingAnchors = new AnchorsApi.Anchor[_queryAnchors.Length];
                        for (int i = 0; i < _queryAnchors.Length; i++)
                        {
                            _pendingAnchors[i] = _queryAnchors[i].Clone();
                        }
                        _pendingLastQuerySuccessful = true;
                        _pendingIsUsingImportedAnchors = isUsingImportedAnchors;

                        ThreadDispatcher.ScheduleMain(() => HandleAnchorsChangedOnMainThread(
                            _pendingAnchors, _pendingIsUsingImportedAnchors));
                    }
                    return;
                }

                // TODO(ghazen): Find and report the root cause for invalid anchor poses.
                Debug.LogError("UpdateAnchorsOnWorkerThread: some anchors have invalid poses");
            }
            else
            {
                Debug.LogError("UpdateAnchorsOnWorkerThread: failed to query anchors "
                               + result);
            }

            _pendingLastQuerySuccessful = false;
            ThreadDispatcher.ScheduleMain(() =>
            {
                 _lastQuerySuccessful = false;
            });
        }

        private void HandleAnchorsChangedOnMainThread(
            AnchorsApi.Anchor[] anchors, bool isUsingImportedAnchors)
        {
            _anchors = anchors;
            _isUsingImportedAnchors = isUsingImportedAnchors;
            _lastQuerySuccessful = true;

            HashSet<string> removedAnchorIds = new HashSet<string>();
            foreach (KeyValuePair<string, GameObject> anchorEntry in _anchorGameObjectsMap)
            {
                removedAnchorIds.Add(anchorEntry.Key);
            }

            foreach (AnchorsApi.Anchor anchor in _anchors)
            {
                removedAnchorIds.Remove(anchor.Id);

                if (_anchorPrefab != null && _anchorContainer != null)
                {
                    GameObject anchorGameObject;
                    AnchorVisual anchorVisual;
                    bool animateAnchor = true;
                    if (!_anchorGameObjectsMap.TryGetValue(anchor.Id, out anchorGameObject))
                    {
                        Debug.Log("Anchor Found: " + anchor.Id);

                        anchorGameObject = Instantiate(_anchorPrefab, _anchorContainer.transform);
                        anchorVisual = anchorGameObject.GetComponent<AnchorVisual>();
                        anchorVisual.Initialize(anchor, _preferences.ShowSpatialAnchors.Value);

                        _anchorGameObjectsMap.Add(anchor.Id, anchorGameObject);

                        animateAnchor = false;
                    }
                    else
                    {
                        anchorVisual = anchorGameObject.GetComponent<AnchorVisual>();
                    }

                    anchorVisual.UpdateData(anchor, animateAnchor, _anchorLerpAnimation,
                        _maxPositionChangeToAnimate, _maxRotationAngleChangeToAnimate);
                }
            }

            foreach (string anchorId in removedAnchorIds)
            {
                Debug.Log("Anchor Lost: " + anchorId);

                GameObject anchorGameObject;
                if (_anchorGameObjectsMap.Remove(anchorId, out anchorGameObject))
                {
                    Destroy(anchorGameObject);
                }
            }
        }
    }
}