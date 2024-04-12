using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MagicLeap.DesignToolkit.Keyboard
{
    [System.Serializable]
    public class RectTransformSettings
    {
        public bool ChangePos;
        public bool ChangeAnchors;
        public bool ChangePivot;
        public Vector2 AnchorMin;
        public Vector2 AnchorMax;
        public Vector2 Pivot;
        public Vector2 AnchoredPos;
        public Vector3 Scale;
    }

    [System.Serializable]
    public class IconSettings
    {
        public Sprite IconSpriteOn;
        public Sprite IconSpriteOff;
        public Color IconColorOn = Color.black;
        public Color IconColorOff = Color.white;
        public RectTransformSettings IconRectSettings;
    }

    [System.Serializable]
    public class KeyStyle
    {
        public Sprite FillSpriteOn = null;
        public Sprite FillSpriteOff = null;
        public Color MrtkBackplateColorOn;
        public Color MrtkBackplateColorOff;
    }

    [System.Serializable]
    public class KeyBuilderSettings
    {
        public LayoutInfo LayoutInfo;
        public bool LabelAndTypedTextIsSame = false;
        public bool UseDefaultCharAsLabel = false;
        public string Label = "";
        public string TextToType = "";
        public TMP_FontAsset FontAsset = null;
        public KeyStyle KeyButtonStyle = null;
        public Color TextColorOff = Color.white;
        public Color TextColorOn = Color.black;
        public RectTransformSettings LabelRectSettings;
        public RectTransformSettings SecondaryRectSettings;
        public float FontSizeMin;
        public float FontSizeMax;
        public Vector4 LabelMargins;
        public Vector4 SecondaryMargins;
        public List<IconSettings> SettingsForIcons;
        public bool AddShiftKeyBehavior = false;
        public bool AddToggle = false;
    }

    public class KeyBuilder
    {
        private static readonly Vector2 DEFAULT_IMAGE_SIZE = new Vector2(8.75f, 8.75f);

        public static void ScaleKey(
            KeyInfo keyInfo, KeyLayoutDesc keyLayoutDesc, float keyGapSize, float zSize)
        {
            RectTransform rectTransform = (RectTransform) keyInfo.transform;
            Vector2 size = rectTransform.sizeDelta;
            size.x = size.x * keyLayoutDesc.KeyWidthWeight + Mathf.Ceil(keyLayoutDesc.KeyWidthWeight - 1) * keyGapSize;
            size.y = size.y * keyLayoutDesc.KeyHeightWeight +
                     Mathf.Ceil(keyLayoutDesc.KeyHeightWeight - 1) * keyGapSize;
            rectTransform.sizeDelta = size;
        }

        public static void ConfigureKey(
            KeyInfo keyInfo, KeyBuilderSettings settings, KeyLayoutDesc keyLayoutDesc)
        {
            // Set up Label Text
            string label = !settings.UseDefaultCharAsLabel
                ? settings.Label
                : keyLayoutDesc.ThisKeyDesc.DefaultLabeledKey.Label.ToString().Trim();
            keyInfo.KeyTMP.text = label;
            keyInfo.TextToType = settings.LabelAndTypedTextIsSame ? label : settings.TextToType;

            // Set up Secondary Text
            keyInfo.SecondaryKeyTMP.text = keyLayoutDesc.SecondaryStr;

            // Set up first and secondary labels' position
            ChangeAnchorPos(keyInfo.SecondaryKeyTMP.rectTransform, settings.SecondaryRectSettings);
            ChangeAnchorPos(keyInfo.KeyTMP.rectTransform, settings.LabelRectSettings);

            // Set up first and secondary labels' font
            keyInfo.KeyTMP.font = settings.FontAsset;
            keyInfo.SecondaryKeyTMP.font = settings.FontAsset;
            keyInfo.KeyTMP.rectTransform.localScale = keyInfo.KeyTMP.rectTransform.localScale;
            keyInfo.SecondaryKeyTMP.rectTransform.localScale =
                keyInfo.SecondaryKeyTMP.rectTransform.localScale;

            if (keyLayoutDesc.OverrideLabelMargins)
            {
                keyInfo.KeyTMP.margin = keyLayoutDesc.LabelMargins;
            }
            else
            {
                keyInfo.KeyTMP.margin = settings.LabelMargins;
            }

            if (keyLayoutDesc.OverrideSecondaryMargins)
            {
                keyInfo.SecondaryKeyTMP.margin = keyLayoutDesc.SecondaryMargins;
            }
            else
            {
                keyInfo.SecondaryKeyTMP.margin = settings.SecondaryMargins;
            }

            if (0 !=
                (keyLayoutDesc.OverrideFontSize & OverrideFontSizeOptions.OVERRIDE_MIN))
            {
                keyInfo.KeyTMP.fontSizeMin = keyLayoutDesc.FontSizeMin;
                keyInfo.SecondaryKeyTMP.fontSizeMin = keyLayoutDesc.FontSizeMin;
            }
            else
            {
                keyInfo.KeyTMP.fontSizeMin = settings.FontSizeMin;
                keyInfo.SecondaryKeyTMP.fontSizeMin = settings.FontSizeMin;
            }

            if (0 !=
                (keyLayoutDesc.OverrideFontSize & OverrideFontSizeOptions.OVERRIDE_MAX))
            {
                keyInfo.KeyTMP.fontSizeMax = keyLayoutDesc.FontSizeMax;
                keyInfo.SecondaryKeyTMP.fontSizeMax = keyLayoutDesc.FontSizeMax;
            }
            else
            {
                keyInfo.KeyTMP.fontSizeMax = settings.FontSizeMax;
                keyInfo.SecondaryKeyTMP.fontSizeMax = settings.FontSizeMax;
            }

            if (settings.KeyButtonStyle != null)
            {
                keyInfo.KeyFillImage.sprite = settings.KeyButtonStyle.FillSpriteOff;
                keyInfo.MrtkBackplateImage.color = settings.KeyButtonStyle.MrtkBackplateColorOff;
            }
            if (settings.SettingsForIcons != null)
            {
                for (int idx = 0; idx < settings.SettingsForIcons.Count; idx++)
                {
                    GameObject iconObj = new GameObject("Icon(" + idx + ")");
                    Image iconImage = iconObj.AddComponent<Image>();
                    iconImage.rectTransform.sizeDelta = DEFAULT_IMAGE_SIZE;
                    iconImage.rectTransform.parent = keyInfo.Container.transform;
                    keyInfo.KeyIconImages.Add(iconImage);
                    iconImage.sprite = settings.SettingsForIcons[idx].IconSpriteOff;
                    iconImage.color = settings.SettingsForIcons[idx].IconColorOff;
                    ChangeAnchorPos(iconImage.rectTransform,
                                    settings.SettingsForIcons[idx].IconRectSettings);
                }
            }
            if (settings.AddShiftKeyBehavior)
            {
                AddShiftKeyBehavior(keyInfo, settings);
            }
            if (settings.AddToggle)
            {
                AddToggle(keyInfo, settings);
            }
            keyInfo.AccentEllipse.gameObject.SetActive(keyLayoutDesc.ShowAccentEllipse);
        }

        private static void ChangeAnchorPos(
            RectTransform rectTransform, RectTransformSettings settings)
        {
            if (settings.ChangeAnchors)
            {
                rectTransform.anchorMin = settings.AnchorMin;
                rectTransform.anchorMax = settings.AnchorMax;
            }
            if (settings.ChangePivot)
            {
                rectTransform.pivot = settings.Pivot;
            }
            if (settings.ChangePos)
            {
                rectTransform.anchoredPosition3D = settings.AnchoredPos;
            }
            rectTransform.localScale = settings.Scale;
        }

        private static void AddShiftKeyBehavior(KeyInfo keyInfo, KeyBuilderSettings settings)
        {
            ShiftKeyBehavior behavScript = keyInfo.gameObject.AddComponent<ShiftKeyBehavior>();
            behavScript.Init(keyInfo, settings);
            settings.LayoutInfo.ShiftKeysBehavs.Add(behavScript);
            ShiftKeyState state = ShiftKeyState.Off;
            switch (settings.LayoutInfo.ThisPageCode)
            {
                case PageCode.kUpperLetters:
                case PageCode.kAltUpperLetters:
                    state = ShiftKeyState.OnPerm;
                    break;
                case PageCode.kCapsLock:
                case PageCode.kLowerLetters:
                case PageCode.kAltLowerLetters:
                    state = ShiftKeyState.Off;
                    break;
            }
            behavScript.SwitchStatus(state);
        }

        private static void AddToggle(KeyInfo keyInfo, KeyBuilderSettings settings)
        {
            KeyToggle keyToggle = keyInfo.gameObject.AddComponent<KeyToggle>();
            keyToggle.Init(keyInfo, settings);
            switch (keyInfo.KeyType)
            {
                case KeyType.kChangeLocale:
                    settings.LayoutInfo.ChangeLocaleKeyToggle = keyToggle;
                    break;
                case KeyType.kRayDrumstickSwitch:
                    settings.LayoutInfo.RayDrumstickSwitchKeyToggle = keyToggle;
                    break;
                case KeyType.kAccents:
                    settings.LayoutInfo.JPAccentsKeyToggle = keyToggle;
                    break;
            }
        }
    }
}