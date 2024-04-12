// Copyright (c) 2019-present, Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Developer Agreement, located
// here: https://auth.magicleap.com/terms/developer
using System.Collections.Generic;
using UnityEngine;

namespace MagicLeap.DesignToolkit.Keyboard
{
    /// <summary>
    /// Holds layout keyboard objects
    /// </summary>
    public class LayoutInfo : MonoBehaviour
    {
        #region Public Members
        public bool IsUGUI = false;
        public bool IsRightToLeft = false;
        public Code Locale;
        public PageCode ThisPageCode;
        public Vector3 Extents;
        public Vector3 Scale;
        public GameObject KeysParent;
        public GameObject Background;
        public List<ShiftKeyBehavior> ShiftKeysBehavs = new List<ShiftKeyBehavior>();
        public List<KeySubPanel> SubPanels = new List<KeySubPanel>();
        public KeyToggle ChangeLocaleKeyToggle;
        public KeyToggle RayDrumstickSwitchKeyToggle;
        public KeyToggle JPAccentsKeyToggle;
        public Vector3 ScaledExtents
        {
            get
            {
                return new Vector3(Extents.x * Scale.x,
                                   Extents.y * Scale.y,
                                   Extents.z * Scale.z);
            }
        }
        #endregion Public Members
    }
}