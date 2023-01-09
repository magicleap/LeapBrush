using System;
using System.Collections;
using System.Collections.Generic;
using MagicLeap.DesignToolkit.Actions;
using TMPro;
using UnityEngine;

namespace MagicLeap
{
    public class External3DModelEntry : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI _fileNameText;

        [SerializeField]
        private Interactable _openFileInteractable;

        private External3DModelManager.ModelInfo _modelInfo;

        public void Initialize(External3DModelManager.ModelInfo modelInfo, Action onOpenFile)
        {
            _modelInfo = modelInfo;
            _fileNameText.text = modelInfo.FileName;
            _openFileInteractable.Events.OnSelect.AddListener((Interactor _) =>
            {
                onOpenFile();
            });
        }
    }
}