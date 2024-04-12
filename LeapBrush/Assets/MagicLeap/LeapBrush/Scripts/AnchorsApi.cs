using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MagicLeap.OpenXR.Features.SpatialAnchors;
using MagicLeap.OpenXR.Subsystems;
using Unity.Profiling;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;

namespace MagicLeap.LeapBrush
{
    /// <summary>
    /// Wrapper for the OpenXR Localization and Anchors api to support a few additional use cases,
    /// e.g. Fake anchors provided in a unity editor session.
    /// </summary>
    public class AnchorsApi : MonoBehaviour
    {
        [SerializeField]
        private ARAnchorManager _anchorManager;

        [SerializeField]
        [Tooltip("Anchor to use when on a non-ML2 device or in the unity editor.")]
        private ImportedAnchor _defaultAnchor =
            new()
            {
                Id = "DEFAULT_ANCHOR_ID",
                Pose = Pose.identity
            };

        /// <summary>
        /// Whether the AnchorsApi is ready or not.
        /// </summary>
        public static bool IsReady => _instance._isReady;

        /// <summary>
        /// Query for anchor information within the current localized map.  The result will be returned
        /// asynchronously in the <see cref="AnchorsApi.OnAnchorsUpdatedEvent"/> event.
        /// </summary>
        /// <returns>Returns <see langword="true"/> if the query call succeeded, otherwise <see langword="false"/>.</returns>
        public static bool QueryAnchors() => _instance.QueryAnchorsImpl();

        /// <summary>
        /// Create an anchor to be published to storage within the current localized map.
        /// </summary>
        /// <param name="pose">The pose for the anchor.</param>
        /// <returns>Returns <see langword="true"/> if the create call succeeded, otherwise <see langword="false"/>.</returns>
        public static bool CreateAnchor(Pose pose) => _instance.CreateAnchorImpl(pose);

        public static void SetImportedAnchors(Anchor[] importedAnchors) =>
            _instance.SetImportedAnchorsImpl(importedAnchors);

        public static void ClearImportedAnchors() => _instance.ClearImportedAnchorsImpl();

        /// <summary>
        /// The anchors updated event.
        /// The first parameter is list list of anchors, the second is whether the anchors
        /// were imported from another device
        /// </summary>
        public static event Action<Anchor[], bool> OnAnchorsUpdatedEvent;

        private static AnchorsApi _instance;

        private static readonly ProfilerMarker OnQueryAnchorsCompletePerfMarker =
            new("AnchorsApi.OnQueryAnchorsComplete");
        private static readonly ProfilerMarker OnAnchorManagerAnchorsChangedPerfMarker =
            new ("AnchorsApi.OnAnchorManagerAnchorsChanged");

#if UNITY_ANDROID && !UNITY_EDITOR
        private const bool IsUnityAndroidAndNotEditor = true;
#else
        private const bool IsUnityAndroidAndNotEditor = false;
#endif

        private bool _isReady;
        private MLXrAnchorSubsystem _mlXrAnchorSubsystem;
        private MagicLeapSpatialAnchorsFeature _spatialAnchorsFeature;
        private MagicLeapSpatialAnchorsStorageFeature _spatialAnchorsStorageFeature;
        private Dictionary<string, Anchor> _activeAnchors;
        private Anchor[] _importedAnchors;
        private Dictionary<string, AnchorImpl> _arSubsystemPublishedAnchors = new();
        private YieldInstruction _waitForEndOfFrame = new WaitForEndOfFrame();

        public abstract class Anchor : IEquatable<Anchor>
        {
            /// <summary>
            /// The anchor's unique ID. This is a unique identifier for a single Spatial Anchor
            /// that is generated and managed by the Spatial Anchor system.
            /// </summary>
            public string Id;

            /// <summary>
            /// Pose.
            /// </summary>
            public Pose Pose;

            /// <summary>
            /// Indicates whether or not the anchor has been persisted via a call to
            /// PublishSpatialAnchorsToStorage.
            /// </summary>
            public bool IsPersisted;

            public abstract Anchor Clone();

            public static bool ArraysEqual(Anchor[] a, Anchor[] b)
            {
                if (a.Length != b.Length)
                {
                    return false;
                }

                for (int i = 0; i < a.Length; i++)
                {
                    if (!a[i].Equals(b[i]))
                    {
                        return false;
                    }
                }

                return true;
            }

            public bool Equals(Anchor other)
            {
                return Id == other.Id && Pose.Equals(other.Pose)
                                      && IsPersisted == other.IsPersisted;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((Anchor) obj);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Id, Pose, IsPersisted);
            }
        }

        public class AnchorImpl : Anchor
        {
            /// <summary>
            /// The associated ARAnchor component representing this anchor in the XR Anchor
            /// Subsystem. Used when creating and publishing an anchor. Can be null if this anchor
            /// already existed and was created from storage.
            /// </summary>
            public ARAnchor ArAnchor;

            public AnchorImpl(string id, Pose pose, bool isPersisted, ARAnchor arAnchor)
            {
                Id = id;
                Pose = pose;
                IsPersisted = isPersisted;
                ArAnchor = arAnchor;
            }

            public override Anchor Clone()
            {
                return new AnchorImpl(Id, Pose, IsPersisted, ArAnchor);
            }
        }

        /// <summary>
        /// A virtual anchor that was imported from a remote user.
        /// </summary>
        [Serializable]
        public class ImportedAnchor : Anchor
        {
            public override Anchor Clone()
            {
                return new ImportedAnchor
                {
                    Id = Id,
                    Pose = Pose,
                    IsPersisted = IsPersisted
                };
            }
        }

        private void Awake()
        {
            _instance = this;
        }

        private IEnumerator Start()
        {
            if (IsUnityAndroidAndNotEditor)
            {
                yield return new WaitUntil(AreOpenXRSubsystemsLoaded);
                yield return new WaitUntil(AreOpenXRFeaturesEnabled);

                _spatialAnchorsStorageFeature.OnQueryComplete += OnQueryAnchorsComplete;
                _anchorManager.anchorsChanged += OnAnchorManagerAnchorsChanged;
            }

            _isReady = true;
        }

        private bool AreOpenXRSubsystemsLoaded()
        {
            if (XRGeneralSettings.Instance == null ||
                XRGeneralSettings.Instance.Manager == null ||
                XRGeneralSettings.Instance.Manager.activeLoader == null)
            {
                return false;
            }
            _mlXrAnchorSubsystem = XRGeneralSettings.Instance.Manager.activeLoader
                .GetLoadedSubsystem<XRAnchorSubsystem>() as MLXrAnchorSubsystem;
            return _mlXrAnchorSubsystem != null;
        }

        private bool AreOpenXRFeaturesEnabled()
        {
            _spatialAnchorsFeature = OpenXRSettings.Instance.GetFeature<MagicLeapSpatialAnchorsFeature>();
            _spatialAnchorsStorageFeature = OpenXRSettings.Instance.GetFeature<MagicLeapSpatialAnchorsStorageFeature>();

            if (_spatialAnchorsFeature == null || !_spatialAnchorsFeature.enabled ||
                _spatialAnchorsStorageFeature == null || !_spatialAnchorsStorageFeature.enabled)
            {
                Debug.LogError("The OpenXR localization and/or spatial anchor features are not enabled.");
                return false;
            }
            return true;
        }

        private void OnDestroy()
        {
            if (IsUnityAndroidAndNotEditor && _isReady)
            {
                _spatialAnchorsStorageFeature.OnQueryComplete -= OnQueryAnchorsComplete;
                _anchorManager.anchorsChanged -= OnAnchorManagerAnchorsChanged;
            }
        }

        private void OnAnchorManagerAnchorsChanged(ARAnchorsChangedEventArgs eventArgs)
        {
            using (OnAnchorManagerAnchorsChangedPerfMarker.Auto())
            {
                List<ARAnchor> arAnchorsToPublish = null;

                // Check for newly added or updated anchors
                foreach (ARAnchor arAnchor in eventArgs.added.Concat(eventArgs.updated))
                {
                    if (_mlXrAnchorSubsystem.IsStoredAnchor(arAnchor))
                    {
                        string anchorMapPositionId =
                            _mlXrAnchorSubsystem.GetAnchorMapPositionId(arAnchor);
                        if (!_arSubsystemPublishedAnchors.ContainsKey(anchorMapPositionId))
                        {
                            AnchorImpl anchor = new(anchorMapPositionId,
                                _mlXrAnchorSubsystem.GetAnchorPose(arAnchor), true, arAnchor);
                            _arSubsystemPublishedAnchors[anchor.Id] = anchor;
                        }
                    }
                    else
                    {
                        if (arAnchorsToPublish == null)
                        {
                            arAnchorsToPublish = new();
                        }

                        arAnchorsToPublish.Add(arAnchor);
                    }
                }

                // Publish any non-published anchors
                if (_spatialAnchorsStorageFeature != null && arAnchorsToPublish != null)
                {
                    if (!_spatialAnchorsStorageFeature.PublishSpatialAnchorsToStorage(
                            arAnchorsToPublish, 0))
                    {
                        Debug.LogError("Failed to publish spatial anchors to storage");
                    }
                }

                // Check for newly removed Anchors.
                foreach (ARAnchor arAnchor in eventArgs.removed)
                {
                    string anchorIdToRemove = null;
                    foreach (var entry in _arSubsystemPublishedAnchors)
                    {
                        if (entry.Value.ArAnchor == arAnchor)
                        {
                            anchorIdToRemove = entry.Key;
                        }
                    }

                    if (anchorIdToRemove != null)
                    {
                        _arSubsystemPublishedAnchors.Remove(anchorIdToRemove);
                    }
                }
            }
        }

        private void OnQueryAnchorsComplete(List<string> anchorMapPositionIds)
        {
            using (OnQueryAnchorsCompletePerfMarker.Auto())
            {
                List<string> trackedAnchorMapPositionIds = new();
                List<string> deletedAnchorIds = new();

                // Process current anchors: update poses, look for expired anchors.
                foreach (AnchorImpl anchor in _arSubsystemPublishedAnchors.Values)
                {
                    if (anchor.ArAnchor == null)
                    {
                        deletedAnchorIds.Add(anchor.Id);
                        continue;
                    }

                    // Store the anchorMapPositionId to check for new anchors below.
                    trackedAnchorMapPositionIds.Add(anchor.Id);

                    if (anchorMapPositionIds.Contains(anchor.Id))
                    {
                        anchor.Pose = _mlXrAnchorSubsystem.GetAnchorPose(anchor.ArAnchor);
                        anchor.ArAnchor.transform.SetWorldPose(anchor.Pose);
                    }
                    else
                    {
                        Destroy(anchor.ArAnchor.gameObject);
                    }
                }

                // Remove anchors that were deleted
                foreach (string anchorId in deletedAnchorIds)
                {
                    _arSubsystemPublishedAnchors.Remove(anchorId);
                }

                // Check for new stored anchors
                IEnumerable<string> newAnchors = anchorMapPositionIds.Except(
                    trackedAnchorMapPositionIds);

                if (newAnchors.Any())
                {
                    if (!_spatialAnchorsStorageFeature.CreateSpatialAnchorsFromStorage(
                            newAnchors.ToList()))
                    {
                        Debug.LogError("Failed to create new anchors from query.");
                    }
                }
                else
                {
                    if (MaybeUpdateActiveAnchors(_arSubsystemPublishedAnchors.Values))
                    {
                        StartCoroutine(CallActionAtEndOfFrame(() =>
                            OnAnchorsUpdatedEvent?.Invoke(_activeAnchors.Values.ToArray(), false)));
                    }
                }
            }
        }

        private bool QueryAnchorsImpl()
        {
            if (_importedAnchors != null)
            {
                if (MaybeUpdateActiveAnchors(_importedAnchors))
                {
                    StartCoroutine(CallActionAtEndOfFrame(() =>
                        OnAnchorsUpdatedEvent?.Invoke(_activeAnchors.Values.ToArray(), true)));
                }
                return true;
            }

            if (!IsUnityAndroidAndNotEditor)
            {
                // Return a default anchor for non-ML2 applications.
                if (MaybeUpdateActiveAnchors(new [] { _defaultAnchor }))
                {
                    StartCoroutine(CallActionAtEndOfFrame(() =>
                        OnAnchorsUpdatedEvent?.Invoke(_activeAnchors.Values.ToArray(), false)));
                }
                return true;
            }

            if (!_isReady)
            {
                return false;
            }

            return _spatialAnchorsStorageFeature.QueryStoredSpatialAnchors(
                Camera.main.transform.position, 0f);
        }

        private bool MaybeUpdateActiveAnchors(IEnumerable<Anchor> newAnchors)
        {
            bool anchorsChanged = false;

            if (_activeAnchors == null)
            {
                _activeAnchors = new();
                anchorsChanged = true;
            }

            HashSet<string> newAnchorIds = new();
            foreach (Anchor newAnchor in newAnchors)
            {
                var anchorId = newAnchor.Id;
                newAnchorIds.Add(anchorId);
                _activeAnchors.TryGetValue(anchorId, out Anchor anchor);
                if (!System.Object.Equals(newAnchor, anchor))
                {
                    _activeAnchors[anchorId] = newAnchor.Clone();
                    anchorsChanged = true;
                }
            }

            foreach (var anchorId in _activeAnchors.Keys.ToArray())
            {
                if (!newAnchorIds.Contains(anchorId))
                {
                    _activeAnchors.Remove(anchorId);
                    anchorsChanged = true;
                }
            }

            return anchorsChanged;
        }

        private bool CreateAnchorImpl(Pose pose)
        {
            if (!IsUnityAndroidAndNotEditor)
            {
                return false;
            }

            if (_spatialAnchorsStorageFeature != null)
            {
                var pendingAnchorObject = new GameObject("Pending Anchor");
                pendingAnchorObject.transform.SetParent(transform);
                // The object transform must be set to the desired pose before adding ARAnchor
                pendingAnchorObject.transform.SetPositionAndRotation(pose.position, pose.rotation);
                ARAnchor arAnchor = pendingAnchorObject.AddComponent<ARAnchor>();

                return true;
            }

            return false;
        }

        private IEnumerator CallActionAtEndOfFrame(Action action)
        {
            yield return _waitForEndOfFrame;
            action();
        }

        private void SetImportedAnchorsImpl(Anchor[] importedAnchors)
        {
            lock (this)
            {
                _importedAnchors = importedAnchors;
            }
        }

        private void ClearImportedAnchorsImpl()
        {
            lock (this)
            {
                _importedAnchors = null;
            }
        }
    }
}