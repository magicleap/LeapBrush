using System;
using System.Collections.Generic;
using UnityEngine;

namespace MagicLeap.LeapBrush
{
    /// <summary>
    /// The eraser tool.
    /// </summary>
    public class EraserTool : MonoBehaviour
    {
        public delegate void OnCollisionEnterDelegate(Collider other);

        public event OnCollisionEnterDelegate OnTriggerEnterEvent;

        /// <summary>
        /// Unity event handler for a collision trigger.
        /// </summary>
        /// <param name="other">The collider triggering the collision.</param>
        private void OnTriggerEnter(Collider other)
        {
            OnTriggerEnterEvent?.Invoke(other);
        }
    }
}