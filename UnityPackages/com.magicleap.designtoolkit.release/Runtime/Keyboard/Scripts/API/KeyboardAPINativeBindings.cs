using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.XR.MagicLeap;
using UnityEngine.XR.MagicLeap.Native;

namespace MagicLeap.DesignToolkit.Keyboard
{
    /// <summary>
    /// This class defines the C# interface to the C functions/structures,
    /// that are included in the .so files
    /// </summary>
    public partial class KeyboardAPIImpl
    {
        private class NativeBindings : MagicLeapNativeBindings
        {
            [StructLayout(LayoutKind.Sequential)]
            public struct JapaneseIME_ResultText
            {
                [MarshalAs(UnmanagedType.LPStr)]
                public string Content;

                public static JapaneseIME_ResultText Create()
                {
                    return new JapaneseIME_ResultText()
                    {
                        Content = ""
                    };
                }

                public String ToUnity()
                {
                    return Content;
                }
            };

            [StructLayout(LayoutKind.Sequential)]
            public struct JapaneseIME_ResultTexts
            {
                public IntPtr ResultTexts;
                public ulong Count;

                public static JapaneseIME_ResultTexts Create()
                {
                    return new JapaneseIME_ResultTexts()
                    {
                        ResultTexts = IntPtr.Zero,
                        Count = 0
                    };
                }

                public List<String> ToUnity()
                {
                    List<String> results = new List<string>();
                    IntPtr resultTextsArr = ResultTexts;
                    for (ulong i = 0; i < Count; i++)
                    {
                        JapaneseIME_ResultText resultTextNative =
                            Marshal.PtrToStructure<JapaneseIME_ResultText>(resultTextsArr);
                        results.Add(resultTextNative.ToUnity());
                        resultTextsArr += Marshal.SizeOf<JapaneseIME_ResultText>();
                    }
                    return results;
                }
            };

            [DllImport("japaneseime_unity", CallingConvention = CallingConvention.Cdecl)]
            public static extern MLResult.Code JapaneseIME_Create(
                ref ulong outHandle,
                [MarshalAs(UnmanagedType.LPStr)] string dataPath,
                ulong maxPrimaryResults,
                ulong maxSecondaryResults
            );

            [DllImport("japaneseime_unity", CallingConvention = CallingConvention.Cdecl)]
            public static extern MLResult.Code JapaneseIME_Destroy(ulong handle);

            [DllImport("japaneseime_unity", CallingConvention = CallingConvention.Cdecl)]
            public static extern MLResult.Code JapaneseIME_FindPrimaryResults(
                ulong handle,
                [MarshalAs(UnmanagedType.LPStr)] string query,
                ref JapaneseIME_ResultTexts resultTexts);

            [DllImport("japaneseime_unity", CallingConvention = CallingConvention.Cdecl)]
            public static extern MLResult.Code JapaneseIME_FindSecondaryResults(
                ulong handle, ref JapaneseIME_ResultTexts resultTexts);

            [DllImport("japaneseime_unity", CallingConvention = CallingConvention.Cdecl)]
            public static extern MLResult.Code JapaneseIME_SetCurrentCandidate(
                ulong handle,
                [MarshalAs(UnmanagedType.LPStr)] string candidate,
                ref JapaneseIME_ResultText outResult);

            [DllImport("japaneseime_unity", CallingConvention = CallingConvention.Cdecl)]
            public static extern MLResult.Code JapaneseIME_SelectCandidate(
                ulong handle,
                [MarshalAs(UnmanagedType.LPStr)] string candidate,
                ref JapaneseIME_ResultText outResult);

            [DllImport("japaneseime_unity", CallingConvention = CallingConvention.Cdecl)]
            public static extern MLResult.Code JapaneseIME_SelectCurrentCandidate(
                ulong handle, ref JapaneseIME_ResultText outResult);

            [DllImport("japaneseime_unity", CallingConvention = CallingConvention.Cdecl)]
            public static extern MLResult.Code JapaneseIME_AnalyzeContext(
                ulong handle, [MarshalAs(UnmanagedType.LPStr)] string precedingText);
        }
    }
}