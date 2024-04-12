// Copyright (c) 2019-present, Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Developer Agreement, located
// here: https://auth.magicleap.com/terms/developer
using System.Collections;
using TMPro;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace MagicLeap.DesignToolkit.Keyboard
{
    /// <summary>
    /// Text selection adaptation for MLDK input field
    /// </summary>
    public class TextSelection : MonoBehaviour
    {
        #region Private Members [SerializeField]
        [SerializeField]
        private bool _enableTextSelection;
        [SerializeField]
        [Tooltip("If not null, is going to update selection positions on Keyboard Manager for " +
                 "allowing keyboard text selection")]
        private KeyboardManager _keyboardManager;
        [Tooltip("Input field child text object")]
        [SerializeField]
        private TextMeshProUGUI _tmpInputFieldText;
        [Tooltip("Input field parent object")]
        [SerializeField]
        private TMP_InputField _tmpInputFieldParent;
        [SerializeField]
        private bool _debug;
#if FIXME
        [SerializeField]
        private Interactable _interactable;
#endif
        [SerializeField]
        private Collider _selectionBounds;
        [SerializeField]
        private bool _selectionTest;
        [SerializeField]
        private int _selectionTestStart;
        [SerializeField]
        private int _selectionTestEnd;
        #endregion Private Members [SerializeField]

        #region Private Members
#if FIXME
        private Interactor[] _directInteractors;
        private RayInteractor[] _rayInteractors;
#endif
        private int _startIndex;
        private bool _selectionIsActive;
        public Transform _selector;
        #endregion Private Members

        #region Monobehaviour Methods
        private void OnEnable()
        {
            if (!_enableTextSelection)
            {
                return;
            }
            Initialize();
        }

        private void Update()
        {
            if (!_enableTextSelection)
            {
                return;
            }
            if (_selectionTest)
            {
                _tmpInputFieldParent.ActivateInputField();
                _tmpInputFieldParent.selectionAnchorPosition = _selectionTestStart;
                _tmpInputFieldParent.selectionFocusPosition = ClosestChar(_selector);
                _tmpInputFieldParent.ForceLabelUpdate();
                if (_debug)
                {
                    Debug.Log("Selector at " + ClosestChar(_selector));
                }
            }
            _selector = FindSelector();
            SelectionActive(_selector);
        }
        #endregion Monobehaviour Methods

        #region Public Methods
        public void SelectStart()
        {
            if (!_enableTextSelection)
            {
                return;
            }
            if (!_selectionIsActive && _selectionBounds.bounds.Contains(_selector.position))
            {
                _startIndex = ClosestChar(_selector);
                _tmpInputFieldParent.ActivateInputField();
                _tmpInputFieldParent.selectionAnchorPosition = _startIndex;
                _tmpInputFieldParent.selectionFocusPosition = _startIndex;
                StartCoroutine(SelectionIsActive());
            }
        }

        public void SelectionActive(Transform selector)
        {
            if (!_enableTextSelection)
            {
                return;
            }
            if (_selectionIsActive)
            {
                _tmpInputFieldParent.ActivateInputField();
                _tmpInputFieldParent.selectionAnchorPosition = _startIndex;
                _tmpInputFieldParent.selectionFocusPosition = ClosestChar(selector);
                _tmpInputFieldParent.ForceLabelUpdate();
            }
            if (_debug)
            {
                Debug.Log("Selection starts at " + _tmpInputFieldParent.selectionAnchorPosition +
                          " Selection ends at " + _tmpInputFieldParent.selectionFocusPosition +
                          " There are " + _tmpInputFieldText.text.Length + " char on the input field");
            }
        }

        public void SelectionEnds()
        {
            if (!_enableTextSelection)
            {
                return;
            }
            _selectionIsActive = false;
        }
        #endregion Public Methods

        #region Private Methods
        private int ClosestChar(Transform selector)
        {
            int closestIndexToInteractor = 0;
            float distance = 0;
            float minValue = Mathf.Infinity;
            for (int i = 0; i < _tmpInputFieldText.text.Length; i++)
            {
                Vector3 charPosition = GetCharPositionSpaceCanvasToWorld(i, _tmpInputFieldText);
                distance = Vector3.Distance(selector.position, charPosition);
                if (distance < minValue)
                {
                    minValue = distance;
                    closestIndexToInteractor = i;
                }
            }
            return closestIndexToInteractor;
        }

        private Transform FindSelector()
        {
#if FIXME
            if (_rayInteractors[0].gameObject.activeInHierarchy)
            {
                return _rayInteractors[0].ManipulatorTransform.transform;
            }
            else
            {
                return _directInteractors[0].transform;
            }
#else
            return null;
#endif
        }

        private void Initialize()
        {
#if FIXME
            _directInteractors = new DirectInteractor [0];
            _directInteractors = (DirectInteractor[])
                GameObject.FindObjectsOfType<DirectInteractor>(true);
            _rayInteractors = new RayInteractor [0];
            _rayInteractors = (RayInteractor[])
                GameObject.FindObjectsOfType<RayInteractor>(true);
            if (_rayInteractors[0] == null || _directInteractors[0] == null)
            {
                Debug.Log("No interactors in scene");
            }
            if (_keyboardManager == null)
            {
                Debug.Log("Text Selection won't be updated on Keyboard, set up " +
                          "the manager if you would like to use it");
            }
#endif
        }

        private Vector3 GetCharPositionSpaceCanvasToWorld(int charPosition, TextMeshProUGUI tmp)
        {
            Vector3 worldPos;
            tmp.ForceMeshUpdate();
            Vector3[] vertices = tmp.mesh.vertices;
            TMP_CharacterInfo charInfo = tmp.textInfo.characterInfo[charPosition];
            worldPos = tmp.transform.TransformPoint(charInfo.bottomRight);
            return worldPos;
        }
        #endregion Private Methods

        #region Coroutines
        private IEnumerator SelectionIsActive()
        {
            yield return new WaitForSeconds(0.02f);
            _selectionIsActive = true;
            yield return null;
        }
        #endregion Coroutines
    }
}
