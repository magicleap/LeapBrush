// Copyright (c) 2022 Magic Leap, Inc. All Rights Reserved.
// Please see the top-level LICENSE.md in this distribution
// for terms and conditions governing this file.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using TMPro;
using System.IO;
using System.Linq;
using UnityEngine.Serialization;

namespace MagicLeap.DesignToolkit.Keyboard
{
    using PageID = System.UInt32;

    public class VirtualKeyboardLayoutGen : MonoBehaviour
    {
        [System.Serializable]
        public class AudioAssets
        {
#if FIXME
            public SoundDefinition Default;
            public SoundDefinition Hover;
            public SoundDefinition KeyDown;
            public SoundDefinition AltChar;
            public SoundDefinition Clear;
            public SoundDefinition Delete;
            public SoundDefinition Shift;
            public SoundDefinition Space;
            public SoundDefinition Submit;
            public SoundDefinition SwitchLayout;
            public SoundDefinition Tab;
            public SoundDefinition SelectEntered;
            [FormerlySerializedAs("Select")]
            public SoundDefinition SelectExited;
            public SoundDefinition Nav;
            public SoundDefinition Grab;
            public SoundDefinition Release;
            public SoundDefinition ExpandOption;
            public SoundDefinition None;
#endif
        }

        public static readonly string KEYBOARD_KEY_NAME_PREFIX = "ML_VKB_btn-";
        public static readonly float DEFAULT_KEYBOARD_THICKNESS = 30f;

        // Background scale and position
        public static readonly float BACKGROUND_Z_OFFSET = 0.005f;
        public static readonly float BACKGROUND_EDGE_WIDTH = 0.006f;
        public static readonly float HANDLE_WIDTH = 0.02f;
        public static readonly float BACKGROUND_SIL_WIDTH = 0.003f;
        public static readonly Vector3 CENTER_TEXT_POS = new Vector3(0.0f, 0.0f, -0.0001f);
        public static readonly Vector3 CENTER_TEXT_POS_CHARACTER = new Vector3(0f, 0.075f, 0f);
        public Code Locale = Code.kEn_US_Unity;
        public bool OverwriteStoredPrefab = false;
        public bool DestroyGeneratedLayoutWhenDone = true;
        [SerializeField]
        private GameObject _backgroundPrefab;
        [SerializeField]
        private GameObject _kCharacterPrefab;
        [SerializeField]
        private GameObject _kTabPrefab;
        [SerializeField]
        private GameObject _kShiftPrefab;
        [SerializeField]
        private GameObject _kAltGrPrefab;
        [SerializeField]
        private GameObject _kBackspacePrefab;
        [SerializeField]
        private GameObject _kEnterPrefab;
        [SerializeField]
        private GameObject _kSpacebarPrefab;
        [SerializeField]
        private GameObject _kCapsLockPrefab;
        [SerializeField]
        private GameObject _kCharacterJapanesePrefab;
        [SerializeField]
        private GameObject _kJPNumSymPrefab;
        [SerializeField]
        private GameObject _kJPEnterPrefab;
        [SerializeField]
        private GameObject _kAccentsPrefab;
        [SerializeField]
        private GameObject _kJPSpacebarPrefab;
        [SerializeField]
        private GameObject _kRayDrumstickSwitchPrefab;
        [SerializeField]
        private GameObject _kHideKeyboardPrefab;
        [SerializeField]
        private GameObject _kAltCharTogglePrefab;
        [SerializeField]
        private GameObject _kPageHiraganaPrefab;
        [SerializeField]
        private GameObject _kPageKatakanaPrefab;
        [SerializeField]
        private GameObject _kJPNewLinePrefab;
        [SerializeField]
        private KeyInfo _UGUIKeyPrefab;
        [SerializeField]
        private KeySubPanel _UGUISubPanelPrefab;
        [SerializeField]
        private GameObject _UGUIKeyboardPrefab;
        [SerializeField]
        private SubPanelCanvasInfo _UGUISubPanelCanvasPrefab;
        [SerializeField]
        private GameObject _kChangeLocalePrefab;
        [SerializeField]
        private GameObject _layoutsParent;
        [SerializeField,
         Tooltip("The Eng prefab that will be overwritten if OverwriteStoredPrefab == true")]
        private GameObject _engLayoutsPrefab;
        [SerializeField,
         Tooltip("The Japan prefab that will be overwritten if OverwriteStoredPrefab == true")]
        private GameObject _japanLayoutsPrefab;
        [SerializeField,
         Tooltip("The Arabic prefab that will be overwritten if OverwriteStoredPrefab == true")]
        private GameObject _arabicLayoutsPrefab;
        [SerializeField,
         Tooltip("The German prefab that will be overwritten if OverwriteStoredPrefab == true")]
        private GameObject _germanLayoutsPrefab;
        [SerializeField,
         Tooltip("The French prefab that will be overwritten if OverwriteStoredPrefab == true")]
        private GameObject _frenchLayoutsPrefab;
        [SerializeField,
         Tooltip("The Spanish prefab that will be overwritten if OverwriteStoredPrefab == true")]
        private GameObject _spanishLayoutsPrefab;
        [SerializeField,
         Tooltip("The Portuguese prefab that will be overwritten if OverwriteStoredPrefab == true")]
        private GameObject _portugueseLayoutsPrefab;
        [SerializeField,
         Tooltip("The Spanish prefab that will be overwritten if OverwriteStoredPrefab == true")]
        private GameObject _italianLayoutsPrefab;
        [SerializeField]
        private AudioAssets _audioAssets;
        [SerializeField]
        private bool _enableHoverSound;

        private VirtualKeyboardBuilder _virtualKeyboardBuilder;
        private Dictionary<KeyType, GameObject> _keyTypeToPrefabMap = new ();
        private Dictionary<Code, GameObject> _languageToPrefabMap = new ();
#if FIXME
        private Dictionary<AudioAssetType, SoundDefinition> _audioAssetTypeToSoundDefinitionMap =
            new ();
#endif
        private GameObject _backgroundParentObj = null;
        private GameObject _keysParentObj;
        private GameObject _keysAndBackgroundParentObj;
        private LayoutInfo _layoutInfo;
        private LayoutType _layoutType = LayoutType.kFull; // currently only support this one
        private Vector2 _maxKeysCanvasSize;

        public bool Init()
        {
            _virtualKeyboardBuilder = GetComponent<VirtualKeyboardBuilder>();
            if (!_virtualKeyboardBuilder)
            {
                Debug.LogError("Cannot find VirtualKeyboardBuilder! Abort!");
                return false;
            }
            bool success = _virtualKeyboardBuilder.Init();
            if (!success)
            {
                Debug.LogError("_virtualKeyboardBuilder init failed!");
                return false;
            }
            CreateKeyTypeToPrefabMap();
            CreateLanguageToPrefabMap();
            CreateAudioAssetTypeToSoundDefinitionMap();
            _maxKeysCanvasSize = Vector2.zero;
            return true;
        }

        public bool GenLanguageLayouts(Code locale)
        {
            if (locale == Code.kAll_Unity)
            {
                List<Code> codesEnLast = Enum.GetValues(typeof(Code)).Cast<Code>()
                    .Where((code) => code != Code.kAll_Unity)
                    .ToList();
                codesEnLast.Sort((a, b) =>
                {
                    if (a == Code.kEn_US_Unity && b != Code.kEn_US_Unity)
                    {
                        return 1;
                    }
                    if (b == Code.kEn_US_Unity && a != Code.kEn_US_Unity)
                    {
                        return -1;
                    }
                    return a.CompareTo(b);
                });

                foreach (Code code in codesEnLast)
                {
                    Debug.LogFormat("Generating layout for {0}", code);
                    if (!GenLanguageLayouts(code))
                    {
                        return false;
                    }
                }

                return true;
            }

            bool success = Init();
            if (success)
            {
                Debug.Log("VirtualKeyboard init() successfully finished");
            }
            else
            {
                Debug.LogError("VirtualKeyboard init() failed");
                return false;
            }
            LocaleDesc localeDesc = GetLocaleDesc(locale);
            ClearLayouts(locale, _layoutsParent);
            foreach (KeyValuePair<PageID, PageLayoutDesc> pair in localeDesc.LayoutMap)
            {
                if (!localeDesc.IsUGUI)
                {
                    _keysAndBackgroundParentObj =
                        new GameObject("layout__" + locale + "__" + (PageCode) pair.Key);
                    _keysAndBackgroundParentObj.transform.localScale = Vector3.one;
                }
                else
                {
                    _keysAndBackgroundParentObj = Instantiate(_UGUIKeyboardPrefab);
                }
                _keysAndBackgroundParentObj.transform.parent = _layoutsParent.transform;
                _keysAndBackgroundParentObj.transform.localPosition = Vector3.zero;
                _keysAndBackgroundParentObj.transform.localRotation = Quaternion.identity;
                GenOneLayout(locale, (PageCode) pair.Key);
            }
            CreateKeySubPanels(locale);

            // Choose which one to set active (the first layout to load when using this language)
            LayoutInfo[] layoutInfos = _layoutsParent.GetComponentsInChildren<LayoutInfo>();
            PageID defaultPage = GetPageLayoutProperties(locale, _layoutType).DefaultPage;
            for (int i = 0; i < layoutInfos.Length; i++)
            {
                GameObject layoutObj = _layoutsParent.transform.GetChild(i).gameObject;
                LayoutInfo layoutInfo = layoutObj.GetComponent<LayoutInfo>();
                if (!layoutInfo)
                {
                    Debug.LogError("Missing LayoutInfo!");
                    continue;
                }
                PageCode curPageCode = layoutInfo.ThisPageCode;
                layoutInfo.IsRightToLeft = localeDesc.IsRightToLeft;
                if (defaultPage != (PageID) curPageCode)
                {
                    _layoutsParent.transform.GetChild(i).gameObject.SetActive(false);
                }
            }
#if UNITY_EDITOR
            if (OverwriteStoredPrefab)
            {
                OverwritePrefab(locale);
            }
#endif
            if (DestroyGeneratedLayoutWhenDone)
            {
                ClearLayouts(locale, _layoutsParent);
            }
            return true;
        }

        private void CreateKeyTypeToPrefabMap()
        {
            _keyTypeToPrefabMap.Clear();
            _keyTypeToPrefabMap.Add(KeyType.kCharacter, _kCharacterPrefab);
            _keyTypeToPrefabMap.Add(KeyType.kTab, _kTabPrefab);
            _keyTypeToPrefabMap.Add(KeyType.kShift, _kShiftPrefab);
            _keyTypeToPrefabMap.Add(KeyType.kAltGr, _kAltGrPrefab);
            _keyTypeToPrefabMap.Add(KeyType.kBackspace, _kBackspacePrefab);
            _keyTypeToPrefabMap.Add(KeyType.kEnter, _kEnterPrefab);
            _keyTypeToPrefabMap.Add(KeyType.kSpacebar, _kSpacebarPrefab);
            _keyTypeToPrefabMap.Add(KeyType.kCapsLock, _kCapsLockPrefab);
            _keyTypeToPrefabMap.Add(KeyType.kHiragana, _kCharacterJapanesePrefab);
            _keyTypeToPrefabMap.Add(KeyType.kKatakana, _kCharacterJapanesePrefab);
            _keyTypeToPrefabMap.Add(KeyType.kBlank, _kCharacterJapanesePrefab);
            _keyTypeToPrefabMap.Add(KeyType.kJPNumSym, _kJPNumSymPrefab);
            _keyTypeToPrefabMap.Add(KeyType.kJPEnter, _kJPEnterPrefab);
            _keyTypeToPrefabMap.Add(KeyType.kAccents, _kAccentsPrefab);
            _keyTypeToPrefabMap.Add(KeyType.kJPSpacebar, _kJPSpacebarPrefab);
            _keyTypeToPrefabMap.Add(KeyType.kJPNewLine, _kJPNewLinePrefab);
            _keyTypeToPrefabMap.Add(KeyType.kRayDrumstickSwitch, _kRayDrumstickSwitchPrefab);
            _keyTypeToPrefabMap.Add(KeyType.kHideKeyboard, _kHideKeyboardPrefab);
            _keyTypeToPrefabMap.Add(KeyType.kJPNextSuggestion, _kCharacterJapanesePrefab);
            _keyTypeToPrefabMap.Add(KeyType.kAltCharToggle, _kAltCharTogglePrefab);
            _keyTypeToPrefabMap.Add(KeyType.kPageHiragana, _kPageHiraganaPrefab);
            _keyTypeToPrefabMap.Add(KeyType.kPageKatakana, _kPageKatakanaPrefab);
            _keyTypeToPrefabMap.Add(KeyType.kChangeLocale, _kChangeLocalePrefab);
        }

        private void CreateLanguageToPrefabMap()
        {
            _languageToPrefabMap.Clear();
            _languageToPrefabMap.Add(Code.kEn_US_Unity, _engLayoutsPrefab);
            _languageToPrefabMap.Add(Code.kJp_JP_Unity, _japanLayoutsPrefab);
            _languageToPrefabMap.Add(Code.kAr_AR_Unity, _arabicLayoutsPrefab);
            _languageToPrefabMap.Add(Code.kDe_DE_Unity, _germanLayoutsPrefab);
            _languageToPrefabMap.Add(Code.kFr_FR_Unity, _frenchLayoutsPrefab);
            _languageToPrefabMap.Add(Code.kEs_ES_Unity, _spanishLayoutsPrefab);
            _languageToPrefabMap.Add(Code.kPt_PT_Unity, _portugueseLayoutsPrefab);
            _languageToPrefabMap.Add(Code.kIt_IT_Unity, _italianLayoutsPrefab);
        }


        private void CreateAudioAssetTypeToSoundDefinitionMap()
        {
#if FIXME
            _audioAssetTypeToSoundDefinitionMap.Clear();
            _audioAssetTypeToSoundDefinitionMap.Add(AudioAssetType.kDefault, _audioAssets.Default);
            _audioAssetTypeToSoundDefinitionMap.Add(AudioAssetType.kHover, _audioAssets.Hover);
            _audioAssetTypeToSoundDefinitionMap.Add(AudioAssetType.kKeyDown, _audioAssets.KeyDown);
            _audioAssetTypeToSoundDefinitionMap.Add(AudioAssetType.kAltChar, _audioAssets.AltChar);
            _audioAssetTypeToSoundDefinitionMap.Add(AudioAssetType.kClear, _audioAssets.Clear);
            _audioAssetTypeToSoundDefinitionMap.Add(AudioAssetType.kDelete, _audioAssets.Delete);
            _audioAssetTypeToSoundDefinitionMap.Add(AudioAssetType.kShift, _audioAssets.Shift);
            _audioAssetTypeToSoundDefinitionMap.Add(AudioAssetType.kSpace, _audioAssets.Space);
            _audioAssetTypeToSoundDefinitionMap.Add(AudioAssetType.kSubmit, _audioAssets.Submit);
            _audioAssetTypeToSoundDefinitionMap.Add(
                AudioAssetType.kSwitchLayout, _audioAssets.SwitchLayout);
            _audioAssetTypeToSoundDefinitionMap.Add(AudioAssetType.kTab, _audioAssets.Tab);
            _audioAssetTypeToSoundDefinitionMap.Add(AudioAssetType.kSelectEntered, _audioAssets.SelectEntered);
            _audioAssetTypeToSoundDefinitionMap.Add(AudioAssetType.kSelectExited, _audioAssets.SelectExited);
            _audioAssetTypeToSoundDefinitionMap.Add(AudioAssetType.kNav, _audioAssets.Nav);
            _audioAssetTypeToSoundDefinitionMap.Add(AudioAssetType.kGrab, _audioAssets.Grab);
            _audioAssetTypeToSoundDefinitionMap.Add(AudioAssetType.kRelease, _audioAssets.Release);
            _audioAssetTypeToSoundDefinitionMap.Add(
                AudioAssetType.kExpandOption, _audioAssets.ExpandOption);
            _audioAssetTypeToSoundDefinitionMap.Add(AudioAssetType.kNone, _audioAssets.None);
#endif
        }

        private List<SubPanelDesc> GetSubPanelDescs(Code locale)
        {
            if (!_virtualKeyboardBuilder.GetLocaleMap().ContainsKey(locale))
            {
                Debug.LogError("Could not find map entry for locale: " + locale);
                return null;
            }
            return _virtualKeyboardBuilder.GetLocaleMap()[locale].SubPanels;
        }

        private PageLayoutDesc GetPageLayoutDesc(Code locale, PageID pageID)
        {
            if (!_virtualKeyboardBuilder.GetLocaleMap().ContainsKey(locale))
            {
                Debug.LogError("Could not find map entry for locale: " + locale);
                return null;
            }
            LocaleDesc localeDesc = _virtualKeyboardBuilder.GetLocaleMap()[locale];
            if (!localeDesc.LayoutMap.ContainsKey(pageID))
            {
                Debug.LogError("Could not find page in that locale, page: " + (PageCode) pageID);
                return null;
            }
            return localeDesc.LayoutMap[pageID];
        }

        private LocaleDesc GetLocaleDesc(Code locale)
        {
            if (!_virtualKeyboardBuilder.GetLocaleMap().ContainsKey(locale))
            {
                Debug.LogError("Could not find map entry for locale: " + locale);
                return null;
            }
            return _virtualKeyboardBuilder.GetLocaleMap()[locale];
        }

        private PageLayoutProperties GetPageLayoutProperties(Code locale, LayoutType layoutType)
        {
            if (!_virtualKeyboardBuilder.GetLocaleMap().ContainsKey(locale))
            {
                Debug.LogError("Could not find map entry for locale: " + locale);
                return null;
            }
            LocaleDesc localeDesc = _virtualKeyboardBuilder.GetLocaleMap()[locale];
            if (!localeDesc.PropertyMap.ContainsKey(layoutType))
            {
                Debug.LogError("Could not find map entry for layoutType: " + layoutType);
                return null;
            }
            return localeDesc.PropertyMap[layoutType];
        }

        private bool GenOneLayout(Code locale, PageCode pageCode)
        {
            PageLayoutDesc pageLayoutDesc = GetPageLayoutDesc(locale, (PageID) pageCode);
            LocaleDesc localeDesc = GetLocaleDesc(locale);
            if (pageLayoutDesc == null)
            {
                Debug.LogError("Layout description missing for given page: locale: " +
                               locale + " page: " + pageCode);
                return false;
            }
            Vector3 extents = GetKeyboardKeysExtents(pageLayoutDesc);
            _maxKeysCanvasSize.x =
                extents.x > _maxKeysCanvasSize.x ? extents.x : _maxKeysCanvasSize.x;
            _maxKeysCanvasSize.y =
                extents.y > _maxKeysCanvasSize.y ? extents.y : _maxKeysCanvasSize.y;
            CreateInfo(locale, pageCode, extents);
            CreateKeys(
                pageLayoutDesc,
                new Vector2(-extents.x * 0.5f, extents.y * 0.5f),
                locale, localeDesc);
            CreateBackground(localeDesc, extents);
            return true;
        }

        private Vector3 GetKeyboardKeysExtents(PageLayoutDesc pageLayoutDesc)
        {
            // Currently no key spans more than one row, so we can calculate height like this
            float keyGap = pageLayoutDesc.KeyGap;
            Vector2 defKeySize = new Vector2(pageLayoutDesc.DefaultKeySize.x, pageLayoutDesc.DefaultKeySize.y);
            int rowCount = pageLayoutDesc.KeyLayoutRows.Count;
            float height = (rowCount + 1) * keyGap + rowCount * defKeySize.y;
            float maxRowWidth = 0.0f;
            for (int i = 0; i < rowCount; i++)
            {
                float rowWidth = pageLayoutDesc.KeyGap;
                for (int j = 0; j < pageLayoutDesc.KeyLayoutRows[i].Count; j++)
                {
                    float widthWeight = pageLayoutDesc.KeyLayoutRows[i][j].KeyWidthWeight;
                    float gapOnLeft = pageLayoutDesc.KeyLayoutRows[i][j].GapOnLeft;
                    rowWidth += (widthWeight * defKeySize.x + pageLayoutDesc.KeyGap + gapOnLeft +
                                 Mathf.Ceil(widthWeight - 1.0f) * pageLayoutDesc.KeyGap);
                }
                if (rowWidth > maxRowWidth)
                {
                    maxRowWidth = rowWidth;
                }
            }
            return new Vector3(maxRowWidth, height, DEFAULT_KEYBOARD_THICKNESS);
        }

        private void ClearLayouts(Code locale, GameObject layoutsParent)
        {
            GameObject origParent = layoutsParent.transform.parent ? layoutsParent.transform.parent.gameObject : null;
            DestroyImmediate(_layoutsParent);
            _layoutsParent = new GameObject("Layouts__" + locale);
            if (origParent)
            {
                _layoutsParent.transform.SetParent(origParent.transform);
            }
            _layoutsParent.transform.localPosition = Vector3.zero;
            _layoutsParent.transform.localRotation = Quaternion.identity;
            _layoutsParent.transform.localScale = Vector3.one;
        }

        private void CreateKeySubPanels(Code locale)
        {
            SubPanelCanvasInfo subPanelCanvasInfo =
                Instantiate<SubPanelCanvasInfo>(_UGUISubPanelCanvasPrefab, _layoutsParent.transform);
            RectTransform canvasTransform =
                (RectTransform)(subPanelCanvasInfo.CanvasObj).transform;
            canvasTransform.sizeDelta = _maxKeysCanvasSize;
            List<SubPanelDesc> subPanelDescs = GetSubPanelDescs(locale);
            LocaleDesc localeDesc = GetLocaleDesc(locale);
            if (subPanelDescs == null || subPanelDescs.Count == 0)
            {
                return;
            }
            foreach (SubPanelDesc subPanelDesc in subPanelDescs)
            {
                KeySubPanel subPanel =
                    Instantiate<KeySubPanel>(_UGUISubPanelPrefab, canvasTransform);
                float mult = 1f;
                Vector2 size = Vector2.zero;
                GetKeySubPanelSize(subPanelDesc, ref mult, ref size);
                Vector2 keyPosition = Vector2.zero;
                SetSubPanelSizeAndKeyPosition(
                    subPanelDesc, subPanel, size, mult, ref keyPosition);
                for (int idx = 0; idx < subPanelDesc.Keys.Count; idx++)
                {
                    ConfigSubPanelKey(subPanelDesc, subPanel, idx, ref keyPosition, locale, localeDesc);
                }
                subPanel.IsVertical = subPanelDesc.IsVerticalPanel;
                subPanel.SubPanelID = subPanelDesc.SubPanelID;
                subPanel.ZOffset = subPanelDesc.DefaultKeySize.z;
                subPanel.gameObject.SetActive(false);
                subPanelCanvasInfo.SubPanels.Add(subPanel);
            }
        }

        private void GetKeySubPanelSize(SubPanelDesc subPanelDesc, ref float mult, ref Vector2 size)
        {
            if (!subPanelDesc.IsVerticalPanel)
            {
                float defaultHeight = subPanelDesc.DefaultKeySize.y + subPanelDesc.KeyGap;
                size = new Vector2(0, defaultHeight);
                foreach (KeyLayoutDesc keyDesc in subPanelDesc.Keys)
                {
                    Vector2 keySize = GetKeySize(keyDesc,
                        subPanelDesc.DefaultKeySize,
                        subPanelDesc.KeyGap);
                    if (keySize.y + subPanelDesc.KeyGap > size.y)
                    {
                        size.y = keySize.y + subPanelDesc.KeyGap;
                        mult = size.y / defaultHeight;
                    }
                    size.x += keySize.x + subPanelDesc.KeyGap + keyDesc.GapOnLeft;
                }
            }
            else
            {
                float defaultWidth = subPanelDesc.DefaultKeySize.x + subPanelDesc.KeyGap;
                size = new Vector2(defaultWidth, 0);
                foreach (KeyLayoutDesc keyDesc in subPanelDesc.Keys)
                {
                    Vector2 keySize = GetKeySize(keyDesc,
                        subPanelDesc.DefaultKeySize,
                        subPanelDesc.KeyGap);
                    if (keySize.x + subPanelDesc.KeyGap > size.x)
                    {
                        size.x = keySize.x + subPanelDesc.KeyGap;
                        mult = size.x / defaultWidth;
                    }
                    size.y += keySize.y + subPanelDesc.KeyGap + keyDesc.GapOnLeft;
                }
            }
        }

        private void SetSubPanelSizeAndKeyPosition(SubPanelDesc subPanelDesc,
                                                   KeySubPanel subPanel,
                                                   Vector2 size,
                                                   float mult,
                                                   ref Vector2 position)
        {
            RectTransform subPanelTransform = (RectTransform)subPanel.transform;
            subPanelTransform.sizeDelta = size;
            subPanel.BackgroundCollider.size =
                (Vector3)size + Vector3.forward * subPanel.BackgroundCollider.size.z;
            if (!subPanelDesc.IsVerticalPanel)
            {
                subPanelTransform.pivot = new Vector2(subPanelTransform.pivot.x,
                                                      subPanelTransform.pivot.y / mult);
                position = Vector2.right * (-size.x + subPanelDesc.KeyGap) / 2;
            }
            else
            {
                subPanelTransform.pivot = new Vector2(-subPanelTransform.pivot.x / mult,
                                                      -subPanelTransform.pivot.y);
                position = Vector2.up * (size.y - subPanelDesc.KeyGap) / 2;
            }
        }

        private void ConfigSubPanelKey(
            SubPanelDesc subPanelDesc,
            KeySubPanel subPanel,
            int idx,
            ref Vector2 keyPosition,
            Code locale,
            LocaleDesc localeDesc)
        {
            GameObject key = CreateKey(subPanelDesc.Keys[idx], subPanel.Container);
            Vector2 keySize = GetKeySize(subPanelDesc.Keys[idx],
                                         subPanelDesc.DefaultKeySize,
                                         subPanelDesc.KeyGap);

            if (!subPanelDesc.IsVerticalPanel)
            {
                keyPosition.y = keySize.y / 2;
            }
            else
            {
                keyPosition.x = keySize.x / 2;
            }

            PositionKey(key,
                        keySize,
                        keyPosition,
                        subPanelDesc.Keys[idx].GapOnLeft,
                        subPanelDesc.IsVerticalPanel);
            ConfigKey(key,
                      subPanelDesc.Keys[idx],
                      subPanelDesc.KeyGap,
                      subPanelDesc.DefaultKeySize.z,
                      locale,
                      localeDesc);

            if (!subPanelDesc.IsVerticalPanel)
            {
                keyPosition.x +=
                    keySize.x + subPanelDesc.KeyGap + subPanelDesc.Keys[idx].GapOnLeft;
            }
            else
            {
                keyPosition.y -=
                    keySize.y + subPanelDesc.KeyGap + subPanelDesc.Keys[idx].GapOnLeft;
            }
        }

        private GameObject CreateKey(KeyLayoutDesc origKeyLayoutDesc, GameObject parentNode)
        {
            KeyDesc origKeyDesc = origKeyLayoutDesc.ThisKeyDesc;
            string label = origKeyDesc.DefaultLabeledKey.Label.ToString();

            // Instantiate the key
            GameObject keyButton;
            if (_layoutInfo.IsUGUI)
            {
                keyButton = Instantiate(_UGUIKeyPrefab, parentNode.transform, false).gameObject;
            }
            else
            {
                GameObject prefabToUse = _keyTypeToPrefabMap.ContainsKey(origKeyDesc.KeyType)
                    ? _keyTypeToPrefabMap[origKeyDesc.KeyType]
                    : _kCharacterPrefab;
                keyButton = GameObject.Instantiate(
                    prefabToUse,
                    parentNode.transform,
                    false
                );
            }
            keyButton.transform.localPosition = Vector3.zero;
            keyButton.transform.localRotation = Quaternion.identity;
            keyButton.transform.localScale = Vector3.one;
            keyButton.name = KEYBOARD_KEY_NAME_PREFIX + label;
            return keyButton;
        }

        private void PositionKey(GameObject key, Vector2 keySize,
            Vector2 startCoords, float gapOnLeft = 0f, bool IsVertical = false)
        {
            float xOffset = keySize.x * 0.5f;
            float yOffset = keySize.y * 0.5f;
            if (key.GetComponent<KeyInfo>().IsUGUI)
            {
                if (!IsVertical)
                {
                    ((RectTransform)key.transform).anchoredPosition = new Vector2(
                        startCoords.x + xOffset + gapOnLeft, startCoords.y - yOffset);
                }
                else
                {
                    ((RectTransform)key.transform).anchoredPosition = new Vector2(
                        startCoords.x - xOffset, startCoords.y - yOffset - gapOnLeft);
                }
            }
            else
            {
                key.transform.localPosition = new Vector3(
                    startCoords.x + xOffset + gapOnLeft, startCoords.y - yOffset, key.transform.localPosition.z);
            }
        }

        private void ConfigKey(
            GameObject keyObj,
            KeyLayoutDesc origKeyLayoutDesc,
            float keyGapSize,
            float zSize,
            Code locale,
            LocaleDesc localeDesc)
        {
            KeyInfo keyInfo = keyObj.GetComponent<KeyInfo>();
            if (!keyInfo)
            {
                Debug.LogError("Could not find KeyInfo script on key prefab, aborting!");
                return;
            }
            keyInfo.KeyType = origKeyLayoutDesc.ThisKeyDesc.KeyType;
            keyInfo.SubPanelID = origKeyLayoutDesc.SubPanelID;
            if (keyInfo.IsUGUI)
            {
                KeyBuilderSettings settings =
                    _virtualKeyboardBuilder.GetKeyBuilderSettings(locale, keyInfo.KeyType);
                settings.LayoutInfo = _layoutInfo;

                KeyStyle prevKeyStyle = settings.KeyButtonStyle;
                if (origKeyLayoutDesc.OverrideButtonStyle)
                {
                    settings.KeyButtonStyle =
                        _virtualKeyboardBuilder.GetKeyStyle(origKeyLayoutDesc.KeyButtonStyle);
                }

                KeyBuilder.ConfigureKey(keyInfo, settings, origKeyLayoutDesc);
                KeyBuilder.ScaleKey(keyInfo, origKeyLayoutDesc, keyGapSize, zSize);

                // Toggle the key on when necessary
                if ((keyInfo.KeyType == KeyType.kPageHiragana &&
                     _layoutInfo.ThisPageCode == PageCode.kHiragana) ||
                    (keyInfo.KeyType == KeyType.kPageKatakana &&
                     _layoutInfo.ThisPageCode == PageCode.kKatakana) ||
                    (keyInfo.KeyType == KeyType.kJPNumSym &&
                     _layoutInfo.ThisPageCode == PageCode.kNumericSymbols) ||
                    (keyInfo.KeyType == KeyType.kCapsLock &&
                     _layoutInfo.ThisPageCode == PageCode.kCapsLock) ||
                    (keyInfo.KeyType == KeyType.kAltGr &&
                     (_layoutInfo.ThisPageCode == PageCode.kAltLowerLetters ||
                      _layoutInfo.ThisPageCode == PageCode.kAltUpperLetters)))
                {
                    keyObj.GetComponent<KeyToggle>().Toggle(true);
                }
                settings.KeyButtonStyle = prevKeyStyle;
                ConfigKeyAudioHandler(keyInfo, localeDesc);
                ConfigInteractable(keyObj);
                return;
            }
            switch (keyInfo.KeyType)
            {
                case KeyType.kCharacter:
                case KeyType.kCapsLock:
                case KeyType.kAltGr:
                case KeyType.kBackspace:
                case KeyType.kHiragana:
                case KeyType.kKatakana:
                case KeyType.kJPNumSym:
                case KeyType.kAccents:
                case KeyType.kJPNewLine:
                case KeyType.kJPNextSuggestion:
                case KeyType.kAltCharToggle:
                case KeyType.kPageHiragana:
                case KeyType.kPageKatakana:
                    if (keyInfo.KeyTextPrimary == null)
                    {
                        Debug.LogError("Missing KeyTextMesh on KeyInfo, aborting!");
                        return;
                    }
                    string text = origKeyLayoutDesc.ThisKeyDesc.DefaultLabeledKey.Label.ToString();
                    keyInfo.KeyTextPrimary.text = text;
                    // If the key includes secondary text
                    if (keyInfo.KeyTextSecondary != null &&
                        origKeyLayoutDesc.SecondaryStr != "")
                    {
                        keyInfo.KeyTextSecondary.gameObject.SetActive(true);
                        keyInfo.KeyTextSecondary.text = origKeyLayoutDesc.SecondaryStr;
                    }
                    else if (keyInfo.KeyType == KeyType.kCharacter)
                    {
                        // Center the text
                        keyInfo.KeyTextPrimary.gameObject.transform.localPosition = CENTER_TEXT_POS_CHARACTER;
                        keyInfo.KeyTextSecondary.gameObject.SetActive(false);
                    }
                    if (keyInfo.KeyType == KeyType.kCharacter ||
                        keyInfo.KeyType == KeyType.kHiragana ||
                        keyInfo.KeyType == KeyType.kKatakana)
                    {
                        // Set the content to type when key click handler is invoked
                        keyInfo.TextToType = text;
                    }
                    else
                    {
                        keyInfo.TextToType = "";
                    }

                    // Toggle the key on when necessary
                    if ((keyInfo.KeyType == KeyType.kPageHiragana &&
                         _layoutInfo.ThisPageCode == PageCode.kHiragana) ||
                        (keyInfo.KeyType == KeyType.kPageKatakana &&
                         _layoutInfo.ThisPageCode == PageCode.kKatakana) ||
                        (keyInfo.KeyType == KeyType.kJPNumSym &&
                         _layoutInfo.ThisPageCode == PageCode.kNumericSymbols) ||
                        (keyInfo.KeyType == KeyType.kCapsLock &&
                         _layoutInfo.ThisPageCode == PageCode.kCapsLock) ||
                        (keyInfo.KeyType == KeyType.kAltGr &&
                         (_layoutInfo.ThisPageCode == PageCode.kAltLowerLetters ||
                          _layoutInfo.ThisPageCode == PageCode.kAltUpperLetters)))
                    {
                        keyObj.GetComponent<KeyToggle>().Toggle(true);
                    }

                    // Add special key(s) to layout info
                    if (keyInfo.KeyType == KeyType.kAccents)
                    {
                        _layoutInfo.JPAccentsKeyToggle = keyObj.GetComponent<KeyToggle>();
                    }
                    break;
                case KeyType.kSpacebar:
                case KeyType.kJPSpacebar:
                    keyInfo.TextToType = " ";
                    break;
                case KeyType.kTab:
                    keyInfo.TextToType = "    ";
                    break;
                case KeyType.kShift:
                    ShiftKeyBehavior behavScript = keyObj.GetComponent<ShiftKeyBehavior>();
                    if (behavScript)
                    {
                        _layoutInfo.ShiftKeysBehavs.Add(behavScript);
                        ShiftKeyState state = ShiftKeyState.Off;
                        switch (_layoutInfo.ThisPageCode)
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
                    break;
                case KeyType.kRayDrumstickSwitch:
                    _layoutInfo.RayDrumstickSwitchKeyToggle = keyObj.GetComponent<KeyToggle>();
                    break;
                case KeyType.kChangeLocale:
                    _layoutInfo.ChangeLocaleKeyToggle = keyObj.GetComponent<KeyToggle>();
                    break;
                default:
                    break;
            }

            // Key fill style through materials
            //if (origKeyLayoutDesc.KeyButtonStyle != KeyButtonStyle.kDefault)
            //{
            //    Renderer fillObj = new Renderer();
            //    if (keyInfo.KeyFillRenderer != null)
            //    {
            //        fillObj = keyInfo.KeyFillRenderer;
            //    }
            //    if (fillObj == null)
            //    {
            //        Debug.LogError("Missing KeyFillRenderer on KeyInfo, aborting!");
            //        return;
            //    }
            //    Material fillMatToLoad = _buttonFillLightMat;
            //    switch (origKeyLayoutDesc.KeyButtonStyle)
            //    {
            //        case KeyButtonStyle.kLight:
            //            fillMatToLoad = _buttonFillLightMat;
            //            break;
            //        case KeyButtonStyle.kMedium:
            //            fillMatToLoad = _buttonFillMediumMat;
            //            break;
            //        case KeyButtonStyle.kDark:
            //            fillMatToLoad = _buttonFillDarkMat;
            //            break;
            //        default:
            //            break;
            //    }
            //    fillObj.GetComponent<Renderer>().material = fillMatToLoad;
            //}
        }

        private void ConfigKeyAudioHandler(KeyInfo keyInfo, LocaleDesc localeDesc)
        {
            var audioAssetType = AudioAssetType.kDefault;
            if (localeDesc.AudioAssetMap.ContainsKey(keyInfo.KeyType))
            {
                audioAssetType = localeDesc.AudioAssetMap[keyInfo.KeyType];
            }

#if FIXME
            SoundDefinition selectEnteredSoundDefinition = null;
            SoundDefinition selectExitedSoundDefinition = null;
            SoundDefinition hoverSoundDefinition = null;

            if (_audioAssetTypeToSoundDefinitionMap.ContainsKey(AudioAssetType.kSelectEntered))
            {
                selectEnteredSoundDefinition = _audioAssetTypeToSoundDefinitionMap[AudioAssetType.kSelectEntered];
            }

            if (_audioAssetTypeToSoundDefinitionMap.ContainsKey(audioAssetType))
            {
                selectExitedSoundDefinition = _audioAssetTypeToSoundDefinitionMap[audioAssetType];
            }

            if (_audioAssetTypeToSoundDefinitionMap.ContainsKey(AudioAssetType.kHover))
            {
                hoverSoundDefinition = _audioAssetTypeToSoundDefinitionMap[AudioAssetType.kHover];
            }

            keyInfo.ControllerUIAudioBridge.SetSelectEnteredSound(selectEnteredSoundDefinition);
            keyInfo.ControllerUIAudioBridge.SetSelectExitedSound(selectExitedSoundDefinition);
            keyInfo.ControllerUIAudioBridge.SetHoverSound(
                _enableHoverSound ? hoverSoundDefinition : null);
            keyInfo.GestureUIAudioBridge.SetSelectEnteredSound(selectEnteredSoundDefinition);
            keyInfo.GestureUIAudioBridge.SetSelectExitedSound(selectExitedSoundDefinition);
            keyInfo.GestureUIAudioBridge.SetHoverSound(
                _enableHoverSound ? hoverSoundDefinition : null);
#endif
        }

        private void ConfigInteractable(GameObject keyObj)
        {
#if FIXME
            var interactable = keyObj.GetComponent<Interactable>();
            interactable.TimeToDisableInteractionOnEnable = 0.5f;
#endif
        }

        private void CreateKeys(
            PageLayoutDesc pageLayoutDesc,
            Vector2 topLeftCorner,
            Code locale,
            LocaleDesc localeDesc)
        {
            if (!localeDesc.IsUGUI)
            {
                _keysParentObj = new GameObject("Keys");
                _keysParentObj.transform.SetParent(_keysAndBackgroundParentObj.transform);
                _keysParentObj.transform.localPosition = Vector3.zero;
                _keysParentObj.transform.localScale = Vector3.one;
                _keysParentObj.transform.localRotation = Quaternion.identity;
                _layoutInfo.KeysParent = _keysParentObj;
            }
            else
            {
                _keysParentObj = _layoutInfo.KeysParent;
                ((RectTransform) _keysParentObj.transform).sizeDelta = _layoutInfo.Extents;
            }
            float nextKeyStartCoordsX = topLeftCorner.x + pageLayoutDesc.KeyGap;
            float nextKeyStartCoordsY = topLeftCorner.y - pageLayoutDesc.KeyGap;
            float rowSpacing = pageLayoutDesc.DefaultKeySize.y + pageLayoutDesc.KeyGap;
            int rowCount = pageLayoutDesc.KeyLayoutRows.Count;
            for (int i = 0; i < rowCount; i++)
            {
                int rowSize = pageLayoutDesc.KeyLayoutRows[i].Count;
                for (int j = 0; j < rowSize; j++)
                {
                    KeyLayoutDesc keyLayoutDesc = pageLayoutDesc.KeyLayoutRows[i][j];
                    Vector2 keySize = GetKeySize(keyLayoutDesc,
                        pageLayoutDesc.DefaultKeySize,
                        pageLayoutDesc.KeyGap);
                    GameObject keyButton = CreateKey(keyLayoutDesc, _keysParentObj);
                    if (keyButton)
                    {
                        PositionKey(
                            keyButton,
                            keySize,
                            new Vector2(nextKeyStartCoordsX, nextKeyStartCoordsY),
                            keyLayoutDesc.GapOnLeft
                        );
                        ConfigKey(keyButton,
                            keyLayoutDesc,
                            pageLayoutDesc.KeyGap,
                            pageLayoutDesc.DefaultKeySize.z,
                            locale,
                            localeDesc);
                    }
                    nextKeyStartCoordsX += (keySize.x + pageLayoutDesc.KeyGap + keyLayoutDesc.GapOnLeft);
                }
                nextKeyStartCoordsX = topLeftCorner.x + pageLayoutDesc.KeyGap;
                nextKeyStartCoordsY -= rowSpacing;
            }
        }

        private Vector2 GetKeySize(
            KeyLayoutDesc keyLayoutDesc, Vector2 defaultKeySize, float keyGap)
        {
            return new Vector2(
                keyLayoutDesc.KeyWidthWeight * defaultKeySize.x +
                Mathf.Ceil(keyLayoutDesc.KeyWidthWeight - 1.0f) * keyGap,
                keyLayoutDesc.KeyHeightWeight * defaultKeySize.y +
                Mathf.Ceil(keyLayoutDesc.KeyHeightWeight - 1.0f) * keyGap);
        }

#if UNITY_EDITOR
        private void OverwritePrefab(Code locale)
        {
            if (!_languageToPrefabMap.ContainsKey(locale))
            {
                Debug.LogError("Could not find prefab for language of " + locale +
                               "when trying to overwrite it, aborting!");
                return;
            }
            var path = AssetDatabase.GetAssetPath(_languageToPrefabMap[locale]);
            Debug.Log("Path to overwrite prefab is: " + path);
            bool success = false;
            PrefabUtility.SaveAsPrefabAsset(_layoutsParent, path, out success);
            Debug.Log("saving layouts prefab success: " + success);
        }
#endif

        private void CreateBackground(LocaleDesc localeDesc, Vector3 extents)
        {
            if (!localeDesc.IsUGUI)
            {
                if (_backgroundPrefab == null)
                {
                    return;
                }
                _backgroundParentObj = GameObject.Instantiate(_backgroundPrefab, _keysAndBackgroundParentObj.transform);
                _backgroundParentObj.name = "Background";
                _backgroundParentObj.transform.localScale = Vector3.one;
                _backgroundParentObj.transform.localPosition = Vector3.zero;
                _backgroundParentObj.transform.localRotation = Quaternion.identity;
                _layoutInfo.Background = _backgroundParentObj;
            }
            else
            {
                _backgroundParentObj = _layoutInfo.Background;
            }
            BackgroundInfo backgroundInfo =
                _layoutInfo.IsUGUI
                    ? _backgroundParentObj.GetComponentInChildren<BackgroundInfo>()
                    : _backgroundParentObj.GetComponent<BackgroundInfo>();
            if (backgroundInfo == null)
            {
                Debug.LogError("Missing BackgroundInfo script on created Background prefab instance, aborting!");
                return;
            }
            GameObject leftHandleParent = backgroundInfo.LeftHandleParent;
            GameObject leftHandle = backgroundInfo.LeftHandle;
            GameObject leftHandleHighlight = backgroundInfo.LeftHandleHighlight;
            GameObject rightHandleParent = backgroundInfo.RightHandleParent;
            GameObject rightHandle = backgroundInfo.RightHandle;
            GameObject rightHandleHighlight = backgroundInfo.RightHandleHighlight;
            GameObject silLarge = backgroundInfo.SilLarge;
            GameObject panel = backgroundInfo.Panel;
            if (!(leftHandleParent && leftHandle && leftHandleHighlight &&
                  rightHandleParent && rightHandle && rightHandleHighlight &&
                  silLarge && panel))
            {
                Debug.LogError("One or more components in the keyboard background cannot be found, " +
                               "aborting on creating background!");
                return;
            }
            if (_layoutInfo.IsUGUI)
            {
                // With this method, we need to make sure the panel in the background prefab
                // has the correct offsets and the handle parents have the correct width
                // other items will scale accordingly
                RectTransform backgroundTransform = (RectTransform) _backgroundParentObj.transform;
                RectTransform panelTransform = (RectTransform) panel.transform;
                float vEdgeLength =
                    Mathf.Abs(panelTransform.offsetMin.y) + Mathf.Abs(panelTransform.offsetMax.y);
                float hEdgeLength =
                    Mathf.Abs(panelTransform.offsetMin.x) + Mathf.Abs(panelTransform.offsetMax.x);

                // entire background's size
                Vector2 newSize = new Vector2(extents.x + hEdgeLength, extents.y + vEdgeLength);
                backgroundTransform.sizeDelta = newSize;

                // panel's collider
                backgroundInfo.PanelBoxCollider.size = extents;
                backgroundInfo.PanelBoxCollider.center = Vector3.forward * (extents.z / 2);

                // colliders' sizes
                Vector3 leftBoxSize = backgroundInfo.LeftBoxCollider.size;
                Vector3 rightBoxSize = backgroundInfo.RightBoxCollider.size;
                return;
            }
            leftHandleParent.transform.localPosition = new Vector3(
                -extents.x * 0.5f - HANDLE_WIDTH * 0.5f - BACKGROUND_EDGE_WIDTH,
                0.0f,
                BACKGROUND_Z_OFFSET
            );
            leftHandleParent.transform.localScale = new Vector3(
                HANDLE_WIDTH, extents.y + 2.0f * BACKGROUND_EDGE_WIDTH, 1.0f
            );
            leftHandle.transform.localPosition = Vector3.zero;
            leftHandle.transform.localScale = Vector3.one;
            leftHandleHighlight.transform.localPosition = Vector3.zero;
            leftHandleHighlight.transform.localScale = Vector3.one;
            rightHandleParent.transform.localPosition = new Vector3(
                extents.x * 0.5f + HANDLE_WIDTH * 0.5f + BACKGROUND_EDGE_WIDTH,
                0.0f,
                BACKGROUND_Z_OFFSET
            );
            rightHandleParent.transform.localScale = new Vector3(
                HANDLE_WIDTH, extents.y + 2.0f * BACKGROUND_EDGE_WIDTH, 1.0f
            );
            rightHandle.transform.localPosition = Vector3.zero;
            rightHandle.transform.localScale = Vector3.one;
            rightHandleHighlight.transform.localPosition = Vector3.zero;
            rightHandleHighlight.transform.localScale = Vector3.one;
            panel.transform.localPosition = new Vector3(
                0.0f, 0.0f, BACKGROUND_Z_OFFSET
            );
            panel.transform.localScale = new Vector3(
                extents.x + 2.0f * BACKGROUND_EDGE_WIDTH,
                extents.y + 2.0f * BACKGROUND_EDGE_WIDTH,
                1.0f
            );
            silLarge.transform.localPosition = new Vector3(
                0.0f, 0.0f, BACKGROUND_Z_OFFSET
            );
            silLarge.transform.localScale = new Vector3(
                extents.x + 2.0f * (BACKGROUND_EDGE_WIDTH + BACKGROUND_SIL_WIDTH) + 2.0f * HANDLE_WIDTH,
                extents.y + 2.0f * (BACKGROUND_EDGE_WIDTH + BACKGROUND_SIL_WIDTH),
                1.0f
            );
        }

        private void CreateInfo(Code locale, PageCode pageCode, Vector3 extents)
        {
            if (!GetLocaleDesc(locale).IsUGUI)
            {
                _layoutInfo = _keysAndBackgroundParentObj.AddComponent<LayoutInfo>();
            }
            else
            {
                _layoutInfo = _keysAndBackgroundParentObj.GetComponent<LayoutInfo>();
            }
            _layoutInfo.Locale = locale;
            _layoutInfo.ThisPageCode = pageCode;
            _layoutInfo.Extents = extents;
            _layoutInfo.Scale = _layoutInfo.transform.localScale;
            _layoutInfo.ShiftKeysBehavs.Clear();
        }
    }
}
