using UnityEngine;

namespace MagicLeap.LeapBrush
{
    /// <summary>
    /// Visualization of a snap point where the user could join or stop a polygon drawing by
    /// clicking.
    /// </summary>
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