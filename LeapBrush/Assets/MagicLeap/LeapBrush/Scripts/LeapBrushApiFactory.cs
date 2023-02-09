namespace MagicLeap.LeapBrush
{
    public class LeapBrushApiFactory
    {
        public LeapBrushApiBase.LeapBrushClient Connect(string serverUrl, bool drawSolo, string persistentDataPath)
        {
            if (drawSolo)
            {
                return new LeapBrushApiOnDevice(persistentDataPath).Connect();
            }

#if UNITY_EDITOR || !UNITY_ANDROID
            return new LeapBrushApiCsharpImpl().Connect(serverUrl);
#else
            return new LeapBrushApiCppImpl().Connect(serverUrl);
#endif
        }
    }
}