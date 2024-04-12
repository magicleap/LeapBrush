// Copyright (c) 2022 Magic Leap, Inc. All Rights Reserved.
// Please see the top-level LICENSE.md in this distribution
// for terms and conditions governing this file.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MagicLeap.DesignToolkit.Keyboard
{
    public enum ShiftKeyState
    {
        Off,
        OnTemp,
        OnPerm
    }

    public class ShiftKeyBehavior : MonoBehaviour
    {
        #region [SerializeField] Private Members
        [SerializeField]
        private bool _isUGUI = false;
        [SerializeField]
        private Renderer _iconRend;
        [SerializeField]
        private Renderer _fillRend;
        [SerializeField]
        private Image _UGUIFillImage;
        [SerializeField]
        private Image _UGUIIconImage;
        [SerializeField]
        private Sprite _iconSpriteOff;
        [SerializeField]
        private Sprite _iconSpriteOn;
        [SerializeField]
        private Color _iconColorOff;
        [SerializeField]
        private Color _iconColorOn;
        [SerializeField]
        private Sprite _fillSpriteOff;
        [SerializeField]
        public Sprite _fillSpriteOn;
        #endregion [SerializeField] Private Members

        #region Public Methods
        public void Init(KeyInfo keyInfo, KeyBuilderSettings settings)
        {
            _UGUIFillImage = keyInfo.KeyFillImage;
            _UGUIIconImage = keyInfo.KeyIconImages[0];
            _iconSpriteOn = settings.SettingsForIcons[0].IconSpriteOn;
            _iconSpriteOff = settings.SettingsForIcons[0].IconSpriteOff;
            _iconColorOn = settings.SettingsForIcons[0].IconColorOn;
            _iconColorOff = settings.SettingsForIcons[0].IconColorOff;
            _fillSpriteOn = settings.KeyButtonStyle.FillSpriteOn;
            _fillSpriteOff = settings.KeyButtonStyle.FillSpriteOff;
            _isUGUI = true;
        }

        public void SwitchStatus(ShiftKeyState keyState)
        {
            if (_isUGUI)
            {
                switch (keyState)
                {
                    case ShiftKeyState.Off:
                        _UGUIIconImage.sprite = _iconSpriteOff;
                        _UGUIFillImage.sprite = _fillSpriteOff;
                        _UGUIIconImage.color =  _iconColorOff;
                        break;
                    case ShiftKeyState.OnTemp:
                        _UGUIIconImage.sprite = _iconSpriteOn;
                        _UGUIFillImage.sprite = _fillSpriteOn;
                        _UGUIIconImage.color = _iconColorOn;
                        break;
                    case ShiftKeyState.OnPerm:
                        _UGUIIconImage.sprite = _iconSpriteOn;
                        _UGUIFillImage.sprite = _fillSpriteOn;
                        _UGUIIconImage.color = _iconColorOn;
                        break;
                    default:
                        break;
                }
                return;
            }
        }
        #endregion Public Methods

        #region Monobehaviour Methods
        void Awake()
        {
            if (_isUGUI)
            {
                if (_UGUIFillImage == null || _UGUIIconImage == null)
                {
                    Debug.LogError("_UGUIFillImage and _UGUIIconImage should not be null, " +
                                   "they need to be hard-referenced in the Editor");
                }
                return;
            }
            if (_fillRend == null || _iconRend == null)
            {
                Debug.LogError("_fillRend and _iconRend should not be null, " +
                               "they need to be hard-referenced in the Editor");
            }
        }
        #endregion Monobehaviour Methods
    }
}