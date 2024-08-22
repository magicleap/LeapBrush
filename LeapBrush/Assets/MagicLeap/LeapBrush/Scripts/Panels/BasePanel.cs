using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.SmartFormat.PersistentVariables;

namespace MagicLeap.LeapBrush
{
    public class BasePanel : MonoBehaviour
    {
        [Header("Base Panel Internal Dependencies")]

        [SerializeField]
        private TMP_Text _titleText;

        protected void Start()
        {
            LocalizeStringEvent textLocalized =
                _titleText.GetComponent<LocalizeStringEvent>();

            ((StringVariable) textLocalized.StringReference["AppName"]).Value
                = Application.productName;

            textLocalized.StringReference.RefreshString();
        }
    }
}