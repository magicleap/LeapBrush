using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace MagicLeap
{
    /// <summary>
    /// Manage the current color selection for brushes.
    /// </summary>
    /// <remarks>
    /// This class attempts to allocate a unique brush color to each user who joins a scene,
    /// provided they haven't yet made any drawings. Once a user picks an explicit color or
    /// begins drawing, their color selection locks in.
    /// </remarks>
    public class BrushColorManager : MonoBehaviour
    {
        [SerializeField]
        private List<Color32> _defaultBrushColors;

        public event Action OnBrushColorsChanged;

        public Color32 FallbackBrushColor => new Color32(0x88, 0x88, 0x88, 0xff);

        public Color32 StrokeColor => _strokeColor;
        public Color32 FillColor => _fillColor;
        public float FillDimmerAlpha => _fillDimmerAlpha;

        private Color32 _strokeColor;
        private Color32 _fillColor;
        private float _fillDimmerAlpha;
        private bool _manuallySelected;
        private List<Color32> _unusedBrushColors = new();

        private void Awake()
        {
            _strokeColor = _defaultBrushColors[0];
            _fillColor = Color.clear;
            _unusedBrushColors.AddRange(_defaultBrushColors);
        }

        public void SetStrokeColor(Color32 color, bool manual)
        {
            _manuallySelected = manual;

            if ((Color) color != _strokeColor)
            {
                _strokeColor = color;
                OnBrushColorsChanged?.Invoke();
            }
        }

        public void SetFillColor(Color32 color)
        {
            if ((Color) color != _fillColor)
            {
                _fillColor = color;
                OnBrushColorsChanged?.Invoke();
            }
        }

        public void SetFillDimmerAlpha(float fillDimmerAlpha)
        {
            if (_fillDimmerAlpha != fillDimmerAlpha)
            {
                _fillDimmerAlpha = fillDimmerAlpha;
                OnBrushColorsChanged?.Invoke();
            }
        }

        public void SetManuallySelected()
        {
            _manuallySelected = true;
        }

        public void OtherUserBrushColorObserved(Color32 brushColor)
        {
            if (_manuallySelected || _unusedBrushColors.Count == 0)
            {
                return;
            }

            _unusedBrushColors.Remove(brushColor);
            if (_unusedBrushColors.Count > 0)
            {
                SetStrokeColor(_unusedBrushColors[0], false);
            }
            else
            {
                SetStrokeColor(_defaultBrushColors[new Random().Next(_defaultBrushColors.Count - 1)], false);
            }
        }
    }
}