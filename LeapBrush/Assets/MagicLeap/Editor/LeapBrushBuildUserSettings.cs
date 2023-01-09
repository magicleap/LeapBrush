using UnityEditor;
using UnityEngine;

namespace MagicLeap
{
    public class LeapBrushBuildUserSettings : ScriptableObject
    {
        private const string AssetPath = "Assets/MagicLeap/Editor/LeapBrushBuildUserSettings.asset";

        public static LeapBrushBuildUserSettings Load()
        {
            var settings = AssetDatabase.LoadAssetAtPath<LeapBrushBuildUserSettings>(AssetPath);
            if (settings == null)
            {
                Debug.LogWarningFormat(
                    "No standalone build settings found at {0}, creating default", AssetPath);
                settings = CreateInstance<LeapBrushBuildUserSettings>();
            }

            return settings;
        }

        public static void CreateOrEditInInspector()
        {
            var settings = AssetDatabase.LoadAssetAtPath<LeapBrushBuildUserSettings>(AssetPath);
            if (settings == null)
            {
                settings = CreateInstance<LeapBrushBuildUserSettings>();
                AssetDatabase.CreateAsset(settings, AssetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = settings;
        }

        public string VersionStringSuffix;
        public string OutputPath;
    }
}