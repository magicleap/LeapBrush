using MagicLeap.DesignToolkit.Actions;
using UnityEngine;

namespace MagicLeap.LeapBrush
{
    /// <summary>
    /// Panel displayed when the user is not yet localized to a Local or Shared space.
    /// </summary>
    /// <remarks>
    /// The user is provided the opportunity to open the Spaces tool to create or localize
    /// to a space before continuing.
    /// </remarks>
    public class NotLocalizedPanel : MonoBehaviour
    {
        [SerializeField]
        private Interactable _openSpacesAppButton;

        private DelayedButtonHandler _delayedButtonHandler;

        private void Awake()
        {
            _delayedButtonHandler = gameObject.AddComponent<DelayedButtonHandler>();
        }

        private void Start()
        {
            _openSpacesAppButton.Events.OnSelect.AddListener(OnStartSpacesAppButtonSelected);
        }

        private void OnStartSpacesAppButtonSelected(Interactor interactor)
        {
            _delayedButtonHandler.InvokeAfterDelayExclusive(() =>
            {
                SpacesAppApi.StartApp();
            });
        }
    }
}
