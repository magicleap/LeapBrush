// Copyright (c) 2019-present, Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Developer Agreement, located
// here: https://auth.magicleap.com/terms/developer
using UnityEngine;
using UnityEngine.Events;

namespace MagicLeap.DesignToolkit.Keyboard
{
    /// <summary>
    ///  Helper for Suggestion Panel that check if button has been pre-selected 
    /// </summary>
    public class SuggestionPreselect : MonoBehaviour
    {
        #region Public Members
        public UnityEvent OnPreselected;
        public UnityEvent OnUnPreselected;
        #endregion Public Members

        #region Private Members
        private bool _isPreselected = false;
        #endregion Private Members

        #region Public Methods
        public void Preselect()
        {
            if (!_isPreselected)
            {
                OnPreselected.Invoke();
                _isPreselected = true;
            }
        }

        public void UnPreselect()
        {
            if (_isPreselected)
            {
                OnUnPreselected.Invoke();
                _isPreselected = false;
            }
        }
        #endregion Public Methods

        #region Protected Methods
        protected void OnDisable()
        {
            if (_isPreselected)
            {
                UnPreselect();
            }
        }
        #endregion Protected Methods
    }
}