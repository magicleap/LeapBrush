using System;
using UnityEngine;

namespace MagicLeap.LeapBrush
{
    /// <summary>
    /// The eraser tool.
    /// </summary>
    public class EraserTool : MonoBehaviour
    {
        public event Action<EraserTool, Collider> OnTriggerEnterEvent;

        /// <summary>
        /// Unity event handler for a collision trigger.
        /// </summary>
        /// <param name="other">The collider triggering the collision.</param>
        private void OnTriggerEnter(Collider other)
        {
            OnTriggerEnterEvent?.Invoke(this, other);
        }
    }
}