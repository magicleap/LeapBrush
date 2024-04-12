#if (UNITY_EDITOR)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(SerializableDictionary), true)]
public class SerializableDictionaryPropertyDrawer : PropertyDrawer
{
    SerializedProperty keysProp;
    SerializedProperty valuesProp;
    SerializedProperty openedProp;
    SerializedProperty openedKVPairsProp;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        EditorGUI.PrefixLabel(position, label, EditorStyles.boldLabel);

        GetProperties(property);

        if (keysProp == null ||
            valuesProp == null ||
            openedProp == null ||
            openedKVPairsProp == null)
        {
            EditorGUI.EndProperty();
            return;
        }

        openedProp.boolValue =
            EditorGUILayout.BeginFoldoutHeaderGroup(openedProp.boolValue, "Content");

        if (!openedProp.boolValue)
        {
            EditorGUILayout.EndFoldoutHeaderGroup();
            return;
        }

        EditorGUILayout.BeginVertical();

        EditorGUI.indentLevel++;

        int count = EditorGUILayout.DelayedIntField("Count: ", keysProp.arraySize);

        ReBalanceArray(count);

        DrawDictionary(count);

        EditorGUI.indentLevel--;
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUI.EndProperty();
    }

    private void GetProperties(SerializedProperty property)
    {
        if (keysProp == null)
        {
            keysProp = property.FindPropertyRelative("_keys");
        }

        if (valuesProp == null)
        {
            valuesProp = property.FindPropertyRelative("_values");
        }

        if (openedProp == null)
        {
            openedProp = property.FindPropertyRelative("_opened");
        }

        if (openedKVPairsProp == null)
        {
            openedKVPairsProp = property.FindPropertyRelative("_openedKVPairs");
        }
    }

    private void ReBalanceArray(int count)
    {
        if (keysProp.arraySize != count)
        {
            keysProp.arraySize = count;
        }

        if (valuesProp.arraySize != count)
        {
            valuesProp.arraySize = count;
        }

        if (openedKVPairsProp.arraySize != count)
        {
            openedKVPairsProp.arraySize = count;
        }
    }

    private void DrawDictionary(int count)
    {
        EditorGUILayout.BeginVertical();

        for (int idx = 0; idx < count; idx++)
        {
            EditorGUI.indentLevel++;
            bool res = EditorGUILayout.Foldout(
                openedKVPairsProp.GetArrayElementAtIndex(idx).boolValue,
                new GUIContent(keysProp.GetArrayElementAtIndex(idx).displayName));

            if (!openedKVPairsProp.GetArrayElementAtIndex(idx).boolValue)
            {
                EditorGUI.indentLevel--;
                openedKVPairsProp.GetArrayElementAtIndex(idx).boolValue = res;
                continue;
            }

            EditorGUILayout.BeginVertical();
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(
                keysProp.GetArrayElementAtIndex(idx), new GUIContent("Key:"));
            EditorGUILayout.PropertyField(
                valuesProp.GetArrayElementAtIndex(idx), new GUIContent("Value:"));

            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();

            EditorGUI.indentLevel--;
            openedKVPairsProp.GetArrayElementAtIndex(idx).boolValue = res;
        }

        EditorGUILayout.EndVertical();
    }
}
#endif