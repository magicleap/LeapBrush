using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MagicLeap.DesignToolkit.Keyboard
{
    public class SubPanelCanvasInfo : MonoBehaviour
    {
        public Canvas CanvasObj;
        public List<KeySubPanel> SubPanels = new List<KeySubPanel>();
        private Dictionary<string, int> SubPanelIdxDictionary;
        private bool Inited = false;

        public void Init()
        {
            if (Inited)
            {
                return;
            }
            SubPanelIdxDictionary = new Dictionary<string, int>();
            for (int idx = 0; idx < SubPanels.Count; idx++)
            {
                SubPanelIdxDictionary.Add(SubPanels[idx].SubPanelID, idx);
                ((RectTransform) SubPanels[idx].transform).anchoredPosition3D =
                    Vector3.back * SubPanels[idx].ZOffset;
            }
            Inited = true;
        }

        public KeySubPanel GetSubPanel(string id)
        {
            if (!SubPanelIdxDictionary.ContainsKey(id))
            {
                return null;
            }
            return SubPanels[SubPanelIdxDictionary[id]];
        }

        public bool HasInited
        {
            get { return Inited; }
        }
    }
}