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
        [SerializeField, Tooltip("The parent node where anchor game objects should be added.")]
        private GameObject _anchorContainer;

        [SerializeField, Tooltip("The anchor prefab.")]
        private GameObject _anchorPrefab;

        private const float AnchorsUpdateDelaySeconds = .1f;

        private AnchorsApi.Anchor[] _anchors = new AnchorsApi.Anchor[0];
        private bool _isUsingImportedAnchors;
        private Dictionary<string, GameObject> _anchorGameObjectsMap = new();

        private IEnumerator _updateAnchorsCoroutine;
        private bool _anchorVisualShown;

        private void Awake()
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            // Side effect: Ensure MLDevice is initialized early
            Debug.Log("AnchorsManager Awake: MLDevice Platform Level: "
                      + MLDevice.PlatformLevel);
#endif
        }

        public void SetShown(bool shown)
        {
            _anchorVisualShown = shown;
            foreach (var entry in _anchorGameObjectsMap)
            {
                entry.Value.GetComponent<AnchorView>().SetShown(shown);
            }
        }

        public void SetContentShown(bool shown)
        {
            _anchorContainer.SetActive(shown);
        }

        void Start()
        {
            _updateAnchorsCoroutine = UpdateAnchorsPeriodically();
            StartCoroutine(_updateAnchorsCoroutine);
        }

        void OnDestroy()
        {
            StopCoroutine(_updateAnchorsCoroutine);
        }

        /// <summary>
        /// Get the current anchor results, sorted by Id
        /// </summary>
        public AnchorsApi.Anchor[] Anchors => _anchors;

        public bool IsUsingImportedAnchors => _isUsingImportedAnchors;

        public bool TryGetAnchorGameObject(string anchorId, out GameObject gameObject)
        {
            if (anchorId == null)
            {
                gameObject = null;
                return false;
            }
            return _anchorGameObjectsMap.TryGetValue(anchorId, out gameObject);
        }

        private IEnumerator UpdateAnchorsPeriodically()
        {
            while (true)
            {
                ThreadDispatcher.ScheduleWork(UpdateAnchorsOnWorkerThread);

                // Wait before querying again for localization status
                yield return new WaitForSeconds(AnchorsUpdateDelaySeconds);
            }
        }

        private void UpdateAnchorsOnWorkerThread()
        {
            AnchorsApi.Anchor[] anchors;
            bool isUsingImportedAnchors;
            MLResult result = AnchorsApi.QueryAnchors(out anchors, out isUsingImportedAnchors);
            if (result.IsOk)
            {
                bool hasValidPoses = true;
                foreach (AnchorsApi.Anchor anchor in anchors)
                {
                    if (anchor.Pose.rotation.x == 0 && anchor.Pose.rotation.y == 0
                        && anchor.Pose.rotation.z == 0 && anchor.Pose.rotation.w == 0)
                    {
                        hasValidPoses = false;
                    }
                }

                if (!hasValidPoses)
                {
                    // TODO(ghazen): Find and report the root cause for invalid anchor poses.
                    return;
                }

                Array.Sort(anchors, (AnchorsApi.Anchor a, AnchorsApi.Anchor b) =>
                    string.CompareOrdinal(a.Id, b.Id));

                ThreadDispatcher.ScheduleMain(() => UpdateAnchorsOnMainThread(
                    anchors, isUsingImportedAnchors));
            }
        }

        private void UpdateAnchorsOnMainThread(
            AnchorsApi.Anchor[] anchors, bool isUsingImportedAnchors)
        {
            _anchors = anchors;
            _isUsingImportedAnchors = isUsingImportedAnchors;

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
                    AnchorView anchorView;
                    if (!_anchorGameObjectsMap.TryGetValue(anchor.Id, out anchorGameObject))
                    {
                        Debug.Log("Anchor Found: " + anchor.Id);

                        anchorGameObject = Instantiate(_anchorPrefab, _anchorContainer.transform);
                        anchorView = anchorGameObject.GetComponent<AnchorView>();
                        anchorView.Initialize(anchor, _anchorVisualShown);

                        _anchorGameObjectsMap.Add(anchor.Id, anchorGameObject);
                    }
                    else
                    {
                        anchorView = anchorGameObject.GetComponent<AnchorView>();
                    }

                    anchorView.UpdateData(anchor);
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