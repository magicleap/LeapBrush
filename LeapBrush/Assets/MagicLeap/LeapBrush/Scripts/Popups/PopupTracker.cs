using System;
using System.Collections.Generic;
using UnityEngine;

namespace MagicLeap.LeapBrush
{
    public class PopupTracker : MonoBehaviour
    {
        public event Action<bool> OnPopupsShownChanged;

        public bool PopupsShown => _popupsShown;

        private bool _popupsShown;
        private readonly Dictionary<IPopup, bool> _popupsShownMap = new();

        public void TrackPopup(IPopup popup)
        {
            if (!_popupsShownMap.TryAdd(popup, popup.IsShown))
            {
                throw new Exception("Popup already tracked");
            }

            popup.OnShownChanged += OnPopupShownChanged;
        }

        private void OnDisable()
        {
            foreach (var entry in _popupsShownMap)
            {
                entry.Key.OnShownChanged -= OnPopupShownChanged;
            }
            _popupsShownMap.Clear();
            _popupsShown = false;
        }

        private void OnPopupShownChanged(IPopup popup, bool isShown)
        {
            _popupsShownMap[popup] = isShown;

            bool popupsShown = false;
            foreach (var entry in _popupsShownMap)
            {
                popupsShown = popupsShown || entry.Value;
            }

            if (_popupsShown == popupsShown)
            {
                return;
            }

            _popupsShown = popupsShown;
            OnPopupsShownChanged?.Invoke(_popupsShown);
        }
    }
}