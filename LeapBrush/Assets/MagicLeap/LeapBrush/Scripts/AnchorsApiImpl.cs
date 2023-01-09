using UnityEngine;
using UnityEngine.XR.MagicLeap;

#if UNITY_ANDROID

namespace MagicLeap.LeapBrush
{
    public class AnchorsApiImpl : AnchorsApiImplBase
    {
        private class AnchorImpl : AnchorsApi.Anchor
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

        public override void Create()
        {
        }

        public override void Destroy()
        {
        }

        public override MLResult GetLocalizationInfo(out AnchorsApi.LocalizationInfo info)
        {
            MLAnchors.LocalizationInfo mlInfo;
            MLResult result = MLAnchors.GetLocalizationInfo(out mlInfo);
            info = new AnchorsApi.LocalizationInfo(mlInfo.LocalizationStatus, mlInfo.MappingMode,
                mlInfo.SpaceName, mlInfo.SpaceId, mlInfo.SpaceOrigin);
            return result;
        }

        public override MLResult QueryAnchors(out AnchorsApi.Anchor[] anchors)
        {
            anchors = new AnchorsApi.Anchor[0];

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
                anchors = new AnchorsApi.Anchor[resultData.anchors.Length];
                for (int i = 0; i < anchors.Length; ++i)
                {
                    anchors[i] = new AnchorImpl(resultData.anchors[i]);
                }
            }
            return result;
        }

        public override MLResult CreateAnchor(Pose pose, ulong expirationTimeStamp, out AnchorsApi.Anchor anchor)
        {
            MLAnchors.Anchor mlAnchor;
            MLResult result = MLAnchors.Anchor.Create(pose, (long) expirationTimeStamp, out mlAnchor);
            if (!result.IsOk)
            {
                anchor = null;
                return result;
            }

            anchor = new AnchorImpl(mlAnchor);
            return result;
        }
    }
}

#endif