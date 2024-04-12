// Copyright (c) 2019-present, Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Developer Agreement, located
// here: https://auth.magicleap.com/terms/developer
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace MagicLeap.DesignToolkit.Keyboard
{
    /// <summary>
    /// A panel with fixed number of rows & columns of buttons
    /// Currently used for the Japanese suggestions
    /// </summary>
    public class SuggestionPanel : MonoBehaviour
    {
        #region Public Members
        public GameObject PreSelectedKey;
        #endregion Public Members

        #region [SerializeField] Private Members
        [SerializeField]
        private bool _debug;
        [SerializeField]
        private TextMeshProUGUI _pageDisplay;
        [SerializeField]
        private int _setButtonsBatchSize = 4;
        [SerializeField]
        private float _setButtonsInterval = 0.05f;
        // These 2 numbers need to match the structure of the actual prefab of Suggestion Panel
        [SerializeField]
        private int _numRows = 5;
        [SerializeField]
        private int _numCols = 4;
        [SerializeField]
        private KeyInfo _keyPageNext;
        [SerializeField]
        private KeyInfo _keyPagePrev;
        [SerializeField]
        private Color PageButtonEnabledColor = Color.white;
        [SerializeField]
        private Color PageButtonDisabledColor = new Color(0.61f, 0.61f, 0.62f, 1.0f);
        #endregion [SerializeField] Private Members

        #region Private Members
        private int _curPageIdx;
        private int _totalNumPages;
        private Coroutine _setButtonsRoutine = null;
        private List<String> _contents;
        #endregion Private Members

        #region Public Members
        public void PreSelectKey(GameObject keyObj)
        {
            if (PreSelectedKey == keyObj)
            {
                return;
            }
            if (_debug)
            {
                Debug.Log("Preselecting the key of " +
                          keyObj.GetComponent<KeyInfo>().TextToType);
            }
            UnPreSelectKey();
            keyObj.GetComponent<SuggestionPreselect>().Preselect();
            PreSelectedKey = keyObj;
        }

        public void UnPreSelectKey()
        {
            if (PreSelectedKey == null)
            {
                return;
            }
            if (_debug)
            {
                Debug.Log("UnPreselecting the key of " +
                          PreSelectedKey.GetComponent<KeyInfo>().TextToType);
            }
            PreSelectedKey.GetComponent<SuggestionPreselect>().UnPreselect();
            PreSelectedKey = null;
        }

        public void PreSelectNextKey()
        {
            int curCol = PreSelectedKey == null ? -1 : PreSelectedKey.transform.GetSiblingIndex();
            int curRow = PreSelectedKey == null ? 0 : PreSelectedKey.transform.parent.GetSiblingIndex();
            ++curCol;
            if (curCol >= _numCols)
            {
                curCol = 0;
                ++curRow;
            }
            if (curRow >= _numRows)
            {
                if (!TurnPage(true))
                {
                    return;
                }
                curRow = 0;
                curCol = 0;
            }
            GameObject toSelect = transform.GetChild(curRow).GetChild(curCol).gameObject;
            if (!toSelect.activeSelf)
            {
                return;
            }
            PreSelectKey(transform.GetChild(curRow).GetChild(curCol).gameObject);
        }

        public bool TurnPage(bool pageDown)
        {
            int newPageIdx = pageDown ? _curPageIdx + 1 : _curPageIdx - 1;
            return TurnToPage(newPageIdx);
        }

        public bool TurnToPage(int pageIdx)
        {
            int pageSize = _numRows * _numCols;
            int startingIdx = pageSize * pageIdx;

            if (startingIdx < 0 || startingIdx >= _contents.Count)
            {
                return false;
            }
            _curPageIdx = pageIdx;
            SetButtons(0, 0, startingIdx, _contents);
            UnPreSelectKey();
            EnableSuggestionPageKeys();
            ShowPageNum(_curPageIdx + 1, _totalNumPages);
            return true;
        }

        public void PopulateNewContents(List<String> contents)
        {
            Debug.Log("Populating with new contents");
            _contents = contents;
            _curPageIdx = 0;
            SetButtons(0, 0, 0, _contents);
            int itemsPerPage = _numRows * _numCols;
            _totalNumPages = (contents.Count + itemsPerPage - 1) / itemsPerPage;
            EnableSuggestionPageKeys();
            ShowPageNum(_curPageIdx + 1, _totalNumPages);
        }
        #endregion Public Methods

        #region Private Methods
        private void SetButtons(int startingRow, int startingCol, int startingIdx,
            List<String> contents)
        {
            if (_setButtonsRoutine != null)
            {
                StopCoroutine(_setButtonsRoutine);
                _setButtonsRoutine = null;
            }
            _setButtonsRoutine = StartCoroutine(SetButtonsRoutine(
                startingRow, startingCol, startingIdx, contents, _setButtonsBatchSize));
        }

        // Returns true if the current page cannot be populated anymore,
        // either by running out of page space or running out of contents
        private bool SetButtonsBatch(ref int row, ref int col,
            ref int idx, List<String> contents,
            int batchSize)
        {
            int count = 0;
            while (count < batchSize)
            {
                if (idx >= contents.Count)
                {
                    return true;
                }
                if (col >= _numCols) // turn to the next row
                {
                    ++row;
                    col = 0;
                }
                if (row >= _numRows)
                {
                    return true;
                }
                SetButton(row, col, contents[idx]);
                ++count;
                ++col;
                ++idx;
            }
            return false;
        }

        private void SetButton(int row, int col, String content)
        {
            if (row > transform.childCount)
            {
                Debug.LogError("Trying to access row that is out of bound!");
                return;
            }
            Transform rowTrs = transform.GetChild(row);
            GameObject rowObj = rowTrs.gameObject;
            if (!rowObj.activeSelf)
            {
                rowObj.SetActive(true);
            }
            if (col > rowTrs.childCount)
            {
                Debug.LogError("Trying to access col that is out of bound!");
                return;
            }
            GameObject buttonObj = rowTrs.GetChild(col).gameObject;
            KeyInfo keyInfo = buttonObj.GetComponent<KeyInfo>();
            keyInfo.KeyTMP.text = content;
            keyInfo.TextToType = content;
            if (!buttonObj.activeSelf)
            {
                buttonObj.SetActive(true);
            }
        }

        private void DisableAllButtons()
        {
            foreach (Transform row in transform)
            {
                foreach (Transform button in row)
                {
                    button.gameObject.SetActive(false);
                }
            }
        }

        private void DisableUnusedButtons(int row, int col)
        {
            while (row < _numRows)
            {
                if (col >= _numCols) // turn to the next row
                {
                    ++row;
                    col = 0;
                }
                if (row >= _numRows)
                {
                    return;
                }
                transform.GetChild(row).GetChild(col).gameObject.SetActive(false);
                ++col;
            }
        }

        private void EnableSuggestionPageKeys()
        {
            if (_keyPageNext != null)
            {
                bool nextPageKeyEnabled = _curPageIdx < _totalNumPages - 1;
                _keyPageNext.KeyInteractable.enabled = nextPageKeyEnabled;
                _keyPageNext.KeyIconImages[0].color =
                    nextPageKeyEnabled ? PageButtonEnabledColor : PageButtonDisabledColor;
            }

            if (_keyPagePrev != null)
            {
                bool prevPageKeyEnabled = _curPageIdx > 0;
                _keyPagePrev.KeyInteractable.enabled = prevPageKeyEnabled;
                _keyPagePrev.KeyIconImages[0].color =
                    prevPageKeyEnabled ? PageButtonEnabledColor : PageButtonDisabledColor;
            }
        }

        private void ShowPageNum(int curPage, int totalPages)
        {
            if (_pageDisplay != null)
            {
                _pageDisplay.text = "" + curPage + " / " + totalPages;
            }
        }

        IEnumerator SetButtonsRoutine(int row, int col,
            int idx, List<String> contents,
            int batchSize)
        {
            DisableAllButtons();
            while (!SetButtonsBatch(ref row, ref col, ref idx, contents, batchSize))
            {
                yield return new WaitForSeconds(_setButtonsInterval);
            }
            DisableUnusedButtons(row, col);
            Debug.Log("End of SetButtonsRoutine");
        }
        #endregion Private Methods
    }
}