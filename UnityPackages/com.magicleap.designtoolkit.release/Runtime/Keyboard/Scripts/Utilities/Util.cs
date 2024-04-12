// Copyright (c) 2022 Magic Leap, Inc. All Rights Reserved.
// Please see the top-level LICENSE.md in this distribution
// for terms and conditions governing this file.
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System;
using UnityEngine;
using TMPro;

namespace MagicLeap.Keyboard
{
    public static class Util
    {
        public static void PrintDebugInfo(TMP_Text debugInfo, string text)
        {
            if (!debugInfo)
            {
                return;
            }
            debugInfo.text += text;
            if (debugInfo.text.Length > 500)
            {
                debugInfo.text = "";
            }
        }

        public static String ConcatStrs(List<String> strs)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (String str in strs)
            {
                stringBuilder.Append(str);
                stringBuilder.Append(", ");
            }
            return stringBuilder.ToString();
        }
    }
}