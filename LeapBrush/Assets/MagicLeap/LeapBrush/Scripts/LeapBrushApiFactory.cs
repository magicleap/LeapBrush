namespace MagicLeap.LeapBrush
{
    public class LeapBrushApiFactory
    {
        public static LeapBrushApiBase Create()
        {
#if UNITY_EDITOR || !UNITY_ANDROID
            return new LeapBrushApiCsharpImpl();
#else
            return new LeapBrushApiCppImpl();
#endif
        }
    }
}