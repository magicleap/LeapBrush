// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) (2018-2022) Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MagicLeap.MRTK.Settings
{
    /// <summary>
    /// Base class for all MagicLeap MRTK3 Settings ScriptableObjects
    /// </summary>
    public abstract class MagicLeapMRTK3SettingsObject : ScriptableObject
    {
        /// <summary>
        /// Process settings early within <see cref="RuntimeInitializeLoadType.BeforeSceneLoad"/>.
        /// </summary>
        public abstract void ProcessOnBeforeSceneLoad();

        /// <summary>
        /// Process settings after the first scene is loaded within <see cref="RuntimeInitializeLoadType.AfterSceneLoad"/>.
        /// </summary>
        public abstract void ProcessOnAfterSceneLoad();

#if UNITY_EDITOR

        /// <summary>
        /// SerializedObject representation in Editor.
        /// </summary>
        protected SerializedObject serializedObject;

        private void OnEnable()
        {
            serializedObject = new SerializedObject(this);
        }

        /// <summary>
        /// Draw settings in Inspector.
        /// </summary>
        public abstract void OnGUI();

        /// <summary>
        /// Helper method to draw a line separator in GUI.
        /// </summary>
        /// <param name="height">Height of the line.</param>
        /// <param name="width">Width of the line.</param>
        protected static void DrawGUILineSeparator(float height = 2, float width = 0)
        {
            Rect lineSeparator = EditorGUILayout.GetControlRect(false, height);
            lineSeparator.width = width > 0 ? width : lineSeparator.width;
            EditorGUI.DrawRect(lineSeparator, new Color(.35f, .35f, .35f));
        }
#endif
    }
}