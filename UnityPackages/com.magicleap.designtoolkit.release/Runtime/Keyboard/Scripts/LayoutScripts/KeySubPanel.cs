using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MagicLeap.DesignToolkit.Keyboard
{
    public class KeySubPanel : MonoBehaviour
    {
        #region Public Members
        public bool IsVertical;
        public string SubPanelID;
        public GameObject BackgroundObj;
        public BoxCollider BackgroundCollider;
        public GameObject Container;
        public float ZOffset;
        #endregion Public Members

        #region Monobehaviour Methods
        private void Start()
        {
            if (BackgroundCollider != null)
            {
                BackgroundCollider.center = new Vector3(BackgroundCollider.center.x,
                                                       BackgroundCollider.center.y,
                                                       ZOffset / 2);

                BackgroundCollider.size = new Vector3(BackgroundCollider.size.x,
                                                      BackgroundCollider.size.y,
                                                      ZOffset);
            }
        }
        #endregion Monobehaviour Methods
    }
}