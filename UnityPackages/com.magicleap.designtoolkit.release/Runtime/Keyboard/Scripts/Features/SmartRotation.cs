// Copyright (c) 2019-present, Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Developer Agreement, located
// here: https://auth.magicleap.com/terms/developer
using System;
using UnityEngine;

namespace MagicLeap.DesignToolkit.Utilities
{
    /// <summary>
    /// Remaps rotation of an object on the x axis an object based on its distance from another object
    /// </summary>
    public class SmartRotation : MonoBehaviour
    {
        [System.Serializable]
        public class RotationObject
        {
            public GameObject rotationObject;
            public float startRotation;
#if FIXME
            public FloatRangeMapping _rangeMapping;
#endif
        }

        #region [SerializeField] Private Members
        public RotationObject[] rotationObjects;
        [SerializeField]
        private Transform _target;
        [SerializeField]
        private Transform rotationObjectParent;
        #endregion [SerializeField] Private Members

        #region Public Members
        public bool rotate;
        public float multiplier;
        #endregion Public Members

        #region Private Members
        private float mappedValue;
        #endregion Private Members

        #region Monobehavior
        private void Awake()
        {
            if (_target == null)
            {
                _target = Camera.main.transform;
            }
        }

        void Update()
        {
#if FIXME
            float tHeight = Height(rotationObjectParent, _target);
            if (rotate && tHeight > 0.15f)
            {
                for (int i = 0; i < rotationObjects.Length; i++)
                {
                    mappedValue = rotationObjects[i]._rangeMapping.GetOutputValue(tHeight, true);
                    if (tHeight > rotationObjects[i].startRotation)
                    {
                        rotationObjects[i].rotationObject.transform.eulerAngles =
                            new Vector3(
                                Math.Abs(
                                    multiplier * mappedValue),
                                0, // handle y axis with another solver 
                                rotationObjects[i].rotationObject.transform.eulerAngles.z);
                    }
                }
            }
#endif
        }
        #endregion Monobehavior

        #region Public Methods
        public void EnableSmartRotation()
        {
            rotate = true;
        }

        public void DisableSmartRotation()
        {
            rotate = false;
        }

        public float Height(Transform a, Transform b)
        {
            float dist = b.position.y - a.position.y;
            return dist;
            ;
        }
        #endregion Public Methods
    }
}