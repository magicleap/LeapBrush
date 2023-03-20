using UnityEngine;
using System;

namespace MagicLeap.LeapBrush
{
    /// <summary>
    /// Component managing the display of other user's Controllers present in the area.
    /// </summary>
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