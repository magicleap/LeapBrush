using UnityEngine;
using System;

namespace MagicLeap.LeapBrush
{
    public class OtherUserController : MonoBehaviour
    {
        public DateTimeOffset LastUpdateTime = DateTimeOffset.Now;

        public event Action OnDestroyed;

        public void OnDestroy()
        {
            OnDestroyed?.Invoke();
        }
    }
}