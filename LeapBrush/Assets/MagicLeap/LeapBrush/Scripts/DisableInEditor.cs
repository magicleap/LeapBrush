using System;
using System.Collections.Generic;
using UnityEngine;

namespace MagicLeap.LeapBrush
{
    /// <summary>
    /// Helper to disable a particular component when the app is run from the Unity Editor.
    /// </summary>
    public class DisableInEditor : MonoBehaviour
    {
        [SerializeField]
        private List<MonoBehaviour> _behaviorsToDisableInEditor;

        private void Awake()
        {
#if UNITY_EDITOR
            foreach (var component in _behaviorsToDisableInEditor)
            {
                component.enabled = false;
            }
#endif
        }
    }
}