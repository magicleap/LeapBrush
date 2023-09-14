using UnityEditor;
using UnityEngine;

namespace MagicLeap.LeapBrush
{
    [CustomEditor(typeof(LeapBrushPreferences))]
    public class LeapBrushPreferencesEditor : Editor
    {
        [CustomPropertyDrawer(typeof(LeapBrushPreferences.BoolPref))]
        public class BoolPrefPropertyDrawer : PropertyDrawer
        {
            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                EditorGUI.BeginProperty(position, label, property);

                position = EditorGUI.PrefixLabel(
                    position, GUIUtility.GetControlID(FocusType.Passive), label);

                LeapBrushPreferences.BoolPref boolPref = fieldInfo.GetValue(
                    property.serializedObject.targetObject) as LeapBrushPreferences.BoolPref;

                EditorGUIUtility.labelWidth = 40;
                EditorGUI.LabelField(position, boolPref.ToString());

                EditorGUI.EndProperty();
            }
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            LeapBrushPreferences preferences = (LeapBrushPreferences) target;

            if (GUILayout.Button("Reset Preferences To Default"))
            {
                preferences.ResetToDefaults();

                EditorUtility.SetDirty(preferences);
            }
        }
    }
}
