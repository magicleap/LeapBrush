using System;
using UnityEngine;

namespace MagicLeap.LeapBrush
{
    public class BasePopup : MonoBehaviour, IPopup
    {
        public event Action<IPopup, bool> OnShownChanged;

        public bool IsShown => gameObject.activeSelf;

        public virtual void Show()
        {
            if (gameObject.activeSelf)
            {
                return;
            }
            gameObject.SetActive(true);
            OnShownChanged?.Invoke(this, true);
        }

        public virtual void Hide()
        {
            if (!gameObject.activeSelf)
            {
                return;
            }
            gameObject.SetActive(false);
            OnShownChanged?.Invoke(this, false);
        }
    }
}