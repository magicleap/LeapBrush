using System;
using System.Collections.Generic;
using Grpc.Core;
using MixedReality.Toolkit;
using MixedReality.Toolkit.UX.Experimental;
using UnityEngine;

namespace MagicLeap.LeapBrush
{
    /// <summary>
    /// Popup showing a list of users connected to the server who could be joined.
    /// </summary>
    public class JoinUserPopup : BasePopup
    {
        public event Action<QueryUsersResponse.Types.Result> OnJoinUserSessionRemotelySelected;

        [Header("External Dependencies")]

        [SerializeField]
        private JoinUserLocalOrRemotePopup _joinUserLocalOrRemotePopup;

        [Header("Internal Dependencies")]

        [SerializeField]
        private StatefulInteractable _joinUserCancelButton;

        [SerializeField]
        private StatefulInteractable _joinUserRefreshButton;

        [SerializeField]
        private GameObject _joinUserNoUsersFoundGameObject;

        [SerializeField]
        private GameObject _joinUserScrollArea;

        [SerializeField]
        private VirtualizedScrollRectList _scrollList;

        [SerializeField]
        private StatefulInteractable _scrollPreviousButton;

        [SerializeField]
        private StatefulInteractable _scrollNextButton;

        private DelayedButtonHandler _delayedButtonHandler;
        private PopupTracker _popupTracker;
        private string _userName;
        private LeapBrushApiBase.LeapBrushClient _leapBrushClient;
        private QueryUsersResponse.Types.Result[] _userResults
            = Array.Empty<QueryUsersResponse.Types.Result>();
        private float targetScrollPosition;
        private bool scrollListAnimating;

        public void Show(string userName, LeapBrushApiBase.LeapBrushClient leapBrushClient)
        {
            base.Show();

            _userName = userName;
            _leapBrushClient = leapBrushClient;

            _joinUserScrollArea.SetActive(false);

            RefreshUsers();
        }

        private void Awake()
        {
            _delayedButtonHandler = gameObject.AddComponent<DelayedButtonHandler>();
            _popupTracker = gameObject.AddComponent<PopupTracker>();
        }

        private void Start()
        {
            _joinUserCancelButton.OnClicked.AddListener(OnCancelButtonClicked);
            _joinUserRefreshButton.OnClicked.AddListener(OnRefreshButtonClicked);

            _scrollPreviousButton.OnClicked.AddListener(OnScrollPreviousButtonClicked);
            _scrollNextButton.OnClicked.AddListener(OnScrollNextButtonClicked);

            _scrollList.OnVisible += UpdateListItem;
        }

        private void OnEnable()
        {
            _popupTracker.TrackPopup(_joinUserLocalOrRemotePopup);
            _popupTracker.OnPopupsShownChanged += OnPopupsShownChanged;
            OnPopupsShownChanged(_popupTracker.PopupsShown);
        }

        private void OnDisable()
        {
            _popupTracker.OnPopupsShownChanged -= OnPopupsShownChanged;
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

        private void OnPopupsShownChanged(bool popupsShown)
        {
            _joinUserCancelButton.enabled = !popupsShown;
            _joinUserRefreshButton.enabled = !popupsShown;

            UpdateAllListItems();
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

        private void UpdateListItem(GameObject listItem, int index)
        {
            // TODO(ghazen): Work around the Instantiate call for scroll rect items
            // causing items to have identity world rotations.
            listItem.transform.localRotation = Quaternion.identity;

            var entry = listItem.GetComponent<JoinUserListItem>();
            entry.SetUserResult(_userResults[index]);
            entry.SetSelectAction(() =>
            {
                _delayedButtonHandler.InvokeAfterDelayExclusive(() =>
                {
                    OnJoinUserSessionSelected(_userResults[index]);
                });
            });

            listItem.GetComponent<StatefulInteractable>().enabled =
                !_joinUserLocalOrRemotePopup.isActiveAndEnabled;
        }

        private void OnRefreshButtonClicked()
        {
            RefreshUsers();

            _joinUserNoUsersFoundGameObject.SetActive(false);
            _joinUserScrollArea.SetActive(false);
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

            List<QueryUsersResponse.Types.Result> userResults = new();
            foreach (var userResult in response.Results)
            {
                if (_userName == userResult.UserName || userResult.SpaceInfo == null ||
                    string.IsNullOrEmpty(userResult.SpaceInfo.SpaceId) ||
                    userResult.SpaceInfo.Anchor.Count == 0)
                {
                    continue;
                }

                userResults.Add(userResult);
            }
            userResults.Sort((a, b) =>
                string.CompareOrdinal(a.UserDisplayName, b.UserDisplayName));

            _userResults = userResults.ToArray();

            _joinUserNoUsersFoundGameObject.SetActive(_userResults.Length == 0);
            _joinUserScrollArea.SetActive(_userResults.Length > 0);

            _scrollList.SetItemCount(_userResults.Length);
            UpdateAllListItems();
        }

        private void UpdateAllListItems()
        {
            for (int i = 0; i < _userResults.Length; i++)
            {
                if (_scrollList.TryGetVisible(i, out GameObject listItem))
                {
                    UpdateListItem(listItem, i);
                }
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
    }
}