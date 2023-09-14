// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) (2018-2022) Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

using System;
using Unity.XR.CoreUtils;
using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MagicLeap.MRTK.Settings
{
    /// <summary>
    /// Magic Leap MRTK3 settings available in Editor and Runtime.
    /// </summary>
    [Serializable]
    [ScriptableSettingsPath("Assets")]
    public sealed class MagicLeapMRTK3Settings : ScriptableSettings<MagicLeapMRTK3Settings>
    {
        private const uint SettingsFileVersion = 1;

        [SerializeField]
        [HideInInspector]
        private uint version = SettingsFileVersion;
        public uint Version => version;

        [SerializeField]
        private MagicLeapMRTK3SettingsGeneral generalSettings = null;

        [SerializeField]
        private MagicLeapMRTK3SettingsRigConfig rigConfig = null;

        [SerializeField]
        private MagicLeapMRTK3SettingsPermissionsConfig permissionsConfig = null;

        /// <summary>
        /// Provides enumerable access to all contained <see cref="MagicLeapMRTK3SettingsObject"/>s.
        /// </summary>
        public IEnumerable<MagicLeapMRTK3SettingsObject> SettingsObjects
        {
            get
            {
                yield return generalSettings;
                yield return rigConfig;
                yield return permissionsConfig;
            }
        }

        /// <summary>
        /// Gets the specified type of <see cref="MagicLeapMRTK3SettingsObject"/>.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="MagicLeapMRTK3SettingsObject"/>.</typeparam>
        /// <returns>The contained <see cref="MagicLeapMRTK3SettingsObject"/>, or a default instance if not present.</returns>
        public T GetSettingsObject<T>() where T : MagicLeapMRTK3SettingsObject
        {
            if (TryGetSettingsObject(out T settingsObject))
            {
                return settingsObject;
            }

            // Create a default instance if no contained settings object.
            return CreateInstance<T>();
        }

        /// <summary>
        /// Attempts to retrieve a specific <see cref="MagicLeapMRTK3SettingsObject"/>.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="MagicLeapMRTK3SettingsObject"/>.</typeparam>
        /// <param name="settingsObjectOut">The returned <see cref="MagicLeapMRTK3SettingsObject"/>.</param>
        /// <returns>True if the collection contains a settings object of the type, or false if not.</returns>
        public bool TryGetSettingsObject<T>(out T settingsObjectOut) where T : MagicLeapMRTK3SettingsObject
        {
            foreach (var settingsObject in SettingsObjects)
            {
                if (settingsObject.GetType() == typeof(T))
                {
                    settingsObjectOut = settingsObject as T;
                    return settingsObjectOut != null;
                }
            }

            settingsObjectOut = null;
            return false;
        }

        private void OnEnable()
        {
            // Deserialization has occurred at this point, so validate settings objects.
            ValidateSettingsObject(ref generalSettings);
            ValidateSettingsObject(ref rigConfig);
            ValidateSettingsObject(ref permissionsConfig);

#if UNITY_EDITOR
            serializedObject = new SerializedObject(this);
#endif
        }

        private void ValidateSettingsObject<T>(ref T settingsObject) where T : MagicLeapMRTK3SettingsObject
        {
            if (settingsObject == null)
            {
                settingsObject = CreateInstance<T>();
            }
        }

#if UNITY_EDITOR

        // Maintain a SerializedObject in Editor
        private SerializedObject serializedObject;

        private static readonly string DefaultPathBase = "Packages/com.magicleap.mrtk3/Editor/Settings/Defaults/";

        /// <summary>
        /// Initialize and validate setting objects in Editor
        /// </summary>
        public void Initialize()
        {
            ValidateSettingsObjectInEditor(ref generalSettings);
            ValidateSettingsObjectInEditor(ref rigConfig);
            ValidateSettingsObjectInEditor(ref permissionsConfig);
        }

        private void ValidateSettingsObjectInEditor<T>(ref T settingsObject) where T : MagicLeapMRTK3SettingsObject
        {
            // In Editor, if the settings object doesn't exist or hasn't been added to the parent settings object yet,
            // attempt to load the pre-setup, default, asset and then add to parent settings object.
            if (settingsObject == null || !AssetDatabase.IsSubAsset(settingsObject))
            {
                LoadDefault(ref settingsObject);
            }
        }

        /// <summary>
        /// Load default settings manually in Editor
        /// </summary>
        public void LoadDefaults()
        {
            LoadDefault(ref generalSettings);
            LoadDefault(ref rigConfig);
            LoadDefault(ref permissionsConfig);
        }

        private void LoadDefault<T>(ref T settingsObject) where T : MagicLeapMRTK3SettingsObject
        {
            // Remove any existing sub object settingsObject
            if (settingsObject != null)
            {
                AssetDatabase.RemoveObjectFromAsset(settingsObject);
            }
            // Load and clone default asset
            var defaultPath = $"{DefaultPathBase}{typeof(T).Name}_Default.asset";
            var defaults = AssetDatabase.LoadAssetAtPath<T>(defaultPath);
            settingsObject = defaults != null ? Instantiate(defaults) :
                                                CreateInstance<T>();
            settingsObject.name = typeof(T).Name;
            AssetDatabase.AddObjectToAsset(settingsObject, this);
            EditorUtility.SetDirty(this);
            EditorUtility.SetDirty(settingsObject);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Draws the settings in Editor.
        /// </summary>
        public void OnGUI()
        {
            serializedObject.Update();

            foreach (var settingsObject in SettingsObjects)
            {
                settingsObject.OnGUI();
                EditorGUILayout.Space(20);
            }

            serializedObject.ApplyModifiedProperties();
        }

#endif // UNITY_EDITOR

    }
}