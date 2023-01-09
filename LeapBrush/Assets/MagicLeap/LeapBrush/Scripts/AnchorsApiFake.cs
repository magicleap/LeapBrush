using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.MagicLeap;

namespace MagicLeap.LeapBrush
{
    public class AnchorsApiFake : AnchorsApiImplBase
    {
        [Serializable]
        public class FakeAnchor : AnchorsApi.Anchor
        {
            private static string FakeSpace = "SPACE_0";

            public FakeAnchor()
            {
                Id = "FAKE_ANCHOR_" + (_nextAnchorId++);
                SpaceId = FakeSpace;
                Pose = Pose.identity;
                ExpirationTimeStamp = (ulong) DateTimeOffset.Now.AddYears(1).ToUnixTimeMilliseconds();
                IsPersisted = false;
            }

            public override MLResult Publish()
            {
                IsPersisted = true;
                return MLResult.Create(MLResult.Code.Ok);
            }
        }

        [SerializeField]
        private List<FakeAnchor> _anchors = new();

        [SerializeField]
        private AnchorsApi.LocalizationInfo _localizationInfo = new AnchorsApi.LocalizationInfo(
            MLAnchors.LocalizationStatus.Localized, MLAnchors.MappingMode.ARCloud, "TEST_SPACE_0", "{98765-0}",
            new Pose());

        private static int _nextAnchorId = 0;

        public override void Create()
        {
        }

        public override void Destroy()
        {
        }

        public void SetFakeAnchors(List<FakeAnchor> anchors)
        {
            _anchors = anchors;
        }

        public override MLResult GetLocalizationInfo(out AnchorsApi.LocalizationInfo info)
        {
            info = _localizationInfo.Clone();
            return MLResult.Create(MLResult.Code.Ok);
        }

        public override MLResult QueryAnchors(out AnchorsApi.Anchor[] anchors)
        {
            anchors = _anchors.ToArray();
            return MLResult.Create(MLResult.Code.Ok);
        }

        public override MLResult CreateAnchor(Pose pose, ulong expirationTimeStamp, out AnchorsApi.Anchor anchor)
        {
            FakeAnchor fakeAnchor = new FakeAnchor();
            _anchors.Add(fakeAnchor);

            anchor = fakeAnchor;
            return  MLResult.Create(MLResult.Code.Ok);
        }

        public void ImportAnchors(IList<AnchorProto> foundAnchors)
        {
            _anchors.Clear();

            foreach (var anchorProto in foundAnchors)
            {
                _anchors.Add(new FakeAnchor
                {
                    Id = anchorProto.Id,
                    Pose = ProtoUtils.FromProto(anchorProto.Pose)
                });
            }
        }
    }
}