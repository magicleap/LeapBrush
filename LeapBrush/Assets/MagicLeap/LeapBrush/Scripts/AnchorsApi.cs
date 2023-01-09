using System;
using System.IO;
using UnityEngine;
using UnityEngine.XR.MagicLeap;

namespace MagicLeap.LeapBrush
{
    [RequireComponent(typeof(AnchorsApiFake))]
    public class AnchorsApi : MonoBehaviour
    {
#pragma warning disable CS0414
        [SerializeField]
        private bool _useFakeData = false;
#pragma warning restore CS0414

        private AnchorsApiImplBase _apiImpl = null;

        private static AnchorsApi _instance;

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

            public string SpaceId;

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

        private void Awake()
        {
            _instance = this;

#if UNITY_EDITOR || !UNITY_ANDROID
            // When running from the unity editor or on a non-magicleap device, fake data and the
            // fake api must be used.
            _apiImpl = GetComponent<AnchorsApiFake>();
            _useFakeData = true;
#else
            // On a magic-leap device, use a unity property to determine whether to use the fake
            // api and fake data. This allows a special version of the app to be built for
            // side-loading.
            _useFakeData |= UseFakeDataRuntimeOverride();
            if (_useFakeData)
            {
                _apiImpl = GetComponent<AnchorsApiFake>();
            }
            else
            {
                _apiImpl = gameObject.AddComponent<AnchorsApiImpl>();
            }
#endif

            _apiImpl.Create();
        }

        private bool UseFakeDataRuntimeOverride()
        {
            string isSpectatorFilePath = Path.Join(
                Application.persistentDataPath, "isSpectator.txt");

            try
            {
                // TODO(ghazen): Inefficient on main thread.
                bool isSpectator = File.Exists(isSpectatorFilePath);
                Debug.LogFormat("AnchorsApi: use fake data override (create path {0}) = {1}",
                    isSpectatorFilePath, isSpectator);
                return isSpectator;
            }
            catch (IOException e)
            {
                Debug.LogException(e);
                return false;
            }
        }

        private void OnDestroy()
        {
            _apiImpl.Destroy();
        }

        private static AnchorsApi Instance
        {
            get
            {
                return _instance;
            }
        }

        public static bool UseFakeData => Instance._useFakeData;

        public static AnchorsApiFake TryGetFakeApi()
        {
            return Instance._apiImpl as AnchorsApiFake;
        }

        public static MLResult GetLocalizationInfo(out LocalizationInfo info) =>
            Instance._apiImpl.GetLocalizationInfo(out info);

        public static MLResult QueryAnchors(out Anchor[] anchors) =>
            Instance._apiImpl.QueryAnchors(out anchors);

        public static MLResult CreateAnchor(Pose pose, ulong expirationTimeStamp, out Anchor anchor) =>
            Instance._apiImpl.CreateAnchor(pose, expirationTimeStamp, out anchor);
    }
}