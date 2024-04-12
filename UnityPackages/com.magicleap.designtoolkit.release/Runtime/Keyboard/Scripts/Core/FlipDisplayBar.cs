// Copyright (c) 2019-present, Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Developer Agreement, located
// here: https://auth.magicleap.com/terms/developer
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace MagicLeap.DesignToolkit.Keyboard
{
    public class FlipDisplayBar : MonoBehaviour
    {
        #region Private Members
        private bool _isFlipped = false;

        private bool _initialized = false;

        private List<RectTransform> _rectTransformsToFlip = new List<RectTransform>();

        private List<TMP_Text> _textMeshesToFlip = new List<TMP_Text>();
        #endregion

        #region Public Methods
        public void Init()
        {
            if (_initialized)
            {
                return;
            }

            _rectTransformsToFlip.Clear();
            _textMeshesToFlip.Clear();

            RectTransform[] rectTransforms = GetComponentsInChildren<RectTransform>(true);
            foreach (RectTransform rectTransform in rectTransforms)
            {
                if (!_rectTransformsToFlip.Contains(rectTransform))
                {
                    _rectTransformsToFlip.Add(rectTransform);
                }
            }

            TMP_Text[] textMeshes = GetComponentsInChildren<TMP_Text>(true);
            foreach (TMP_Text textMesh in textMeshes)
            {
                if (!_textMeshesToFlip.Contains(textMesh))
                {
                    _textMeshesToFlip.Add(textMesh);
                }
            }

            _initialized = true;
        }

        public void Flip(bool flipped)
        {
            if (_isFlipped == flipped)
            {
                return;
            }

            foreach (RectTransform rectTransform in _rectTransformsToFlip)
            {
                FlipRectTransform(rectTransform);
            }

            foreach (TMP_Text textMesh in _textMeshesToFlip)
            {
                FlipTextMesh(textMesh);
            }

            _isFlipped = flipped;
        }
        #endregion Public Methods

        #region Private Methods
        private void FlipRectTransform(RectTransform rectTransform)
        {
            Vector2 anchorMin = rectTransform.anchorMin;
            Vector2 anchorMax = rectTransform.anchorMax;

            if (anchorMin.x != 0 || anchorMax.x != 1)
            {
                anchorMin.x = Mathf.Abs(anchorMin.x - 1);
                anchorMax.x = Mathf.Abs(anchorMax.x - 1);

                if (anchorMin.x > anchorMax.x)
                {
                    float temp = anchorMin.x;
                    anchorMin.x = anchorMax.x;
                    anchorMax.x = temp;
                }

                rectTransform.anchorMin = anchorMin;
                rectTransform.anchorMax = anchorMax;
            }

            Vector2 pivot = rectTransform.pivot;
            pivot.x = Mathf.Abs(pivot.x - 1);
            rectTransform.pivot = pivot;

            rectTransform.anchoredPosition *= Vector2.left + Vector2.up;
        }

        private void FlipTextMesh(TMP_Text textMesh)
        {
            switch (textMesh.horizontalAlignment)
            {
                case HorizontalAlignmentOptions.Left:
                    textMesh.horizontalAlignment = HorizontalAlignmentOptions.Right;
                    break;
                case HorizontalAlignmentOptions.Right:
                    textMesh.horizontalAlignment = HorizontalAlignmentOptions.Left;
                    break;
            }
        }
        #endregion Private Methods
    }
}