using System;
using System.Text;
using MagicLeap.DesignToolkit.Actions;
using MagicLeap.LeapBrush;
using TMPro;
using UnityEngine;

namespace MagicLeap
{
    /// <summary>
    /// Entry in the join users popup representing a single user that can be joined.
    /// </summary>
    public class JoinUserEntry : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI _titleText;

        [SerializeField]
        private Interactable _joinUserInteractable;

        public void Initialize(QueryUsersResponse.Types.Result userResult, Action onJoinUser)
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

            _joinUserInteractable.Events.OnSelect.AddListener(_ =>
            {
                onJoinUser();
            });
        }
    }
}