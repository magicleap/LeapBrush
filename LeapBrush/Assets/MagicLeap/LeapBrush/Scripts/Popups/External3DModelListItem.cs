using System;
using MixedReality.Toolkit;
using TMPro;
using UnityEngine;

namespace MagicLeap
{
    /// <summary>
    /// An item in the list of 3D models UI that the user can pick from.
    /// </summary>
    public class External3DModelListItem : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text _fileNameText;

        [SerializeField]
        private StatefulInteractable _openFileButton;

        private Action selectAction;

        private void Start()
        {
            _openFileButton.OnClicked.AddListener(() =>
            {
                selectAction?.Invoke();
            });
        }

        public void SetText(string text)
        {
            _fileNameText.text = text;
        }

        public void SetSelectAction(Action selectAction)
        {
            this.selectAction = selectAction;
        }
    }
}