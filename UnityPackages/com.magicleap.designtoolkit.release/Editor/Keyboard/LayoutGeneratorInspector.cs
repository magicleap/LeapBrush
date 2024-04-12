// Copyright (c) 2019-present, Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Developer Agreement, located
// here: https://auth.magicleap.com/terms/developer
using MagicLeap.DesignToolkit.Keyboard;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace MagicLeapUI
{
    [CustomEditor(typeof(VirtualKeyboardLayoutGen))]
    public class LayoutGeneratorInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            VirtualKeyboardLayoutGen layoutGenScript = (VirtualKeyboardLayoutGen) target;
            if (GUILayout.Button("Generate Layout"))
            {
                layoutGenScript.GenLanguageLayouts(layoutGenScript.Locale);

                EditorSceneManager.MarkSceneDirty(layoutGenScript.gameObject.scene);
            }
        }
    }
}