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

        private Anchor[] _importedAnchors;

        [Serializable]
        public struct LocalizationInfo
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
                this.LocalizationStatus = localizationStatus;
                this.MappingMode = mappingMode;
                this.SpaceName = spaceName;
                this.SpaceId = spaceId;
                this.TargetSpaceOriginPose = targetSpaceOriginPose;
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

            public override string ToString() => $"LocalizationStatus: {this.LocalizationStatus}, MappingMode: {this.MappingMode},\nSpaceName: {this.SpaceName}, SpaceId: {this.SpaceId}";
        }

        public abstract class Anchor
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

            public abstract MLResult Publish();
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
        }

        public static MLResult GetLocalizationInfo(out LocalizationInfo info) =>
            _instance.GetLocalizationInfoImpl(out info);

        public static MLResult QueryAnchors(out Anchor[] anchors, out bool isUsingImportedAnchors)
            => _instance.QueryAnchorsImpl(out anchors, out isUsingImportedAnchors);

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
                _mlAnchor = mlAnchor;
                Id = _mlAnchor.Id;
                SpaceId = _mlAnchor.SpaceId;
                Pose = mlAnchor.Pose;
                ExpirationTimeStamp = mlAnchor.ExpirationTimeStamp;
                IsPersisted = mlAnchor.IsPersisted;
            }

            public override MLResult Publish()
            {
                return _mlAnchor.Publish();
            }
        }

        private void Awake()
        {
            _instance = this;
        }

        private MLResult GetLocalizationInfoImpl(out LocalizationInfo info)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            MLAnchors.LocalizationInfo mlInfo;
            MLResult result = MLAnchors.GetLocalizationInfo(out mlInfo);
            info = new AnchorsApi.LocalizationInfo(mlInfo.LocalizationStatus, mlInfo.MappingMode,
                mlInfo.SpaceName, mlInfo.SpaceId, mlInfo.SpaceOrigin);
            return result;
#else
            info = new LocalizationInfo(
                MLAnchors.LocalizationStatus.NotLocalized, MLAnchors.MappingMode.OnDevice,
                "Default", "DEFAULT_SPACE_ID", Pose.identity);
            return MLResult.Create(MLResult.Code.Ok);
#endif
        }

        private MLResult QueryAnchorsImpl(out Anchor[] anchors, out bool isUsingImportedAnchors)
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
            anchors = Array.Empty<Anchor>();

            MLAnchors.Request request = new MLAnchors.Request();
            MLAnchors.Request.Params queryParams = new MLAnchors.Request.Params();
            MLResult result = request.Start(queryParams);
            if (!result.IsOk)
            {
                return result;
            }

            MLAnchors.Request.Result resultData;
            result = request.TryGetResult(out resultData);
            if (result.IsOk)
            {
                anchors = new Anchor[resultData.anchors.Length];
                for (int i = 0; i < anchors.Length; ++i)
                {
                    anchors[i] = new AnchorImpl(resultData.anchors[i]);
                }
            }
            return result;
#else
            // Return a default anchor for non-ML2 applications.
            anchors = new Anchor[]
            {
                new ImportedAnchor
                {
                    Id = "DEFAULT_ANCHOR_ID",
                    Pose = Pose.identity
                }
            };
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