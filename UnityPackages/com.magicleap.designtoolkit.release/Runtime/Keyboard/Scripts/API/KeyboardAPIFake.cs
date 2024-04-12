// Copyright (c) 2019-present, Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Developer Agreement, located
// here: https://auth.magicleap.com/terms/developer
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MagicLeap.DesignToolkit.Keyboard
{
    /// <summary>
    /// This class implements a fake version of the keyboard api
    /// </summary>
    public class KeyboardAPIFake : KeyboardAPIBase
    {
        [SerializeField]
        private List<String> _primaryResults;
        [SerializeField]
        private List<String> _secondaryResults;
        [SerializeField]
        private String _setCurrentCandidateResult;
        [SerializeField]
        private String _selectCandidateResult;
        [SerializeField]
        private String _selectCurrentCandidateResult;

        public override void Create()
        {
        }

        public override void Destroy()
        {
        }

        public override List<String> FindPrimaryResults(String query)
        {
            return _primaryResults;
        }

        public override List<String> FindSecondaryResults()
        {
            return _secondaryResults;
        }

        public override String SetCurrentCandidate(String candidate)
        {
            return _setCurrentCandidateResult;
        }

        public override String SelectCandidate(String candidate)
        {
            return _selectCandidateResult;
        }

        public override String SelectCurrentCandidate()
        {
            return _selectCurrentCandidateResult;
        }

        public override void AnalyzeContext(String precedingText)
        {
        }
    }
}