using System;
using System.Collections;
using MixedReality.Toolkit;
using MixedReality.Toolkit.UX.Experimental;
using TMPro;
using UnityEngine;

namespace MagicLeap.LeapBrush
{
    /// <summary>
    /// Popup UI to select a 3D model to load from the current list of available models.
    /// </summary>
    public class ImportModelsPopup : BasePopup
    {
        public event Action<External3DModelManager.ModelInfo> OnPlaceNewExternal3DModel;

        [Header("External Dependencies")]

        [SerializeField]
        private External3DModelManager _external3dModelManager;

        [Header("Internal Dependencies")]

        [SerializeField]
        private StatefulInteractable _importModelCancelButton;

        [SerializeField]
        private StatefulInteractable _scrollPreviousButton;

        [SerializeField]
        private StatefulInteractable _scrollNextButton;

        [SerializeField]
        private TMP_Text _importModelDirectoryInfoText;

        [SerializeField]
        private VirtualizedScrollRectList _scrollList;

        private External3DModelManager.ModelInfo[] _models =
            Array.Empty<External3DModelManager.ModelInfo>();
        private DelayedButtonHandler _delayedButtonHandler;

        private float targetScrollPosition;
        private bool scrollListAnimating;
        private IEnumerator _load3DModelListPeriodicallyCoroutine;

        public void OnExternal3DModelsListUpdated(External3DModelManager.ModelInfo[] models)
        {
            _models = models;
            _scrollList.SetItemCount(_models.Length);

            for (int i = 0; i < _models.Length; i++)
            {
                if (_scrollList.TryGetVisible(i, out GameObject listItem))
                {
                    OnListItemVisible(listItem, i);
                }
            }
        }

        public override void Show()
        {
            _external3dModelManager.RefreshModelList();
            base.Show();
        }

        private void Awake()
        {
            _delayedButtonHandler = gameObject.AddComponent<DelayedButtonHandler>();
        }

        private void Start()
        {
            _importModelDirectoryInfoText.text = string.Format("(Add files to {0})",
                Application.persistentDataPath);

            _importModelCancelButton.OnClicked.AddListener(OnCancelButtonClicked);
            _scrollPreviousButton.OnClicked.AddListener(OnScrollPreviousButtonClicked);
            _scrollNextButton.OnClicked.AddListener(OnScrollNextButtonClicked);

            _scrollList.OnVisible += OnListItemVisible;
        }


        private void OnEnable()
        {
            _load3DModelListPeriodicallyCoroutine = Load3DModelListPeriodicallyCoroutine();
            StartCoroutine(_load3DModelListPeriodicallyCoroutine);
        }

        private void OnDisable()
        {
            StopCoroutine(_load3DModelListPeriodicallyCoroutine);
            _load3DModelListPeriodicallyCoroutine = null;
        }

        private void Update()
        {
            if (scrollListAnimating)
            {
                float newScroll = Mathf.Lerp(
                    _scrollList.Scroll, targetScrollPosition, 8 * Time.deltaTime);
                _scrollList.Scroll = newScroll;
                if (Mathf.Abs(_scrollList.Scroll - targetScrollPosition) < 0.02f)
                {
                    _scrollList.Scroll = targetScrollPosition;
                    scrollListAnimating = false;
                }
            }
        }

        private void OnListItemVisible(GameObject listItem, int index)
        {
            // TODO(ghazen): Work around the Instantiate call for scroll rect items
            // causing items to have identity world rotations.
            listItem.transform.localRotation = Quaternion.identity;

            var entry = listItem.GetComponent<External3DModelListItem>();
            entry.SetText(_models[index].FileName);
            entry.SetSelectAction(() =>
            {
                _delayedButtonHandler.InvokeAfterDelayExclusive(() =>
                {
                    OnPlaceNewExternal3DModel?.Invoke(_models[index]);
                });
            });
        }

        private void OnCancelButtonClicked()
        {
            _delayedButtonHandler.InvokeAfterDelayExclusive(() =>
            {
                Hide();
            });
        }

        private void OnScrollPreviousButtonClicked()
        {
            scrollListAnimating = true;
            targetScrollPosition = Mathf.Max(
                0, Mathf.Floor(_scrollList.Scroll / _scrollList.RowsOrColumns)
                * _scrollList.RowsOrColumns - _scrollList.TotallyVisibleCount);
        }

        private void OnScrollNextButtonClicked()
        {
            scrollListAnimating = true;
            targetScrollPosition = Mathf.Min(_scrollList.MaxScroll, Mathf.Floor(
                    _scrollList.Scroll / _scrollList.RowsOrColumns)
                * _scrollList.RowsOrColumns + _scrollList.TotallyVisibleCount);
        }

        private IEnumerator Load3DModelListPeriodicallyCoroutine()
        {
            while (true)
            {
                _external3dModelManager.RefreshModelList();

                yield return new WaitForSeconds(0.5f);
            }
        }
    }
}