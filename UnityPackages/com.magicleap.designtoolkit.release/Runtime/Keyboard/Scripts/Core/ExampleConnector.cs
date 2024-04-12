// Copyright (c) 2019-present, Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Developer Agreement, located
// here: https://auth.magicleap.com/terms/developer
using UnityEngine;
using TMPro;

namespace MagicLeap.DesignToolkit.Keyboard
{
    ///<summary>
    /// Handy utility to debug visually if plugged to Keyboard Manager  
    ///</summary>
    public class ExampleConnector : MonoBehaviour
    {
        #region [SerializeField] Private Members
        [SerializeField]
        private TextMeshPro _info;

        public void KeyEventHandler(string justTyped, MagicLeap.DesignToolkit.Keyboard.KeyType keyType,
            bool doubleClick,
            string allTyped)
        {
            string toPrint = "Just typed: " + justTyped + "\n" +
                             "KeyType: " + keyType + "\n" +
                             "Double click?: " + doubleClick + "\n\n" +
                             "All typed content: " + allTyped;
            _info.text = toPrint;
            Debug.Log(toPrint);
        }
        #endregion [SerializeField] Private Members
    }
}