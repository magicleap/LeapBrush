// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) (2018-2022) Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

using UnityEditor;
using UnityEngine;

namespace MagicLeap.MRTK.Settings
{
    /// <summary>
    /// PropertyDrawer for the InputActionOverride struct display in the Inspector.
    /// 
    /// InputActionOverride contains an InputActionReference with a corresponding
    /// override path string in order to override the binding for the InputAciton
    /// at runtime.
    /// </summary>
    [CustomPropertyDrawer(typeof(MagicLeapMRTK3SettingsRigConfig.InputActionOverride))]
    public class InputActionOverridePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Calculate Rects for properties
            float halfWidth = position.width / 2.0f;
            float buffer = 5.0f;
            var actionRect = new Rect(position.x, position.y, halfWidth - buffer/2, position.height);
            var overrideRect = new Rect(position.x + halfWidth + buffer/2, position.y, halfWidth - buffer/2, position.height);

            // Draw properties
            DrawSubProperty("Action", property.FindPropertyRelative("actionRef"), actionRect);
            DrawSubProperty("Path", property.FindPropertyRelative("overridePath"), overrideRect);

            void DrawSubProperty(string label, SerializedProperty property, Rect parentRect)
            {
                float labelWidth = EditorStyles.label.CalcSize(new GUIContent(label)).x + 2.0f;
                Rect labelRect = new Rect(parentRect.x, parentRect.y, labelWidth, parentRect.height);
                Rect propertyRect = new Rect(parentRect.x + labelWidth, parentRect.y, parentRect.width - labelWidth, parentRect.height);
                EditorGUI.LabelField(labelRect, label);
                EditorGUI.PropertyField(propertyRect, property, GUIContent.none);
            }

            EditorGUI.EndProperty();
        }
    }
}