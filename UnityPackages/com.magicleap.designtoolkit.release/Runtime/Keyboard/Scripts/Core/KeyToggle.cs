// Copyright (c) 2019-present, Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Developer Agreement, located
// here: https://auth.magicleap.com/terms/developer
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MagicLeap.DesignToolkit.Keyboard
{
    /// <summary>
    /// Toggles key material properties
    /// </summary>
    public class KeyToggle : MonoBehaviour
    {
        #region [SerializeField] Private Members
        [SerializeField]
        private Renderer[] _iconRenderers;
        [SerializeField]
        private Sprite[] _iconSpritesWhenOn;
        [SerializeField]
        private Sprite[] _iconSpritesWhenOff;
        [SerializeField]
        private Color[] _iconColorsWhenOn;
        [SerializeField]
        private Color[] _iconColorsWhenOff;
        [SerializeField]
        private Renderer _fillRenderer;
        [SerializeField]
        private KeyStyle _keyStyle;
        [SerializeField]
        private TextMeshPro _textMeshPro;
        [SerializeField]
        private Color _textColorOff = Color.white;
        [SerializeField]
        private Color _textColorOn = Color.black;
        [SerializeField]
        private bool _isUGUI = false;
        [SerializeField]
        private Image _fillImage;
        [SerializeField]
        private RawImage _mrtkBackplateImage;
        [SerializeField]
        private TMP_Text _keyTMP;
        [SerializeField]
        private Image _accentEllipse;
        [SerializeField]
        private Image[] _iconImages;
        [SerializeField]
        private TextMesh _textMesh;
        #endregion [SerializeField] Private Members

        #region Private Members
#if FIXME
        private Interactable _interactable;
        private DirectInteractorSwitch[] _directInteractorSwitch;
        private DirectInteractorSwitch _directInteractorState;
#endif
        #endregion Private Members

        #region Monobehaviour Methods
        private void OnEnable()
        {
            StartCoroutine(Subscribe());
        }

        IEnumerator Subscribe()
        {
#if FIXME
            yield return new WaitForSeconds(0.01f);
            if (GetComponent<KeyInfo>().KeyType == KeyType.kRayDrumstickSwitch)
            {
                FindSwitch();
                if (_directInteractorSwitch != null)
                {
                    var interactable = GetInteractable();
                    if (interactable != null)
                    {
                        interactable.Events.OnSelectExited.AddListener(InputToggle);
                    }
                }
            }
#else
            yield return null;
#endif
        }

#if FIXME
        private void FindSwitch()
        {
            _directInteractorSwitch = new DirectInteractorSwitch[1];
            _directInteractorSwitch = (DirectInteractorSwitch[])
                GameObject.FindObjectsOfType<DirectInteractorSwitch>(true);
            _directInteractorState = _directInteractorSwitch[0];
        }
#endif

        private void Update()
        {
#if FIXME
            if (_directInteractorState == null)
            {
                FindSwitch();
            }
#endif
        }
        #endregion Monobehaviour Methods

        #region Private Methods
#if FIXME
        private void InputToggle(Interactor interactor)
        {
            _directInteractorState.SwitchInput();
        }

        private Interactable GetInteractable()
        {
            if (_interactable == null)
            {
                _interactable = GetComponent<Interactable>();
            }

            return _interactable;
        }
#endif
        #endregion Private Methods

        #region Public Methods
        public void Init(KeyInfo keyInfo, KeyBuilderSettings settings)
        {
            _isUGUI = true;
            _keyStyle = settings.KeyButtonStyle;
            _fillImage = keyInfo.KeyFillImage;
            _mrtkBackplateImage = keyInfo.MrtkBackplateImage;
            _keyTMP = keyInfo.KeyTMP;
            _accentEllipse = keyInfo.AccentEllipse;
            _textColorOff = settings.TextColorOff;
            _textColorOn = settings.TextColorOn;
            if (keyInfo.KeyIconImages.Count == 0)
            {
                return;
            }
            _iconImages = new Image[keyInfo.KeyIconImages.Count];
            _iconSpritesWhenOff = new Sprite[settings.SettingsForIcons.Count];
            _iconSpritesWhenOn = new Sprite[settings.SettingsForIcons.Count];
            _iconColorsWhenOff = new Color[settings.SettingsForIcons.Count];
            _iconColorsWhenOn = new Color[settings.SettingsForIcons.Count];
            for (int idx = 0; idx < keyInfo.KeyIconImages.Count; idx++)
            {
                _iconImages[idx] = keyInfo.KeyIconImages[idx];
                _iconSpritesWhenOff[idx] = settings.SettingsForIcons[idx].IconSpriteOff;
                _iconSpritesWhenOn[idx] = settings.SettingsForIcons[idx].IconSpriteOn;
                _iconColorsWhenOff[idx] = settings.SettingsForIcons[idx].IconColorOff;
                _iconColorsWhenOn[idx] = settings.SettingsForIcons[idx].IconColorOn;
            }
        }

        public void Toggle(bool on)
        {
            int numIconRends = 0;
            if (_isUGUI && _iconImages != null)
            {
                numIconRends = _iconImages.Length;
            }
            else if (!_isUGUI && _iconRenderers != null)
            {
                numIconRends = _iconRenderers.Length;
            }

            if (on)
            {
                for (int i = 0; i < numIconRends; i++)
                {
                    if (_isUGUI)
                    {
                        _iconImages[i].sprite = _iconSpritesWhenOn[i];
                        _iconImages[i].color = _iconColorsWhenOn[i];
                        continue;
                    }
                }
                if (_isUGUI)
                {
                    _fillImage.sprite = _keyStyle.FillSpriteOn;
                    _mrtkBackplateImage.color = _keyStyle.MrtkBackplateColorOn;
                    _keyTMP.color = _textColorOn;
                    _accentEllipse.color = _textColorOn;
                }
            }
            else
            {
                for (int i = 0; i < numIconRends; i++)
                {
                    if (_isUGUI)
                    {
                        _iconImages[i].sprite = _iconSpritesWhenOff[i];
                        _iconImages[i].color = _iconColorsWhenOff[i];
                        continue;
                    }
                }
                if (_isUGUI)
                {
                    _fillImage.sprite = _keyStyle.FillSpriteOff;
                    _mrtkBackplateImage.color = _keyStyle.MrtkBackplateColorOff;
                    _keyTMP.color = _textColorOff;
                    _accentEllipse.color = _textColorOff;
                }
            }
        }

        // A hack to disable direct interaction (drumstick) for R4
        public void Disable(Color disableColor)
        {
            var keyInfo = GetComponent<KeyInfo>();
            if (keyInfo.IsUGUI)
            {
                foreach (Image image in _iconImages)
                {
                    image.color = disableColor;
                }
            }
            else
            {
                foreach (Renderer iconRend in _iconRenderers)
                {
                    iconRend.material.SetColor("_Color", disableColor);
                }
            }

#if FIXME
            Interactable interactable = GetInteractable();
            if (interactable != null)
            {
                interactable.SetInteractionsEnabled(false);
            }
#endif
        }
        #endregion Public Methods
    }
}
