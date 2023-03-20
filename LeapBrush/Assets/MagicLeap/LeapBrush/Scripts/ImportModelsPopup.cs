using System;
using MagicLeap.DesignToolkit.Actions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MagicLeap.LeapBrush
{
    /// <summary>
    /// Popup UI to select a 3D model to load from the current list of available models.
    /// </summary>
    public class ImportModelsPopup : MonoBehaviour
    {
        public event Action<External3DModelManager.ModelInfo> OnPlaceNewExternal3DModel;

        [SerializeField]
        private Interactable _importModelCancelButton;

        [SerializeField]
        private TextMeshProUGUI _importModelDirectoryInfoText;

        [SerializeField]
        private VerticalLayoutGroup _externalModelListLayout;

        [SerializeField]
        private GameObject _externalModelEntryPrefab;

        private DelayedButtonHandler _delayedButtonHandler;

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void Awake()
        {
            _delayedButtonHandler = gameObject.AddComponent<DelayedButtonHandler>();
        }

        private void Start()
        {
            _importModelDirectoryInfoText.text = string.Format("(Add files to {0})",
                Application.persistentDataPath);

            _importModelCancelButton.Events.OnSelect.AddListener(OnCancelButtonSelected);
        }

        private void OnCancelButtonSelected(Interactor interactor)
        {
            _delayedButtonHandler.InvokeAfterDelayExclusive(() =>
            {
                Hide();
            });
        }

        public void OnExternal3DModelsListUpdated(External3DModelManager.ModelInfo[] models)
        {
            foreach (Transform child in _externalModelListLayout.transform)
            {
                Destroy(child.gameObject);
            }

            foreach (External3DModelManager.ModelInfo modelInfo in models)
            {
                External3DModelEntry external3DModelEntry = Instantiate(
                    _externalModelEntryPrefab, _externalModelListLayout.transform)
                    .GetComponent<External3DModelEntry>();
                external3DModelEntry.Initialize(modelInfo, () =>
                {
                    _delayedButtonHandler.InvokeAfterDelayExclusive(() =>
                    {
                        OnPlaceNewExternal3DModel?.Invoke(modelInfo);
                    });
                });
            }
        }
    }
}