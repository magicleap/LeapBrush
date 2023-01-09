using System;
using System.Collections.Generic;
using UnityEngine;

namespace MagicLeap.LeapBrush
{
    public class EraserTool : MonoBehaviour
    {
        public delegate void OnCollisionEnterDelegate(Collider other);

        public event OnCollisionEnterDelegate OnTriggerEnterEvent;

        private void OnTriggerEnter(Collider other)
        {
            OnTriggerEnterEvent?.Invoke(other);
        }
    }
}