using System;
using TMPro;
using UnityEngine;

namespace MagicLeap.LeapBrush
{
    /// <summary>
    /// UI attached to the user's Controller pose to give them instructions about how to use
    /// each button, the touchpad, etc. Dynamically changes based on context.
    /// </summary>
    public class ControlInstructions : MonoBehaviour
    {
        [Serializable]
        public class InstructionSet
        {
            public string TriggerSubtext;
            public string BumperSubtext;
            public string MenuSubtext;
            public string TouchpadSubtext;
        }

        #region [SerializeField] Private Members
        [SerializeField]
        private TextMeshPro _triggerText;
        [SerializeField]
        private TextMeshPro _triggerSubtext;
        [SerializeField]
        private TextMeshPro _bumperText;
        [SerializeField]
        private TextMeshPro _bumperSubtext;
        [SerializeField]
        private TextMeshPro _touchpadText;
        [SerializeField]
        private TextMeshPro _touchpadSubtext;
        [SerializeField]
        private TextMeshPro _menuText;
        [SerializeField]
        private TextMeshPro _menuSubtext;
        [SerializeField]
        private TextMeshPro _homeText;
        [SerializeField]
        private TextMeshPro _homeSubtext;
        [SerializeField]
        private InstructionSet _mainMenuInstructions = new();
        [SerializeField]
        private InstructionSet _brushInstructions = new();
        [SerializeField]
        private InstructionSet _polyToolInstructions = new();
        [SerializeField]
        private InstructionSet _eraserInstructions = new();
        [SerializeField]
        private InstructionSet _laserPointerInstructions = new();
        #endregion [SerializeField] Private Members

        public enum InstructionType
        {
            MainMenu,
            Brush,
            PolyTool,
            Eraser,
            LaserPointer
        }

        private InstructionType _instructionType = InstructionType.MainMenu;

        private void Awake()
        {
#if !UNITY_ANDROID || UNITY_EDITOR
            gameObject.SetActive(false);
#endif
        }

        private void Start()
        {
            SetInstructionSet(InstructionType.MainMenu);
        }

        public void SetInstructionSet(InstructionType instructionType)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            InstructionSet instructionSet;
            switch (instructionType)
            {
                case InstructionType.MainMenu:
                    instructionSet = _mainMenuInstructions;
                    break;
                case InstructionType.Brush:
                    instructionSet = _brushInstructions;
                    break;
                case InstructionType.PolyTool:
                    instructionSet = _polyToolInstructions;
                    break;
                case InstructionType.Eraser:
                    instructionSet = _eraserInstructions;
                    break;
                case InstructionType.LaserPointer:
                    instructionSet = _laserPointerInstructions;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(instructionType), instructionType, null);
            }

            _triggerSubtext.text = instructionSet.TriggerSubtext;
            _triggerText.gameObject.SetActive(!string.IsNullOrEmpty(instructionSet.TriggerSubtext));
            _bumperSubtext.text = instructionSet.BumperSubtext;
            _bumperText.gameObject.SetActive(!string.IsNullOrEmpty(instructionSet.BumperSubtext));
            _menuSubtext.text = instructionSet.MenuSubtext;
            _menuText.gameObject.SetActive(!string.IsNullOrEmpty(instructionSet.MenuSubtext));
            _touchpadSubtext.text = instructionSet.TouchpadSubtext;
            _touchpadText.gameObject.SetActive(
                !string.IsNullOrEmpty(instructionSet.TouchpadSubtext));
        }
    }
}