using UnityEngine;
using System;
using TMPro;

namespace MagicLeap.LeapBrush
{
    public class OtherUserWearable : MonoBehaviour
    {
        [SerializeField]
        private GameObject _nameTag;

        [SerializeField]
        private TMP_Text _userNameText;

        public DateTimeOffset LastUpdateTime = DateTimeOffset.Now;

        private float _nameTagVerticalOffset;

        private void Start()
        {
            _nameTagVerticalOffset = (_nameTag.transform.position - transform.position).magnitude;
        }

        private void Update()
        {
            Transform nameTagTransform = _nameTag.transform;
            Vector3 lookDir = (
                nameTagTransform.position - Camera.main.transform.position).normalized;
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
            {
                nameTagTransform.SetPositionAndRotation(
                    transform.position + new Vector3(0, _nameTagVerticalOffset, 0),
                    Quaternion.LookRotation(lookDir, Vector3.up));
            }
        }

        public void SetUserDisplayName(string userDisplayName)
        {
            _userNameText.text = userDisplayName;
        }
    }
}