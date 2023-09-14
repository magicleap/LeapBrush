using System;
using Google.Protobuf.Collections;
using MixedReality.Toolkit;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace MagicLeap.LeapBrush
{
    public class OtherUserToolManager : MonoBehaviour
    {
        [Header("Internal Dependencies")]

        [SerializeField]
        private ScribbleBrushTool _scribbleBrush;

        [SerializeField]
        private PolyBrushTool _polyBrush;

        [SerializeField]
        private EraserTool _eraser;

        [SerializeField]
        private LineRenderer _bendyRay;

        [SerializeField, Tooltip(
             "The base pose for tools that can have a relative offset from the Controller")]
        private Transform _toolOffset;

        private Vector3[] _rayPositions = Array.Empty<Vector3>();
        private MaterialPropertyBlock _rayMaterialPropertyBlock;
        private Gradient _baseRayGradient;

        public void ShowRay(UserStateProto userState, float selectProgress,
            RepeatedField<Vector3Proto> rayPoints, bool isServerEcho)
        {
            if (_rayPositions.Length < rayPoints.Count)
            {
                _rayPositions = new Vector3[rayPoints.Count];
            }

            for (int i = 0; i < rayPoints.Count; i++)
            {
                _rayPositions[i] = ProtoUtils.FromProto(rayPoints[i]);
                if (isServerEcho)
                {
                    _rayPositions[i] += OtherUserVisual.ServerEchoPositionOffset;
                }
            }

            _bendyRay.positionCount = rayPoints.Count;
            _bendyRay.SetPositions(_rayPositions);
            _bendyRay.enabled = true;

            if (_rayPositions.Length > 1)
            {
                if (_baseRayGradient == null)
                {
                    _baseRayGradient = _bendyRay.colorGradient;
                }
                if (_rayMaterialPropertyBlock == null)
                {
                    _rayMaterialPropertyBlock = new();
                }
                _bendyRay.GetPropertyBlock(_rayMaterialPropertyBlock);
                _rayMaterialPropertyBlock.SetFloat("_Shift_", selectProgress);
                _bendyRay.SetPropertyBlock(_rayMaterialPropertyBlock);

                Gradient newGradient = new Gradient();
                GradientColorKey[] colorKeys = _baseRayGradient.colorKeys;
                Color32 toolColor = ColorUtils.FromRgbaUint(userState.ToolColorRgb);
                for (int i = 0; i < colorKeys.Length; i++)
                {
                    colorKeys[i].color = toolColor;
                }
                newGradient.SetKeys(colorKeys, _baseRayGradient.alphaKeys);

                float rayLengthIfStraight = (_rayPositions[^1] - _rayPositions[0]).sqrMagnitude;
                if (rayLengthIfStraight > 0)
                {
                    var compressionAmount = Mathf.Clamp(
                        10 * 0.3f / rayLengthIfStraight, 0.0f, 1.0f);
                    newGradient = ColorUtilities.GradientCompress(
                        newGradient, 0.0f, compressionAmount);
                }

                _bendyRay.colorGradient = newGradient;
            }
        }

        public void HideRay()
        {
            _bendyRay.enabled = false;
        }

        public void ShowTool(UserStateProto userState, Pose toolOffsetPose)
        {
            _toolOffset.SetLocalPose(toolOffsetPose);
            _toolOffset.gameObject.SetActive(true);

            Color32 toolColor = ColorUtils.FromRgbaUint(userState.ToolColorRgb);

            _scribbleBrush.gameObject.SetActive(
                userState.ToolState == UserStateProto.Types.ToolState.BrushScribble);
            if (_scribbleBrush.isActiveAndEnabled)
            {
                _scribbleBrush.Brush.SetColors(toolColor, Color.clear, 0);
            }

            _polyBrush.gameObject.SetActive(
                userState.ToolState == UserStateProto.Types.ToolState.BrushPoly);
            if (_polyBrush.isActiveAndEnabled)
            {
                _polyBrush.Brush.SetColors(toolColor, Color.clear, 0);
            }

            _eraser.gameObject.SetActive(
                userState.ToolState == UserStateProto.Types.ToolState.Eraser);
        }

        public void HideTool()
        {
            _toolOffset.gameObject.SetActive(false);
        }
    }
}