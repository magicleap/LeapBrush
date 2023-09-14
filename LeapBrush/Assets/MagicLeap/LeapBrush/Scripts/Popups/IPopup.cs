using System;

namespace MagicLeap.LeapBrush
{
    public interface IPopup
    {
        public event Action<IPopup, bool> OnShownChanged;

        public bool IsShown { get; }
    }
}