using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using MixedReality.Toolkit;
using MixedReality.Toolkit.UX.Experimental;
using TMPro;
using UnityEngine;

namespace MagicLeap.LeapBrush
{
    /// <summary>
    /// Popup UI to show about information and legal notices.
    /// </summary>
    public class AboutPopup : BasePopup
    {
        [SerializeField]
        private StatefulInteractable _closeButton;

        [SerializeField]
        private GameObject _loadingNoticesGameObject;

        [SerializeField]
        private GameObject _noticesScrollArea;

        [SerializeField]
        private StatefulInteractable _scrollPreviousButton;

        [SerializeField]
        private StatefulInteractable _scrollNextButton;

        [SerializeField]
        private VirtualizedScrollRectList _scrollList;

        private List<String> _noticeLines = new();
        private DelayedButtonHandler _delayedButtonHandler;

        private float targetScrollPosition;
        private bool scrollListAnimating;

        private void Awake()
        {
            _delayedButtonHandler = gameObject.AddComponent<DelayedButtonHandler>();
        }

        private void Start()
        {
            _closeButton.OnClicked.AddListener(OnCancelButtonClicked);
            _scrollPreviousButton.OnClicked.AddListener(OnScrollPreviousButtonClicked);
            _scrollNextButton.OnClicked.AddListener(OnScrollNextButtonClicked);

            _scrollList.OnVisible += OnListItemVisible;

            if (_noticeLines.Count == 0)
            {
                ResourceRequest resourceRequest = Resources.LoadAsync<TextAsset>(
                    OpenSourceNoticesResource.ResourceName);
                resourceRequest.completed += operation =>
                {
                    if (resourceRequest.asset is TextAsset textAsset)
                    {
                        _loadingNoticesGameObject.SetActive(false);
                        _noticesScrollArea.SetActive(true);

                        using (MemoryStream memoryStream = new MemoryStream(textAsset.bytes))
                        using (GZipStream gzipStream = new GZipStream(
                                   memoryStream, CompressionMode.Decompress))
                        using (StreamReader reader = new StreamReader(gzipStream))
                        {
                            PopulateNoticeLines(reader.ReadToEnd());
                        }

                        _scrollList.SetItemCount(_noticeLines.Count);
                        UpdateAllListItems();
                    }
                };
            }
        }

        private void PopulateNoticeLines(string noticeText)
        {
            _noticeLines.Clear();

            int lastSplitIndex = -1;
            int lastWhitespaceIndex = -1;
            for (int i = 0; i < noticeText.Length; i++)
            {
                if (noticeText[i] == ' ' || noticeText[i] == '\t' || noticeText[i] == '\n')
                {
                    lastWhitespaceIndex = i;
                }
                if (noticeText[i] == '\n' || i - lastSplitIndex - 1 >= 70)
                {
                    if (i - lastWhitespaceIndex < 40 && lastWhitespaceIndex != i)
                    {
                        _noticeLines.Add(noticeText.Substring(
                            lastSplitIndex + 1, lastWhitespaceIndex - lastSplitIndex));
                        lastSplitIndex = lastWhitespaceIndex;
                    }
                    else
                    {
                        _noticeLines.Add(noticeText.Substring(
                            lastSplitIndex + 1, i - lastSplitIndex));
                        lastSplitIndex = i;
                    }
                }
            }

            _noticeLines.Add(noticeText.Substring(lastSplitIndex + 1));
        }

        private void Update()
        {
            if (scrollListAnimating)
            {
                float newScroll = Mathf.Lerp(
                    _scrollList.Scroll, targetScrollPosition, 8 * Time.deltaTime);
                _scrollList.Scroll = newScroll;
                if (Mathf.Abs(_scrollList.Scroll - targetScrollPosition) < 0.02f)
                {
                    _scrollList.Scroll = targetScrollPosition;
                    scrollListAnimating = false;
                }
            }
        }

        private void OnListItemVisible(GameObject listItem, int index)
        {
            // TODO(ghazen): Work around the Instantiate call for scroll rect items
            // causing items to have identity world rotations.
            listItem.transform.localRotation = Quaternion.identity;

            var textNode = listItem.GetComponentInChildren<TMP_Text>();
            textNode.SetText(_noticeLines[index]);
        }

        private void UpdateAllListItems()
        {
            for (int i = 0; i < _noticeLines.Count; i++)
            {
                if (_scrollList.TryGetVisible(i, out GameObject listItem))
                {
                    OnListItemVisible(listItem, i);
                }
            }
        }

        private void OnCancelButtonClicked()
        {
            _delayedButtonHandler.InvokeAfterDelayExclusive(() =>
            {
                Hide();
            });
        }

        private void OnScrollPreviousButtonClicked()
        {
            scrollListAnimating = true;
            targetScrollPosition = Mathf.Max(
                0, Mathf.Floor(_scrollList.Scroll / _scrollList.RowsOrColumns)
                * _scrollList.RowsOrColumns - _scrollList.TotallyVisibleCount);
        }

        private void OnScrollNextButtonClicked()
        {
            scrollListAnimating = true;
            targetScrollPosition = Mathf.Min(_scrollList.MaxScroll, Mathf.Floor(
                    _scrollList.Scroll / _scrollList.RowsOrColumns)
                * _scrollList.RowsOrColumns + _scrollList.TotallyVisibleCount);
        }
    }
}