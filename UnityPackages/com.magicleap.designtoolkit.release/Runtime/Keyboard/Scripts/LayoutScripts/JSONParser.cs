// Copyright (c) 2022 Magic Leap, Inc. All Rights Reserved.
// Please see the top-level LICENSE.md in this distribution
// for terms and conditions governing this file.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MagicLeap.DesignToolkit.Keyboard
{
    [System.Serializable]
    public class AlternateKey_JSON
    {
        public string Key;
    }

    [System.Serializable]
    public class AlternateKeyGroup_JSON
    {
        public string AlternateKeyGroupID;
        public AlternateKey_JSON[] AlternateKeys;
    }

    [System.Serializable]
    public class AlternateKeySelectorConfig_JSON
    {
        public string AlternateKeySelectorID;
        public AlternateKeyGroup_JSON[] AlternateKeyGroups;
    }

    [System.Serializable]
    public class KeyRectTransform_JSON
    {
        public string AnchorMinX;
        public string AnchorMinY;
        public string AnchorMaxX;
        public string AnchorMaxY;
        public string PivotX;
        public string PivotY;
        public string AnchorPosX;
        public string AnchorPosY;
        public string ScaleX;
        public string ScaleY;
        public string ScaleZ;
    }

    [System.Serializable]
    public class KeyIconSettings_JSON
    {
        public string IconMatOn;
        public string IconMatOff;
        public KeyRectTransform_JSON IconTransform;
        public Color_JSON IconColorOn;
        public Color_JSON IconColorOff;
    }

    [System.Serializable]
    public class KeyBuilderSettings_JSON
    {
        public string KeyTypeID;
        public string Label;
        public string TextToType;
        public string LabelAndTypedTextIsSame;
        public string UseDefaultCharAsLabel;
        public string KeyButtonStyle;
        public Color_JSON TextColorOn;
        public Color_JSON TextColorOff;
        public KeyRectTransform_JSON LabelTransform;
        public KeyRectTransform_JSON SecondaryTransform;
        public string FontSizeMin;
        public string FontSizeMax;
        public Margins_JSON LabelMargins;
        public Margins_JSON SecondaryMargins;
        public KeyIconSettings_JSON[] SettingsForIcons;
        public string AddShiftKeyBehavior;
        public string AddToggle;
        public string DefaultToggleValue;
    }

    [System.Serializable]
    public class KeyBuilder_JSON
    {
        public string LanguageID;
        public KeyBuilderSettings_JSON[] Keys;
    }

    [System.Serializable]
    public class Color_JSON
    {
        public string Red;
        public string Blue;
        public string Green;
        public string Alpha;
    }

    [System.Serializable]
    public class Margins_JSON
    {
        public string Left;
        public string Top;
        public string Right;
        public string Bottom;
    }

    public class SubPanel_JSON
    {
        public string IsVertical;
        public string LanguageID;
        public string SubPanelID;
        public float DefaultKeySizeX;
        public float DefaultKeySizeY;
        public float DefaultKeySizeZ;
        public float MotionRangeZStart;
        public float MotionRangeZEnd;
        public float FrontSideDetectThreshold;
        public float KeyGap;
        public Key_JSON[] Keys;
    }

    ////////// JSON for layout configs ////////////////////////////////
    [System.Serializable]
    public class Key_JSON
    {
        public string KeyTypeID;
        public string DefaultChar;
        public string AlternateKeySelectorID;
        public string WidthWeight; // meant to be float
        public string HeightWeight;
        public string GapOnLeft; // meant to be float
        public string KeyButtonStyle;
        public string SecondaryStr;
        public string SubPanelID;
        public string FontSizeMin; // meant to be float
        public string FontSizeMax; // meant to be float
        public string ShowAccentEllipse;
        public Margins_JSON LabelMargins;
        public Margins_JSON SecondaryMargins;
    }

    [System.Serializable]
    public class KeyRow_JSON
    {
        public Key_JSON[] KeyRow;
    }

    [System.Serializable]
    public class PageLayout_JSON
    {
        public string PageLayoutID;
        public KeyRow_JSON[] KeyRows;
        public float DefaultKeySizeX;
        public float DefaultKeySizeY;
        public float DefaultKeySizeZ;
        public float MotionRangeZStart;
        public float MotionRangeZEnd;
        public float FrontSideDetectThreshold;
        public float KeyboardWidth;
        public float KeyGap;
    }

    [System.Serializable]
    public class KeyPage_JSON
    {
        public string KeyTypeID;
        public string PageLayoutID;
    }

    [System.Serializable]
    public class KeyboardPageSetProperty_JSON
    {
        public string PageLayoutID;
        public KeyPage_JSON[] KeyPage;
    }

    [System.Serializable]
    public class KeyboardPageSet_JSON
    {
        public string KeyboardPageSetID;
        public KeyboardPageSetProperty_JSON[] KeyboardPageSetProperties;
        public string PageLayoutID;
    }

    [System.Serializable]
    public class Label_JSON
    {
        public string Label;
    }

    [System.Serializable]
    public class SwappableLabelKey_JSON
    {
        public string KeyTypeID;
        public string Char;
        public Label_JSON[] Labels;
    }

    [System.Serializable]
    public class KeyAudioAsset_JSON
    {
        public string KeyTypeID;
        public string AudioAssetType;
    }

    [System.Serializable]
    public class LayoutConfig_JSON
    {
        public string LanguageID;
        public string IsRightToLeft;
        public string IsUGUI;
        public PageLayout_JSON[] PageLayouts;
        public KeyboardPageSet_JSON[] KeyboardPageSets;
        public SwappableLabelKey_JSON[] SwappableLabelKeys;
        public KeyAudioAsset_JSON[] KeyAudioAssets;
    }

    public class JSONParser
    {
        public AlternateKeySelectorConfig_JSON ParseAlternateKeySelectorConfig(TextAsset textAsset)
        {
            return JsonUtility.FromJson<AlternateKeySelectorConfig_JSON>(textAsset.text);
        }

        public LayoutConfig_JSON ParsePageLayout(TextAsset textAsset)
        {
            return JsonUtility.FromJson<LayoutConfig_JSON>(textAsset.text);
        }

        public KeyBuilder_JSON ParseKeyBuilderSettings(TextAsset textAsset)
        {
            return JsonUtility.FromJson<KeyBuilder_JSON>(textAsset.text);
        }

        public SubPanel_JSON ParseSubPanel(TextAsset textAsset)
        {
            return JsonUtility.FromJson<SubPanel_JSON>(textAsset.text);
        }
    }
}
