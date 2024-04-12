// Copyright (c) 2019-present, Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Developer Agreement, located
// here: https://auth.magicleap.com/terms/developer
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Transactions;
using TMPro;

namespace MagicLeap.DesignToolkit.Keyboard
{
    public class LayoutSwitchData
    {
        public PageCode PrevPageCode;
        public PageCode NextPageCode;
    }

    ///<summary>
    /// Helper to enable / disable groups of contenT
    /// modified for keyboard to track destroyed layouts on runtime 
    ///</summary>
    [RequireComponent(typeof(KeyboardManager))]
    public class ContentSwitcherKeyboard : MonoBehaviour
    {
        #region [SerializeField] Private Members
        [SerializeField]
        private KeyboardManager _keyboardManager;
        #endregion [SerializeField] Private Members

        #region Private Members
        private Code _currentLocaleCode;
        private PageCode _currentPageCode;
        private GameObject _keyboardLayoutParent;
        private Dictionary<PageCode, LayoutSwitchData> _layoutSwitchDictionary =
            new Dictionary<PageCode, LayoutSwitchData>();
        #endregion Private Members

        #region MonoBehaviour Methods
        private void OnEnable()
        {
            _currentLocaleCode = _keyboardManager.Locale;
            _currentPageCode = _keyboardManager.CurrentPageCode();

            _keyboardManager.KeyboardLayoutChanged.AddListener(OnKeyboardLayoutChange);
        }

        private void OnDisable()
        {
            _keyboardManager.KeyboardLayoutChanged.RemoveListener(OnKeyboardLayoutChange);
        }
        #endregion MonoBehaviour Methods

        #region
        private void OnKeyboardLayoutChange(
            Code newLocaleCode, PageCode newPageCode, bool firstTimeInitialization)
        {
            if (firstTimeInitialization || _currentLocaleCode != newLocaleCode)
            {
                AddLayouts();
                _currentLocaleCode = newLocaleCode;
            }

            _currentPageCode = newPageCode;
        }

        private void AddLayouts()
        {
            _keyboardLayoutParent = _keyboardManager.LayoutsParent();
            _layoutSwitchDictionary.Clear();
            LayoutInfo[] layoutInfos =
                _keyboardLayoutParent.GetComponentsInChildren<LayoutInfo>(true);
            PageCode prevPageCode = 0;
            for (int i = 0; i < layoutInfos.Length; i++)
            {
                LayoutSwitchData switchData = new LayoutSwitchData();
                if (_layoutSwitchDictionary.Count > 0)
                {
                    _layoutSwitchDictionary[prevPageCode].NextPageCode =
                        layoutInfos[i].ThisPageCode;
                    switchData.PrevPageCode = prevPageCode;
                }
                _layoutSwitchDictionary.Add(layoutInfos[i].ThisPageCode, switchData);
                prevPageCode = layoutInfos[i].ThisPageCode;
            }

            if (layoutInfos.Length > 0)
            {
                PageCode topPageCode = layoutInfos[0].ThisPageCode;
                PageCode bottomPageCode = layoutInfos[layoutInfos.Length - 1].ThisPageCode;
                _layoutSwitchDictionary[topPageCode].PrevPageCode = bottomPageCode;
                _layoutSwitchDictionary[bottomPageCode].NextPageCode = topPageCode;
            }
        }
        #endregion

        #region Public Methods
        public void Next()
        {
            PageCode nextPageCode = _layoutSwitchDictionary[_currentPageCode].NextPageCode;
            if (nextPageCode != _currentPageCode)
            {
                Open(nextPageCode);
            }
        }

        public void Previous()
        {
            PageCode nextPageCode = _layoutSwitchDictionary[_currentPageCode].PrevPageCode;
            if (nextPageCode != _currentPageCode)
            {
                Open(nextPageCode);
            }
        }

        public void Open(GameObject content)
        {
            LayoutInfo layoutInfo;
            if ((layoutInfo = content.GetComponent<LayoutInfo>()) == null)
            {
                return;
            }

            if (_layoutSwitchDictionary.ContainsKey(layoutInfo.ThisPageCode))
            {
                Open(layoutInfo.ThisPageCode);
            }
            else
            {
                Debug.LogError("Gameobject " + content + " does not exist in this content switcher.");
            }
        }

        public void Open(PageCode code)
        {
            _keyboardManager.SwitchCurrentLayout(code);
        }
        #endregion Public Methods
    }
}