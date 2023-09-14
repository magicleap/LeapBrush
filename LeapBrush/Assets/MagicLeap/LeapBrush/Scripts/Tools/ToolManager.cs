using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

namespace MagicLeap.LeapBrush
{
    /// <summary>
    /// Manage tools including the current color selection for brushes.
    /// </summary>
    /// <remarks>
    /// This class attempts to allocate a unique brush color to each user who joins a scene,
    /// provided they haven't yet made any drawings. Once a user picks an explicit color or
    /// begins drawing, their color selection locks in.
    /// </remarks>
    public class ToolManager : MonoBehaviour
    {
        public event Action OnBrushColorsChanged;
        public event Action OnActiveToolChanged;
        public event Action OnToolOffsetChanged;

        [SerializeField]
        private List<Color32> _defaultBrushColors;

        [SerializeField]
        private InteractorToolManager _controllerToolManager;

        [SerializeField]
        private InteractorToolManager _leftHandToolManager;

        [SerializeField]
        private InteractorToolManager _rightHandToolManager;

        public Color32 FallbackBrushColor => new(0x88, 0x88, 0x88, 0xff);

        public Color32 StrokeColor => _strokeColor;
        public Color32 FillColor => _fillColor;
        public float FillDimmerAlpha => _fillDimmerAlpha;
        public float ToolOffset => _toolOffset;
        public ToolType ActiveTool => _activeTool;
        public bool LaserToolOverride => _laserToolOverride;
        public InteractorToolManager[] InteractorToolManagers => _interactorToolManagers;
        public GameObject PolyBrushPrefab => _controllerToolManager.PolyBrush.Prefab;
        public GameObject ScribbleBrushPrefab => _controllerToolManager.ScribbleBrush.Prefab;
        public InteractorToolManager ControllerManager => _controllerToolManager;
        public InteractorToolManager LeftHandManager => _leftHandToolManager;
        public InteractorToolManager RightHandManager => _rightHandToolManager;

        public const float MinToolOffset = 0;
        public const float MaxToolOffset = 1.5f;

        private InteractorToolManager[] _interactorToolManagers;
        private Color32 _strokeColor;
        private Color32 _fillColor;
        private float _fillDimmerAlpha;
        private bool _brushColorManuallySelected;
        private List<Color32> _unusedBrushColors = new();
        private ToolType _activeTool = ToolType.BrushScribble;
        private bool _laserToolOverride;
        private float _toolOffset;

        private void Awake()
        {
            _strokeColor = _defaultBrushColors[0];
            _fillColor = Color.clear;
            _unusedBrushColors.AddRange(_defaultBrushColors);

            _interactorToolManagers = new[]
            {
                _controllerToolManager,
                _leftHandToolManager,
                _rightHandToolManager
            };
        }

        private void Start()
        {
            HandleBrushColorsChanged(false);
            HandleToolOffsetChanged(false);
        }

        public void SetStrokeColor(Color32 color, bool manual)
        {
            _brushColorManuallySelected = manual;

            if ((Color) color != _strokeColor)
            {
                _strokeColor = color;
                HandleBrushColorsChanged();
            }
        }

        public void SetFillColor(Color32 color)
        {
            if ((Color) color != _fillColor)
            {
                _fillColor = color;
                HandleBrushColorsChanged();
            }
        }

        public void SetFillDimmerAlpha(float fillDimmerAlpha)
        {
            if (!Mathf.Approximately(_fillDimmerAlpha, fillDimmerAlpha))
            {
                _fillDimmerAlpha = fillDimmerAlpha;
                HandleBrushColorsChanged();
            }
        }

        public void SetBrushColorManuallySelected()
        {
            _brushColorManuallySelected = true;
        }

        public void OtherUserBrushColorObserved(Color32 brushColor)
        {
            if (_brushColorManuallySelected || _unusedBrushColors.Count == 0)
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
                SetStrokeColor(_defaultBrushColors[
                    new Random().Next(_defaultBrushColors.Count - 1)], false);
            }
        }

        public void AdjustToolOffset(float delta)
        {
            SetToolOffset(_toolOffset + delta);
        }

        public void SetToolOffset(float newToolOffset)
        {
            newToolOffset = Mathf.Clamp(
                newToolOffset, MinToolOffset, MaxToolOffset);
            if (!Mathf.Approximately(newToolOffset, _toolOffset))
            {
                _toolOffset = newToolOffset;
                HandleToolOffsetChanged();
            }
        }

        public void SetActiveTool(ToolType activeTool)
        {
            if (_activeTool == activeTool)
            {
                return;
            }

            _activeTool = activeTool;
            OnActiveToolChanged?.Invoke();

            UpdateActiveTool();
        }

        public void SetLaserToolOverride(bool laserToolOverride)
        {
            if (_laserToolOverride == laserToolOverride)
            {
                return;
            }

            _laserToolOverride = laserToolOverride;
            UpdateActiveTool();
        }

        private void UpdateActiveTool()
        {
            foreach (InteractorToolManager toolManager in _interactorToolManagers)
            {
                toolManager.SetActiveTool(_laserToolOverride ? ToolType.Laser : _activeTool);
            }
        }

        private void HandleBrushColorsChanged(bool fireEvents = true)
        {
            foreach (InteractorToolManager toolContainer in _interactorToolManagers) {
                toolContainer.ScribbleBrush.Brush.SetColors(
                    _strokeColor, _fillColor, _fillDimmerAlpha);
                toolContainer.PolyBrush.Brush.SetColors(
                    _strokeColor, _fillColor, _fillDimmerAlpha);
            }

            if (fireEvents)
            {
                OnBrushColorsChanged?.Invoke();
            }
        }

        private void HandleToolOffsetChanged(bool fireEvents = true)
        {
            foreach (InteractorToolManager toolContainer in _interactorToolManagers)
            {
                toolContainer.SetToolOffsetZ(_toolOffset);
            }

            if (fireEvents)
            {
                OnToolOffsetChanged?.Invoke();
            }
        }
    }
}