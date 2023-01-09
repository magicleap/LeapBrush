using System;
using MagicLeap.DesignToolkit.Actions;
using MagicLeap.LeapBrush;
using TMPro;
using UnityEngine;

namespace MagicLeap
{
    public class JoinUserEntry : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI _userNameText;

        [SerializeField]
        private Interactable _joinUserInteractable;

        private QueryUsersResponse.Types.Result _userResult;

        public void Initialize(QueryUsersResponse.Types.Result userResult, Action onJoinUser)
        {
            _userResult = userResult;
            if (!string.IsNullOrEmpty(userResult.SpaceInfo.SpaceName))
            {
                if (userResult.SpaceInfo.MappingMode == SpaceInfoProto.Types.MappingMode.ArCloud)
                {
                    _userNameText.text = string.Format("{0} (@ {1})",
                        userResult.UserDisplayName, userResult.SpaceInfo.SpaceName);
                }
                else
                {
                    _userNameText.text = string.Format("{0} (@ <color=#ffa500>{1}</color>)",
                        userResult.UserDisplayName, userResult.SpaceInfo.SpaceName);
                }
            }
            else
            {
                _userNameText.text = userResult.UserDisplayName;
            }

            _joinUserInteractable.Events.OnSelect.AddListener((Interactor _) =>
            {
                onJoinUser();
            });
        }
    }
}