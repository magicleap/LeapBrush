using System;
using UnityEngine;
using UnityEngine.XR.MagicLeap;

namespace MagicLeap.LeapBrush
{
    /// <summary>
    /// Wrapper for the MLAnchors api to support a few additional use cases, e.g. Anchor poses
    /// being imported from another user.
    /// </summary>
    public class AnchorsApi : MonoBehaviour
    {
        private static AnchorsApi _instance;

        [SerializeField]
        private ImportedAnchor _defaultAnchor;

        [SerializeField]
        private LocalizationInfo _defaultLocalizationInfo;

        private Anchor[] _importedAnchors;

        [Serializable]
        public struct LocalizationInfo : IEquatable<LocalizationInfo>
        {
            /// <summary>
            /// The localization status at the time this structure was returned.
            /// </summary>
            public MLAnchors.LocalizationStatus LocalizationStatus;

            /// <summary>
            /// The current mapping mode.
            /// </summary>
            public MLAnchors.MappingMode MappingMode;

            /// <summary>
            /// If localized, this will contain the name of the current space.
            /// </summary>
            public string SpaceName;

            /// <summary>
            /// If localized, the identifier of the space.
            /// </summary>
            public string SpaceId;

            /// <summary>
            /// The space origin for the purposes of 3D mesh alignment, etc.
            /// </summary>
            public Pose TargetSpaceOriginPose;

            public LocalizationInfo(MLAnchors.LocalizationStatus localizationStatus, MLAnchors.MappingMode mappingMode,
                string spaceName, string spaceId, Pose targetSpaceOriginPose)
            {
                LocalizationStatus = localizationStatus;
                MappingMode = mappingMode;
                SpaceName = spaceName;
                SpaceId = spaceId;
                TargetSpaceOriginPose = targetSpaceOriginPose;
            }

            public LocalizationInfo Clone()
            {
                return new LocalizationInfo
                {
                    LocalizationStatus = LocalizationStatus,
                    MappingMode = MappingMode,
                    SpaceName = SpaceName,
                    SpaceId = SpaceId,
                    TargetSpaceOriginPose = TargetSpaceOriginPose
                };
            }

            public override string ToString() => $"LocalizationStatus: {LocalizationStatus}, MappingMode: {MappingMode},\nSpaceName: {SpaceName}, SpaceId: {SpaceId}";

            public bool Equals(LocalizationInfo other)
            {
                return LocalizationStatus == other.LocalizationStatus
                       && MappingMode == other.MappingMode
                       && SpaceName == other.SpaceName
                       && SpaceId == other.SpaceId
                       && TargetSpaceOriginPose.Equals(other.TargetSpaceOriginPose);
            }

            public override bool Equals(object obj)
            {
                return obj is LocalizationInfo other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine((int) LocalizationStatus,
                    (int) MappingMode, SpaceName, SpaceId, TargetSpaceOriginPose);
            }

            public void CopyFrom(LocalizationInfo other)
            {
                LocalizationStatus = other.LocalizationStatus;
                MappingMode = other.MappingMode;
                SpaceName = other.SpaceName;
                SpaceId = other.SpaceId;
                TargetSpaceOriginPose = other.TargetSpaceOriginPose;
            }

            public void CopyFrom(MLAnchors.LocalizationInfo other)
            {
                LocalizationStatus = other.LocalizationStatus;
                MappingMode = other.MappingMode;
                SpaceName = other.SpaceName;
                SpaceId = other.SpaceId;
                TargetSpaceOriginPose = other.SpaceOrigin;
            }
        }

        public abstract class Anchor : IEquatable<Anchor>
        {
            /// <summary>
            /// The anchor's unique ID.  This is a unique identifier for a single Spatial Anchor that is generated and managed by the
            /// Spatial Anchor system.  The ID is created when MLSpatialAnchorCreateSpatialAnchor is called.
            /// </summary>
            public string Id;

            /// <summary>
            /// The ID of the space that this anchor belongs to. This is only relevant if IsPersisted is true.
            /// </summary>
            public string SpaceId;

            /// <summary>
            /// Pose.
            /// </summary>
            public Pose Pose;

            /// <summary>
            /// The suggested expiration time for this anchor represented in seconds since the Unix epoch.  This is implemented as an
            /// expiration timestamp in the future after which the associated anchor should be considered no longer valid and may be
            /// removed by the Spatial Anchor system.
            /// </summary>
            public ulong ExpirationTimeStamp;

            /// <summary>
            /// Indicates whether or not the anchor has been persisted via a call to #MLSpatialAnchorPublish.
            /// </summary>
            public bool IsPersisted;

            /// <summary>
            /// Publish this anchor so it persists across sessions.
            /// </summary>
            public abstract MLResult Publish();

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
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return Id == other.Id && SpaceId == other.SpaceId && Pose.Equals(other.Pose)
                       && ExpirationTimeStamp == other.ExpirationTimeStamp
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
                return HashCode.Combine(Id, SpaceId, Pose, ExpirationTimeStamp, IsPersisted);
            }
        }

        /// <summary>
        /// A virtual anchor that was imported from a remote user.
        /// </summary>
        [Serializable]
        public class ImportedAnchor : Anchor
        {
            public ImportedAnchor()
            {
                ExpirationTimeStamp = (ulong)
                    DateTimeOffset.Now.AddYears(1).ToUnixTimeMilliseconds();
            }

            public override MLResult Publish()
            {
                return MLResult.Create(MLResult.Code.NotImplemented);
            }

            public override Anchor Clone()
            {
                return new ImportedAnchor
                {
                    Id = Id,
                    SpaceId = SpaceId,
                    Pose = Pose,
                    ExpirationTimeStamp = ExpirationTimeStamp,
                    IsPersisted = IsPersisted
                };
            }

            public void CopyFrom(ImportedAnchor other)
            {
                Id = other.Id;
                SpaceId = other.SpaceId;
                Pose = other.Pose;
                ExpirationTimeStamp = other.ExpirationTimeStamp;
                IsPersisted = other.IsPersisted;
            }
        }

        public static MLResult GetLocalizationInfo(ref LocalizationInfo info) =>
            _instance.GetLocalizationInfoImpl(ref info);

        public static MLResult QueryAnchors(ref Anchor[] anchors, out bool isUsingImportedAnchors)
            => _instance.QueryAnchorsImpl(ref anchors, out isUsingImportedAnchors);

        public static MLResult CreateAnchor(Pose pose, ulong expirationTimeStamp,
            out Anchor anchor) =>
            _instance.CreateAnchorImpl(pose, expirationTimeStamp, out anchor);

        public static void SetImportedAnchors(Anchor[] importedAnchors) =>
            _instance.SetImportedAnchorsImpl(importedAnchors);

        public static void ClearImportedAnchors() => _instance.ClearImportedAnchorsImpl();

        private class AnchorImpl : Anchor
        {
            private MLAnchors.Anchor _mlAnchor;

            public AnchorImpl(MLAnchors.Anchor mlAnchor)
            {
                CopyFrom(mlAnchor);
            }

            public override MLResult Publish()
            {
                return _mlAnchor.Publish();
            }

            public override Anchor Clone()
            {
                return new AnchorImpl(_mlAnchor);
            }

            public void CopyFrom(MLAnchors.Anchor other)
            {
                _mlAnchor = other;
                Id = other.Id;
                SpaceId = other.SpaceId;
                Pose = other.Pose;
                ExpirationTimeStamp = other.ExpirationTimeStamp;
                IsPersisted = other.IsPersisted;
            }
        }

        private void Awake()
        {
            _instance = this;
        }

        private MLResult GetLocalizationInfoImpl(ref LocalizationInfo info)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            MLAnchors.LocalizationInfo mlInfo;
            MLResult result = MLAnchors.GetLocalizationInfo(out mlInfo);
            if (result.IsOk)
            {
                info.CopyFrom(mlInfo);
            }
            return result;
#else
            info.CopyFrom(_defaultLocalizationInfo);
            return MLResult.Create(MLResult.Code.Ok);
#endif
        }

        private MLResult QueryAnchorsImpl(ref Anchor[] anchors, out bool isUsingImportedAnchors)
        {
            lock (this)
            {
                if (_importedAnchors != null)
                {
                    anchors = _importedAnchors;
                    isUsingImportedAnchors = true;
                    return MLResult.Create(MLResult.Code.Ok);
                }
            }

            isUsingImportedAnchors = false;

#if UNITY_ANDROID && !UNITY_EDITOR
            MLAnchors.Request request = new MLAnchors.Request();
            MLAnchors.Request.Params queryParams = new MLAnchors.Request.Params();
            MLResult result = request.Start(queryParams);
            if (!result.IsOk)
            {
                anchors = Array.Empty<Anchor>();
                return result;
            }

            MLAnchors.Request.Result resultData;
            result = request.TryGetResult(out resultData);
            if (result.IsOk)
            {
                if (anchors.Length != resultData.anchors.Length)
                {
                    anchors = new Anchor[resultData.anchors.Length];
                }
                for (int i = 0; i < anchors.Length; ++i)
                {
                    if (anchors[i] is AnchorImpl existingAnchor)
                    {
                        existingAnchor.CopyFrom(resultData.anchors[i]);
                    }
                    else
                    {
                        anchors[i] = new AnchorImpl(resultData.anchors[i]);
                    }
                }
            }
            return result;
#else
            // Return a default anchor for non-ML2 applications.
            if (anchors.Length != 1)
            {
                anchors = new Anchor[1];
            }

            if (anchors[0] is ImportedAnchor existingAnchor)
            {
                existingAnchor.CopyFrom(_defaultAnchor);
            }
            else
            {
                anchors[0] = _defaultAnchor.Clone();
            }
            return MLResult.Create(MLResult.Code.Ok);
#endif
        }

        private MLResult CreateAnchorImpl(Pose pose, ulong expirationTimeStamp, out Anchor anchor)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            MLAnchors.Anchor mlAnchor;
            MLResult result = MLAnchors.Anchor.Create(pose, (long) expirationTimeStamp, out mlAnchor);
            if (!result.IsOk)
            {
                anchor = null;
                return result;
            }

            anchor = new AnchorImpl(mlAnchor);
            return result;
#else
            anchor = null;
            return MLResult.Create(MLResult.Code.NotImplemented);
#endif
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