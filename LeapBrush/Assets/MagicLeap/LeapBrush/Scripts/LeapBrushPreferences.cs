using System;
using UnityEngine;

namespace MagicLeap.LeapBrush
{
    public class LeapBrushPreferences : MonoBehaviour
    {
        public BoolPref ShowSpatialAnchors = new(PrefShowSpatialAnchors, false);
        public BoolPref ShowOrigins = new(PrefShowOrigins, false);
        public BoolPref ShowOtherHeadsets = new(PrefShowOtherHeadsets, !IsUnityAndroid);
        public BoolPref ShowOtherHandsAndControls = new(PrefShowOtherHandsAndControls, true);
        public BoolPref ShowFloorGrid = new(PrefShowFloorGrid, !IsUnityAndroid);
        public BoolPref ShowSpaceMesh = new(PrefShowSpaceMesh, !IsUnityAndroid);

        public BoolPref HandLasersEnabled = new(PrefKeyHandLasersEnabled, true);
        public BoolPref HandToolsEnabled = new(PrefKeyHandToolsEnabled, true);
        public BoolPref GazePinchEnabled = new(PrefKeyGazePinchEnabled, false);

        public BoolPref PhoneSpecatorEnabled = new(PrefPhoneSpectatorEnabled, false);

        public interface IPref
        {
            public string Key { get; }

            public void ResetToDefault();
        }

        public abstract class Pref<T> : IPref where T : IEquatable<T>
        {
            public string Key => _key;

            public T Value
            {
                get
                {
                    if (!_valueCached)
                    {
                        _value = loadPlayerPref();
                        _valueCached = true;
                    }

                    return _value;
                }
                set
                {
                    if (!Value.Equals(value))
                    {
                        _value = value;
                        storePlayerPref(value);
                        OnChanged?.Invoke();
                    }
                }
            }

            public event Action OnChanged;

            protected readonly string _key;
            protected readonly T _defaultValue;

            private bool _valueCached;

            private T _value;

            public Pref(string key, T defaultValue)
            {
                _key = key;
                _defaultValue = defaultValue;
            }

            public void ResetToDefault()
            {
                PlayerPrefs.DeleteKey(_key);
                if (_valueCached && !_value.Equals(_defaultValue))
                {
                    _value = _defaultValue;
                    _valueCached = false;
                    OnChanged?.Invoke();
                }
            }

            protected abstract void storePlayerPref(T value);
            protected abstract T loadPlayerPref();
        }

        [Serializable]
        public class BoolPref : Pref<bool>
        {
            public BoolPref(string key, bool defaultValue) : base(key, defaultValue)
            {
            }

            protected override void storePlayerPref(bool value)
            {
                PlayerPrefs.SetInt(_key, value ? 1 : 0);
            }

            protected override bool loadPlayerPref()
            {
                return PlayerPrefs.GetInt(_key, _defaultValue ? 1 : 0) != 0;
            }

            public override string ToString()
            {
                if (Value != _defaultValue)
                {
                    return $"BoolPref<value={Value}, default={_defaultValue}>";
                }
                else
                {
                    return $"BoolPref<default={Value}>";
                }
            }
        }

        private const string PrefShowSpatialAnchors = "ShowSpatialAnchors";
        private const string PrefShowOrigins = "ShowOrigins";
        private const string PrefShowOtherHeadsets = "ShowOtherHeadsets";
        private const string PrefShowOtherHandsAndControls = "ShowOtherHandsAndControls";
        private const string PrefShowFloorGrid = "ShowFloorGrid";
        private const string PrefShowSpaceMesh = "ShowSpaceMesh";
        private const string PrefKeyGazePinchEnabled = "GazePinchEnabled";
        private const string PrefKeyHandLasersEnabled = "HandLasersEnabled";
        private const string PrefKeyHandToolsEnabled = "HandToolsEnabled";
        private const string PrefPhoneSpectatorEnabled = "PhoneSpectatorEnabled";

#if UNITY_ANDROID
        private const bool IsUnityAndroid = true;
#else
        private const bool IsUnityAndroid = false;
#endif

        private BoolPref[] _prefs;

        public void ResetToDefaults()
        {
            foreach (IPref pref in GetPrefs())
            {
                pref.ResetToDefault();
            }
        }

        private IPref[] GetPrefs()
        {
            if (_prefs == null)
            {
                _prefs = new[]
                {
                    ShowSpatialAnchors,
                    ShowOrigins,
                    ShowOtherHeadsets,
                    ShowOtherHandsAndControls,
                    ShowFloorGrid,
                    ShowSpaceMesh,
                    HandLasersEnabled,
                    HandToolsEnabled,
                    GazePinchEnabled,
                    PhoneSpecatorEnabled
                };
            }

            return _prefs;
        }
    }
}