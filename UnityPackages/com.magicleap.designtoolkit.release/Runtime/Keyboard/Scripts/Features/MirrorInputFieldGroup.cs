// Copyright (c) 2019-present, Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Developer Agreement, located
// here: https://auth.magicleap.com/terms/developer
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using MagicLeap.Keyboard;

namespace MagicLeap.DesignToolkit.Keyboard
{
    /// <summary>
    /// Utility used to handle multiple MirrorInputField and one reference input field on a distant panel
    /// </summary>
    public class MirrorInputFieldGroup : MonoBehaviour
    {
        #region NestedType / Constructors
        [System.Serializable]
        public class InputFieldReferences
        {
            [Tooltip("Reference input field")]
            public TMP_InputField InputField;
            [Tooltip("List of input field mirroring the content of the reference input field")]
            public List<MirrorInputField> MirrorInputFields = new();
            [Tooltip("Current input field that is actively copying the content from the reference input field")]
            public MirrorInputField CurrentInputField;
            [Tooltip("If this is applied to keyboard, needs to be reset the word count on keyboard")]
            public KeyboardManager KeyboardManager;
        }
        #endregion NestedType / Constructors

        #region Public Members
        public InputFieldReferences References;
        public bool MatchHorizontalAlignmentWithVKBInputField = true;
        #endregion Public Members

        #region [SerializeField] Private Members
        [SerializeField]
        private bool _isListeningToKeyboardManager = false;
        #endregion [SerializeField] Private Members

        #region Private Members
        private bool _currentlyListeningToKeyboardManager = false;
        #endregion Private Members

        #region Monobehaviour
        private void Awake()
        {
            if (References.InputField == null)
            {
                // Attach to the keyboard's input field automatically for mirroring if
                // no other is provided.
                References.InputField = References.KeyboardManager.InputField;
            }
        }

        private void OnEnable()
        {
            if (References.KeyboardManager != null && _isListeningToKeyboardManager)
            {
                References.KeyboardManager.PublishKeyEvent.AddListener(OnPublishKeyEvent);
                _currentlyListeningToKeyboardManager = true;
                return;
            }

            References.InputField.onValueChanged.AddListener(Mirror);
        }

        private void OnDisable()
        {
            if (_currentlyListeningToKeyboardManager)
            {
                References.KeyboardManager.PublishKeyEvent.RemoveListener(OnPublishKeyEvent);
                _currentlyListeningToKeyboardManager = false;
            }
            else
            {
                References.InputField.onValueChanged.RemoveListener(Mirror);
            }
        }
        #endregion Monobehaviour

        #region Public Methods
        public bool IsListeningToKeyboardManager()
        {
            return _isListeningToKeyboardManager;
        }

        public void ClearAllInputFields()
        {
            if (References.KeyboardManager != null &&
                References.KeyboardManager.InputField == References.InputField)
            {
                References.KeyboardManager.ResetKeyboardField("");
            }
            else
            {
                References.InputField.SetTextWithoutNotify("");
            }

            foreach (MirrorInputField mirrorInputField in References.MirrorInputFields)
            {
                mirrorInputField.SetInputField("", false);
            }
        }

        public void ClearCurrentInputFields()
        {
            if (References.KeyboardManager != null &&
                References.KeyboardManager.InputField == References.InputField)
            {
                References.KeyboardManager.ResetKeyboardField("");
            }
            else
            {
                References.InputField.SetTextWithoutNotify("");
            }

            References.CurrentInputField.SetInputField("", false);
        }
        #endregion

        #region Private Methods
        // after updating the reference input field, mirror the reference input field content on this input field 
        private void Mirror(string reference)
        {
            if (References.KeyboardManager != null &&
                References.KeyboardManager.InputField != null &&
                References.KeyboardManager.InputField == References.InputField)
            {
                reference = References.KeyboardManager.TypedContent;
                SetInputFieldTMProToRightToLeft();
            }

            References.CurrentInputField.SetInputField(reference);
        }

        private void OnPublishKeyEvent(
            string TextToType, KeyType keyType, bool doubleClicked, string TypedContent)
        {
            if (keyType == KeyType.kEnter || keyType == KeyType.kJPEnter)
            {
                return;
            }

            SetInputFieldTMProToRightToLeft();
            References.CurrentInputField.SetInputField(TypedContent);
        }

        private void SetInputFieldTMProToRightToLeft()
        {
            TMP_Text textComp =
                References.CurrentInputField.References.MirrorInputField.textComponent;
            if (textComp.GetType() == typeof(KeyboardInputFieldTextMeshPro))
            {
                textComp.isRightToLeftText = References.KeyboardManager.IsRTL();
            }
            
            if (MatchHorizontalAlignmentWithVKBInputField)
            {
                textComp.horizontalAlignment =
                    References.KeyboardManager.IsRTL() ? HorizontalAlignmentOptions.Right :
                                                         HorizontalAlignmentOptions.Left;
            }
        }
        #endregion Private Methods
    }
}
