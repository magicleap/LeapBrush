using System;
using MagicLeap.DesignToolkit.Actions;
using UnityEngine;
using UnityEngine.UI;

namespace MagicLeap.LeapBrush
{
    public class JoinUserPopup : MonoBehaviour
    {
        public event Action OnRefreshRequested;

        [SerializeField]
        private Interactable _joinUserCancelButton;

        [SerializeField]
        private Interactable _joinUserRefreshButton;

        [SerializeField]
        private GameObject _joinUserNoUsersFoundGameObject;

        [SerializeField]
        private GameObject _joinUserScrollView;

        [SerializeField]
        private VerticalLayoutGroup _joinUserListLayout;

        [SerializeField]
        private GameObject _joinUserEntryPrefab;

        private DelayedButtonHandler _delayedButtonHandler;

        public void Show()
        {
            gameObject.SetActive(true);
            _joinUserScrollView.SetActive(false);
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
            _joinUserCancelButton.Events.OnSelect.AddListener(OnCancelButtonSelected);
            _joinUserRefreshButton.Events.OnSelect.AddListener(OnRefreshButtonSelected);
        }

        public void OnCancelButtonSelected(Interactor interactor)
        {
            _delayedButtonHandler.InvokeAfterDelayExclusive(() =>
            {
                Hide();
            });
        }

        public void OnRefreshButtonSelected(Interactor interactor)
        {
            OnRefreshRequested?.Invoke();

            _joinUserNoUsersFoundGameObject.SetActive(false);
            _joinUserScrollView.SetActive(false);
        }

        public void ClearUsersList()
        {
            foreach (Transform child in _joinUserListLayout.transform)
            {
                Destroy(child.gameObject);
            }

            _joinUserNoUsersFoundGameObject.SetActive(true);
            _joinUserScrollView.SetActive(false);
        }

        public void AddUser(QueryUsersResponse.Types.Result userResult, Action onJoinUser)
        {
            JoinUserEntry joinUserEntry = Instantiate(
                _joinUserEntryPrefab, _joinUserListLayout.transform).GetComponent<JoinUserEntry>();
            joinUserEntry.Initialize(userResult, onJoinUser);

            _joinUserNoUsersFoundGameObject.SetActive(false);
            _joinUserScrollView.SetActive(true);
        }
    }
}