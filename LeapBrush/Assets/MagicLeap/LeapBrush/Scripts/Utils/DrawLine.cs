// Copyright (c) 2019-present, Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Developer Agreement, located
// here: https://auth.magicleap.com/terms/developer 
using UnityEngine;

namespace MagicLeap.LeapBrush
{
    ///<summary>
    /// Draws a line renderer between 2 transforms.
    ///</summary>
    [ExecuteInEditMode]
    [RequireComponent(typeof(LineRenderer))]
    public class DrawLine : MonoBehaviour
    {
        #region Public Members
        public Transform Start => _start;
        public Transform End => _end;
        #endregion Public Members

        #region [SerializeField] Private Members
        [SerializeField]
        private Transform _start;
        [SerializeField]
        private Transform _end;
        [SerializeField]
        int _positionCount = 2;
        [SerializeField]
        float _lineMaxWidth = 0.01f;
        [SerializeField]
        [Range(0.0f, 1.0f)]
        private float _lineWidthPercent = 1.0f;
        #endregion [SerializeField] Private Members

        #region Private Members
        LineRenderer _line;
        float _delta;
        Vector3 _startToEnd;
        #endregion Private Members

        #region MonoBehaviour Methods
        private void Awake()
        {
            _line = GetComponent<LineRenderer>();
            SetWidthPercent(_lineWidthPercent);
        }

        private void Update()
        {
            if (_start == null || _end == null)
            {
                return;
            }
            Draw();
        }
        #endregion MonoBehaviour Methods

        #region Public Methods
        public void SetWidthPercent(float percent)
        {
            _lineWidthPercent = percent;
            _line.widthMultiplier = _lineWidthPercent * _lineMaxWidth;
        }

        public void SetWidthPercentInverse(float percent)
        {
            SetWidthPercent(1.0f - percent);
        }
        #endregion Public Methods

        #region Private Methods
        void Draw()
        {
            if (_line == null)
            {
                _line = GetComponent<LineRenderer>();
            }
            if (_positionCount == 2)
            {
                _line.SetPosition(0, _start.position);
                _line.SetPosition(1, _end.position);
            }
            else
            {
                _startToEnd = _end.position - _start.position;
                _delta = 1.0f / (_line.positionCount - 1);
                for (int i = 0; i < _line.positionCount; i++)
                {
                    _line.SetPosition(i, _start.position + _startToEnd * (_delta * i));
                }
            }
        }
        #endregion Private Methods
    }
}