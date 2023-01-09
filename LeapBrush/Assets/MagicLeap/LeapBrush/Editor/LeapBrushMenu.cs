using UnityEditor;

namespace MagicLeap.LeapBrush
{
    public class LeapBrushMenu
    {

        [MenuItem("Leap Brush/Build Current Platform")]
        static void Build_Menu()
        {
            var settings = LeapBrushBuildUserSettings.Load();
            LeapBrushBuild.Build(settings);
        }

        [MenuItem("Leap Brush/Edit user build settings...")]
        static void CreateOrEditStandaloneBuildUserSettings_Menu()
        {
            LeapBrushBuildUserSettings.CreateOrEditInInspector();
        }
   }
}