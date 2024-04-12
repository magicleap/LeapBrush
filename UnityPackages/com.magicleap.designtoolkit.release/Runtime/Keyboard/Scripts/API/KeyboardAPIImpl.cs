// Copyright (c) 2019-present, Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Developer Agreement, located
// here: https://auth.magicleap.com/terms/developer
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.MagicLeap;
using UnityEngine.XR.MagicLeap.Native;

namespace MagicLeap.DesignToolkit.Keyboard
{
    /// <summary>
    /// This class implements a real keyboard api,
    /// which talks to native C/C++ code packaged in .so files
    /// </summary>
    public partial class KeyboardAPIImpl : KeyboardAPIBase
    {
        /// <summary>
        /// Stores the handle to the keyboard C instance.
        /// </summary>
        private static ulong _keyboardHandle = MagicLeapNativeBindings.InvalidHandle;

        public override void Create()
        {
            // Mozc is the Japanese IME we use to translate Hiragana/Katakana to Kanjis
            // Mozc is from Google Inc. See "this keyboard package/Runtime/3rdParty/Mozc"
            // KeyboardAPI.MozcDataPath: the path to the pre-generated Mozc data file.
            // KeyboardAPI.MozcMaxPrimaryResults: maximum number of primary suggestions
            // KeyboardAPI.MozcMaxSecondaryResults: maximum number of secondary suggestions
            MLResult.Code result = NativeBindings.JapaneseIME_Create(
                ref _keyboardHandle,
                KeyboardAPI.MozcDataPath,
                KeyboardAPI.MozcMaxPrimaryResults,
                KeyboardAPI.MozcMaxSecondaryResults);
            if (result != MLResult.Code.Ok)
            {
                Debug.LogError("JapaneseIME Create() failed: " + result);
            }
            else
            {
                Debug.Log("Japanese IME Create() successful");
            }
        }

        public override void Destroy()
        {
            MLResult.Code result = NativeBindings.JapaneseIME_Destroy(_keyboardHandle);
            if (result != MLResult.Code.Ok)
            {
                Debug.LogError("JapaneseIME Destroy() failed: " + result);
            }
        }

        public override List<String> FindPrimaryResults(String query)
        {
            NativeBindings.JapaneseIME_ResultTexts resultTextsNative =
                NativeBindings.JapaneseIME_ResultTexts.Create();
            MLResult.Code result = NativeBindings.JapaneseIME_FindPrimaryResults(
                _keyboardHandle, query, ref resultTextsNative);
            if (result == MLResult.Code.Ok)
            {
                return resultTextsNative.ToUnity();
            }
            else
            {
                Debug.LogError("FindPrimaryResults failed");
                return null;
            }
        }

        public override List<String> FindSecondaryResults()
        {
            NativeBindings.JapaneseIME_ResultTexts resultTextsNative =
                NativeBindings.JapaneseIME_ResultTexts.Create();
            MLResult.Code result = NativeBindings.JapaneseIME_FindSecondaryResults(
                _keyboardHandle, ref resultTextsNative);
            if (result == MLResult.Code.Ok)
            {
                return resultTextsNative.ToUnity();
            }
            else
            {
                Debug.LogError("FindSecondaryResults failed");
                return null;
            }
        }

        public override String SetCurrentCandidate(String candidate)
        {
            NativeBindings.JapaneseIME_ResultText resultTextNative =
                NativeBindings.JapaneseIME_ResultText.Create();
            MLResult.Code result = NativeBindings.JapaneseIME_SetCurrentCandidate(
                _keyboardHandle, candidate, ref resultTextNative);
            if (result == MLResult.Code.Ok)
            {
                return resultTextNative.ToUnity();
            }
            else
            {
                Debug.LogError("SetCurrentCandidate failed");
                return null;
            }
        }

        public override String SelectCandidate(String candidate)
        {
            NativeBindings.JapaneseIME_ResultText resultTextNative =
                NativeBindings.JapaneseIME_ResultText.Create();
            MLResult.Code result = NativeBindings.JapaneseIME_SelectCandidate(
                _keyboardHandle, candidate, ref resultTextNative);
            if (result == MLResult.Code.Ok)
            {
                return resultTextNative.ToUnity();
            }
            else
            {
                Debug.LogError("SelectCandidate failed");
                return null;
            }
        }

        public override String SelectCurrentCandidate()
        {
            NativeBindings.JapaneseIME_ResultText resultTextNative =
                NativeBindings.JapaneseIME_ResultText.Create();
            MLResult.Code result = NativeBindings.JapaneseIME_SelectCurrentCandidate(
                _keyboardHandle, ref resultTextNative);
            if (result == MLResult.Code.Ok)
            {
                return resultTextNative.ToUnity();
            }
            else
            {
                Debug.LogError("SelectCurrentCandidate failed");
                return null;
            }
        }

        public override void AnalyzeContext(String precedingText)
        {
            MLResult.Code result = NativeBindings.JapaneseIME_AnalyzeContext(
                _keyboardHandle, precedingText);
            if (result != MLResult.Code.Ok)
            {
                Debug.LogError("AnalyzeContext failed");
            }
        }
    }
}