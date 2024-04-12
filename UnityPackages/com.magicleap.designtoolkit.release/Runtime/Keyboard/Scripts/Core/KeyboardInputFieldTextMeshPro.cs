// Copyright (c) 2019-present, Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Developer Agreement, located
// here: https://auth.magicleap.com/terms/developer
using System.Collections.Generic;
using RTLTMPro;
using TMPro;
using System.Text.RegularExpressions;
using UnityEngine;

namespace MagicLeap.DesignToolkit.Keyboard
{
    public class KeyboardInputFieldTextMeshPro : TextMeshProUGUI
    {
        #region [Constant] Private Members
        private const char ZERO_WIDTH_CHAR = '\u200B';
        #endregion [Constant] Private Members

        #region Private Members
        /// <summary>
        /// Flag that states if the text being passed in should be modified or not.
        /// Should be true in most cases except for the input field on the main
        /// Keyboard Manager Prefab.
        /// </summary>
        private bool _fixText = true;

        /// <summary>
        /// Handles fixing text for Language Mixing.
        /// </summary>
        private KeyboardInputFieldFixer _fixer = new KeyboardInputFieldFixer();

        /// <summary>
        /// States whether or not the string passed had a zero width character at
        /// at the end of the string. Used in combination with InputFields.
        /// </summary>
        private bool _hadZeroWidthCharAtEnd = false;
        #endregion

        #region [SerializedField] Protected Members
        /// <summary>
        /// The unmodified string passed into the TextMeshPro.
        /// </summary>
        [SerializeField]
        [TextArea(3, 10)]
        protected string _originalText = "";

        /// <summary>
        /// Boolean flag that will automatically set the isRightToLeftText flag on the TextMeshPro
        /// based on if the text being displayed is in RTL or not
        /// </summary>
        [SerializeField]
        protected bool _autoSetRTL = true;
        #endregion [SerializedField] Protected Members

        #region [Inherited] Public Members
#if TMP_VERSION_2_1_0_OR_NEWER
        public override string text
#else
        public new string text
#endif
        {
            get { return base.text; }
            set
            {
                string temp = "";
                if (value != null &&
                value.Length > 0 &&
                value[value.Length - 1] == ZERO_WIDTH_CHAR)
                {
                    temp = value.Substring(0, value.Length - 1);
                    _hadZeroWidthCharAtEnd = true;
                }
                else
                {
                    temp = value;
                    _hadZeroWidthCharAtEnd = false;
                }

                if (temp == _originalText)
                {
                    return;
                }
                _originalText = temp;

                UpdateText();
            }
        }
        #endregion [Inherited] Public Members

        #region Public Methods
        public void SetFixText(bool fixText)
        {
            _fixText = fixText;
            havePropertiesChanged = true;
        }
        #endregion Public Methods

        #region Monobehaviour Methods
        private void Update()
        {
            if (havePropertiesChanged)
            {
                UpdateText();
            }
        }
        #endregion Monobehaviour Methods

        #region Private Methods
        /// <summary>
        /// Updates text so that it may be display properly on text field
        /// </summary>
        private void UpdateText()
        {
            if (_autoSetRTL)
            {
                isRightToLeftText = RTLTextHelper.IsRTLString(_originalText);
            }

            string newText = (_fixText ?
                                _fixer.FixText(_originalText, isRightToLeftText) :
                                _originalText);

            base.text = _hadZeroWidthCharAtEnd ? newText + ZERO_WIDTH_CHAR : newText;

            havePropertiesChanged = true;
        }
        #endregion Private Methods
    }
}