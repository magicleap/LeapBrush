// Copyright (c) 2019-present, Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Developer Agreement, located
// here: https://auth.magicleap.com/terms/developer
using System.Collections.Generic;
using System;
using UnityEngine;

namespace MagicLeap.DesignToolkit.Keyboard
{
    /// <summary>
    /// This abstract class is the base of the keyboard api implementations
    /// Currently there is an implementation with fake data called KeyboardAPIFake
    /// and one that calls the real .so libraries called KeyboardAPIImpl
    /// </summary>
    public abstract class KeyboardAPIBase : MonoBehaviour
    {
        public abstract void Create();
        public abstract void Destroy();
        public abstract List<String> FindPrimaryResults(String query);
        public abstract List<String> FindSecondaryResults();
        public abstract String SetCurrentCandidate(String candidate);
        public abstract String SelectCandidate(String candidate);
        public abstract String SelectCurrentCandidate();
        public abstract void AnalyzeContext(String precedingText);
    }
}