using UnityEngine;

namespace MagicLeap.LeapBrush
{
    public class PolyBrushSnapVisualization : MonoBehaviour
    {
        public float TargetRadius
        {
            set
            {
                if (Mathf.Abs(value - _radius) > RadiusEpsilon)
                {
                    _radius = value;
                    transform.localScale = Vector3.one * (_radius * 2.0f);
                }
            }
        }

        private float _radius;

        private const float RadiusEpsilon = 0.0001f;
    }
}