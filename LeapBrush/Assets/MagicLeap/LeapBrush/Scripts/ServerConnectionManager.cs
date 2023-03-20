using System;
using UnityEngine;

#if !UNITY_EDITOR
using System.IO;
#endif

namespace MagicLeap.LeapBrush
{
    /// <summary>
    /// Manage the url of the server to connect to, and provide an api to start server connections.
    /// </summary>
    public class ServerConnectionManager : MonoBehaviour
    {
        public event Action<string> OnServerUrlChanged;

        public bool ServerEcho => _serverEcho;
        public string MinServerVersion => _minServerVersion;

        public string ServerUrl
        {
            get
            {
                lock (_serverUrlLock)
                {
                    return _serverUrl;
                }
            }
        }

        [SerializeField]
        private string _defaultServerUrl;

        [SerializeField]
        private string _minServerVersion = ".2";

        [SerializeField]
        [Tooltip("Enable to receive back user state from the server")]
        private bool _serverEcho;

        private string _persistentDataPath;

        private object _serverUrlLock = new();
        private string _serverUrl;

        private void Awake()
        {
            _persistentDataPath = Application.persistentDataPath;
        }

        public LeapBrushApiBase.LeapBrushClient Connect(bool drawSolo)
        {
            string serverUrl = ServerUrl;
            Debug.Assert(serverUrl != null);

            if (drawSolo)
            {
                return new LeapBrushApiOnDevice(_persistentDataPath).Connect();
            }

            return new LeapBrushApiCsharpImpl().Connect(serverUrl);
        }

        public void LoadServerUrl()
        {
            string serverUrl = ServerUrl;
            if (!string.IsNullOrEmpty(serverUrl))
            {
                return;
            }

#if !UNITY_EDITOR
            string serverHostPortPrefPath = Path.Join(_persistentDataPath, "serverHostPort.txt");

            try
            {
                using (StreamReader reader = new StreamReader(serverHostPortPrefPath))
                {
                    serverUrl = reader.ReadToEnd().Trim();
                }
            }
            catch (FileNotFoundException)
            {
                Debug.Log(string.Format(
                    "LeapBrush server host:port not configured! Set a url by placing it in the file {0}",
                    serverHostPortPrefPath));
            }
#endif

            if (string.IsNullOrEmpty(serverUrl))
            {
                if (!string.IsNullOrEmpty(_defaultServerUrl))
                {
                    serverUrl = _defaultServerUrl;
                }
                else
                {
                    serverUrl = "localhost:8402";
                }
            }

            lock (_serverUrlLock)
            {
                _serverUrl = serverUrl;
            }

            ThreadDispatcher.ScheduleMain(() =>
            {
                OnServerUrlChanged?.Invoke(serverUrl);
            });
        }

        public void SetServerUrl(string serverUrl)
        {
            if (string.IsNullOrEmpty(serverUrl))
            {
                return;
            }

            lock (_serverUrlLock)
            {
                if (_serverUrl == serverUrl)
                {
                    return;
                }
                _serverUrl = serverUrl;
            }

#if !UNITY_EDITOR
            ThreadDispatcher.ScheduleWork(() =>
            {
                string serverHostPortPath = Path.Join(_persistentDataPath, "serverHostPort.txt");

                try
                {
                    using (StreamWriter writer = new StreamWriter(serverHostPortPath))
                    {
                        writer.Write(serverUrl);
                    }
                }
                catch (IOException e)
                {
                }
            });
#endif

            OnServerUrlChanged?.Invoke(serverUrl);
        }
    }
}