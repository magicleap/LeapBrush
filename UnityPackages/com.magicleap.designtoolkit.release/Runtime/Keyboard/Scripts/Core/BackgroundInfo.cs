// Copyright (c) 2019-present, Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Developer Agreement, located
// here: https://auth.magicleap.com/terms/developer
using UnityEngine;

namespace MagicLeap.DesignToolkit.Keyboard
{
    ///<summary>
    /// Holds the component of keyboard background 
    ///</summary>
    public class BackgroundInfo : MonoBehaviour
    {
        #region Public Members
        public GameObject LeftHandleParent;
        public GameObject LeftHandle;
        public GameObject LeftHandleHighlight;
        public GameObject RightHandleParent;
        public GameObject RightHandle;
        public GameObject RightHandleHighlight;
        public BoxCollider RightBoxCollider;
        public BoxCollider LeftBoxCollider;
        public BoxCollider PanelBoxCollider;
        [Tooltip("The large silhouette around the entire keyboard when the keyboard is grabbed")]
        public GameObject SilLarge;
        public GameObject Panel;
        #endregion Public Members
    }
}