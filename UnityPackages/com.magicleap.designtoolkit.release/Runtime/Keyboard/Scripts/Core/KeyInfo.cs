// Copyright (c) 2019-present, Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Developer Agreement, located
// here: https://auth.magicleap.com/terms/developer
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MixedReality.Toolkit;

namespace MagicLeap.DesignToolkit.Keyboard
{
    public class KeyInfo : MonoBehaviour
    {
        #region Public Members
        public bool IsUGUI = false;
        public StatefulInteractable KeyInteractable;
        public KeyType KeyType;
        public TextMeshPro KeyTextPrimary = null;
        public TextMeshPro KeyTextSecondary = null;
        public string TextToType = "";
        public string SubPanelID = "";
        public Image KeyFillImage;
        public Image KeyHoverImage;
        public Image KeyShadowImage;
        public RawImage MrtkBackplateImage;
        public BoxCollider boxCollider;
        public Renderer KeyFillRenderer = null;
        public KeyboardKeyUIAudioBridge ControllerUIAudioBridge;
        public KeyboardKeyUIAudioBridge GestureUIAudioBridge;
        public TMP_Text KeyTMP;
        public TMP_Text SecondaryKeyTMP;
        public GameObject Container;
        public List<Image> KeyIconImages;
        public Image AccentEllipse;
        #endregion Public Members

#if FIXME
        #region [SerializeField] Private Members
        [SerializeField]
        private AnimatePosition _animatePosition;
        #endregion [SerializeField] Private Members
#endif

        #region Monobehaviour Methods
        private void OnEnable()
        {
#if FIXME
            if (_animatePosition != null)
            {
                _animatePosition.ResetPosition();
            }
#endif
        }
        #endregion Monobehaviour Methods
    }
}