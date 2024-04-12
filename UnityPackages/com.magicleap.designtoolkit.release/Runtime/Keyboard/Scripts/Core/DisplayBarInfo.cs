// Copyright (c) 2019-present, Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Developer Agreement, located
// here: https://auth.magicleap.com/terms/developer
using TMPro;
using UnityEngine;

namespace MagicLeap.DesignToolkit.Keyboard
{
    ///<summary>
    /// Holds the component of display bar 
    ///</summary>
    public class DisplayBarInfo : MonoBehaviour
    {
        #region Public Members
        public GameObject ClearKey;

        // EFIGS suggestions
        public GameObject SuggestionsParent;
        public KeyInfo[] SuggestionsKeyInfos;

        // Expaned JP suggestion panel
        public SuggestionPanel JPSuggestsPanelExpanded;
        public GameObject JPSuggestsParentExpanded;
        public GameObject JPPageUpExpanded;
        public GameObject JPPageDownExpanded;
        public GameObject JPCollapseDown;

        // Collapsed JP suggestion panel
        public SuggestionPanel JPSuggestsPanelCollapsed;
        public GameObject JPSuggestsParentCollapsed;
        public GameObject JPPageUpCollapsed;
        public GameObject JPPageDownCollapsed;
        public GameObject JPExpandUp;
        public FlipDisplayBar FlipDisplayBar;
        #endregion Public Members
    }
}