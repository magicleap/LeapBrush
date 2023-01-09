using UnityEngine;
using UnityEngine.XR.MagicLeap;

namespace MagicLeap.LeapBrush
{
    public abstract class AnchorsApiImplBase : MonoBehaviour
    {
        public abstract void Create();

        public abstract void Destroy();

        public abstract MLResult GetLocalizationInfo(out AnchorsApi.LocalizationInfo info);

        public abstract MLResult QueryAnchors(out AnchorsApi.Anchor[] anchors);

        public abstract MLResult CreateAnchor(Pose pose, ulong expirationTimeStamp, out AnchorsApi.Anchor anchor);
    }
}