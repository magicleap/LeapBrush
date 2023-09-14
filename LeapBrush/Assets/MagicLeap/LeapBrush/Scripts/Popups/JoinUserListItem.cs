using System;
using System.Text;
using MagicLeap.LeapBrush;
using MixedReality.Toolkit;
using TMPro;
using UnityEngine;

namespace MagicLeap
{
    /// <summary>
    /// List item in the join users popup representing a single user that can be joined.
    /// </summary>
    public class JoinUserListItem : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI _titleText;

        [SerializeField]
        private StatefulInteractable _joinUserButton;

        private Action selectAction;

        private void Start()
        {
            _joinUserButton.OnClicked.AddListener(() =>
            {
                selectAction?.Invoke();
            });
        }

        public void SetUserResult(QueryUsersResponse.Types.Result userResult)
        {
            StringBuilder titleBuilder = new StringBuilder(userResult.UserDisplayName);
            if (userResult.SpaceInfo.UsingImportedAnchors)
            {
                titleBuilder.AppendFormat(" @ Joined a session");
            }
            else
            {
                if (!string.IsNullOrEmpty(userResult.SpaceInfo.SpaceName))
                {
                    if (userResult.SpaceInfo.MappingMode == SpaceInfoProto.Types.MappingMode.ArCloud)
                    {
                        titleBuilder.AppendFormat(" @ <color=#00ff00>{0}</color>",
                            userResult.SpaceInfo.SpaceName);
                    }
                    else
                    {
                        titleBuilder.AppendFormat(" @ <color=#ffa500>{0}</color>",
                            userResult.SpaceInfo.SpaceName);
                    }
                }
            }

            if (userResult.HasDeviceType)
            {
                switch (userResult.DeviceType)
                {
                    case UserStateProto.Types.DeviceType.DesktopSpectator:
                        titleBuilder.AppendFormat(" (Spectator App)");
                        break;
                    case UserStateProto.Types.DeviceType.MagicLeap:
                        titleBuilder.AppendFormat(" (Magic Leap Device)");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            _titleText.text = titleBuilder.ToString();
        }

        public void SetSelectAction(Action selectAction)
        {
            this.selectAction = selectAction;
        }
    }
}