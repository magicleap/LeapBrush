using TMPro.EditorUtilities;
using UnityEditor;
using UnityEngine;

namespace MagicLeap.DesignToolkit.Keyboard
{
    [CustomEditor(typeof(KeyboardInputFieldTextMeshPro)), CanEditMultipleObjects]
    public class KeyboardInputFieldTextMeshProInspector : TMP_EditorPanelUI
    {
        private SerializedProperty _originalTextProperty;
        private SerializedProperty _autoSetRTLProperty;

        protected override void OnEnable()
        {
            base.OnEnable();
            _originalTextProperty = serializedObject.FindProperty("_originalText");
            _autoSetRTLProperty = serializedObject.FindProperty("_autoSetRTL");
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(_originalTextProperty, new GUIContent("Original Text"));

            EditorGUILayout.PropertyField(_autoSetRTLProperty, new GUIContent("Automatically set RTL?"));

            serializedObject.ApplyModifiedProperties();

            base.OnInspectorGUI();
        }
    }
}
