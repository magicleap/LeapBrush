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

namespace MagicLeap.MRTK
{
    /// <summary>
    /// Java Utilities and Helpers
    /// </summary>
    internal static class JavaUtils
    {
        private static AndroidJavaObject currentActivity = null;
        private static AndroidJavaObject contentResolver = null;
        private static AndroidJavaClass systemSettings = null;
        private static AndroidJavaClass systemProperties = null;

        public static AndroidJavaObject CurrentActivity
        {
            get
            {
                if (currentActivity == null)
                {
                    var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                    currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                }

                return currentActivity;
            }
        }

        public static AndroidJavaObject ContentResolver
        {
            get
            {
                if (contentResolver == null)
                {
                    contentResolver = CurrentActivity.Call<AndroidJavaObject>("getContentResolver");
                }

                return contentResolver;
            }
        }

        public static AndroidJavaClass SystemSettings
        {
            get
            {
                if (systemSettings == null)
                {
                    systemSettings = new AndroidJavaClass("android.provider.Settings$System");
                }

                return systemSettings;
            }
        }

        public static AndroidJavaClass SystemProperties
        {
            get
            {
                if (systemProperties == null)
                {
                    systemProperties = new AndroidJavaClass("android.os.SystemProperties");
                }

                return systemProperties;
            }
        }

        /// <summary>
        /// Obtains the value for the system setting.
        /// It is not recommended to call this method with a high frequency.
        /// This method must be called on the main thread.
        /// </summary>
        /// <typeparam name="T">The type of value.</typeparam>
        /// <param name="methodName">The method name to use, like "getInt", "getBoolean", etc.</param>
        /// <param name="key">The system setting key.</param>
        /// <returns>The system setting value.</returns>
        public static T GetSystemSetting<T>(string methodName, string key)
        {
            return SystemSettings.CallStatic<T>(methodName, ContentResolver, key);
        }

        /// <summary>
        /// Obtains the value for the system property.
        /// It is not recommended to call this method with a high frequency.
        /// This method must be called on the main thread.
        /// </summary>
        /// <typeparam name="T">The type of value.</typeparam>
        /// <param name="methodName">The method name to use, like "getInt", "getBoolean", etc.</param>
        /// <param name="key">The system property key.</param>
        /// <param name="defaultValue">The default value if the key is not found or the value type cannot be cast to type.</param>
        /// <returns>The system property value.</returns>
        public static T GetSystemProperty<T>(string methodName, string key, T defaultValue)
        {
            return SystemProperties.CallStatic<T>(methodName, key, defaultValue);
        }
    }
}
