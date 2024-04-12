// Copyright (c) 2022 Magic Leap, Inc. All Rights Reserved.
// Please see the top-level LICENSE.md in this distribution
// for terms and conditions governing this file.
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;
using System.Text;
using TMPro;

namespace MagicLeap.DesignToolkit.Keyboard
{
    using KeyTypePageLinkVec =
        System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<KeyType, PageLink>>;
    using PageID = System.UInt32;

    public struct KeyLayoutDesc
    {
        public KeyDesc ThisKeyDesc;
        public float KeyWidthWeight;
        public float KeyHeightWeight;
        public float GapOnLeft;
        public float FontSizeMin;
        public float FontSizeMax;
        public KeyButtonStyle KeyButtonStyle;
        public string SecondaryStr;
        public string SubPanelID;
        public bool OverrideButtonStyle;
        public OverrideFontSizeOptions OverrideFontSize;
        public bool ShowAccentEllipse;
        public Vector4 LabelMargins;
        public Vector4 SecondaryMargins;
        public bool OverrideLabelMargins;
        public bool OverrideSecondaryMargins;

        public string ToStr()
        {
            return "KeyLayoutDesc {keyDesc: " + ThisKeyDesc.ToString() +
                   "; keyWidthWeight: " + KeyWidthWeight +
                   "; gapOnLeft: " + GapOnLeft + "; KeyButtonStyle: " + KeyButtonStyle +
                   "; SecondaryStr: " + SecondaryStr + "} ";
        }
    }

    public class PageLayoutDesc
    {
        public List<List<KeyLayoutDesc>> KeyLayoutRows;
        public Vector3 DefaultKeySize;
        public float KeyboardWidth;
        public float MotionRangeZStart;
        public float MotionRangeZEnd;
        public float FrontSideDetectThreshold;
        public float KeyGap;

        public PageLayoutDesc()
        {
            KeyLayoutRows = new List<List<KeyLayoutDesc>>();
            DefaultKeySize = new Vector3();
            KeyboardWidth = 0.0f;
            KeyGap = 0.0f;
        }

        public string ToStr()
        {
            string result = "PageLayoutDesc {keyLayoutRows: ";
            for (int i = 0; i < KeyLayoutRows.Count; i++)
            {
                string line = "Row No. " + i + ": ";
                for (int j = 0; j < KeyLayoutRows[i].Count; j++)
                {
                    line += (KeyLayoutRows[i][j].ToStr() + ", ");
                }
                result += (line + ";\n");
            }
            result += ("; defaultKeySizeX: " + DefaultKeySize.x +
                       "; defaultKeySizeY: " + DefaultKeySize.y +
                       "; defaultKeySizeZ: " + DefaultKeySize.z +
                       "; keyboardWidth: " + KeyboardWidth +
                       "; keyGap: " + KeyGap + "}\n");
            return result;
        }
    }

    public enum LinkButton
    {
        kNone,
        kCenterLeft,
        kCenterRight,
        kLowerLeft,
        kLowerRight,
        kUpperLeft,
        kUpperRight
    }

    // This struct could hold more information in the future
    public struct PageLink
    {
        public readonly PageID ThisPageID;

        public PageLink(PageID a_pageID)
        {
            ThisPageID = a_pageID;
        }

        public string ToStr()
        {
            string result = "PageLink {pageID: " + ThisPageID + "} ";
            return result;
        }
    }

    public class PageLayoutProperties
    {
        public SortedDictionary<PageID, KeyTypePageLinkVec> PageLinkMap;
        public PageID DefaultPage;

        public PageLayoutProperties()
        {
            PageLinkMap = new SortedDictionary<PageID, KeyTypePageLinkVec>();
            DefaultPage = new PageID();
        }

        public string ToStr()
        {
            string result = "defaultPage: " + DefaultPage + "\n";
            foreach (KeyValuePair<PageID, KeyTypePageLinkVec> pair in PageLinkMap)
            {
                result += ("key(PageID): " + pair.Key + "\n");
                result += ("value(List<KeyValuePair<KeyType, PageLink>>: ");
                foreach (KeyValuePair<KeyType, PageLink> page in pair.Value)
                {
                    result += "{KeyType: " + page.Key + "; " +
                              "PageLink: " + page.Value.ToStr() + "}, ";
                }
                result += "\n";
            }
            return result;
        }
    }

    public class SubPanelDesc
    {
        public string SubPanelID;
        public Vector3 DefaultKeySize;
        public float MotionRangeZStart;
        public float MotionRangeZEnd;
        public float FrontSideDetectThreshold;
        public float KeyGap;
        public bool IsVerticalPanel;
        public List<KeyLayoutDesc> Keys;

        public SubPanelDesc()
        {
            SubPanelID = "";
            DefaultKeySize = Vector3.one;
            MotionRangeZStart = 0;
            MotionRangeZEnd = 0;
            FrontSideDetectThreshold = 0;
            KeyGap = 0;
            IsVerticalPanel = false;
            Keys = new List<KeyLayoutDesc>();
        }
    }

    public class LocaleDesc
    {
        public SortedDictionary<PageID, PageLayoutDesc> LayoutMap;
        public SortedDictionary<LayoutType, PageLayoutProperties> PropertyMap;
        public SortedDictionary<KeyType, MultiLabeledKey> AdditionalKeyLabels;
        public Dictionary<char, char> NextAlternateKeyMap;
        public Dictionary<KeyType, KeyBuilderSettings> KeyBuilderSettingsMap;
        public List<SubPanelDesc> SubPanels;
        public Dictionary<KeyType, AudioAssetType> AudioAssetMap;
        public TMP_FontAsset FontAsset;
        public bool IsRightToLeft;
        public bool IsUGUI;
        public float MotionRangeZStart;
        public float MotionRangeZEnd;
        public float FrontSideDetect;

        public LocaleDesc()
        {
            LayoutMap = new SortedDictionary<PageID, PageLayoutDesc>();
            PropertyMap = new SortedDictionary<LayoutType, PageLayoutProperties>();
            AdditionalKeyLabels = new SortedDictionary<KeyType, MultiLabeledKey>();
            NextAlternateKeyMap = new Dictionary<char, char>();
            KeyBuilderSettingsMap = new Dictionary<KeyType, KeyBuilderSettings>();
            SubPanels = new List<SubPanelDesc>();
            AudioAssetMap = new Dictionary<KeyType, AudioAssetType>();
            IsRightToLeft = false;
            IsUGUI = false;
        }

        public string ToStr()
        {
            // Print layoutMap
            string result = "LocaleDesc {layoutMap:\n";
            foreach (KeyValuePair<PageID, PageLayoutDesc> pair in LayoutMap)
            {
                result += ("key(PageID): " + pair.Key + ";\n");
                result += ("value(PageLayoutDesc): " + pair.Value.ToStr() + ", ");
            }

            // Print propertyMap
            result += "}\n\n{propertyMap:\n";
            foreach (KeyValuePair<LayoutType, PageLayoutProperties> pair in PropertyMap)
            {
                result += ("key(LayoutType): " + pair.Key + ";\n");
                result += ("value(PageLayoutProperties): " + pair.Value.ToStr() + ";\n");
            }

            // Print additionalKeyLabels
            result += "}\n\n{additionalKeyLabels:\n";
            foreach (KeyValuePair<KeyType, MultiLabeledKey> pair in AdditionalKeyLabels)
            {
                result += ("key(KeyType): " + pair.Key + ";\n");
                result += ("value(MultiLabeledKey): " + pair.Value.ToString() + ";\n");
            }

            // Print nextAlternateKeyMap
            result += "}\n\n{nextAlternateKeyMap:\n";
            foreach (var pair in NextAlternateKeyMap)
            {
                result += ("key(char): " + pair.Key + "; ");
                result += ("value(char): " + pair.Value + ",  ");
            }
            result += ("}\n");
            return result;
        }
    }

    public class VirtualKeyboardBuilder : MonoBehaviour
    {
        public TextAsset[] AlternateKeySelectorConfigFiles;
        public TextAsset[] LayoutConfigFiles;
        public TextAsset[] KeyBuilderFiles;
        public TextAsset[] SubPanelFiles;
        private readonly float DEFAULT_WIDTH_WEIGHT = 1f;
        private readonly float DEFAULT_HEIGHT_WEIGHT = 1f;
        private readonly float DEFAULT_FONT_SIZE_MIN = 7.5f;
        private readonly float DEFAULT_FONT_SIZE_MAX = 7.5f;
        private readonly Vector4 DEFAULT_TEXT_MARGINS = Vector4.zero;
        private readonly CultureInfo CULTURE_FORMATTER = new ("en-US");
        private JSONParser _jsonParser;
        private SortedDictionary<Code, LocaleDesc> _localeMap = new ();
        private SortedDictionary<string, List<LabeledKey>> _alternateKeySelectorMap = new ();
        [SerializeField]
        private SerializableDictionary<string, Sprite> _iconMap = new ();
        [SerializeField]
        private SerializableDictionary<KeyButtonStyle, KeyStyle> _keyStyleMap = new ();
        [SerializeField]
        private SerializableDictionary<Code, TMP_FontAsset> _fontAssetMap = new ();

        public bool Init()
        {
            Debug.Log("Initializing virtual keyboard builder");
            Clear();
            _jsonParser = new JSONParser();
            foreach (TextAsset file in AlternateKeySelectorConfigFiles)
            {
                bool success = BuildAlternateKeySelectorMap(file);
                Debug.Log("Build AlternateKeySelectorMap using " + file.name +
                          ", success: " + success);
                if (!success)
                {
                    return false;
                }
            }
            foreach (TextAsset file in LayoutConfigFiles)
            {
                bool success = BuildLayouts(file);
                Debug.Log("Build Layouts using " + file.name + ", success: " + success);
                if (!success)
                {
                    return false;
                }
            }
            foreach (TextAsset file in KeyBuilderFiles)
            {
                BuildKeyBuilder(file);
            }
            foreach (TextAsset file in SubPanelFiles)
            {
                BuildSubPanel(file);
            }
            BuildNextAlternateKeyMap();
            Debug.Log("Init() of VirtualKeyboardBuilder successfully finished");
            return true;
        }

        public SortedDictionary<Code, LocaleDesc> GetLocaleMap()
        {
            return _localeMap;
        }

        public KeyBuilderSettings GetKeyBuilderSettings(Code localeCode, KeyType keyType)
        {
            return _localeMap[localeCode].KeyBuilderSettingsMap[keyType];
        }

        public TMP_FontAsset GetFontAsset(Code localeCode)
        {
            return _localeMap[localeCode].FontAsset;
        }

        public KeyStyle GetKeyStyle(KeyButtonStyle keyButtonStyle)
        {
            if (!_keyStyleMap.Dictionary.ContainsKey(keyButtonStyle))
            {
                return null;
            }
            return _keyStyleMap.Dictionary[keyButtonStyle];
        }

        private void Clear()
        {
            _jsonParser = null;
            _localeMap.Clear();
            _alternateKeySelectorMap.Clear();
        }

        private bool BuildAlternateKeySelectorMap(TextAsset textAsset)
        {
            AlternateKeySelectorConfig_JSON alternateKeySelectorConfig =
                _jsonParser.ParseAlternateKeySelectorConfig(textAsset);
            if (alternateKeySelectorConfig == null)
            {
                Debug.LogError("JSON parser returns null when parsing alternative key selector config");
                return false;
            }
            Debug.Log("Parsed JSON obj of alternate key selector map is: " +
                      JsonUtility.ToJson(alternateKeySelectorConfig));
            if (alternateKeySelectorConfig.AlternateKeyGroups == null)
            {
                Debug.LogError("The AlternateKeyGroups array is required but not present!");
                return false;
            }
            foreach (AlternateKeyGroup_JSON alternateKeyGroup in alternateKeySelectorConfig.AlternateKeyGroups)
            {
                if (alternateKeyGroup.AlternateKeyGroupID == null)
                {
                    Debug.LogError("The AlternateKeyGroupID value is required but not present!");
                    return false;
                }
                _alternateKeySelectorMap.Add(alternateKeyGroup.AlternateKeyGroupID, new List<LabeledKey>());
                if (alternateKeyGroup.AlternateKeys == null)
                {
                    Debug.LogError("The AlternateKeys array is required but not present!");
                    return false;
                }
                if (alternateKeyGroup.AlternateKeys.Length == 0)
                {
                    Debug.LogError("At lease one Key is required but no present!");
                    return false;
                }

                // Read the list of alternate keys
                foreach (AlternateKey_JSON alternateKey in alternateKeyGroup.AlternateKeys)
                {
                    U32string u32string = new U32string(alternateKey.Key);
                    _alternateKeySelectorMap[alternateKeyGroup.AlternateKeyGroupID].Add(
                        new LabeledKey(u32string.Data[0], u32string)
                    );
                }
            }
            return true;
        }

        private bool BuildLayouts(TextAsset textAsset)
        {
            LayoutConfig_JSON layoutConfig = _jsonParser.ParsePageLayout(textAsset);
            if (layoutConfig == null)
            {
                Debug.LogError("JSON parser returns null when parsing page layout");
                return false;
            }
            if (string.IsNullOrEmpty(layoutConfig.LanguageID))
            {
                Debug.LogError("The LanguageID value is required but not present");
                return false;
            }
            LocaleDesc localeDescRef = BuildLanguage(layoutConfig);
            if (localeDescRef == null)
            {
                Debug.LogError("Supported LanguagedID not found!");
                return false;
            }
            if (layoutConfig.PageLayouts == null)
            {
                Debug.LogError("The PageLayouts array is required but not present!");
                return false;
            }
            if (layoutConfig.PageLayouts.Length == 0)
            {
                Debug.LogError("At least one PageLayout is required but not present!");
                return false;
            }
            foreach (PageLayout_JSON pageLayout in layoutConfig.PageLayouts)
            {
                PageLayoutDesc pageLayoutDescRef = BuildPageLayout(localeDescRef, pageLayout);
                if (pageLayoutDescRef == null)
                {
                    Debug.LogError("Supported PageLayoutID not found!");
                    return false;
                }
                if (pageLayout.KeyRows == null)
                {
                    Debug.LogError("The KeyRows array is required but not present");
                    return false;
                }
                if (pageLayout.KeyRows.Length == 0)
                {
                    Debug.LogError("At least one KeyRow is required but not present!");
                    return false;
                }
                foreach (KeyRow_JSON keyRow in pageLayout.KeyRows)
                {
                    List<KeyLayoutDesc> keys = BuildKeyRow(pageLayoutDescRef);
                    if (keyRow.KeyRow.Length == 0)
                    {
                        Debug.LogError("At least one key is required but not present!");
                        return false;
                    }
                    foreach (Key_JSON key in keyRow.KeyRow)
                    {
                        BuildKey(keys, key);
                    }
                }
            }
            if (layoutConfig.KeyboardPageSets == null)
            {
                Debug.LogError("The KeyboardPageSets value is required but not present!");
                return false;
            }
            if (layoutConfig.KeyboardPageSets.Length == 0)
            {
                Debug.LogError("At least one KeyboardPageSet is required but not present!");
                return false;
            }
            foreach (KeyboardPageSet_JSON keyboardPageSet in layoutConfig.KeyboardPageSets)
            {
                PageLayoutProperties pageLayoutProperties = BuildKeyboardPageSet(
                    localeDescRef, keyboardPageSet
                );
                if (keyboardPageSet.KeyboardPageSetProperties == null) //"kNumeric" "kNumericOnly"
                {
                    continue;
                }
                foreach (KeyboardPageSetProperty_JSON keyboardPageSetProperty in keyboardPageSet
                    .KeyboardPageSetProperties)
                {
                    if (pageLayoutProperties == null)
                    {
                        Debug.LogError("Supported KeyboardPageSetID not found!");
                        return false;
                    }
                    PageID? pageCode = GetPageCode(keyboardPageSetProperty.PageLayoutID);
                    if (pageCode == null)
                    {
                        Debug.LogError(
                            "Cannot BuildKeyboardPageSetProperty() due to unsuupported PageLayoutID!");
                        return false;
                    }
                    if (keyboardPageSetProperty.KeyPage == null)
                    {
                        Debug.LogError("The KeyPage array is required but not present!");
                        return false;
                    }
                    if (keyboardPageSetProperty.KeyPage.Length == 0)
                    {
                        Debug.LogError("At least one KeyPage is required but not present!");
                        return false;
                    }

                    // TO DO: Change this into a map instead of a list
                    KeyTypePageLinkVec keyPages = BuildKeyboardPageSetProperty(
                        pageLayoutProperties, pageCode.Value
                    );
                    foreach (KeyPage_JSON keyPage in keyboardPageSetProperty.KeyPage)
                    {
                        BuildKeyPage(keyPages, keyPage, pageCode.Value);
                    }
                }
            }
            if (layoutConfig.SwappableLabelKeys != null && layoutConfig.SwappableLabelKeys.Length > 0)
            {
                foreach (SwappableLabelKey_JSON swappableLabelKey in layoutConfig.SwappableLabelKeys)
                {
                    List<U32string> labels = BuildSwappableLabelKey(localeDescRef, swappableLabelKey);
                    if (labels == null)
                    {
                        Debug.LogError("Swappable Label Key could not be created!");
                        return false;
                    }
                    foreach (Label_JSON label in swappableLabelKey.Labels)
                    {
                        BuildSwappableLabelKeyLabel(labels, label);
                    }
                }
            }
            if (layoutConfig.KeyAudioAssets != null && layoutConfig.KeyAudioAssets.Length > 0)
            {
                foreach (KeyAudioAsset_JSON keyAudioAsset in layoutConfig.KeyAudioAssets)
                {
                    KeyType keyType = GetKeyType(keyAudioAsset.KeyTypeID);
                    if (!localeDescRef.AudioAssetMap.ContainsKey(keyType))
                    {
                        localeDescRef.AudioAssetMap.Add(
                            keyType,
                            GetAudioAssetType(keyAudioAsset.AudioAssetType));
                    }
                }
            }
            return true;
        }

        private void BuildSubPanel(TextAsset textAsset)
        {
            SubPanel_JSON subPanelJson = _jsonParser.ParseSubPanel(textAsset);
            if (subPanelJson == null)
            {
                Debug.LogError("Failed to parse subPanelJson");
                return;
            }
            if (string.IsNullOrEmpty(subPanelJson.LanguageID))
            {
                Debug.LogError("LanguageID was null or empty!");
                return;
            }
            Code localeCode = StringToLocale(subPanelJson.LanguageID);
            if (localeCode != Code.kAll_Unity && !_localeMap.ContainsKey(localeCode))
            {
                Debug.Log("KeyBuilder LanguangeID was a locale code in the localeMap!");
                return;
            }
            if (string.IsNullOrEmpty(subPanelJson.SubPanelID))
            {
                Debug.LogError("SubPanelID was null or empty!");
                return;
            }
            Vector3 defaultKeySize = new Vector3(subPanelJson.DefaultKeySizeX,
                subPanelJson.DefaultKeySizeY,
                subPanelJson.DefaultKeySizeZ);
            if (subPanelJson.Keys == null)
            {
                Debug.LogError("Keys was null!");
                return;
            }

            bool isVerticalPanel = true;
            if (!bool.TryParse(subPanelJson.IsVertical, out isVerticalPanel))
            {
                isVerticalPanel = false;
            }

            SubPanelDesc subPanelDesc = new SubPanelDesc();
            subPanelDesc.SubPanelID = subPanelJson.SubPanelID;
            subPanelDesc.DefaultKeySize = defaultKeySize;
            subPanelDesc.MotionRangeZStart = subPanelJson.MotionRangeZStart;
            subPanelDesc.MotionRangeZEnd = subPanelJson.MotionRangeZEnd;
            subPanelDesc.FrontSideDetectThreshold = subPanelJson.FrontSideDetectThreshold;
            subPanelDesc.KeyGap = subPanelJson.KeyGap;
            subPanelDesc.IsVerticalPanel = isVerticalPanel;
            foreach (Key_JSON keyJson in subPanelJson.Keys)
            {
                keyJson.SubPanelID = subPanelJson.SubPanelID;
                BuildKey(subPanelDesc.Keys, keyJson);
            }

            if (localeCode == Code.kAll_Unity)
            {
                foreach (Code codeKey in _localeMap.Keys)
                {
                    _localeMap[codeKey].SubPanels.Add(subPanelDesc);
                }
            }
            else
            {
                _localeMap[localeCode].SubPanels.Add(subPanelDesc);
            }
        }

        private void BuildKeyBuilder(TextAsset textAsset)
        {
            KeyBuilder_JSON keyBuilderJson = _jsonParser.ParseKeyBuilderSettings(textAsset);
            if (keyBuilderJson == null)
            {
                Debug.LogError("Failed to parse KeyBuilder JSON!");
                return;
            }
            if (string.IsNullOrEmpty(keyBuilderJson.LanguageID))
            {
                Debug.LogError("LanguageID was null or empty!");
                return;
            }
            Code localeCode = StringToLocale(keyBuilderJson.LanguageID);
            if (!_localeMap.ContainsKey(localeCode))
            {
                Debug.Log("KeyBuilder LanguangeID was a locale code in the localeMap!");
                return;
            }
            foreach (KeyBuilderSettings_JSON settingsJson in keyBuilderJson.Keys)
            {
                KeyType keyType = GetKeyType(settingsJson.KeyTypeID);
                if (!_localeMap[localeCode].KeyBuilderSettingsMap.ContainsKey(keyType))
                {
                    _localeMap[localeCode].KeyBuilderSettingsMap.Add(
                        keyType, BuildKeyBuilderSettings(localeCode, settingsJson));
                }
                else
                {
                    Debug.LogError("Duplicate KeyType \"" + settingsJson.KeyTypeID + "\"");
                }
            }
        }

        private void BuildNextAlternateKeyMap()
        {
            LocaleDesc jpLocaleDesc = _localeMap[Code.kJp_JP_Unity];
            PageID[] pages = {(PageID) PageCode.kHiragana, (PageID) PageCode.kKatakana};
            foreach (var pageID in pages)
            {
                foreach (var row in jpLocaleDesc.LayoutMap[pageID].KeyLayoutRows)
                {
                    foreach (var layoutDesc in row)
                    {
                        if (layoutDesc.ThisKeyDesc.KeyType == KeyType.kHiragana ||
                            layoutDesc.ThisKeyDesc.KeyType == KeyType.kKatakana &&
                            layoutDesc.ThisKeyDesc.AlternativeLabeledKeys.Count > 0)
                        {
                            var origChar =
                                layoutDesc.ThisKeyDesc.DefaultLabeledKey.CharacterCode.ToChar();
                            var nextChar = origChar;

                            // going backwards through list to link each key to the next one
                            var alternativeLabeledKeys =
                                layoutDesc.ThisKeyDesc.AlternativeLabeledKeys;
                            for (int i = alternativeLabeledKeys.Count - 1; i >= 0; i--)
                            {
                                jpLocaleDesc.NextAlternateKeyMap[
                                    alternativeLabeledKeys[i].CharacterCode.ToChar()] = nextChar;
                                nextChar = alternativeLabeledKeys[i].CharacterCode.ToChar();
                            }
                            jpLocaleDesc.NextAlternateKeyMap[origChar] = nextChar;
                        }
                    }
                }
            }
            StringBuilder stringBuilder = new StringBuilder("Japanese NextAlternateKeymap: ");
            foreach (var pair in jpLocaleDesc.NextAlternateKeyMap)
            {
                stringBuilder.Append(pair.Key + "->" + pair.Value + "; ");
            }
            Debug.Log(stringBuilder.ToString());
        }

        private KeyBuilderSettings BuildKeyBuilderSettings(
            Code localeCode, KeyBuilderSettings_JSON settingsJson)
        {
            KeyBuilderSettings keyBuilderSettings = new KeyBuilderSettings();
            keyBuilderSettings.Label = settingsJson.Label != null ? settingsJson.Label : "";
            keyBuilderSettings.TextToType =
                settingsJson.TextToType != null ? settingsJson.TextToType : "";
            keyBuilderSettings.FontAsset = _localeMap[localeCode].FontAsset;
            if (!string.IsNullOrEmpty(settingsJson.LabelAndTypedTextIsSame))
            {
                keyBuilderSettings.LabelAndTypedTextIsSame =
                    bool.Parse(settingsJson.LabelAndTypedTextIsSame);
            }
            if (!string.IsNullOrEmpty(settingsJson.UseDefaultCharAsLabel))
            {
                keyBuilderSettings.UseDefaultCharAsLabel =
                    bool.Parse(settingsJson.UseDefaultCharAsLabel);
            }
            if (!string.IsNullOrEmpty(settingsJson.KeyButtonStyle))
            {
                KeyButtonStyle keybuttonStyle = GetKeyButtonStyle(settingsJson.KeyButtonStyle);
                keyBuilderSettings.KeyButtonStyle = GetKeyStyle(keybuttonStyle);
            }
            keyBuilderSettings.LabelRectSettings =
                GetRectTransformSettings(settingsJson.LabelTransform);
            keyBuilderSettings.SecondaryRectSettings =
                GetRectTransformSettings(settingsJson.SecondaryTransform);
            if (!float.TryParse(settingsJson.FontSizeMin,
                               NumberStyles.Float,
                               CULTURE_FORMATTER,
                               out keyBuilderSettings.FontSizeMin))
            {
                keyBuilderSettings.FontSizeMin = DEFAULT_FONT_SIZE_MIN;
            }
            if (!float.TryParse(settingsJson.FontSizeMax,
                               NumberStyles.Float,
                               CULTURE_FORMATTER,
                               out keyBuilderSettings.FontSizeMax))
            {
                keyBuilderSettings.FontSizeMax = DEFAULT_FONT_SIZE_MAX;
            }
            if (settingsJson.LabelMargins != null)
            {
                TryParseVector4(settingsJson.LabelMargins.Left,
                                settingsJson.LabelMargins.Top,
                                settingsJson.LabelMargins.Right,
                                settingsJson.LabelMargins.Bottom,
                                out keyBuilderSettings.LabelMargins,
                                DEFAULT_TEXT_MARGINS);
            }
            if (settingsJson.SecondaryMargins != null)
            {
                TryParseVector4(settingsJson.SecondaryMargins.Left,
                                settingsJson.SecondaryMargins.Top,
                                settingsJson.SecondaryMargins.Right,
                                settingsJson.SecondaryMargins.Bottom,
                                out keyBuilderSettings.SecondaryMargins,
                                DEFAULT_TEXT_MARGINS);
            }
            if (settingsJson.SettingsForIcons != null)
            {
                keyBuilderSettings.SettingsForIcons = new List<IconSettings>();
                foreach (KeyIconSettings_JSON iconSettingsJson in settingsJson.SettingsForIcons)
                {
                    keyBuilderSettings.SettingsForIcons.Add(GetIconSettings(iconSettingsJson));
                }
            }
            BuildColor(settingsJson.TextColorOn, out Color textColorOn, Color.black);
            keyBuilderSettings.TextColorOn = textColorOn;
            BuildColor(settingsJson.TextColorOff, out Color textColorOff, Color.white);
            keyBuilderSettings.TextColorOff = textColorOff;
            if (!string.IsNullOrEmpty(settingsJson.AddShiftKeyBehavior))
            {
                keyBuilderSettings.AddShiftKeyBehavior =
                    bool.Parse(settingsJson.AddShiftKeyBehavior);
            }
            if (bool.TryParse(settingsJson.AddToggle, out bool addToggle))
            {
                keyBuilderSettings.AddToggle = addToggle;
            }
            return keyBuilderSettings;
        }

        private Sprite GetIconSprite(string key)
        {
            if (string.IsNullOrEmpty(key) ||
                !_iconMap.Dictionary.ContainsKey(key))
            {
                return null;
            }
            return _iconMap.Dictionary[key];
        }

        private IconSettings GetIconSettings(KeyIconSettings_JSON json)
        {
            IconSettings settings = new IconSettings();
            settings.IconSpriteOn = GetIconSprite(json.IconMatOn);
            settings.IconSpriteOff = GetIconSprite(json.IconMatOff);
            settings.IconRectSettings =
                GetRectTransformSettings(json.IconTransform);
            BuildColor(json.IconColorOn, out Color onColor, Color.black);
            settings.IconColorOn = onColor;
            BuildColor(json.IconColorOff, out Color offColor, Color.white);
            settings.IconColorOff = offColor;
            return settings;
        }

        private RectTransformSettings GetRectTransformSettings(KeyRectTransform_JSON json)
        {
            RectTransformSettings settings = new RectTransformSettings();
            bool parsedAnchorMin =
                TryParseVector2(json.AnchorMinX, json.AnchorMinY, out Vector2 anchorMin);
            bool parsedAnchorMax =
                TryParseVector2(json.AnchorMaxX, json.AnchorMaxY, out Vector2 anchorMax);
            settings.ChangePivot = TryParseVector2(json.PivotX, json.PivotY, out Vector2 pivot);
            settings.ChangePos =
                TryParseVector2(json.AnchorPosX, json.AnchorPosY, out Vector2 anchorPos);
            TryParseVector3(
                json.ScaleX, json.ScaleY, json.ScaleZ, out Vector3 scale, Vector3.one);
            settings.ChangeAnchors = (parsedAnchorMin || parsedAnchorMax);
            if (settings.ChangePivot)
            {
                settings.Pivot = pivot;
            }
            if (settings.ChangePos)
            {
                settings.AnchoredPos = anchorPos;
            }
            if (settings.ChangeAnchors)
            {
                settings.AnchorMin = anchorMin;
                settings.AnchorMax = anchorMax;
            }
            settings.Scale = scale;
            return settings;
        }

        private bool TryParseVector2(
            string xStr, string yStr, out Vector2 result, Vector2 def = new Vector2())
        {
            bool xSucceeded = float.TryParse(xStr,
                                             NumberStyles.Float,
                                             CULTURE_FORMATTER,
                                             out float x);
            bool ySucceeded = float.TryParse(yStr,
                                             NumberStyles.Float,
                                             CULTURE_FORMATTER,
                                             out float y);
            result = new Vector2(xSucceeded ? x : def.x, ySucceeded ? y : def.y);
            return xSucceeded || ySucceeded;
        }

        private bool TryParseVector3(string xStr,
            string yStr,
            string zStr,
            out Vector3 result,
            Vector3 def = new Vector3())
        {
            bool xSucceeded = float.TryParse(xStr,
                                             NumberStyles.Float,
                                             CULTURE_FORMATTER,
                                             out float x);
            bool ySucceeded = float.TryParse(yStr,
                                             NumberStyles.Float,
                                             CULTURE_FORMATTER,
                                             out float y);
            bool zSucceeded = float.TryParse(zStr,
                                             NumberStyles.Float,
                                             CULTURE_FORMATTER,
                                             out float z);
            result = new Vector3(xSucceeded ? x : def.x,
                ySucceeded ? y : def.y,
                zSucceeded ? z : def.z);
            return xSucceeded || ySucceeded || zSucceeded;
        }

        private bool TryParseVector4(string xStr,
            string yStr,
            string zStr,
            string wStr,
            out Vector4 result,
            Vector4 def = new Vector4())
        {
            bool xSucceeded = float.TryParse(xStr,
                                             NumberStyles.Float,
                                             CULTURE_FORMATTER,
                                             out float x);
            bool ySucceeded = float.TryParse(yStr,
                                             NumberStyles.Float,
                                             CULTURE_FORMATTER,
                                             out float y);
            bool zSucceeded = float.TryParse(zStr,
                                             NumberStyles.Float,
                                             CULTURE_FORMATTER,
                                             out float z);
            bool wSucceeded = float.TryParse(wStr,
                                             NumberStyles.Float,
                                             CULTURE_FORMATTER,
                                             out float w);
            result = new Vector4(xSucceeded ? x : def.x,
                ySucceeded ? y : def.y,
                zSucceeded ? z : def.z,
                wSucceeded ? w : def.w);
            return xSucceeded || ySucceeded || zSucceeded || wSucceeded;
        }

        private bool BuildColor(Color_JSON colorJson, out Color result, Color defaultColor)
        {
            if (colorJson == null)
            {
                result = defaultColor;
                return false;
            }
            bool redSucceeded = int.TryParse(colorJson.Red,
                                             NumberStyles.Integer,
                                             CULTURE_FORMATTER,
                                             out int redVal);
            bool greenSucceeded = int.TryParse(colorJson.Green,
                                               NumberStyles.Integer,
                                               CULTURE_FORMATTER,
                                               out int greenVal);
            bool blueSucceeded = int.TryParse(colorJson.Blue,
                                              NumberStyles.Integer,
                                              CULTURE_FORMATTER,
                                              out int blueVal);
            bool alphaSucceeded = int.TryParse(colorJson.Alpha,
                                               NumberStyles.Integer,
                                               CULTURE_FORMATTER,
                                               out int alphaVal);

            result = new Color(redSucceeded ? redVal / 255.0f : defaultColor.r,
                               greenSucceeded ? greenVal / 255.0f : defaultColor.g,
                               blueSucceeded ? blueVal / 255.0f : defaultColor.b,
                               alphaSucceeded ? alphaVal / 255.0f : defaultColor.a);
            return true;
        }

        private Code StringToLocale(string val)
        {
            Dictionary<string, Code> umap = new Dictionary<string, Code>
            {
                {"kAll_Unity", Code.kAll_Unity},
                {"kEn_US_Unity", Code.kEn_US_Unity},
                {"kJp_JP_Unity", Code.kJp_JP_Unity},
                {"kAr_AR_Unity", Code.kAr_AR_Unity},
                {"kDe_DE_Unity", Code.kDe_DE_Unity},
                {"kFr_FR_Unity", Code.kFr_FR_Unity},
                {"kEs_ES_Unity", Code.kEs_ES_Unity},
                {"kPt_PT_Unity", Code.kPt_PT_Unity},
                {"kIt_IT_Unity", Code.kIt_IT_Unity}
            };
            if (umap.ContainsKey(val))
            {
                return umap[val];
            }
            else
            {
                Debug.LogWarning("Unknow locale: " + val + ", using default EN US");
                return Code.kEn_US_Unity;
            }
        }

        private PageID? GetPageCode(string val)
        {
            Dictionary<string, PageID> umap = new Dictionary<string, PageID>
            {
                {"kLowerLetters", (PageID) PageCode.kLowerLetters},
                {"kUpperLetters", (PageID) PageCode.kUpperLetters},
                {"kCapsLock", (PageID) PageCode.kCapsLock},
                {"kNumericSymbols", (PageID) PageCode.kNumericSymbols},
                {"kHiragana", (PageID) PageCode.kHiragana},
                {"kKatakana", (PageID) PageCode.kKatakana},
                {"kAltLowerLetters", (PageID) PageCode.kAltLowerLetters},
                {"kAltUpperLetters", (PageID) PageCode.kAltUpperLetters},
            };
            if (umap.ContainsKey(val))
            {
                return umap[val];
            }
            else
            {
                Debug.LogError("Unsupported pageLayoutID: " + val);
                return null;
            }
        }

        KeyType GetKeyType(string val)
        {
            Dictionary<string, KeyType> umap = new Dictionary<string, KeyType>
            {
                {"kCharacter", KeyType.kCharacter},
                {"kBackspace", KeyType.kBackspace},
                {"kShift", KeyType.kShift},
                {"kAltGr", KeyType.kAltGr},
                {"kCapsLock", KeyType.kCapsLock},
                {"kPageNumericSymbols", KeyType.kPageNumericSymbols},
                {"kCancel", KeyType.kCancel},
                {"kSubmit", KeyType.kSubmit},
                {"kClear", KeyType.kClear},
                {"kClose", KeyType.kClose},
                {"kEnter", KeyType.kEnter},
                {"kChangeLocale", KeyType.kChangeLocale},
                {"kPageHiragana", KeyType.kPageHiragana},
                {"kPageKatakana", KeyType.kPageKatakana},
                {"kAltCharToggle", KeyType.kAltCharToggle},
                {"kSpacebar", KeyType.kSpacebar},
                {"kTab", KeyType.kTab},
                {"kCharacterSpecial", KeyType.kCharacterSpecial},
                {"kSuggestion", KeyType.kSuggestion},
                {"kHiragana", KeyType.kHiragana},
                {"kKatakana", KeyType.kKatakana},
                {"kBlank", KeyType.kBlank},
                {"kJPNumSym", KeyType.kJPNumSym},
                {"kJPEnter", KeyType.kJPEnter},
                {"kAccents", KeyType.kAccents},
                {"kJPSpacebar", KeyType.kJPSpacebar},
                {"kJPNewLine", KeyType.kJPNewLine},
                {"kRayDrumstickSwitch", KeyType.kRayDrumstickSwitch},
                {"kHideKeyboard", KeyType.kHideKeyboard},
                {"kJPNextSuggestion", KeyType.kJPNextSuggestion},
                {"kChangeLocaleEn", KeyType.kChangeLocaleEn},
                {"kChangeLocaleJP", KeyType.kChangeLocaleJP},
                {"kChangeLocaleAR", KeyType.kChangeLocaleAR},
                {"kChangeLocaleDE", KeyType.kChangeLocaleDE},
                {"kChangeLocaleFR", KeyType.kChangeLocaleFR},
                {"kChangeLocaleES", KeyType.kChangeLocaleES},
                {"kChangeLocalePT", KeyType.kChangeLocalePT},
                {"kChangeLocaleIT", KeyType.kChangeLocaleIT},
                {"kAccent", KeyType.kAccent},
                {"kPageDownExpanded", KeyType.kPageDownExpanded},
                {"kPageUpExpanded", KeyType.kPageUpExpanded},
                {"kPageDownCollapsed", KeyType.kPageDownCollapsed},
                {"kPageUpCollapsed", KeyType.kPageUpCollapsed},
                {"kExpandUp", KeyType.kExpandUp},
                {"kCollapseDown", KeyType.kCollapseDown},
                {"kCharacter2Labels", KeyType.kCharacter2Labels},
                {"kSubPanel", KeyType.kSubPanel},
                {"kTashkeel", KeyType.kTashkeel},
            };
            if (!umap.ContainsKey(val))
            {
                Debug.LogError("Unsupported KeyTypeID: " + val);
                return KeyType.kNone;
            }
            return umap[val];
        }

        KeyButtonStyle GetKeyButtonStyle(string val)
        {
            Dictionary<string, KeyButtonStyle> umap = new Dictionary<string, KeyButtonStyle>
            {
                {"kDefault", KeyButtonStyle.kDefault},
                {"kLight", KeyButtonStyle.kLight},
                {"kMedium", KeyButtonStyle.kMedium},
                {"kDark", KeyButtonStyle.kDark},
                {"kReturn", KeyButtonStyle.kReturn},
                {"kSpaceBar", KeyButtonStyle.kSpaceBar}
            };
            if (!umap.ContainsKey(val))
            {
                Debug.LogError("Unsupported KeyTypeID: " + val);
                return KeyButtonStyle.kDefault;
            }
            return umap[val];
        }

        private LayoutType? GetLayoutType(string val)
        {
            Dictionary<string, LayoutType> umap = new Dictionary<string, LayoutType>
            {
                {"kFull", LayoutType.kFull},
                {"kEmail", LayoutType.kEmail},
                {"kBasic", LayoutType.kBasic},
                {"kNumeric", LayoutType.kNumeric},
                {"kNumericSymbols", LayoutType.kNumericSymbols},
                {"kURL", LayoutType.kURL}
            };
            if (!umap.ContainsKey(val))
            {
                Debug.LogError("Unsupported KeyboardPageSetID: %s" + val);
                return null;
            }
            else
            {
                return umap[val];
            }
        }

        private LinkButton GetLinkButton(string linkButtonString)
        {
            Dictionary<string, LinkButton> linkButtonStringToEnum = new Dictionary<string, LinkButton>
            {
                {"kNone", LinkButton.kNone},
                {"kCenterLeft", LinkButton.kCenterLeft},
                {"kCenterRight", LinkButton.kCenterRight},
                {"kLowerLeft", LinkButton.kLowerLeft},
                {"kLowerRight", LinkButton.kLowerRight},
                {"kUpperLeft", LinkButton.kUpperLeft},
                {"kUpperRight", LinkButton.kUpperRight}
            };
            if (!linkButtonStringToEnum.ContainsKey(linkButtonString))
            {
                return LinkButton.kNone;
            }
            else
            {
                return linkButtonStringToEnum[linkButtonString];
            }
        }

        private AudioAssetType GetAudioAssetType(string audioAssetTypeString)
        {
            Dictionary<string, AudioAssetType> audioAssetTypeStringToEnum =
                new Dictionary<string, AudioAssetType>
                {
                    {"kDefault", AudioAssetType.kDefault},
                    {"kHover", AudioAssetType.kHover},
                    {"kKeyDown", AudioAssetType.kKeyDown},
                    {"kAltChar", AudioAssetType.kAltChar},
                    {"kClear", AudioAssetType.kClear},
                    {"kDelete", AudioAssetType.kDelete},
                    {"kShift", AudioAssetType.kShift},
                    {"kSpace", AudioAssetType.kSpace},
                    {"kSubmit", AudioAssetType.kSubmit},
                    {"kSwitchLayout", AudioAssetType.kSwitchLayout},
                    {"kTab", AudioAssetType.kTab},
                    {"kSelectEntered", AudioAssetType.kSelectEntered},
                    {"kSelectExited", AudioAssetType.kSelectExited},
                    {"kNav", AudioAssetType.kNav},
                    {"kGrab", AudioAssetType.kGrab},
                    {"kRelease", AudioAssetType.kRelease},
                    {"kExpandOption", AudioAssetType.kExpandOption},
                    {"kNone", AudioAssetType.kNone}
                };
            if (!audioAssetTypeStringToEnum.ContainsKey(audioAssetTypeString))
            {
                Debug.LogError("Unsupported AudioAssetType: %s" + audioAssetTypeString);
                return AudioAssetType.kNone;
            }
            else
            {
                return audioAssetTypeStringToEnum[audioAssetTypeString];
            }
        }

        private LocaleDesc BuildLanguage(LayoutConfig_JSON layoutConfig)
        {
            Code localeCode = StringToLocale(layoutConfig.LanguageID);
            TMP_FontAsset fontAsset = null;
            if (_fontAssetMap.Dictionary.ContainsKey(localeCode))
            {
                fontAsset = _fontAssetMap.Dictionary[localeCode];
            }
            if (!bool.TryParse(layoutConfig.IsRightToLeft, out bool isRTL))
            {
                isRTL = false;
            }
            if (!bool.TryParse(layoutConfig.IsUGUI, out bool isUGUI))
            {
                isUGUI = false;
            }
            LocaleDesc localeDesc = new LocaleDesc();
            localeDesc.FontAsset = fontAsset;
            localeDesc.IsRightToLeft = isRTL;
            localeDesc.IsUGUI = isUGUI;
            _localeMap.Add(localeCode, localeDesc);
            return _localeMap[localeCode];
        }

        private PageLayoutDesc BuildPageLayout(LocaleDesc localeDescription, PageLayout_JSON pageLayout)
        {
            Vector3 defaultKeySize = new Vector3(
                (float) pageLayout.DefaultKeySizeX,
                (float) pageLayout.DefaultKeySizeY,
                (float) pageLayout.DefaultKeySizeZ
            );
            float keyboardWidth = pageLayout.KeyboardWidth;
            float keyGap = pageLayout.KeyGap;
            PageID? pageCode = GetPageCode(pageLayout.PageLayoutID);
            if (!pageCode.HasValue)
            {
                Debug.LogError("Aborting BuildPageLayout early due to unsupported PagelayoutID");
                return null;
            }
            localeDescription.LayoutMap.Add(pageCode.Value, new PageLayoutDesc());
            localeDescription.LayoutMap[pageCode.Value].DefaultKeySize = defaultKeySize;
            localeDescription.LayoutMap[pageCode.Value].MotionRangeZStart =
                pageLayout.MotionRangeZStart;
            localeDescription.LayoutMap[pageCode.Value].MotionRangeZEnd =
                pageLayout.MotionRangeZEnd;
            localeDescription.LayoutMap[pageCode.Value].FrontSideDetectThreshold =
                pageLayout.FrontSideDetectThreshold;
            localeDescription.LayoutMap[pageCode.Value].KeyboardWidth = keyboardWidth;
            localeDescription.LayoutMap[pageCode.Value].KeyGap = keyGap;
            return localeDescription.LayoutMap[pageCode.Value];
        }

        private List<KeyLayoutDesc> BuildKeyRow(PageLayoutDesc pageLayoutDesc)
        {
            List<KeyLayoutDesc> keyRow = new List<KeyLayoutDesc>();
            pageLayoutDesc.KeyLayoutRows.Add(keyRow);
            int index = pageLayoutDesc.KeyLayoutRows.Count - 1;
            return pageLayoutDesc.KeyLayoutRows[index];
        }

        private void BuildKey(List<KeyLayoutDesc> keyRow, Key_JSON key)
        {
            List<LabeledKey> labeledKeys = new List<LabeledKey>();
            LabeledKey defaultLabeledKey = new LabeledKey();
            string defaultChar = "";
            string alternateKeySelectorID = "";
            float widthWeight = DEFAULT_WIDTH_WEIGHT;
            float heightWeight = DEFAULT_HEIGHT_WEIGHT;
            float gapOnLeft = 0.0f;
            float fontSizeMin = DEFAULT_FONT_SIZE_MIN;
            float fontSizeMax = DEFAULT_FONT_SIZE_MAX;
            bool overrideButtonStyle = false;
            OverrideFontSizeOptions overrideFontSize = OverrideFontSizeOptions.NONE;
            bool showAccentEllipse = false;
            Vector4 labelMargins = Vector4.zero;
            Vector4 secondaryMargins = Vector4.zero;
            bool overrideLabelMargins = false;
            bool overrideSecondaryMargins = false;
            if (key.WidthWeight != null)
            {
                widthWeight = float.Parse(key.WidthWeight, NumberStyles.Float, CULTURE_FORMATTER);
                if (widthWeight <= 0.0f)
                {
                    Debug.LogError("Bad WidthWeight value, expected positive number.");
                    return;
                }
            }
            if (key.HeightWeight != null)
            {
                heightWeight = float.Parse(key.HeightWeight,
                                           NumberStyles.Float,
                                           CULTURE_FORMATTER);
                if (heightWeight <= 0.0f)
                {
                    Debug.LogError("Bad HeightWeight value, expected positive number.");
                    return;
                }
            }
            if (key.AlternateKeySelectorID != null)
            {
                alternateKeySelectorID = key.AlternateKeySelectorID;
            }
            if (key.DefaultChar != null)
            {
                defaultChar = key.DefaultChar;
            }
            if (key.KeyTypeID == null)
            {
                Debug.LogError("The KeyTypeID key is required but not present!");
                return;
            }
            if (key.GapOnLeft != null)
            {
                gapOnLeft = float.Parse(key.GapOnLeft, NumberStyles.Float, CULTURE_FORMATTER);
                if (gapOnLeft < 0.0f)
                {
                    Debug.LogError("Bad GapOnLeft value, expected positive number");
                    return;
                }
            }
            if (key.FontSizeMin != null)
            {
                fontSizeMin = float.Parse(key.FontSizeMin, NumberStyles.Float, CULTURE_FORMATTER);
                if (fontSizeMin <= 0.0f)
                {
                    Debug.LogError("Bad FontSizeMin value, expected positive number.");
                    return;
                }
                overrideFontSize |= OverrideFontSizeOptions.OVERRIDE_MIN;
            }
            if (key.FontSizeMax != null)
            {
                fontSizeMax = float.Parse(key.FontSizeMax, NumberStyles.Float, CULTURE_FORMATTER);
                if (fontSizeMax <= 0.0f)
                {
                    Debug.LogError("Bad FontSizeMax value, expected positive number.");
                    return;
                }
                overrideFontSize |= OverrideFontSizeOptions.OVERRIDE_MAX;
            }
            KeyButtonStyle style = KeyButtonStyle.kDefault;
            if (key.KeyButtonStyle != null)
            {
                style = GetKeyButtonStyle(key.KeyButtonStyle);
                overrideButtonStyle = true;
            }
            string secondaryStr = "";
            if (key.SecondaryStr != null && key.SecondaryStr != "")
            {
                secondaryStr = key.SecondaryStr;
            }
            string keyTypeStr = key.KeyTypeID;
            KeyType keyType = GetKeyType(keyTypeStr);
            if (keyType == KeyType.kNone)
            {
                Debug.LogError("Aborting BuildKey early due to unsupported KeyTypeID: " + keyTypeStr);
            }
            if (_alternateKeySelectorMap.ContainsKey(alternateKeySelectorID))
            {
                // Make a new copy here
                labeledKeys = new List<LabeledKey>(_alternateKeySelectorMap[alternateKeySelectorID]);
            }
            if (!string.IsNullOrEmpty(defaultChar))
            {
                U32string defaultChar32 = new U32string(defaultChar);
                defaultLabeledKey.Label = defaultChar32;
                defaultLabeledKey.CharacterCode = defaultChar32.Data[0];
            }
            if (!string.IsNullOrEmpty(key.ShowAccentEllipse))
            {
                showAccentEllipse = bool.Parse(key.ShowAccentEllipse);
            }
            if (key.LabelMargins != null)
            {
                overrideLabelMargins = TryParseVector4(key.LabelMargins.Left,
                                                       key.LabelMargins.Top,
                                                       key.LabelMargins.Right,
                                                       key.LabelMargins.Bottom,
                                                       out labelMargins,
                                                       DEFAULT_TEXT_MARGINS);
            }
            if (key.SecondaryMargins != null)
            {
                overrideSecondaryMargins = TryParseVector4(key.SecondaryMargins.Left,
                                                           key.SecondaryMargins.Top,
                                                           key.SecondaryMargins.Right,
                                                           key.SecondaryMargins.Bottom,
                                                           out secondaryMargins,
                                                           DEFAULT_TEXT_MARGINS);
            }
            string subPanelID = !string.IsNullOrEmpty(key.SubPanelID) ? key.SubPanelID : "";
            KeyDesc keyDesc = new KeyDesc(keyType, defaultLabeledKey, labeledKeys);
            KeyLayoutDesc keyLayoutDesc = new KeyLayoutDesc()
            {
                ThisKeyDesc = keyDesc,
                KeyWidthWeight = widthWeight,
                KeyHeightWeight = heightWeight,
                FontSizeMin = fontSizeMin,
                FontSizeMax = fontSizeMax,
                GapOnLeft = gapOnLeft,
                KeyButtonStyle = style,
                SecondaryStr = secondaryStr,
                SubPanelID = subPanelID,
                OverrideButtonStyle = overrideButtonStyle,
                OverrideFontSize = overrideFontSize,
                ShowAccentEllipse = showAccentEllipse,
                LabelMargins = labelMargins,
                SecondaryMargins = secondaryMargins,
                OverrideLabelMargins = overrideLabelMargins,
                OverrideSecondaryMargins = overrideSecondaryMargins
            };
            keyRow.Add(keyLayoutDesc);
        }

        private PageLayoutProperties BuildKeyboardPageSet(LocaleDesc localeDescription,
            KeyboardPageSet_JSON keyboardPageSet)
        {
            LayoutType? layoutType = GetLayoutType(keyboardPageSet.KeyboardPageSetID);
            if (layoutType == null)
            {
                Debug.LogError("Aborting BuildKeyboardPageSet early due to unsupported KeyboardPageSetID!");
                return null;
            }
            PageID? pageCode = GetPageCode(keyboardPageSet.PageLayoutID);
            if (pageCode == null)
            {
                Debug.LogError("Aborting BuildKeyboardPageSet early due to unsupported PageLayoutID!");
                return null;
            }
            localeDescription.PropertyMap.Add(layoutType.Value, new PageLayoutProperties());
            localeDescription.PropertyMap[layoutType.Value].DefaultPage = pageCode.Value;
            return localeDescription.PropertyMap[layoutType.Value];
        }

        private KeyTypePageLinkVec BuildKeyboardPageSetProperty(
            PageLayoutProperties pageLayoutProperties, PageID pageCode)
        {
            pageLayoutProperties.PageLinkMap.Add(pageCode, new KeyTypePageLinkVec());
            return pageLayoutProperties.PageLinkMap[pageCode];
        }

        private void BuildKeyPage(KeyTypePageLinkVec keyPages, KeyPage_JSON keyPage, PageID defaultPageCode)
        {
            string keyTypeStr = keyPage.KeyTypeID;
            KeyType keyType = GetKeyType(keyTypeStr);
            if (keyType == KeyType.kNone)
            {
                Debug.LogError("Aborting BuildKeyPage early due to unsupported KeyTypeID: " + keyTypeStr);
            }
            string pageLayoutIDString = keyPage.PageLayoutID;
            PageID? pageCode = string.IsNullOrEmpty(pageLayoutIDString)
                ? defaultPageCode
                : GetPageCode(pageLayoutIDString);
            if (pageCode == null)
            {
                pageCode = defaultPageCode;
            }
            keyPages.Add(new KeyValuePair<KeyType, PageLink>(
                keyType, new PageLink(pageCode.Value)));
        }

        private List<U32string> BuildSwappableLabelKey(LocaleDesc localeDescription,
            SwappableLabelKey_JSON swappableLabelKey)
        {
            if (swappableLabelKey.KeyTypeID == null)
            {
                Debug.LogError("The KeyTypeID value is required but not present!");
                return null;
            }
            if (swappableLabelKey.Char == null)
            {
                Debug.LogError("The Char value is required but not present!");
                return null;
            }
            string keyTypeStr = swappableLabelKey.KeyTypeID;
            KeyType keyType = GetKeyType(keyTypeStr);
            if (keyType == KeyType.kNone)
            {
                Debug.LogError(
                    "Aborting BuildSwappableLabelKey early due to unsupported KeyTypeID: " + keyTypeStr);
                return null;
            }
            localeDescription.AdditionalKeyLabels.Add(keyType, new MultiLabeledKey());
            string character = swappableLabelKey.Char;
            U32string character32 = new U32string(character);
            localeDescription.AdditionalKeyLabels[keyType].CharacterCode = character32.Data[0];
            return localeDescription.AdditionalKeyLabels[keyType].Labels;
        }

        private void BuildSwappableLabelKeyLabel(List<U32string> labels, Label_JSON label)
        {
            if (string.IsNullOrEmpty(label.Label))
            {
                Debug.LogError("The Label key is required but not present!");
                return;
            }
            labels.Add(new U32string(label.Label));
        }

        private void PrintAll()
        {
            Debug.Log("_alternateKeySelectorMap is: ");
            foreach (KeyValuePair<string, List<LabeledKey>> pair in _alternateKeySelectorMap)
            {
                Debug.Log("selector ID: " + pair.Key);
                string labeledKeysStr = "labeled keys: ";
                foreach (LabeledKey key in pair.Value)
                {
                    labeledKeysStr += ("LabeledKey {CharacterCode: " + key.CharacterCode.ToString() +
                                       "; label: " + key.Label.ToString() + "}, ");
                }
                Debug.Log(labeledKeysStr);
            }
            Debug.Log("\n\n##########################################");
            Debug.Log("_localeMap is: ");
            foreach (KeyValuePair<Code, LocaleDesc> pair in _localeMap)
            {
                Debug.Log("key (Code): " + pair.Key);
                Debug.Log("value (LocaleDesc): " + pair.Value.ToStr() + "\n");
            }
        }
    }
}
