// Copyright (c) 2019-present, Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Developer Agreement, located
// here: https://auth.magicleap.com/terms/developer
using MixedReality.Toolkit.SpatialManipulation;
using UnityEngine;

namespace MagicLeap.DesignToolkit.Keyboard
{
    /// <summary>
    /// Billboards RotateTarget when grabbed, and unlocks the VKB when grabbed.
    /// </summary>
    public class KeyboardHandle : MonoBehaviour
    {
        #region [SerializeField] Private Members
        [SerializeField]
        private ObjectManipulator _objectManipulator;
        #endregion [SerializeField] Private Members

        #region Private Members
        private Placement _placement;
        #endregion Private Members

        #region Monobehaviour Methods
        private void Awake()
        {
            Transform vkbRootTransform = GetComponentInParent<KeyboardManager>().transform;
            _placement = vkbRootTransform.GetComponent<Placement>();
            _objectManipulator.HostTransform = vkbRootTransform;
        }

        private void OnEnable()
        {
            _objectManipulator.IsGrabSelected.OnEntered.AddListener(OnGrabSelectEntered);
        }

        private void OnDisable()
        {
            _objectManipulator.IsGrabSelected.OnEntered.RemoveListener(OnGrabSelectEntered);
        }
        #endregion Monobehaviour Methods

        #region Public Methods
        public void OnGrabSelectEntered(float arg0)
        {
            _placement.LockToBase(false);
        }
        #endregion Public Methods
    }
}
