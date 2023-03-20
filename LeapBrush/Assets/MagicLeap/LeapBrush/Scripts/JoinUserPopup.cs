using System;
using System.Linq;
using Grpc.Core;
using MagicLeap.DesignToolkit.Actions;
using UnityEngine;
using UnityEngine.UI;

namespace MagicLeap.LeapBrush
{
    /// <summary>
    /// Popup showing a list of users connected to the server who could be joined.
    /// </summary>
    public class JoinUserPopup : MonoBehaviour
    {
        public event Action<QueryUsersResponse.Types.Result> OnJoinUserSessionRemotelySelected;

        [Header("External Dependencies")]

        [SerializeField]
        private JoinUserLocalOrRemotePopup _joinUserLocalOrRemotePopup;

        [Header("Internal Dependencies")]

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
        private string _userName;
        private LeapBrushApiBase.LeapBrushClient _leapBrushClient;

        public void Show(string userName, LeapBrushApiBase.LeapBrushClient leapBrushClient)
        {
            _userName = userName;
            _leapBrushClient = leapBrushClient;

            gameObject.SetActive(true);
            _joinUserScrollView.SetActive(false);

            RefreshUsers();
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

        private void OnCancelButtonSelected(Interactor interactor)
        {
            _delayedButtonHandler.InvokeAfterDelayExclusive(() =>
            {
                Hide();
            });
        }

        private void OnRefreshButtonSelected(Interactor interactor)
        {
            RefreshUsers();

            _joinUserNoUsersFoundGameObject.SetActive(false);
            _joinUserScrollView.SetActive(false);
        }

        private void RefreshUsers()
        {
            ThreadDispatcher.ScheduleWork(() =>
            {
                RpcRequest req = new RpcRequest();
                req.UserName = _userName;

                req.QueryUsersRequest = new QueryUsersRequest();
                try
                {
                    RpcResponse resp = _leapBrushClient.Rpc(req);
                    ThreadDispatcher.ScheduleMain(() =>
                    {
                        HandleQueryUsersResultOnMainThread(resp.QueryUsersResponse);
                    });
                }
                catch (RpcException e)
                {
                    Debug.LogWarning("Rpc.QueryUsersRequest failed: " + e);
                }
            });
        }

        private void HandleQueryUsersResultOnMainThread(QueryUsersResponse response)
        {
            Debug.LogFormat("Join Users: Found {0} users", response.Results.Count);

            ClearUsersList();

            QueryUsersResponse.Types.Result[] userResults = response.Results.ToArray();
            Array.Sort(userResults, (a, b) =>
                string.CompareOrdinal(a.UserDisplayName, b.UserDisplayName));

            foreach (var userResult in userResults)
            {
                if (_userName == userResult.UserName || userResult.SpaceInfo == null ||
                    string.IsNullOrEmpty(userResult.SpaceInfo.SpaceId) ||
                    userResult.SpaceInfo.Anchor.Count == 0)
                {
                    continue;
                }

                AddUser(userResult, () =>
                {
                    OnJoinUserSessionSelected(userResult);
                });
            }
        }

        private void OnJoinUserSessionSelected(QueryUsersResponse.Types.Result userResult)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (userResult.SpaceInfo.MappingMode == SpaceInfoProto.Types.MappingMode.ArCloud)
            {
                _joinUserLocalOrRemotePopup.Show(userResult.UserDisplayName,
                    userResult.SpaceInfo.SpaceName, () =>
                    {
                        _joinUserLocalOrRemotePopup.Hide();
                        OnJoinUserSessionRemotelySelected?.Invoke(userResult);
                    }, () =>
                    {
                        Hide();
                        _joinUserLocalOrRemotePopup.Hide();
                        SpacesAppApi.StartAppToSelectSpace(userResult.SpaceInfo.SpaceId,
                            ProtoUtils.FromProto(userResult.SpaceInfo.MappingMode));
                    });
            }
            else
            {
                OnJoinUserSessionRemotelySelected?.Invoke(userResult);
            }
#else
            OnJoinUserSessionRemotelySelected?.Invoke(userResult);
#endif
        }

        private void ClearUsersList()
        {
            foreach (Transform child in _joinUserListLayout.transform)
            {
                Destroy(child.gameObject);
            }

            _joinUserNoUsersFoundGameObject.SetActive(true);
            _joinUserScrollView.SetActive(false);
        }

        private void AddUser(QueryUsersResponse.Types.Result userResult, Action onJoinUser)
        {
            JoinUserEntry joinUserEntry = Instantiate(
                _joinUserEntryPrefab, _joinUserListLayout.transform).GetComponent<JoinUserEntry>();
            joinUserEntry.Initialize(userResult, onJoinUser);

            _joinUserNoUsersFoundGameObject.SetActive(false);
            _joinUserScrollView.SetActive(true);
        }
    }
}