using System;
using UnityEngine;
using UnityEngine.XR.MagicLeap;

namespace MagicLeap.LeapBrush
{
    /// <summary>
    /// Api for starting the Spaces application on the device in various modes.
    /// </summary>
    public class SpacesAppApi
    {
        /// <summary>
        /// Start the spaces app.
        /// </summary>
        public static void StartApp()
        {
            try
            {
                using (AndroidJavaClass activityClass = new AndroidJavaClass(
                           "com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject activity = activityClass.GetStatic<AndroidJavaObject>(
                           "currentActivity"))
                using (AndroidJavaObject intent = new AndroidJavaObject(
                           "android.content.Intent", "com.magicleap.intent.action.SPACES"))
                {
                    activity.Call("startActivity", intent);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Error while launching spaces app: " + e);
            }
        }

        /// <summary>
        /// Start the spaces app for the purpose of localizing into a particular space.
        /// </summary>
        /// <param name="spaceId">The id of the space to be localized to.</param>
        /// <param name="mappingMode">The mapping mode of the space to be localized to.</param>
        public static void StartAppToSelectSpace(string spaceId, MLAnchors.MappingMode mappingMode)
        {
            try
            {
                using (AndroidJavaClass activityClass = new AndroidJavaClass(
                           "com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject activity = activityClass.GetStatic<AndroidJavaObject>(
                           "currentActivity"))
                using (AndroidJavaObject intent = new AndroidJavaObject(
                           "android.content.Intent",
                           "com.magicleap.intent.action.SELECT_SPACE"))
                {
                   intent.Call<AndroidJavaObject>(
                       "putExtra", "com.magicleap.intent.extra.SPACE_ID",
                       spaceId);
                   intent.Call<AndroidJavaObject>(
                       "putExtra", "com.magicleap.intent.extra.MAPPING_MODE",
                       mappingMode == MLAnchors.MappingMode.ARCloud ? 1 : 0);
                   activity.Call("startActivityForResult", intent, 0);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Error while launching spaces app: " + e);
            }
        }

        private SpacesAppApi()
        {
        }
    }
}