using System;
using System.Collections.Generic;
using UnityEngine;

namespace MagicLeap.LeapBrush
{
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