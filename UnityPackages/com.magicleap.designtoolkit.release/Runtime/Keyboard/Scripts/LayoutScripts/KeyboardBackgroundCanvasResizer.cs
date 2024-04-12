// Copyright (c) 2022 Magic Leap, Inc. All Rights Reserved.
// Please see the top-level LICENSE.md in this distribution
// for terms and conditions governing this file.
using UnityEngine;

namespace MagicLeap.DesignToolkit.Keyboard
{
    public class KeyboardBackgroundCanvasResizer : MonoBehaviour
    {
        #region NestedType / Constructors
        [System.Serializable]
        public class RectTransformDimensions
        {
            public float Width;
            public float Height;
        }
        #endregion NestedType / Constructors

        #region [SerializeField] Private Members
        [SerializeField]
        private KeyboardManager _keyboardManager;
        [SerializeField]
        private RectTransformDimensions _englishOrArabicDimensions;
        [SerializeField]
        private RectTransformDimensions _japaneseDimensions;
        #endregion [SerializeField] Private Members

        #region Private Members
        private RectTransform _rectTransform;
        #endregion Private Members

        #region MonoBehaviour Methods
        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            _keyboardManager.KeyboardLayoutChanged.AddListener(HandleKeyboardLayoutChanged);
        }

        private void OnDisable()
        {
            _keyboardManager.KeyboardLayoutChanged.RemoveListener(HandleKeyboardLayoutChanged);
        }
        #endregion MonoBehaviour Methods

        #region Event Handlers
        private void HandleKeyboardLayoutChanged(
            Code code, PageCode pageCode, bool firstTimeInitialization)
        {
            if (code == Code.kJp_JP_Unity)
            {
                SetDimensions(_japaneseDimensions);
            }
            else
            {
                SetDimensions(_englishOrArabicDimensions);
            }
        }
        #endregion Event Handlers

        #region Private Methods
        private void SetDimensions(RectTransformDimensions dimensions)
        {
            _rectTransform.sizeDelta = new Vector2(dimensions.Width, dimensions.Height);
        }
        #endregion Private Methods
    }
}