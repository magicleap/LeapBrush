// Copyright (c) 2019-present, Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Developer Agreement, located
// here: https://auth.magicleap.com/terms/developer
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MagicLeap.DesignToolkit.Keyboard
{
    /// <summary>
    /// Utility used to mirror one reference input field on a distant panel
    /// </summary>
    [RequireComponent(typeof(TMP_InputField))]
    public class MirrorInputField : MonoBehaviour
    {
        #region NestedType / Constructors
        [System.Serializable]
        public class InputFieldReferences
        {
            public MirrorInputFieldGroup MirrorInputFieldGroup;
            public TMP_InputField MirrorInputField;
        }
        #endregion NestedType / Constructors

        #region Private Members
        private bool _listening;
        private Color _inputFieldHighlight;
        private Color _inputFieldDefault;
        private Color _inputFieldSelected;
        private Image _inputFieldBackground;
        private KeyboardManager _keyboardManager;
        #endregion Private Members

        #region Public Members
        public string TypedContent;
        public InputFieldReferences References;
        public bool Debug;
        #endregion Public Members

        #region [SerializeField] Members
#if FIXME
        [SerializeField]
        private Interactable _interactable;
#endif
        #endregion [SerializeField] Members

        #region Monobehaviour
        private void Awake()
        {
#if FIXME
            if (_interactable == null)
            {
                _interactable = GetComponentInChildren<Interactable>();
            }
#endif
            References.MirrorInputField = GetComponent<TMP_InputField>();
            _inputFieldBackground = GetComponent<Image>();
            _inputFieldDefault = References.MirrorInputField.colors.normalColor;
            _inputFieldHighlight = References.MirrorInputField.colors.highlightedColor;
            _inputFieldSelected = References.MirrorInputField.colors.selectedColor;
            _keyboardManager = References.MirrorInputFieldGroup.References.KeyboardManager;
        }

        private void OnEnable()
        {
#if FIXME
            _interactable.Events.OnSelectExited.AddListener(StartMirroring);
            _interactable.Events.OnTargetEnter.AddListener(HandleTargetEnterColor);
            _interactable.Events.OnTargetExit.AddListener(HandleTargetExitColor);
#endif
        }

        private void OnDisable()
        {
#if FIXME
            _interactable.Events.OnSelectExited.RemoveListener(StartMirroring);
            _interactable.Events.OnTargetEnter.RemoveListener(HandleTargetEnterColor);
            _interactable.Events.OnTargetExit.RemoveListener(HandleTargetExitColor);
#endif
        }
        #endregion Monobehaviour

        #region Private Methods
#if FIXME
        private void StartMirroring(Interactor interactor)
        {
            // if the keyboard is not active, instantiate keyboard
            if (_keyboardManager != null && !_keyboardManager.gameObject.activeSelf)
            {
                _keyboardManager.SetInputFieldContentType(References.MirrorInputField.contentType);
                _keyboardManager.OpenKeyboard(
                    TypedContent, References.MirrorInputField.textComponent.isRightToLeftText);
            }
            References.MirrorInputField.ActivateInputField();
            References.MirrorInputFieldGroup.References.CurrentInputField = this;
            foreach (MirrorInputField inputField
                     in References.MirrorInputFieldGroup.References.MirrorInputFields)
            {
                inputField.StopListening();
                inputField.HandleTargetExitColor(null);
            }
            if (_interactable.InteractorsTargetingThisObject.Count > 0)
            {
                interactor = _interactable.InteractorsTargetingThisObject[0];
            }
            StartListening();
            if (_listening)
            {
                // deselect if selected
                References.MirrorInputField.ReleaseSelection();
                References.MirrorInputFieldGroup.References.InputField.ReleaseSelection();
                // update text
                UpdateFields();
                HandleSelectionColor();
                References.MirrorInputField.ActivateInputField();
                References.MirrorInputField.Select();
            }
            if (Debug)
            {
                UnityEngine.Debug.Log("Selected " + gameObject.name + " Input Field. Start Mirroring " + TypedContent);
            }
        }

        private void HandleTargetEnterColor(Interactor interactor)
        {
            _inputFieldBackground.color = _inputFieldHighlight;
            if (Debug)
            {
                UnityEngine.Debug.Log("Target Enter" + gameObject.name + " Input Field. Start Mirroring ");
            }
        }

        private void HandleTargetExitColor(Interactor interactor)
        {
            _inputFieldBackground.color = _inputFieldDefault;
            if (Debug)
            {
                UnityEngine.Debug.Log("Target Exit" + gameObject.name + " Input Field. Start Mirroring ");
            }
        }
#endif

        private void HandleSelectionColor()
        {
            _inputFieldBackground.color = _inputFieldSelected;
        }

        private void StartListening()
        {
            _listening = true;
        }

        private void StopListening()
        {
            _listening = false;
        }

        private void UpdateFields()
        {
            if (References.MirrorInputFieldGroup.IsListeningToKeyboardManager())
            {
                return;
            }

            if (References.MirrorInputFieldGroup.References.KeyboardManager != null)
            {
                _keyboardManager.SetInputFieldContentType(References.MirrorInputField.contentType);
                _keyboardManager.ResetKeyboardField(TypedContent);
                return;
            }

            References.MirrorInputFieldGroup.References.InputField.contentType =
                References.MirrorInputField.contentType;
            References.MirrorInputFieldGroup.References.InputField.text = TypedContent;
            References.MirrorInputField.text = TypedContent;
            References.MirrorInputField.caretPosition = TypedContent.Length;
            References.MirrorInputFieldGroup.References.InputField.caretPosition = TypedContent.Length;
        }
        #endregion Private Methods

        #region Public Methods
        public void SetInputField(string newText, bool notifyTextChange = true)
        {
            TypedContent = newText;

            if (!notifyTextChange)
            {
                References.MirrorInputField.SetTextWithoutNotify(newText);
            }
            else
            {
                References.MirrorInputField.text = newText;
            }
            References.MirrorInputField.caretPosition = TypedContent.Length;
        }
        #endregion Public Methods
    }
}
