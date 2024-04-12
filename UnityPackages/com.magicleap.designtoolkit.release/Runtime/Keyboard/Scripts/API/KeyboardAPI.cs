using System.Collections.Generic;
using System;
using UnityEngine;

namespace MagicLeap.DesignToolkit.Keyboard
{
    /// <summary>
    /// KeyboardAPI class exposes static functions to control the keyboard api
    /// </summary>
    public class KeyboardAPI : MonoBehaviour
    {
        // Mozc is the Japanese IME from Google
        public static String MozcDataPath;
        public static ulong MozcMaxPrimaryResults = 20;
        public static ulong MozcMaxSecondaryResults = 300;
        [SerializeField]
        private bool _useFakeData = false;
        [SerializeField]
        private TextAsset _mozcDataFile;
        private KeyboardAPIBase _apiImpl = null;
        private static KeyboardAPI _instance;
        private static KeyboardAPI Instance
        {
            get { return _instance; }
        }

        public static List<String> FindPrimaryResults(String query)
        {
            return Instance._apiImpl.FindPrimaryResults(query);
        }

        public static List<String> FindSecondaryResults()
        {
            return Instance._apiImpl.FindSecondaryResults();
        }

        public static String SetCurrentCandidate(String candidate)
        {
            return Instance._apiImpl.SetCurrentCandidate(candidate);
        }

        public static String SelectCandidate(String candidate)
        {
            return Instance._apiImpl.SelectCandidate(candidate);
        }

        public static String SelectCurrentCandidate()
        {
            return Instance._apiImpl.SelectCurrentCandidate();
        }

        public static void AnalyzeContext(String precedingText)
        {
            Instance._apiImpl.AnalyzeContext(precedingText);
        }

        private void Awake()
        {
            CopyMozcDataFile();
            _instance = this;
            KeyboardAPIFake fakeApi = GetComponent<KeyboardAPIFake>();
#if UNITY_EDITOR
            // When running from the unity editor or on a non-magicleap device, fake data and the
            // fake api must be used.
            _apiImpl = fakeApi;
            if (!_useFakeData)
            {
                Debug.Log("The value of useFakeData is ignored in the unity editor and " +
                          "non-magicleap configurations.");
            }
#else
        // On a magic-leap device, use a unity property to determine whether to use the fake
        // api and fake data. This allows a special version of the app to be built for
        // side-loading.
        if (_useFakeData)
        {
            _apiImpl = fakeApi;
        }
        else
        {
            _apiImpl = gameObject.AddComponent<KeyboardAPIImpl>();
             Destroy(fakeApi);
        }
#endif
            _apiImpl.Create();
        }

        private void OnDestroy()
        {
            _apiImpl.Destroy();
        }

        private void CopyMozcDataFile()
        {
            MozcDataPath = System.IO.Path.Combine(Application.persistentDataPath, "mozc.data");
            byte[] mozcDataBytes = _mozcDataFile.bytes;
            System.IO.File.WriteAllBytes(MozcDataPath, mozcDataBytes);
        }
    }
}