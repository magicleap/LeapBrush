// Copyright (c) 2019-present, Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Developer Agreement, located
// here: https://auth.magicleap.com/terms/developer
using MagicLeap.DesignToolkit.Keyboard;
using UnityEngine;
using UnityEditor;

namespace MagicLeapUI
{
    [CustomEditor(typeof(KeyboardManager))]
    public class KeyboardManagerInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            KeyboardManager script = (KeyboardManager) target;
            if (GUILayout.Button("Reload Layouts"))
            {
                script.EditorReloadLayouts();
                EditorUtility.SetDirty(script.gameObject);
            }
        }
    }
}