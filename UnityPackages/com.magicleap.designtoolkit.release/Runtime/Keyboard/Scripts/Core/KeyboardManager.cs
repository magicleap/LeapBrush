// Copyright (c) 2019-present, Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Developer Agreement, located
// here: https://auth.magicleap.com/terms/developer
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.Events;
using RTLTMPro;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Globalization;
using System.Linq;
using MagicLeap.Keyboard;
using MixedReality.Toolkit;

namespace MagicLeap.DesignToolkit.Keyboard
{
    using PageID = System.UInt32;

    [System.Serializable]
    public class PublishKeyEvent : UnityEvent<string, KeyType, bool, string>
    {
    }

    [System.Serializable]
    public class KeyboardLayoutChanged : UnityEvent<Code, PageCode, bool>
    {
    }

    /// <summary>
    /// Manager that handles
    /// 1. Keyboard and Keys configuration
    /// 2. Button Click based on button type
    /// 3. Text Editing (EN)
    /// 4. Text Prediction
    /// </summary>
    public class KeyboardManager : MonoBehaviour
    {
        #region Public Members
        public PublishKeyEvent PublishKeyEvent;
        public KeyboardLayoutChanged KeyboardLayoutChanged;
        public UnityEvent OnKeyboardClose;
        public Code Locale = Code.kEn_US_Unity;
        public string TypedContent = "";
        public bool ExperimentalWordSuggestion = true;
        public bool HideKeyboardOnEnterKeyClicked = true;
        public bool ClearInputFieldOnClose = false;
        public int storedCaretPosition;
        public int anchor;
        public int focus;
        public TMP_InputField InputField => _tmpInputField;
        #endregion Public Members

        #region [SerializeField] Private Members
        [SerializeField]
        private bool _printDebugInfo;
        [SerializeField]
        private string _punctuationCharsToIgnoreOnWhiteSpaceDelete = "@#%&*\"\\";
        [SerializeField]
        private GameObject _layoutsParent;
        [SerializeField]
        private TMP_InputField _tmpInputField;
        [SerializeField]
        private GameObject _displayBar;
        [SerializeField]
        private DisplayBarInfo _displayBarInfo;
        [SerializeField]
        private GameObject _enLayouts;
        [SerializeField]
        private GameObject _jpLayouts;
        [SerializeField]
        private GameObject _arLayouts;
        [SerializeField]
        private GameObject _deLayouts;
        [SerializeField]
        private GameObject _frLayouts;
        [SerializeField]
        private GameObject _esLayouts;
        [SerializeField]
        private GameObject _ptLayouts;
        [SerializeField]
        private GameObject _itLayouts;
        [SerializeField]
        private TextAsset _en_US_dict;
        [SerializeField]
        private TextAsset _ar_dict;
        [SerializeField]
        private TextAsset _de_dict;
        [SerializeField]
        private TextAsset _fr_dict;
        [SerializeField]
        private GameObject _accentsPanel;
        [SerializeField]
        private GameObject _languagePanel;
        [SerializeField]
        private GameObject _suggestionsParent;
        [SerializeField] [Tooltip("If enabled, keyboard will exclude direct interactions")]
        private bool _disableInteractModeSwitch;
#if FIXME
        [SerializeField]
        [Tooltip("If not manually set here, a global search for it is conducted at Start()")]
        private ControllerInput _controllerInput;
#endif
        [SerializeField]
        private bool _debug;
        #endregion [SerializeField] Private Members

        #region [Constants] Private Members
        private readonly char ZERO_WIDTH_CHAR = '\u200B';
        private static readonly float DISPLAY_BAR_Y_OFFSET = 0.02f;
        private static readonly String TEMP_TEXT_PRE_MARK = "<mark>";
        private static readonly String TEMP_TEXT_POST_MARK = "</mark>";
        #endregion [Constants] Private Members

        #region Private Members
        private KeyInfo _curSelectedSubPanelKey = null;
        private Prediction _en_US_prediction;
        private Prediction _ar_prediction;
        private Prediction _de_prediction;
        private Prediction _fr_prediction;
        private int _caretPos;

        private HashSet<char> _punctuationMarksToIgnoreOnWhiteSpaceDeleteSet = new();

        // Info of the current word being processed by the active word suggestion engine
        private volatile int _curWordBeginOffset = -1;
        private volatile StringBuilder _curWord = new();
        private volatile bool _growingCurWord;
        private volatile bool _deleteSpaceAfterSuggestionSelected = false;

        // Info of the EFIGS word suggestion task being launched asynchronously
        private CancellationToken _predictCancelToken;
        private volatile CancellationTokenSource _predictCancelTokenSrc;
        private volatile Task<List<Suggestion>> _launchedTask = null;
        private bool _initialized;
        private VirtualKeyboardBuilder _virtualKeyboardBuilder;
        private PageCode _pageCode = PageCode.kLowerLetters;
        private LayoutType _layoutType = LayoutType.kFull; // currently only support this one
        private Dictionary<Code, GameObject> _languageToPrefabMap = new();
        private LayoutInfo _curLayoutInfo;
        private Code _cacheLastLocale = Code.kEn_US_Unity;
        private PageCode _cacheLastPageCode = PageCode.kLowerLetters;
        private Code _localeToLoadOnEnable;
        private bool _JPSuggestIsExpanded;
        private static int NUM_SUGGESTIONS = 4;
        private static int NUM_CANDIDATES = 20;
        private SubPanelCanvasInfo _subPanelCanvasInfo;
        private bool _isRTL = false;
        private TMP_InputField.ContentType _curInputFieldContentType;
        private KeyboardInputFieldFixer _inputFixer = new KeyboardInputFieldFixer();
        #endregion Private Members

        #region Monobehaviour Methods
        private void Start()
        {
            bool success = Init();
            if (success)
            {
                if (_printDebugInfo)
                {
                    Debug.Log("VirtualKeyboard init() successfully finished");
                }
            }
            else
            {
                Debug.LogError("VirtualKeyboard init() failed");
                return;
            }

            foreach (LayoutInfo layoutInfo in _layoutsParent.GetComponentsInChildren<LayoutInfo>())
            {
                if (layoutInfo.gameObject.activeSelf)
                {
                    _curLayoutInfo = layoutInfo;
                    _pageCode = layoutInfo.ThisPageCode;
                }
            }

            if (_curLayoutInfo == null || _curLayoutInfo.Locale != Locale)
            {
                LoadLayout(Locale, _pageCode, true);
                if (TypedContent == "")
                {
                    _isRTL = _curLayoutInfo.IsRightToLeft;
                }
            }
            // Only needs to connect keys to their callbacks
            else
            {
                ConfigLayouts();
                KeyboardLayoutChanged.Invoke(Locale, _pageCode, true);
            }
            ConfigPersistentMisc();
            ResetKeyboardField(TypedContent);
#if FIXME
            if (_controllerInput == null)
            {
                _controllerInput = FindObjectOfType<ControllerInput>(true);
            }
#endif
            PublishKeyEvent.AddListener(CloseSubPanelOnDeselect);
            PublishKeyEvent.AddListener(CloseNonUGUIPanel);

            if (_tmpInputField)
            {
                _curInputFieldContentType = _tmpInputField.contentType;
                SetTMPFixText(false);
                ResetKeyboardField(TypedContent, _isRTL);
#if !UNITY_EDITOR
                _tmpInputField.interactable = true;
#endif
            }
        }

        private void OnEnable()
        {
            if (!_initialized)
            {
                return;
            }

            PageLayoutProperties pageLayoutProperties =
                    GetPageLayoutProperties(_localeToLoadOnEnable, _layoutType);
            PageCode defaultPageCode = (PageCode)pageLayoutProperties.DefaultPage;

            LoadLayout(_localeToLoadOnEnable, defaultPageCode);

            if (TypedContent == "" && _tmpInputField && _isRTL != _curLayoutInfo.IsRightToLeft)
            {
                _isRTL = _curLayoutInfo.IsRightToLeft;
                _displayBarInfo.FlipDisplayBar.Flip(_isRTL);
                SetDisplayBarTextRTL(_isRTL);
            }
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            ResetGrowingWord();
            CloseOpenedSubPanel();

            if (ClearInputFieldOnClose)
            {
                ResetKeyboardField("");
            }

            OnKeyboardClose?.Invoke();
        }

        private void Update()
        {
            ThreadDispatcher.DispatchAll();

            if (_tmpInputField != null && _tmpInputField.caretPosition != _caretPos)
            {
                _tmpInputField.caretPosition = _caretPos;
            }
        }
        #endregion Monobehaviour Methods

        #region Private Methods
        #region KeyClicked
        private void CharKeyClicked(KeyInfo keyInfo, bool doubleClick = false)
        {
            if (_tmpInputField && !_tmpInputField.isFocused)
            {
                _tmpInputField.ActivateInputField();
            }

            // If we are in Japanese keyboard, use another function to process
            if (Locale == Code.kJp_JP_Unity)
            {
                // handle text editing
                ProcessNewCharInputJP(keyInfo, doubleClick);
            }
            else // EFIGS languages
            {
                ProcessNewCharInput(keyInfo, doubleClick);
            }

            // typing one time in upper case layout should switch the VKB back to lower case layout
            if (_pageCode == PageCode.kUpperLetters)
            {
                LoadLayout(Locale, PageCode.kLowerLetters);
            }

            // typing one time in alt gr upper case layout should switch the VKB back to alt gr
            // lower case layout
            if (_pageCode == PageCode.kAltUpperLetters)
            {
                LoadLayout(Locale, PageCode.kAltLowerLetters);
            }

            if (TypedContent == "")
            {
                FlipDisplayBarBasedOnCurrentLocale();
            }

            PublishKeyEvent.Invoke(
                keyInfo.TextToType,
                keyInfo.KeyType,
                doubleClick,
                GetTypedContentBasedOnPasswordMode(_curWord.ToString()));
        }

        private void SuggestionKeyClicked(GameObject keyObj, bool doubleClick = false)
        {
            string keyText = keyObj.GetComponent<KeyInfo>().TextToType;
            // if key info is missing do not type and return
            if (keyObj.GetComponent<KeyInfo>() == null)
            {
                if (_debug)
                {
                    Debug.Log("The key is missing the key info");
                }
                return;
            }
            // refresh input field activation
            if (_tmpInputField && !_tmpInputField.isFocused)
            {
                _tmpInputField.ActivateInputField();
            }
            string contentWithoutCurWord = TypedContent.Substring(0, _curWordBeginOffset);
            TypedContent = contentWithoutCurWord + keyText + " ";

            _growingCurWord = false;
            _curWordBeginOffset = -1;
            _deleteSpaceAfterSuggestionSelected = true;

            if (_tmpInputField)
            {
                SetDisplayBarText(TypedContent);
                _caretPos = _tmpInputField.text.Length;
            }

            StartCoroutine(CleanSuggestion());
            PublishKeyEvent.Invoke(keyText,
                                   KeyType.kSuggestion,
                                   doubleClick,
                                   GetTypedContentBasedOnPasswordMode(_curWord.ToString()));

            // typing one time in upper case layout should switch the VKB back to lower case layout
            if (_pageCode == PageCode.kUpperLetters)
            {
                LoadLayout(Locale, PageCode.kLowerLetters);
            }
        }

        // This function processes the clicking on keys that result in layout change
        // such as "shift" key that changes between upper/lower case within one language
        // or a language key that switches the keyboard to another language
        private void LayoutChgKeyClicked(KeyInfo keyInfo, bool doubleClick = false)
        {
            KeyType keyType = keyInfo.KeyType;
            if (_printDebugInfo)
            {
                Debug.Log("Function key clicked: " + keyType);
            }
            if (_tmpInputField && !_tmpInputField.isFocused)
            {
                _tmpInputField.ActivateInputField();
            }
            PageCode nextPageCode = _pageCode;
            Code nextLocale = Locale;
            if (doubleClick && keyType == KeyType.kShift)
            {
                // Special case of double clicking the Shift key, not used anymore
                nextPageCode =
                    _pageCode == PageCode.kUpperLetters ?
                        PageCode.kLowerLetters : PageCode.kUpperLetters;
                LoadLayout(nextLocale, nextPageCode, false);
            }
            else if (keyType == KeyType.kShift ||
                     keyType == KeyType.kCapsLock ||
                     keyType == KeyType.kAltGr ||
                     keyType == KeyType.kPageNumericSymbols ||
                     keyType == KeyType.kPageHiragana ||
                     keyType == KeyType.kPageKatakana ||
                     keyType == KeyType.kJPNumSym)
            {
                PageLayoutProperties pageLayoutProperties =
                    GetPageLayoutProperties(Locale, _layoutType);
                var pageLinkMap = pageLayoutProperties.PageLinkMap;
                if (!pageLinkMap.ContainsKey((PageID)_pageCode))
                {
                    if (_printDebugInfo)
                    {
                        Debug.Log("pageLinkMap does not contain the current page");
                    }
                    return;
                }
                var keyPagePairs = pageLayoutProperties.PageLinkMap[(PageID)_pageCode];
                bool found = false;
                foreach (var keyPagePair in keyPagePairs)
                {
                    if (keyPagePair.Key == keyType)
                    {
                        nextPageCode = (PageCode)keyPagePair.Value.ThisPageID;
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    Debug.Log("could not find this key: " + keyType + " in page link map");
                    return;
                }
                LoadLayout(nextLocale, nextPageCode, false);
            }
            else if (keyType == KeyType.kChangeLocaleEn ||
                     keyType == KeyType.kChangeLocaleJP ||
                     keyType == KeyType.kChangeLocaleAR ||
                     keyType == KeyType.kChangeLocaleDE ||
                     keyType == KeyType.kChangeLocaleFR ||
                     keyType == KeyType.kChangeLocaleES ||
                     keyType == KeyType.kChangeLocalePT ||
                     keyType == KeyType.kChangeLocaleIT)
            {
                switch (keyType)
                {
                    case KeyType.kChangeLocaleEn:
                        nextLocale = Code.kEn_US_Unity;
                        break;
                    case KeyType.kChangeLocaleJP:
                        nextLocale = Code.kJp_JP_Unity;
                        break;
                    case KeyType.kChangeLocaleAR:
                        nextLocale = Code.kAr_AR_Unity;
                        break;
                    case KeyType.kChangeLocaleDE:
                        nextLocale = Code.kDe_DE_Unity;
                        break;
                    case KeyType.kChangeLocaleFR:
                        nextLocale = Code.kFr_FR_Unity;
                        break;
                    case KeyType.kChangeLocaleES:
                        nextLocale = Code.kEs_ES_Unity;
                        break;
                    case KeyType.kChangeLocalePT:
                        nextLocale = Code.kPt_PT_Unity;
                        break;
                    case KeyType.kChangeLocaleIT:
                        nextLocale = Code.kIt_IT_Unity;
                        break;
                }

                PageLayoutProperties pageLayoutProperties =
                    GetPageLayoutProperties(nextLocale, _layoutType);
                nextPageCode = (PageCode)pageLayoutProperties.DefaultPage;
                LoadLayout(nextLocale, nextPageCode);
            }

            if ((keyInfo.KeyType == KeyType.kChangeLocaleEn ||
                keyInfo.KeyType == KeyType.kChangeLocaleJP ||
                keyInfo.KeyType == KeyType.kChangeLocaleAR ||
                keyInfo.KeyType == KeyType.kChangeLocaleDE ||
                keyInfo.KeyType == KeyType.kChangeLocaleFR ||
                keyInfo.KeyType == KeyType.kChangeLocaleES ||
                keyInfo.KeyType == KeyType.kChangeLocalePT ||
                keyInfo.KeyType == KeyType.kChangeLocaleIT) &&
                TypedContent == "")
            {
                FlipDisplayBarBasedOnCurrentLocale();
            }

            PublishKeyEvent.Invoke(
                "", keyType, doubleClick, GetTypedContentBasedOnPasswordMode(_curWord.ToString()));
        }

        private void CloseSubPanelOnDeselect(
            string textPart, KeyType keyType, bool doubleClicked, string TypedText)
        {
            if (!_curLayoutInfo.IsUGUI ||
                keyType == KeyType.kSubPanel ||
                keyType == KeyType.kChangeLocale ||
                _subPanelCanvasInfo == null ||
                _curSelectedSubPanelKey == null)
            {
                return;
            }
            KeySubPanel subPanel =
                _subPanelCanvasInfo.GetSubPanel(_curSelectedSubPanelKey.SubPanelID);
            if (subPanel == null)
            {
                Debug.LogError(
                    "Invalid SubPanelID \"" + _curSelectedSubPanelKey.SubPanelID + "\"");
                return;
            }
            ToggleSubPanel(subPanel, _curSelectedSubPanelKey);
        }

        private void CloseNonUGUIPanel(
            string textPart, KeyType keyType, bool doubleClicked, string TypedText)
        {
            if (_curLayoutInfo.IsUGUI ||
                keyType == KeyType.kChangeLocale ||
                keyType == KeyType.kAccents)
            {
                return;
            }

            if (_accentsPanel.gameObject.activeSelf)
            {
                _accentsPanel.SetActive(false);
                _curLayoutInfo.JPAccentsKeyToggle.Toggle(false);
            }

            if (_languagePanel.gameObject.activeSelf)
            {
                _languagePanel.SetActive(false);
                _curLayoutInfo.ChangeLocaleKeyToggle.Toggle(false);
            }
        }

        private void TashkeelKeyClicked(KeyInfo keyInfo, bool doubleClick = false)
        {
            if (keyInfo.TextToType == null ||
                keyInfo.TextToType.Length == 0 ||
                TypedContent.Length == 0)
            {
                return;
            }

            char prevChar = TypedContent[TypedContent.Length - 1];
            if (TextUtils.IsArabicCharacter(prevChar) ||
                (char.IsNumber(prevChar) && !TextUtils.IsRTLCharacter(prevChar)) ||
                char.IsPunctuation(prevChar) ||
                char.IsSymbol(prevChar))
            {
                TypedContent += keyInfo.TextToType;

                if (_tmpInputField)
                {
                    SetDisplayBarText(TypedContent);
                    _caretPos = _tmpInputField.text.Length;
                }
                _deleteSpaceAfterSuggestionSelected = false;
                PublishKeyEvent.Invoke(keyInfo.TextToType,
                                   keyInfo.KeyType,
                                   doubleClick,
                                   GetTypedContentBasedOnPasswordMode(_curWord.ToString()));
            }

            // typing one time in upper case layout should switch the VKB back to lower case layout
            if (_pageCode == PageCode.kUpperLetters)
            {
                LoadLayout(Locale, PageCode.kLowerLetters);
            }
        }

        // TODO(agrancini): add text selection japanese and arabic
        // this is when you click on the suggestion
        private void JPSuggestClicked(KeyInfo keyInfo)
        {
            if (_printDebugInfo)
            {
                Debug.Log("Selecting suggestion button with text: " + keyInfo.TextToType);
            }
            SuggestionPanel panel = _JPSuggestIsExpanded
                ? _displayBarInfo.JPSuggestsPanelExpanded
                : _displayBarInfo.JPSuggestsPanelCollapsed;
            GameObject panelParent = _JPSuggestIsExpanded
                ? _displayBarInfo.JPSuggestsParentExpanded
                : _displayBarInfo.JPSuggestsParentCollapsed;
            panel.UnPreSelectKey();
            // because japanese has a "preselection" text we need to differ two steps
            // preselection below
            string temp = KeyboardAPI.SetCurrentCandidate(keyInfo.TextToType);
            // actual selection below
            // note: we do this for updating the prediction text machine correctly
            string unconvertedTail = KeyboardAPI.SelectCurrentCandidate();
            String previousText = TypedContent.Substring(0, _curWordBeginOffset);
            String convertedText = keyInfo.TextToType;
            if (_printDebugInfo)
            {
                Debug.Log("Previous text is " + previousText +
                          ". Converted text is " + convertedText +
                          ". Unconverted tail is " + unconvertedTail);
            }
            if (unconvertedTail.Length > 0)
            {
                _growingCurWord = true;
                _curWord.Clear();
                _curWord.Append(unconvertedTail);
                _curWordBeginOffset = previousText.Length + convertedText.Length;
                // typed content is updated here
                TypedContent = previousText + convertedText + _curWord;
                ThreadDispatcher.ScheduleWork(() =>
                {
                    // analyze context: whenever you type this updates with the current context of strings
                    // whenever you change the context in the input field you will have this call
                    KeyboardAPI.AnalyzeContext(previousText + convertedText);
                    List<string> results = FindJPSuggestions(_curWord.ToString());
                    ThreadDispatcher.ScheduleMain(() => { panel.PopulateNewContents(results); });
                    return true;
                });
            }
            else
            {
                KeyboardAPI.AnalyzeContext(previousText + convertedText);
                _growingCurWord = false;
                _curWord.Clear();
                _curWordBeginOffset = -1;
                Debug.Log("Setting japanese suggestion panel to inactive");
                panelParent.SetActive(false);
                // typed content is updated here
                TypedContent = previousText + convertedText;
            }
            if (_tmpInputField)
            {
                string displayBarText = GetTypedContentBasedOnPasswordMode(_curWord.ToString(), true);
                SetDisplayBarText(displayBarText);
                _caretPos = _tmpInputField.text.Length;
            }
            PublishKeyEvent.Invoke(keyInfo.TextToType, keyInfo.KeyType, false, GetTypedContentBasedOnPasswordMode(_curWord.ToString()));
        }

        // TODO: add text selection japanese
        // Handles the special key on JP keyboard that switches between alternative characters
        private void JPAltCharKeyClicked()
        {
            if (!_growingCurWord)
            {
                return;
            }
            SuggestionPanel panel = _JPSuggestIsExpanded
                ? _displayBarInfo.JPSuggestsPanelExpanded
                : _displayBarInfo.JPSuggestsPanelCollapsed;
            panel.UnPreSelectKey();
            LocaleDesc localeDesc = _virtualKeyboardBuilder.GetLocaleMap()[Code.kJp_JP_Unity];
            char curChar = _curWord.ToString()[_curWord.Length - 1];
            if (!localeDesc.NextAlternateKeyMap.ContainsKey(curChar))
            {
                if (_printDebugInfo)
                {
                    Debug.Log("Cannot find alternate char for " + curChar +
                              ". Alter key toggle does nothing");
                }
                return;
            }
            char nextChar = localeDesc.NextAlternateKeyMap[curChar];
            if (nextChar == curChar)
            {
                if (_printDebugInfo)
                {
                    Debug.Log("The alternative char and current char are equal" +
                              "both are " + curChar);
                }
                return;
            }
            _curWord.Remove(_curWord.Length - 1, 1);
            _curWord.Append(nextChar);
            ClearTempCandidateDisplay();
            ThreadDispatcher.ScheduleWork(() =>
            {
                List<string> results = FindJPSuggestions(_curWord.ToString());
                ThreadDispatcher.ScheduleMain(() => { panel.PopulateNewContents(results); });
                return true;
            });
            PublishKeyEvent.Invoke("", KeyType.kAltCharToggle, false, GetTypedContentBasedOnPasswordMode(_curWord.ToString()));
        }

        private void JPExpandCollapseClicked(KeyInfo keyInfo)
        {
            GameObject toEnable = keyInfo.KeyType == KeyType.kExpandUp
                ? _displayBarInfo.JPSuggestsParentExpanded
                : _displayBarInfo.JPSuggestsParentCollapsed;
            GameObject toDisable = keyInfo.KeyType == KeyType.kExpandUp
                ? _displayBarInfo.JPSuggestsParentCollapsed
                : _displayBarInfo.JPSuggestsParentExpanded;
            SuggestionPanel toPopulate = keyInfo.KeyType == KeyType.kExpandUp
                ? _displayBarInfo.JPSuggestsPanelExpanded
                : _displayBarInfo.JPSuggestsPanelCollapsed;
            SuggestionPanel toUnPopulate = keyInfo.KeyType == KeyType.kExpandUp
                ? _displayBarInfo.JPSuggestsPanelCollapsed
                : _displayBarInfo.JPSuggestsPanelExpanded;
            toEnable.SetActive(true);
            toDisable.SetActive(false);
            toPopulate.UnPreSelectKey();
            toUnPopulate.UnPreSelectKey();
            _JPSuggestIsExpanded = keyInfo.KeyType == KeyType.kExpandUp;
            ThreadDispatcher.ScheduleWork(() =>
            {
                List<string> results = FindJPSuggestions(_curWord.ToString());
                ThreadDispatcher.ScheduleMain(() => { toPopulate.PopulateNewContents(results); });
                return true;
            });
        }
        #endregion KeyClicked

        #region Process NewChar Input
        private async void ProcessNewCharInput(KeyInfo keyInfo, bool doubleClick = false)
        {
            string textToType = GetRichTextSafeText(keyInfo.TextToType);

            // In case suggestion engine is disabled
            if (!ExperimentalWordSuggestion || !PredictionIsInitialized())
            {
                if (keyInfo.KeyType != KeyType.kBackspace && textToType.Length > 0)
                {
                    TypedContent += textToType;
                }
                else if (keyInfo.KeyType == KeyType.kBackspace && TypedContent.Length > 0)
                {
                    int amtToRemove = (IsLastCharRichTextSafe(TypedContent) ? 2 : 1);
                    TypedContent = TypedContent.Substring(0, TypedContent.Length - amtToRemove);
                }
                if (_tmpInputField)
                {
                    SetDisplayBarText(TypedContent);
                    _caretPos = _tmpInputField.text.Length;
                }
                _deleteSpaceAfterSuggestionSelected = false;
                return;
            }
            // Below will carry out only if the suggestion engine is enabled
            if (keyInfo.KeyType != KeyType.kBackspace && textToType.Length > 0 &&
                ExperimentalWordSuggestion)
            {
                foreach (char ch in textToType)
                {
                    // Eliminate spaces at the tail if the new input is a punctuation mark
                    if (_deleteSpaceAfterSuggestionSelected &&
                        (char.GetUnicodeCategory(ch) == UnicodeCategory.OtherPunctuation ||
                        char.GetUnicodeCategory(ch) == UnicodeCategory.DashPunctuation) &&
                        !_punctuationMarksToIgnoreOnWhiteSpaceDeleteSet.Contains(ch))
                    {
                        int nonEmptyTextEndPos = TypedContent.Length - 1;
                        while (nonEmptyTextEndPos >= 0 &&
                               TypedContent[nonEmptyTextEndPos] == ' ')
                        {
                            nonEmptyTextEndPos--;
                        }

                        TypedContent = TypedContent.Substring(0, nonEmptyTextEndPos + 1);
                    }
                    _deleteSpaceAfterSuggestionSelected = false;

                    // Composing a word
                    if (HasValidPredictionChars(ch))
                    {
                        // Starting a new word
                        if (!_growingCurWord)
                        {
                            if (_printDebugInfo)
                            {
                                Debug.Log("starting a new word");
                            }
                            _curWord.Clear();
                            _curWord.Append(ch);
                            _growingCurWord = true;
                            _curWordBeginOffset = TypedContent.Length;
                        }
                        // Continuing an already-started word
                        else
                        {
                            if (_printDebugInfo)
                            {
                                Debug.Log("continuing an already-started word");
                            }
                            _curWord.Append(ch);
                        }
                    }
                    // It's treated as a separator that ends the current word
                    else
                    {
                        if (_growingCurWord)
                        {
                            if (_printDebugInfo)
                            {
                                Debug.Log("separator");
                            }
                            _curWord.Clear();
                            _growingCurWord = false;
                            _curWordBeginOffset = -1;
                        }
                    }
                    TypedContent += ch;
                }
            }
            else if (keyInfo.KeyType == KeyType.kBackspace)
            {
                if (TypedContent.Length == 0)
                {
                    return;
                }
                int amtToRemove = (IsLastCharRichTextSafe(TypedContent) ? 2 : 1);
                TypedContent = TypedContent.Substring(0, TypedContent.Length - amtToRemove);
                if (_growingCurWord)
                {
                    if (_curWord.Length == 0)
                    {
                        Debug.LogError(
                            "A growing current word should not have a length of 0, " +
                            "something went wrong");
                    }
                    // Shrink the current growing word
                    else
                    {
                        if (_printDebugInfo)
                        {
                            Debug.Log("shrinking the currently growing word");
                        }
                        _curWord.Remove(_curWord.Length - 1, 1);
                    }
                    // If we deleted the entire currently growing word
                    if (_curWord.Length == 0)
                    {
                        if (_printDebugInfo)
                        {
                            Debug.Log("deleting the entire current growing word");
                        }
                        _growingCurWord = false;
                        _curWordBeginOffset = -1;
                    }
                    _deleteSpaceAfterSuggestionSelected = false;
                }
            }

            if (_tmpInputField)
            {
                string displayBarText = GetTypedContentBasedOnPasswordMode(_curWord.ToString(), true);
                SetDisplayBarText(displayBarText);
                _caretPos = _tmpInputField.text.Length;
            }

            if (_printDebugInfo)
            {
                PrintInputFieldStatus();
            }

            // Launch the word suggestion engine with the current growing word
            if (_growingCurWord && _curWord.Length > 0)
            {
                // Whatever happened before has finished execution
                if (_launchedTask == null || _launchedTask.IsCompleted)
                {
                    if (_printDebugInfo)
                    {
                        Debug.Log("Lauching new prediction with text of: " + _curWord.ToString());
                    }
                }
                else if (_launchedTask != null && !_launchedTask.IsCompleted)
                {
                    if (_printDebugInfo)
                    {
                        Debug.Log("Trying to launch new prediction for " + _curWord.ToString() +
                                  " when previous one has not finished: ");
                        Debug.Log("Canceling the previous prediction before: " + _curWord.ToString());
                    }
                    _predictCancelTokenSrc.Cancel();
                }
                try
                {
                    _predictCancelTokenSrc = new CancellationTokenSource();
                    _predictCancelToken = _predictCancelTokenSrc.Token;
                    _launchedTask = LaunchPrediction();
                    await _launchedTask;
                    if (_launchedTask.Status == TaskStatus.RanToCompletion &&
                        _launchedTask.Result.Count > 0)
                    {
                        if (!_displayBarInfo.SuggestionsParent.activeSelf)
                        {
                            _displayBarInfo.SuggestionsParent.SetActive(true);
                        }
                        for (int i = 0; i < NUM_SUGGESTIONS; i++)
                        {
                            if (i < _launchedTask.Result.Count)
                            {
                                _displayBarInfo.SuggestionsKeyInfos[i].gameObject.SetActive(true);
                                _displayBarInfo.SuggestionsKeyInfos[i].KeyTMP.text =
                                    _launchedTask.Result[i].TextToType;
                                _displayBarInfo.SuggestionsKeyInfos[i].TextToType =
                                    _launchedTask.Result[i].TextToType;
                            }
                            else
                            {
                                _displayBarInfo.SuggestionsKeyInfos[i].gameObject.SetActive(false);
                            }
                        }
                    }
                    else if (!_displayBarInfo.SuggestionsParent.activeSelf)
                    {
                        StartCoroutine(CleanSuggestion());
                    }
                }
                catch (OperationCanceledException)
                {
                    if (_printDebugInfo)
                    {
                        Debug.Log("Prediction canceled");
                    }
                }
            }
            else if (_displayBarInfo.SuggestionsParent.activeSelf)
            {
                StartCoroutine(CleanSuggestion());
            }
        }

        private void ProcessNewCharInputJP(KeyInfo keyInfo, bool doubleClick = false)
        {
            string textToType = GetRichTextSafeText(keyInfo.TextToType);

            SuggestionPanel panel = _JPSuggestIsExpanded
                ? _displayBarInfo.JPSuggestsPanelExpanded
                : _displayBarInfo.JPSuggestsPanelCollapsed;
            GameObject panelParent = _JPSuggestIsExpanded
                ? _displayBarInfo.JPSuggestsParentExpanded
                : _displayBarInfo.JPSuggestsParentCollapsed;
            panel.UnPreSelectKey();
            if (keyInfo.KeyType != KeyType.kBackspace && textToType.Length > 0)
            {
                foreach (char ch in textToType)
                {
                    // Composing a word
                    if (keyInfo.KeyType == KeyType.kHiragana ||
                        keyInfo.KeyType == KeyType.kKatakana)
                    {
                        // Starting a new word
                        if (!_growingCurWord)
                        {
                            if (_printDebugInfo)
                            {
                                Debug.Log("starting a new word");
                            }
                            _growingCurWord = true;
                            _curWord.Clear();
                            _curWord.Append(ch);
                            _curWordBeginOffset = TypedContent.Length;
                        }
                        // Continuing an already-started word
                        else
                        {
                            if (_printDebugInfo)
                            {
                                Debug.Log("continuing an already-started word");
                            }
                            _curWord.Append(ch);
                        }
                        TypedContent = TypedContent.Substring(0, _curWordBeginOffset) +
                                       _curWord;
                    }
                    // It's treated as a separator that ends the current word
                    else
                    {
                        if (_growingCurWord)
                        {
                            if (_printDebugInfo)
                            {
                                Debug.Log("separator");
                            }
                            TypedContent = TypedContent.Substring(0, _curWordBeginOffset) +
                                           _curWord;
                            _curWord.Clear();
                            _growingCurWord = false;
                            _curWordBeginOffset = -1;
                        }
                        TypedContent += ch;
                        ThreadDispatcher.ScheduleWork(() =>
                        {
                            KeyboardAPI.AnalyzeContext(TypedContent);
                            return true;
                        });
                    }
                }
            }
            else if (keyInfo.KeyType == KeyType.kBackspace)
            {
                if (TypedContent.Length == 0)
                {
                    return;
                }

                int amtToRemove = (IsLastCharRichTextSafe(TypedContent) ? 2 : 1);

                if (_growingCurWord)
                {
                    if (_curWord.Length == 0)
                    {
                        Debug.LogError(
                            "A growing current word should not have a length of 0, " +
                            "something went wrong");
                    }
                    // Shrink the current growing word
                    else
                    {
                        if (_printDebugInfo)
                        {
                            Debug.Log("shrinking the currently growing word");
                        }
                        _curWord.Remove(_curWord.Length - 1, 1);
                    }

                    // If we deleted the entire currently growing word
                    if (_curWord.Length == 0)
                    {
                        if (_printDebugInfo)
                        {
                            Debug.Log("deleting the entire current growing word");
                        }
                        TypedContent = TypedContent.Substring(0, _curWordBeginOffset);
                        _growingCurWord = false;
                        _curWordBeginOffset = -1;
                        ThreadDispatcher.ScheduleWork(() =>
                        {
                            KeyboardAPI.AnalyzeContext(TypedContent);
                            return true;
                        });
                    }
                    else
                    {
                        TypedContent = TypedContent.Substring(0, _curWordBeginOffset) +
                                       _curWord;
                    }
                }
                else
                {
                    TypedContent = TypedContent.Substring(
                        0, TypedContent.Length - amtToRemove);
                    ThreadDispatcher.ScheduleWork(() =>
                    {
                        KeyboardAPI.AnalyzeContext(TypedContent);
                        return true;
                    });
                }
            }
            if (_printDebugInfo)
            {
                PrintInputFieldStatus();
            }
            if (_tmpInputField)
            {
                string displayBarText = GetTypedContentBasedOnPasswordMode(_curWord.ToString(), true);
                SetDisplayBarText(displayBarText);
                _caretPos = _tmpInputField.text.Length;
            }
            // Launch the mozc engine with the current growing word
            if (_growingCurWord && _curWord.Length > 0)
            {
                ThreadDispatcher.ScheduleWork(() =>
                {
                    List<string> results = FindJPSuggestions(_curWord.ToString());
                    ThreadDispatcher.ScheduleMain(() => { panel.PopulateNewContents(results); });
                    return true;
                });
            }
            if (_growingCurWord && !panelParent.activeSelf)
            {
                panelParent.SetActive(true);
            }
            else if (!_growingCurWord && panelParent.activeSelf)
            {
                panelParent.SetActive(false);
            }
        }
        #endregion Process NewChar Input

        #region Debug
        private void PrintInputFieldStatus()
        {
            Debug.Log("Growing current word: " + _growingCurWord +
                      "; current word is: " + _curWord.ToString() +
                      "; current word begin offset is " + _curWordBeginOffset);
        }
        #endregion Debug

        #region Config
        private bool Init()
        {
            if (!_initialized)
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
                NUM_SUGGESTIONS = _displayBarInfo.SuggestionsKeyInfos.Length;
                CreateLanguageToPrefabMap();
                _en_US_prediction = new Prediction();
                _en_US_prediction.Init(_en_US_dict);
                _ar_prediction = new Prediction();
                _ar_prediction.Init(_ar_dict);
                _de_prediction = new Prediction();
                _de_prediction.Init(_de_dict);
                _fr_prediction = new Prediction();
                _fr_prediction.Init(_fr_dict);

                _punctuationMarksToIgnoreOnWhiteSpaceDeleteSet.Clear();
                foreach (char ch in _punctuationCharsToIgnoreOnWhiteSpaceDelete)
                {
                    if (char.GetUnicodeCategory(ch) == UnicodeCategory.OtherPunctuation ||
                        char.GetUnicodeCategory(ch) == UnicodeCategory.DashPunctuation)
                    {
                        _punctuationMarksToIgnoreOnWhiteSpaceDeleteSet.Add(ch);
                    }
                }
                _displayBarInfo.FlipDisplayBar.Init();
                _initialized = true;
            }
            return true;
        }

        private void CreateLanguageToPrefabMap()
        {
            _languageToPrefabMap.Clear();
            _languageToPrefabMap.Add(Code.kEn_US_Unity, _enLayouts);
            _languageToPrefabMap.Add(Code.kJp_JP_Unity, _jpLayouts);
            _languageToPrefabMap.Add(Code.kAr_AR_Unity, _arLayouts);
            _languageToPrefabMap.Add(Code.kDe_DE_Unity, _deLayouts);
            _languageToPrefabMap.Add(Code.kFr_FR_Unity, _frLayouts);
            _languageToPrefabMap.Add(Code.kEs_ES_Unity, _esLayouts);
            _languageToPrefabMap.Add(Code.kPt_PT_Unity, _ptLayouts);
            _languageToPrefabMap.Add(Code.kIt_IT_Unity, _itLayouts);
        }

        private PageLayoutProperties GetPageLayoutProperties(Code localeCode, LayoutType layoutType)
        {
            if (!_virtualKeyboardBuilder.GetLocaleMap().ContainsKey(localeCode))
            {
                Debug.LogError("Could not find map entry for locale: " + localeCode);
                return null;
            }
            LocaleDesc localeDesc = _virtualKeyboardBuilder.GetLocaleMap()[localeCode];
            if (!localeDesc.PropertyMap.ContainsKey(layoutType))
            {
                Debug.LogError("Could not find layout type in that locale, layoutType: " +
                               layoutType);
                return null;
            }
            return localeDesc.PropertyMap[layoutType];
        }

        private void ClearLayouts(GameObject layoutsParent)
        {
            if (_layoutsParent)
            {
                GameObject.Destroy(_layoutsParent);
                _layoutsParent = null;
            }
        }

        private void EditorClearLayouts(GameObject layoutsParent)
        {
            if (layoutsParent == null)
            {
                return;
            }
            GameObject.DestroyImmediate(_layoutsParent);
            _layoutsParent = null;
        }

        private void ConfigKeys(GameObject keysParent)
        {
            if (!keysParent)
            {
                Debug.LogError("Cannot find the parent of keys when configuring keys!");
                return;
            }
            foreach (Transform child in keysParent.transform)
            {
                ConfigKey(child.gameObject);
            }
        }

        private void ConfigBackground(GameObject backgroundParent)
        {
            if (!backgroundParent)
            {
                Debug.LogError("Cannot find the parent of background when configuring background!");
                return;
            }
            BackgroundInfo backgroundInfo = backgroundParent.GetComponent<BackgroundInfo>();
            if (!backgroundInfo)
            {
                Debug.LogError("Missing BackgroundInfo script on the " +
                               "keyboard background GameObject, aborting!");
                return;
            }
            GameObject leftHandleParent = backgroundInfo.LeftHandleParent;
            GameObject rightHandleParent = backgroundInfo.RightHandleParent;
            if (!leftHandleParent || !rightHandleParent)
            {
                Debug.LogError("Missing reference to handles on BackgroundInfo script, aborting!");
                return;
            }

#if FIXME
            Interactable leftHandleInteractable =
                backgroundInfo.LeftHandleParent.GetComponent<Interactable>();
            Interactable rightHandleInteractable =
                backgroundInfo.RightHandleParent.GetComponent<Interactable>();
            if (!leftHandleInteractable || !rightHandleInteractable)
            {
                Debug.LogError("Missing reference to handles on BackgroundInfo script, aborting!");
                return;
            }

            leftHandleInteractable.References.MoveTarget = transform;
            leftHandleInteractable.References.RotateTarget = transform;
            leftHandleInteractable.References.ScaleTarget = transform;
            rightHandleInteractable.References.MoveTarget = transform;
            rightHandleInteractable.References.RotateTarget = transform;
            rightHandleInteractable.References.ScaleTarget = transform;
#endif
        }

        private void ConfigKey(GameObject keyObj)
        {
            KeyInfo keyInfo = keyObj.GetComponent<KeyInfo>();
            if (keyInfo == null)
            {
                return;
            }
            StatefulInteractable interactable = keyInfo.KeyInteractable;
            if (keyInfo.KeyInteractable == null)
            {
                return;
            }
            switch (keyInfo.KeyType)
            {
                case KeyType.kCharacter:
                case KeyType.kCharacter2Labels:
                case KeyType.kTab:
                case KeyType.kSpacebar:
                case KeyType.kBackspace:
                case KeyType.kHiragana:
                case KeyType.kKatakana:
                case KeyType.kAccent:
                    interactable.OnClicked.AddListener(delegate { CharKeyClicked(keyInfo); });
                    break;
                case KeyType.kPageNumericSymbols:
                case KeyType.kShift:
                case KeyType.kAltGr:
                case KeyType.kCapsLock:
                case KeyType.kPageHiragana:
                case KeyType.kPageKatakana:
                case KeyType.kJPNumSym:
                case KeyType.kChangeLocaleEn:
                case KeyType.kChangeLocaleJP:
                case KeyType.kChangeLocaleAR:
                case KeyType.kChangeLocaleDE:
                case KeyType.kChangeLocaleFR:
                case KeyType.kChangeLocaleES:
                case KeyType.kChangeLocalePT:
                case KeyType.kChangeLocaleIT:
                    interactable.OnClicked.AddListener(delegate { LayoutChgKeyClicked(keyInfo); });
                    if (keyInfo.KeyType == KeyType.kShift)
                    {
#if FIXME
                        interactable.Events.OnDoubleSelect.AddListener(
                            delegate { LayoutChgKeyClicked(keyInfo, true); });
#endif
                    }
                    break;
                case KeyType.kTashkeel:
                    interactable.OnClicked.AddListener(delegate { TashkeelKeyClicked(keyInfo); });
                    break;
                case KeyType.kSuggestion:
                    interactable.OnClicked.AddListener(delegate { SuggestionKeyClicked(keyObj); });
                    break;
                case KeyType.kAltCharToggle:
                    interactable.OnClicked.AddListener(delegate { JPAltCharKeyClicked(); });
                    break;
                // This pre-selects the next suggestion or do regular space
                case KeyType.kJPSpacebar:
                    interactable.OnClicked.AddListener(delegate
                    {
                        if (_growingCurWord)
                        {
                            SuggestionPanel panel = _JPSuggestIsExpanded
                                ? _displayBarInfo.JPSuggestsPanelExpanded
                                : _displayBarInfo.JPSuggestsPanelCollapsed;
                            panel.PreSelectNextKey();
                            KeyInfo newPreSelectedKeyInfo =
                                panel.PreSelectedKey.GetComponent<KeyInfo>();
                            PublishKeyEvent.Invoke(
                                "", keyInfo.KeyType, false, GetTypedContentBasedOnPasswordMode(_curWord.ToString()));
                        }
                        else
                        {
                            CharKeyClicked(keyInfo);
                        }
                    });
                    break;
                case KeyType.kJPNewLine:
                    interactable.OnClicked.AddListener(delegate
                    {
                        SuggestionPanel panel = _JPSuggestIsExpanded
                            ? _displayBarInfo.JPSuggestsPanelExpanded
                            : _displayBarInfo.JPSuggestsPanelCollapsed;
                        GameObject keyObj = panel.PreSelectedKey;
                        StatefulInteractable preselectedClickable = null;
                        if (keyObj != null &&
                            (preselectedClickable = keyObj.GetComponent<StatefulInteractable>()) != null)
                        {
                            preselectedClickable.OnClicked.Invoke();
                        }
                        PublishKeyEvent.Invoke(
                            "", keyInfo.KeyType, false, GetTypedContentBasedOnPasswordMode(_curWord.ToString()));
                    });
                    break;
                case KeyType.kAccents:
                    interactable.OnClicked.AddListener(delegate
                    {
                        if (_languagePanel.gameObject.activeSelf)
                        {
                            _languagePanel.gameObject.SetActive(false);
                            _curLayoutInfo.ChangeLocaleKeyToggle.Toggle(false);
                        }
                        _accentsPanel.SetActive(!_accentsPanel.activeSelf);
                        interactable.GetComponent<KeyToggle>().Toggle(_accentsPanel.activeSelf);
                        PublishKeyEvent.Invoke(
                            "", keyInfo.KeyType, false, GetTypedContentBasedOnPasswordMode(_curWord.ToString()));
                    });
                    break;
                case KeyType.kRayDrumstickSwitch:
                    break;
                case KeyType.kChangeLocale:
                    interactable.OnClicked.AddListener(delegate
                    {
                        if (!keyInfo.IsUGUI)
                        {
                            if (_accentsPanel.gameObject.activeSelf)
                            {
                                _accentsPanel.gameObject.SetActive(false);
                                _curLayoutInfo.JPAccentsKeyToggle.Toggle(false);
                            }
                            _languagePanel.SetActive(!_languagePanel.activeSelf);
                            _languagePanel.transform.localPosition = keyObj.transform.localPosition;
                            interactable.GetComponent<KeyToggle>().Toggle(_languagePanel.activeSelf);
                            PublishKeyEvent.Invoke(
                                "", keyInfo.KeyType, false, GetTypedContentBasedOnPasswordMode(_curWord.ToString()));
                        }
                        else
                        {
                            KeySubPanel subPanel = _subPanelCanvasInfo.GetSubPanel(keyInfo.SubPanelID);
                            if (subPanel == null)
                            {
                                Debug.LogError("Invalid SubPanelID \"" + keyInfo.SubPanelID + "\"");
                                return;
                            }
                            RepositionSubPanelPivot(keyObj, subPanel);
                            ToggleSubPanel(subPanel, keyInfo);
                            PublishKeyEvent.Invoke(
                                "", keyInfo.KeyType, false, GetTypedContentBasedOnPasswordMode(_curWord.ToString()));
                        }
                    });
                    break;
                case KeyType.kSubPanel:
                    interactable.OnClicked.AddListener(delegate
                    {
                        if (!_curLayoutInfo.IsUGUI || _subPanelCanvasInfo == null)
                        {
                            return;
                        }
                        KeySubPanel subPanel = _subPanelCanvasInfo.GetSubPanel(keyInfo.SubPanelID);
                        if (subPanel == null)
                        {
                            Debug.LogError("Invalid SubPanelID \"" + keyInfo.SubPanelID + "\"");
                            return;
                        }
                        RepositionSubPanelPivot(keyObj, subPanel);
                        ToggleSubPanel(subPanel, keyInfo);
                        PublishKeyEvent.Invoke(
                            "", keyInfo.KeyType, false, GetTypedContentBasedOnPasswordMode(_curWord.ToString()));
                    });
                    break;
                case KeyType.kEnter:
                case KeyType.kJPEnter:
                    interactable.OnClicked.AddListener(delegate
                    {
                        PublishKeyEvent.Invoke("", keyInfo.KeyType, false, GetTypedContentBasedOnPasswordMode(_curWord.ToString()));
                        if (HideKeyboardOnEnterKeyClicked)
                        {
                            ResetKeyboardField("");
                            HideKeyboard();
                        }
                    });
                    break;
                default:
                    if (_printDebugInfo)
                    {
                        Debug.Log("This keytype is skipped when configuring the key: " +
                                  keyInfo.KeyType);
                    }
                    break;
                case KeyType.kHideKeyboard:
                    interactable.OnClicked.AddListener(delegate
                    {
                        // If input mode switch key is enabled, we need to call
                        // SwitchInputMethod(true); here
                        HideKeyboard();
                    });
                    break;
            }
        }

        private void RepositionSubPanelPivot(GameObject keyObj, KeySubPanel subPanel)
        {
            RectTransform subPanelTransform = (RectTransform)subPanel.transform;
            RectTransform keysParentTransform =
                (RectTransform)_curLayoutInfo.KeysParent.transform;
            RectTransform keyTransform = (RectTransform)keyObj.transform;

            float subPanelLength = subPanel.IsVertical ?
                subPanelTransform.sizeDelta.y : subPanelTransform.sizeDelta.x;
            float keyPosAlongSubPanelLength = subPanel.IsVertical ?
                           keyTransform.anchoredPosition.y : keyTransform.anchoredPosition.x;
            float keySizeAlongSubPanelLength = subPanel.IsVertical ?
                              keyTransform.sizeDelta.y : keyTransform.sizeDelta.x;
            float vkbRangeAlongSubPanelLength = subPanel.IsVertical ?
                             keysParentTransform.sizeDelta.y : keysParentTransform.sizeDelta.x;

            float distFromKeyEdgeToFurtherVKBEdge = Mathf.Abs(keyPosAlongSubPanelLength) +
                                                    keySizeAlongSubPanelLength * 0.5f +
                                                    vkbRangeAlongSubPanelLength * 0.5f;
            float newPivotAlongSubPanelLength;
            if (distFromKeyEdgeToFurtherVKBEdge < subPanelLength)
            {
                // in this case we align the subpanel's edge with the VKB's edge
                newPivotAlongSubPanelLength =
                    (subPanelLength - distFromKeyEdgeToFurtherVKBEdge +
                     keySizeAlongSubPanelLength * 0.5f) / subPanelLength;
            }
            else
            {
                // in this case we align the subpanel's edge with the key's edge
                newPivotAlongSubPanelLength = keySizeAlongSubPanelLength * 0.5f / subPanelLength;
            }
            if (keyPosAlongSubPanelLength > 0.0f)
            {
                newPivotAlongSubPanelLength *= -1.0f;
            }

            float keyPosAlongSubPanelWidth = subPanel.IsVertical ?
                keyTransform.anchoredPosition.x : keyTransform.anchoredPosition.y;
            float newPivotAlongSubPanelWidth = keyPosAlongSubPanelWidth > 0.0f ? 1.5f : -0.5f;

            subPanelTransform.pivot = subPanel.IsVertical ?
                new Vector2(newPivotAlongSubPanelWidth, newPivotAlongSubPanelLength) :
                new Vector2(newPivotAlongSubPanelLength, newPivotAlongSubPanelWidth);
        }

        private void ToggleSubPanel(KeySubPanel subPanel, KeyInfo keyInfo)
        {
            KeyToggle toggle = keyInfo.GetComponent<KeyToggle>();
            if (_curSelectedSubPanelKey != null &&
                _curSelectedSubPanelKey != keyInfo)
            {
                KeySubPanel prevSubPanel =
                    _subPanelCanvasInfo.GetSubPanel(_curSelectedSubPanelKey.SubPanelID);
                prevSubPanel.gameObject.SetActive(false);
                _curSelectedSubPanelKey.GetComponent<KeyToggle>().Toggle(false);
            }
            subPanel.gameObject.SetActive(!subPanel.gameObject.activeSelf);
            RectTransform subPanelTransform = ((RectTransform)subPanel.transform);
            if (_curLayoutInfo.IsUGUI)
            {
                RectTransform keyTransform = (RectTransform)keyInfo.gameObject.transform;
                subPanelTransform.anchoredPosition = keyTransform.anchoredPosition;
            }
            else
            {
                subPanelTransform.transform.position = keyInfo.gameObject.transform.position;
            }
            toggle.Toggle(subPanel.gameObject.activeSelf);
            _curSelectedSubPanelKey = subPanel.gameObject.activeSelf ? keyInfo : null;
        }

        private void FlipDisplayBarBasedOnCurrentLocale()
        {
            if (_curLayoutInfo == null)
            {
                return;
            }

            bool setRTL = _curLayoutInfo.IsRightToLeft;
            _displayBarInfo.FlipDisplayBar.Flip(setRTL);
            SetDisplayBarTextRTL(setRTL);
            _isRTL = setRTL;
        }

        private void SetDisplayBarTextRTL(bool setRTL)
        {
            if (_tmpInputField.textComponent.GetType() == typeof(KeyboardInputFieldTextMeshPro))
            {
                KeyboardInputFieldTextMeshPro tmp = (KeyboardInputFieldTextMeshPro)_tmpInputField.textComponent;
                tmp.isRightToLeftText = setRTL;
                _caretPos = _tmpInputField.text.Length;
            }
        }

        private GameObject GetSuggestionsParent()
        {
            if (Locale != Code.kJp_JP_Unity)
            {
                return _suggestionsParent;
            }
            else
            {
                return _JPSuggestIsExpanded ?
                    _displayBarInfo.JPSuggestsParentExpanded :
                    _displayBarInfo.JPSuggestsParentCollapsed;
            }
        }

        private string GetTypedContentBasedOnPasswordMode(string curWord, bool withSuggestionText = false)
        {
            bool isPasswordMode =
                _tmpInputField.contentType == TMP_InputField.ContentType.Password;

            string newTypedContent = TypedContent;
            if (withSuggestionText && _growingCurWord)
            {
                string suggestionText = !isPasswordMode ?
                    TEMP_TEXT_PRE_MARK + curWord + TEMP_TEXT_POST_MARK :
                    curWord;
                newTypedContent = TypedContent.Substring(0, _curWordBeginOffset) + suggestionText;
            }

            return isPasswordMode ?
                    newTypedContent.Replace("" + ZERO_WIDTH_CHAR, "") :
                    newTypedContent;
        }

        private void SetTMPFixText(bool fixtText)
        {
            if (_tmpInputField.textComponent.GetType() == typeof(KeyboardInputFieldTextMeshPro))
            {
                KeyboardInputFieldTextMeshPro tmp = (KeyboardInputFieldTextMeshPro)_tmpInputField.textComponent;
                tmp.SetFixText(fixtText);
            }
        }

        private void SetDisplayBarText(string newText)
        {
            if (_tmpInputField.textComponent.GetType() == typeof(KeyboardInputFieldTextMeshPro))
            {
                KeyboardInputFieldTextMeshPro tmp = (KeyboardInputFieldTextMeshPro)_tmpInputField.textComponent;
                newText = _inputFixer.FixText(newText, tmp.isRightToLeftText);
            }

            if (_tmpInputField.contentType == TMP_InputField.ContentType.Password)
            {
                newText = newText.Replace("" + ZERO_WIDTH_CHAR, "");
            }

            _tmpInputField.text = newText;
        }

        private void ResizeDisplayBar(Vector3 curLayoutExtents)
        {
            if (_displayBar)
            {
                _displayBar.transform.localPosition = new Vector3(
                    _displayBar.transform.localPosition.x,
                    curLayoutExtents.y * 0.5f + VirtualKeyboardLayoutGen.BACKGROUND_EDGE_WIDTH +
                    DISPLAY_BAR_Y_OFFSET,
                    _displayBar.transform.localPosition.z
                );
            }
        }

        private void CloseOpenedSubPanel()
        {
            if (_curSelectedSubPanelKey != null)
            {
                KeySubPanel subPanel =
                    _subPanelCanvasInfo.GetSubPanel(_curSelectedSubPanelKey.SubPanelID);
                if (subPanel == null)
                {
                    Debug.LogError(
                        "Invalid SubPanelID \"" + _curSelectedSubPanelKey.SubPanelID + "\"");
                    return;
                }
                KeyToggle toggle = _curSelectedSubPanelKey.GetComponent<KeyToggle>();
                subPanel.gameObject.SetActive(false);
                toggle.Toggle(false);
                _curSelectedSubPanelKey = null;
            }
        }

        private void ResizeSubPanelCanvas(LayoutInfo layoutInfo)
        {
            if (!_curLayoutInfo.IsUGUI)
            {
                return;
            }
            if (_subPanelCanvasInfo != null)
            {
                RectTransform subPanelTransform = (RectTransform) _subPanelCanvasInfo.CanvasObj.transform;
                subPanelTransform.sizeDelta = layoutInfo.IsUGUI ? layoutInfo.Extents : layoutInfo.ScaledExtents;
            }
        }

        private Prediction GetCurrentLocalePrediction()
        {
            switch (Locale)
            {
                case Code.kEn_US_Unity:
                    return _en_US_prediction;
                case Code.kAr_AR_Unity:
                    return _ar_prediction;
                case Code.kDe_DE_Unity:
                    return _de_prediction;
                case Code.kFr_FR_Unity:
                    return _fr_prediction;
                default:
                    return null;
            }
        }

        private bool PredictionIsInitialized()
        {
            Prediction prediction = GetCurrentLocalePrediction();
            if (prediction == null)
            {
                return false;
            }

            return prediction.Initialized;
        }

        private bool HasValidPredictionChars(char ch)
        {
            Prediction prediction = GetCurrentLocalePrediction();
            if (prediction == null)
            {
                return false;
            }

            return prediction.ValidChars.Contains(ch);
        }

        private Task<List<Suggestion>> LaunchPrediction()
        {
            Prediction prediction = GetCurrentLocalePrediction();
            if (prediction == null)
            {
                return null;
            }

            return prediction.LaunchPredict(_curWord.ToString(),
                                            NUM_SUGGESTIONS,
                                            NUM_CANDIDATES,
                                            1,
                                            _predictCancelToken,
                                            _predictCancelTokenSrc);
        }

        private void ConfigJPSuggests(bool expanded)
        {
            ConfigJPSuggestsKeys(expanded); // the JP suggestion keys on the panel
            ConfigJPSuggestsPgKeys(expanded); // the page up/down keys on the panel

            // Install handler for the expand/collapse key
            GameObject expandCollapsKeyObj =
                expanded ? _displayBarInfo.JPExpandUp : _displayBarInfo.JPCollapseDown;
            expandCollapsKeyObj.GetComponent<StatefulInteractable>().OnClicked.AddListener(delegate
            {
                JPExpandCollapseClicked(expandCollapsKeyObj.GetComponent<KeyInfo>());
                ClearTempCandidateDisplay();
                PublishKeyEvent.Invoke(
                    "", expandCollapsKeyObj.GetComponent<KeyInfo>().KeyType, false, GetTypedContentBasedOnPasswordMode(_curWord.ToString()));
            });
            UnityEngine.Debug.Log("Subscribed Expand Up for JP");
        }

        private List<String> FindJPSuggestions(String query)
        {
            if (_printDebugInfo)
            {
                Debug.Log("Launching new find with " + query);
            }
            List<string> primaryResults = KeyboardAPI.FindPrimaryResults(query);
            if (_printDebugInfo)
            {
                Debug.Log("Completed FindPrimaryResults, with " +
                          primaryResults.Count + "results, " +
                          "results are: " + String.Join(", ", primaryResults));
            }
            List<string> secondaryResults = KeyboardAPI.FindSecondaryResults();
            if (_printDebugInfo)
            {
                Debug.Log("Completed FindSecondaryResults, with " +
                          secondaryResults.Count + "results, " +
                          "results are: " + String.Join(", ", secondaryResults));
            }
            return primaryResults.Concat(secondaryResults).ToList();
        }

        private void ClearInputField()
        {
            TypedContent = "";
            if (_tmpInputField)
            {
                SetDisplayBarText("");
                _tmpInputField.ActivateInputField();
            }
            _growingCurWord = false;
            _curWordBeginOffset = -1;
            _curWord.Clear();
            if (Locale == Code.kJp_JP_Unity)
            {
                ThreadDispatcher.ScheduleWork(() =>
                {
                    KeyboardAPI.AnalyzeContext("");
                    return true;
                });
            }
        }

        private void LoadLayout(
            Code nextLocale,
            PageCode nextPageCode,
            bool firstLoad = false)
        {
            if (_printDebugInfo)
            {
                Debug.Log("Loading layout with Language: " + nextLocale +
                          " PageCode: " + nextPageCode);
            }
            if (_curLayoutInfo != null && !_curLayoutInfo.IsUGUI)
            {
                // Hide the Japanese accents panel
                _accentsPanel.SetActive(false);
                if (_curLayoutInfo.JPAccentsKeyToggle)
                {
                    _curLayoutInfo.JPAccentsKeyToggle.Toggle(false);
                }

                // Hide the language selection panel
                _languagePanel.gameObject.SetActive(false);
                _curLayoutInfo.ChangeLocaleKeyToggle.Toggle(false);
            }
            CloseOpenedSubPanel();
            if (nextLocale != Locale || firstLoad) // need to switch in a new asset
            {
                _cacheLastLocale = Locale;
                Locale = nextLocale;
                _cacheLastPageCode = _pageCode;
                _pageCode = nextPageCode;
                if (!_languageToPrefabMap.ContainsKey(nextLocale))
                {
                    Debug.LogError("Cannot find the parent of keys when configuring keys!");

                    // Resume original state before aborting
                    Locale = _cacheLastLocale;
                    _pageCode = _cacheLastPageCode;
                    _localeToLoadOnEnable = Locale;
                    return;
                }
                ResetGrowingWord();
                if (_printDebugInfo)
                {
                    Debug.Log("Spawning new asset cause language changed");
                }
                ClearLayouts(_layoutsParent);
                _layoutsParent = GameObject.Instantiate(_languageToPrefabMap[nextLocale], this.transform);
                ConfigLayouts();
                _localeToLoadOnEnable = Locale;
            }
            else // switch within the currently loaded layouts
            {
                if (nextPageCode == _pageCode)
                {
                    return;
                }
                _cacheLastPageCode = _pageCode;
                _pageCode = nextPageCode;
                LayoutInfo[] layoutInfos = _layoutsParent.GetComponentsInChildren<LayoutInfo>(true);
                foreach (LayoutInfo layoutInfo in layoutInfos)
                {
                    if (layoutInfo.ThisPageCode == nextPageCode)
                    {
                        layoutInfo.gameObject.SetActive(true);
                        _curLayoutInfo = layoutInfo;
                    }
                    else
                    {
                        layoutInfo.gameObject.SetActive(false);
                    }
                }
                ResizeDisplayBar(_curLayoutInfo.ScaledExtents);
                ResizeSubPanelCanvas(_curLayoutInfo);
            }

            KeyboardLayoutChanged.Invoke(Locale, _pageCode, firstLoad);
        }

        // Called once everytime a new layout prefab for a language is switched in
        private void ConfigLayouts()
        {
            LayoutInfo[] layoutInfos = _layoutsParent.GetComponentsInChildren<LayoutInfo>(true);
            foreach (LayoutInfo layoutInfo in layoutInfos)
            {
                // Below are needed every time a new language asset is loaded in
                ConfigKeys(layoutInfo.KeysParent);
                ConfigBackground(layoutInfo.IsUGUI
                    ? layoutInfo.Background.GetComponentInChildren<BackgroundInfo>().gameObject
                    : layoutInfo.Background);

                // Find the layout that'll be first used
                if (layoutInfo.gameObject.activeSelf)
                {
                    _curLayoutInfo = layoutInfo;
                    _pageCode = layoutInfo.ThisPageCode;
                }
            }
            ResizeDisplayBar(_curLayoutInfo.ScaledExtents);
            ConfigSubPanelCanvas();
            ResizeSubPanelCanvas(_curLayoutInfo);
            _displayBarInfo.JPSuggestsParentExpanded.SetActive(false);
            _displayBarInfo.JPSuggestsParentCollapsed.SetActive(false);
            StartCoroutine(CleanSuggestion());

            if (_disableInteractModeSwitch)
            {
                DisableInteractModeSwitchKey();
            }
        }

        private void ConfigJPSuggestsKeys(bool expanded)
        {
            SuggestionPanel panel = expanded
                ? _displayBarInfo.JPSuggestsPanelExpanded
                : _displayBarInfo.JPSuggestsPanelCollapsed;
            for (int i = 0; i < panel.transform.childCount; i++)
            {
                Transform row = panel.transform.GetChild(i);
                for (int j = 0; j < row.childCount; j++)
                {
                    GameObject key = row.GetChild(j).gameObject;
                    key.GetComponent<StatefulInteractable>().OnClicked.AddListener(
                        delegate { JPSuggestClicked(key.GetComponent<KeyInfo>()); });
                }
            }
        }

        private void ConfigJPSuggestsPgKeys(bool expanded)
        {
            GameObject pgDnKeyObj = expanded
                ? _displayBarInfo.JPPageDownExpanded
                : _displayBarInfo.JPPageDownCollapsed;
            GameObject pgUpKeyObj = expanded
                ? _displayBarInfo.JPPageUpExpanded
                : _displayBarInfo.JPPageUpCollapsed;
            SuggestionPanel panel = expanded
                ? _displayBarInfo.JPSuggestsPanelExpanded
                : _displayBarInfo.JPSuggestsPanelCollapsed;
            pgDnKeyObj.GetComponent<StatefulInteractable>().OnClicked.AddListener(
                delegate
                {
                    panel.TurnPage(true);
                    ClearTempCandidateDisplay();
                    PublishKeyEvent.Invoke(
                        "", pgDnKeyObj.GetComponent<KeyInfo>().KeyType, false, GetTypedContentBasedOnPasswordMode(_curWord.ToString()));
                });
            pgUpKeyObj.GetComponent<StatefulInteractable>().OnClicked.AddListener(
                delegate
                {
                    panel.TurnPage(false);
                    ClearTempCandidateDisplay();
                    PublishKeyEvent.Invoke(
                        "", pgUpKeyObj.GetComponent<KeyInfo>().KeyType, false, GetTypedContentBasedOnPasswordMode(_curWord.ToString()));
                });
        }

        // This includes configure functions that only need to be called once at Init()
        private void ConfigPersistentMisc()
        {
            _displayBarInfo.ClearKey?.GetComponent<StatefulInteractable>().OnClicked.AddListener(
                delegate
                {
                    ClearInputField();
                    _displayBarInfo.JPSuggestsPanelExpanded.UnPreSelectKey();
                    _displayBarInfo.JPSuggestsPanelCollapsed.UnPreSelectKey();
                    if (_displayBarInfo.SuggestionsParent.activeSelf)
                    {
                        StartCoroutine(CleanSuggestion());
                    }
                    if (_displayBarInfo.JPSuggestsParentExpanded.activeSelf)
                    {
                        _displayBarInfo.JPSuggestsParentExpanded.SetActive(false);
                    }
                    if (_displayBarInfo.JPSuggestsParentCollapsed.activeSelf)
                    {
                        _displayBarInfo.JPSuggestsParentCollapsed.SetActive(false);
                    }
                    _deleteSpaceAfterSuggestionSelected = false;

                    FlipDisplayBarBasedOnCurrentLocale();

                    PublishKeyEvent.Invoke("", KeyType.kClear, false, GetTypedContentBasedOnPasswordMode(_curWord.ToString()));
                }
            );
            foreach (KeyInfo keyInfo in _displayBarInfo.SuggestionsKeyInfos)
            {
                ConfigKey(keyInfo.gameObject);
            }
            ConfigJPSuggests(true);
            ConfigJPSuggests(false);
            ConfigKeys(_accentsPanel);
            ConfigKeys(_languagePanel);
            _JPSuggestIsExpanded = false;
        }

        // Reset the TypeContent (and the text in the input field)
        // back to "old content + current growing word (that suggestions are based on)"
        private void ClearTempCandidateDisplay()
        {
            if (!_growingCurWord)
            {
                return;
            }
            TypedContent = TypedContent.Substring(0, _curWordBeginOffset) + _curWord;
            if (_tmpInputField)
            {
                string displayBarText =
                    _tmpInputField.contentType != TMP_InputField.ContentType.Password ?
                        TypedContent.Substring(0, _curWordBeginOffset) +
                        TEMP_TEXT_PRE_MARK + _curWord + TEMP_TEXT_POST_MARK :
                        TypedContent;
                SetDisplayBarText(displayBarText);
                _caretPos = _tmpInputField.text.Length;
            }
        }

        // Use "old content + current growing word" as is
        // as the TypedContent (and text in input field) and unhighlight anything in input field
        private void ResetGrowingWord()
        {
            if (!_growingCurWord)
            {
                return;
            }

            string inputFieldText = _inputFixer.GetUnModifiedText();
            TypedContent = _growingCurWord
                ? inputFieldText.Substring(0, _curWordBeginOffset) + _curWord
                : inputFieldText;

            if (_tmpInputField)
            {
                SetDisplayBarText(TypedContent);
                _caretPos = _tmpInputField.text.Length;
            }
            _growingCurWord = false;
            _curWordBeginOffset = -1;
            _curWord.Clear();
            GetSuggestionsParent().SetActive(false);
            _deleteSpaceAfterSuggestionSelected = false;
        }

        private void ConfigSubPanelCanvas()
        {
            if (!_curLayoutInfo.IsUGUI)
            {
                return;
            }
            _subPanelCanvasInfo = _layoutsParent.GetComponentInChildren<SubPanelCanvasInfo>(true);
            if (_subPanelCanvasInfo != null && !_subPanelCanvasInfo.HasInited)
            {
                _subPanelCanvasInfo.Init();
                foreach (KeySubPanel subPanel in _subPanelCanvasInfo.SubPanels)
                {
                    ConfigKeys(subPanel.Container);
                }
                _curSelectedSubPanelKey = null;
            }
        }

        // A hack to disable direct interaction (drumstick) for R4
        private void DisableInteractModeSwitchKey()
        {
            foreach (Transform child in _layoutsParent.transform)
            {
                var layoutInfo = child.GetComponent<LayoutInfo>();
                if (layoutInfo != null)
                {
                    layoutInfo.RayDrumstickSwitchKeyToggle.Disable(Color.gray);
                }
            }
        }
        #endregion Config

        #region RichText
        private string GetRichTextSafeText(string text)
        {
            StringBuilder stringBuilder = new StringBuilder();
            for (int idx = 0; idx < text.Length; idx++)
            {
                if (text[idx] == '<')
                {
                    stringBuilder.Append('<');
                    stringBuilder.Append(ZERO_WIDTH_CHAR);
                }
                else if (text[idx] == '>')
                {
                    stringBuilder.Append(ZERO_WIDTH_CHAR);
                    stringBuilder.Append('>');
                }
                else
                {
                    stringBuilder.Append(text[idx]);
                }
            }
            return stringBuilder.ToString();
        }

        private bool IsLastCharRichTextSafe(string text)
        {
            if (text.Length < 2)
            {
                return false;
            }
            char firstToLastChar = text[text.Length - 1];
            char secondToLastChar = text[text.Length - 2];
            return (firstToLastChar == ZERO_WIDTH_CHAR && secondToLastChar == '<') ||
                   (firstToLastChar == '>' && secondToLastChar == ZERO_WIDTH_CHAR);
        }
        #endregion RichText
        #endregion Private Methods

        #region Public Methods
        #region Config
        public void EditorReloadLayouts()
        {
#if UNITY_EDITOR
            // We have to unpack the prefab first here if we want to modify it in Editor
            if (PrefabUtility.IsAnyPrefabInstanceRoot(gameObject))
            {
                PrefabUtility.UnpackPrefabInstance(
                    gameObject, PrefabUnpackMode.Completely, InteractionMode.UserAction);
            }
#endif
            Init(); // TODO: agrancini check if needed
            CreateLanguageToPrefabMap();
            if (!_languageToPrefabMap.ContainsKey(Locale))
            {
                Debug.LogError("Cannot find the layouts prefab for this language: " + Locale);
                return;
            }
            EditorClearLayouts(_layoutsParent);
            _layoutsParent = GameObject.Instantiate(_languageToPrefabMap[Locale], this.transform);
            foreach (Transform child in _layoutsParent.transform)
            {
                if (child.gameObject.activeInHierarchy) //Find the one that'll be first used
                {
                    LayoutInfo layoutInfo = child.gameObject.GetComponent<LayoutInfo>();
                    if (layoutInfo)
                    {
                        _curLayoutInfo = layoutInfo;
                    }
                }
            }
            ResizeDisplayBar(_curLayoutInfo.ScaledExtents);
            ResizeSubPanelCanvas(_curLayoutInfo);
        }
        #endregion Config

        private void HideKeyboard()
        {
            gameObject.SetActive(false);
        }

        public void ResetKeyboardField(string newTypedContent)
        {
            newTypedContent = newTypedContent.Replace("" + ZERO_WIDTH_CHAR, "");
            StringBuilder stringBuilder = new StringBuilder();
            ResetGrowingWord();
            for (int i = 0; i < newTypedContent.Length; i++)
            {
                string textToAdd = GetRichTextSafeText("" + newTypedContent[i]);
                stringBuilder.Append(textToAdd);
            }
            TypedContent = stringBuilder.ToString();
            if (_tmpInputField && _initialized)
            {
                SetDisplayBarText(TypedContent);
                _caretPos = _tmpInputField.text.Length;
            }
        }

        public void ResetKeyboardField(string newTypedContent, bool isRightToLeft)
        {
            if (_initialized)
            {
                _displayBarInfo.FlipDisplayBar.Flip(isRightToLeft);
                SetDisplayBarTextRTL(isRightToLeft);
            }
            _isRTL = isRightToLeft;

            ResetKeyboardField(newTypedContent);
        }

        public void OpenKeyboard(string newTypedContent)
        {
            ResetKeyboardField(newTypedContent);
            gameObject.SetActive(true);
        }

        public void OpenKeyboard(string newTypedContent, bool isRightToLeft)
        {
            ResetKeyboardField(newTypedContent, isRightToLeft);
            gameObject.SetActive(true);
        }

        public GameObject LayoutsParent()
        {
            return _layoutsParent;
        }

        public PageCode CurrentPageCode()
        {
            return _pageCode;
        }

        public bool IsRTL()
        {
            return _isRTL;
        }

        public string GetNonRichTypedContent()
        {
            string typedText = GetTypedContentBasedOnPasswordMode(_curWord.ToString());
            return typedText.Replace("" + ZERO_WIDTH_CHAR, "");
        }

        public string SetInputFieldContentType(TMP_InputField.ContentType contentType,
                                               bool notifyTextChange = false)
        {
            if (_tmpInputField && _curInputFieldContentType != contentType)
            {
                _curInputFieldContentType = contentType;
                _tmpInputField.contentType = contentType;
                if (contentType == TMP_InputField.ContentType.Standard)
                {
                    _tmpInputField.lineType = TMP_InputField.LineType.MultiLineSubmit;
                }
                if (_initialized)
                {
                    string displayBarText =
                        GetTypedContentBasedOnPasswordMode(_curWord.ToString(), true);
                    SetDisplayBarText(displayBarText);
                    _caretPos = _tmpInputField.text.Length;
                }

                if (notifyTextChange)
                {
                    string sentTypedContent =
                        GetTypedContentBasedOnPasswordMode(_curWord.ToString());
                    PublishKeyEvent.Invoke("",
                                           KeyType.kCharacter,
                                           false,
                                           sentTypedContent);
                }
            }

            return GetTypedContentBasedOnPasswordMode(_curWord.ToString());
        }

        public void AddPunctuationCharToIgnoreOnWhiteSpaceDeleteSet(char ch)
        {
            if ((char.GetUnicodeCategory(ch) != UnicodeCategory.OtherPunctuation &&
                char.GetUnicodeCategory(ch) != UnicodeCategory.DashPunctuation) ||
                _punctuationMarksToIgnoreOnWhiteSpaceDeleteSet.Contains(ch))
            {
                return;
            }

            _punctuationMarksToIgnoreOnWhiteSpaceDeleteSet.Add(ch);
        }

        public void RemovePunctuationCharToIgnoreOnWhiteSpaceDeleteSet(char ch)
        {
            if ((char.GetUnicodeCategory(ch) != UnicodeCategory.OtherPunctuation &&
                char.GetUnicodeCategory(ch) != UnicodeCategory.DashPunctuation) ||
                !_punctuationMarksToIgnoreOnWhiteSpaceDeleteSet.Contains(ch))
            {
                return;
            }

            _punctuationMarksToIgnoreOnWhiteSpaceDeleteSet.Remove(ch);
        }

        public void SwitchCurrentLayout(PageCode nextPageCode)
        {
            if (!gameObject.activeSelf || !_initialized)
            {
                return;
            }

            LoadLayout(Locale, nextPageCode);
        }

        public void SwitchCurrentLayout(Code newLocale)
        {
            if (!_initialized)
            {
                Locale = newLocale;
                return;
            }


            if (gameObject.activeSelf)
            {
                PageLayoutProperties pageLayoutProperties =
                    GetPageLayoutProperties(newLocale, _layoutType);
                PageCode defaultPageCode = (PageCode)pageLayoutProperties.DefaultPage;

                LoadLayout(newLocale, defaultPageCode);

                if (TypedContent == "")
                {
                    FlipDisplayBarBasedOnCurrentLocale();
                }
            }
            else
            {
                _localeToLoadOnEnable = newLocale;
            }
        }

        // Give the consumers of VKB the access to switch input method
        // between direct interaction and ray-casting
        // Not used for R4 as the interaction switch key on VKB is disabled,
        // which is the safer way as nothing else in our apps uses the direct input (drumstick)
        public void SwitchInputMethod(bool toRayCasting)
        {
#if FIXME
            if (_controllerInput == null)
            {
                return;
            }
            var interactorSwitch = _controllerInput.GetComponent<DirectInteractorSwitch>();
            if (interactorSwitch == null || interactorSwitch.DirectInteractor != toRayCasting)
            {
                return;
            }
            interactorSwitch.SwitchInput();
            foreach (Transform child in _layoutsParent.transform)
            {
                var layoutInfo = child.GetComponent<LayoutInfo>();
                layoutInfo.RayDrumstickSwitchKeyToggle.Toggle(!toRayCasting);
            }
#endif
        }
        #endregion Public Methods

        #region Coroutines
        private IEnumerator CleanSuggestion()
        {
            yield return new WaitForSeconds(0.1f);
            _suggestionsParent.SetActive(false);
            if (_debug)
            {
                Debug.Log("Caret Position is at " + storedCaretPosition);
            }
            yield return null;
        }
        #endregion Coroutines
    }
}
