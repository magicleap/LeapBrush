using System;
using MagicLeap.DesignToolkit.Input.Controller;
using UnityEngine;

namespace MagicLeap.LeapBrush.Scripts
{
    namespace MagicLeap.LeapBrush
    {
        /// <summary>
        /// Provides additional controller simulation when running the app from the unity
        /// editor or a desktop computer.
        /// </summary>
        [RequireComponent(typeof(ControllerInput))]
        public class AdditionalControllerInputSimulation : MonoBehaviour
        {
            public KeyCode Menu = KeyCode.Tab;

            private ControllerInput _controllerInput;

            public void Awake()
            {
                _controllerInput = GetComponent<ControllerInput>();
            }

            public void Update()
            {
#if UNITY_EDITOR || UNITY_STANDALONE
                SimulateInput();
#endif
            }

            private void SimulateInput()
            {
                if (Input.GetKeyDown(Menu))
                {
                    _controllerInput.Events.OnMenuDown?.Invoke();
                }
                if (Input.GetKeyUp(Menu))
                {
                    _controllerInput.Events.OnMenuUp?.Invoke();
                }
            }
        }
    }
}