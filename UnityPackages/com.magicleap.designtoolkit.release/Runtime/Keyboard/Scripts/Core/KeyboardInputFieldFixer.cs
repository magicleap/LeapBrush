using System.Collections;
using System.Collections.Generic;
using RTLTMPro;
using System.Text.RegularExpressions;
using UnityEngine;

namespace MagicLeap.DesignToolkit.Keyboard
{
    public class KeyboardInputFieldFixer
    {
        #region [Constant] Private Members
        private const bool FARSI = true;
        private const bool PRESERVE_NUMBERS = false;
        private const bool FIX_TAGS = false;
        private const char ZERO_WIDTH_CHAR = '\u200B';
        private const string TAG_REGEX_PATTERN =
            "[<]([\\/][a-zA-Z]{1,}|[a-zA-Z]{1,})(\\s\\S{1,}[=]\\S{1,}){0,}[>]";
        private const int MAX_TOP_ACCENT_CHAR_COUNT = 3;
        private const int MAX_BOTTOM_ACCENT_CHAR_COUNT = 2;
        #endregion [Constant] Private Members

        #region Private Members
        private string _originalText = "";
        private string _modifiedText = "";
        private bool _isRightToLeftText = false;
        private RTLTextPointSetter _textPointSetter = new RTLTextPointSetter();
        private List<RTLTextPoint> _rtlTextPointList = new List<RTLTextPoint>();
        private Dictionary<int, TagStringData> _indexTagDictionary =
            new Dictionary<int, TagStringData>();
        private Dictionary<int, int> _tagIndexOffsetsDictionary = new Dictionary<int, int>();
        private readonly FastStringBuilder _rtlStringBuilder =
            new FastStringBuilder(RTLSupport.DefaultBufferSize);
        private readonly FastStringBuilder _stringBuilder =
            new FastStringBuilder(RTLSupport.DefaultBufferSize);
        private Dictionary<char, int> _accentCountDictionary = new Dictionary<char, int>()
        {
            [(char)TashkeelCharacters.Shadda] = 0,
            [(char)TashkeelCharacters.Fathan] = 0,
            [(char)TashkeelCharacters.Fatha] = 0,
            [(char)TashkeelCharacters.Damma] = 0,
            [(char)TashkeelCharacters.Dammatan] = 0,
            [(char)TashkeelCharacters.Sukun] = 0,
            [(char)TashkeelCharacters.MaddahAbove] = 0,
            [(char)TashkeelCharacters.SuperscriptAlef] = 0
        };
        #endregion Private Members

        #region Public Methods
        /// <summary>
        /// Fixes text so that it can be displayed properly.
        /// </summary>
        /// <param name="newText">the text to fixed</param>
        /// <param name="isRightToLeftText">whether or the text should be fixed in
        /// RTL text flow</param>
        /// <returns>the modified text</returns>
        public string FixText(string newText, bool isRightToLeftText)
        {
            if (_isRightToLeftText == isRightToLeftText &&
                newText == _originalText)
            {
                return _modifiedText;
            }

            _originalText = newText;
            _isRightToLeftText = isRightToLeftText;
            UpdateText();

            return _modifiedText;
        }

        /// <summary>
        /// Returns the original text passed in to the text mesh pro component.
        /// </summary>
        /// <returns>the original, unmodified string</returns>
        public string GetUnModifiedText()
        {
            return _originalText;
        }

        /// <summary>
        /// Returns the resulting processed text generated from FixText()
        /// </summary>
        /// <returns>the modified text generated from FixText()</returns>
        public string GetModifiedText()
        {
            return _modifiedText;
        }
        #endregion Public Methods

        #region Private Methods
        /// <summary>
        /// Responsible for modifying text to be displayed properly on the Text Mesh Pro
        /// component
        /// </summary>
        private void UpdateText()
        {
            GenerateTagDictionary();

            SetRTLTextPoints();

            SetRichTextAltText();

            // If the string is empty or just containing whitespace chars, there's no reason to
            // process it further. Therefore, we can just set the display text to equal to be
            // the original text and exit the method.
            if (_rtlTextPointList.Count == 0)
            {
                _modifiedText = _originalText;
                return;
            }

            _modifiedText = "";
            for (int i = 0; i < _rtlTextPointList.Count; i++)
            {
                int startIndex = _rtlTextPointList[i].StartIndex;
                int endIndex = i + 1 < _rtlTextPointList.Count ?
                                _rtlTextPointList[i + 1].StartIndex - 1 :
                                _originalText.Length - 1;

                if (_rtlTextPointList[i].IsRTL)
                {
                    _modifiedText += FixRTLText(startIndex, endIndex);
                }
                else
                {
                    _modifiedText += FixLTRText(startIndex, endIndex);
                }
            }
        }

        /// <summary>
        /// Helper for grouping character in _originalText together based on text flow
        /// </summary>
        private void SetRTLTextPoints()
        {
            _rtlTextPointList.Clear();
            _textPointSetter.IsRightToLeftText = _isRightToLeftText;
            _textPointSetter.SetRTLTextPoints(
                _originalText, _rtlTextPointList, _indexTagDictionary, _tagIndexOffsetsDictionary);
        }

        /// <summary>
        /// Caches info for Rich Text Tags in the text
        /// </summary>
        private void GenerateTagDictionary()
        {
            _indexTagDictionary.Clear();
            _tagIndexOffsetsDictionary.Clear();
            MatchCollection matches = Regex.Matches(_originalText, TAG_REGEX_PATTERN);

            // Cache indexes of tags as they appear in the typed text and right tag that should
            // be placed when typing based on the current text flow.
            foreach (Match match in matches)
            {
                int index = match.Index;
                TagStringData tagData = new TagStringData()
                {
                    Str = match.ToString(),
                    AltStr = match.ToString()
                };
                _indexTagDictionary.Add(index, tagData);
                _tagIndexOffsetsDictionary.Add(index, match.Length - 1);
            }
        }

        /// <summary>
        /// Returns true if the character is a Tashkeel accent
        /// </summary>
        /// <param name="ch">the character to check</param>
        /// <returns>whether or not ch was a Tashkeel accent</returns>
        private bool IsTashkeelAccent(char ch)
        {
            if (char.IsNumber(ch))
            {
                return false;
            }

            char fathan = (char)TashkeelCharacters.Fathan;
            char damma = (char)TashkeelCharacters.Damma;
            char kasra = (char)TashkeelCharacters.Kasra;
            char maddahAbove = (char)TashkeelCharacters.MaddahAbove;
            char superscriptAlef = (char)TashkeelCharacters.SuperscriptAlef;
            char dammatanIsolated = (char)TashkeelCharacters.ShaddaWithDammatanIsolatedForm;
            char superscriptAlefIsolated =
                (char)TashkeelCharacters.ShaddaWithSuperscriptAlefIsolatedForm;

            return (ch >= fathan && ch <= damma) ||
                   (ch >= kasra && ch <= maddahAbove) ||
                   ch == superscriptAlef ||
                   (ch >= dammatanIsolated && ch <= superscriptAlefIsolated);
        }

        /// <summary>
        /// Sets the AltStr text in TagStringDatas
        /// </summary>
        private void SetRichTextAltText()
        {
            if (_rtlTextPointList.Count < 1)
            {
                return;
            }

            Stack<int> rtlTagStack = new Stack<int>();

            int textPointIndex = 0;
            RTLTextPoint textPoint = _rtlTextPointList[textPointIndex];
            int endIndex = textPointIndex + 1 < _rtlTextPointList.Count ?
                                _rtlTextPointList[textPointIndex + 1].StartIndex - 1 :
                                _originalText.Length - 1;

            foreach (int tagIndex in _indexTagDictionary.Keys)
            {
                TagStringData tagData = _indexTagDictionary[tagIndex];

                while (tagIndex > endIndex)
                {
                    textPointIndex++;
                    textPoint = _rtlTextPointList[textPointIndex];
                    endIndex = textPointIndex + 1 < _rtlTextPointList.Count ?
                                _rtlTextPointList[textPointIndex + 1].StartIndex - 1 :
                                _originalText.Length - 1;
                }

                // if we encounter a closing tag...
                if (tagData.Str[1] == '/')
                {
                    // check to see if the top tag on the rtl tag stack was the opening tag for it.
                    string openVariant = tagData.Str[0] + tagData.Str.Substring(2);
                    if (rtlTagStack.Count != 0 &&
                        _indexTagDictionary[rtlTagStack.Peek()].Str == openVariant &&
                        textPoint.IsRTL != _isRightToLeftText)
                    {
                        int swapIdx = rtlTagStack.Pop();
                        _indexTagDictionary[swapIdx].AltStr = tagData.Str;
                        _indexTagDictionary[tagIndex].AltStr = openVariant;
                    }
                }
                else if (textPoint.IsRTL != _isRightToLeftText)
                {
                    // Place tag on the top of the rtl tag stack if otherwise
                    rtlTagStack.Push(tagIndex);
                }
            }
        }

        /// <summary>
        /// Adjust substring of text to be displayed properly in RTL
        /// </summary>
        /// <param name="startIndex">the index of the first character in _originalText
        /// for the substring</param>
        /// <param name="endIndex">the index of the first character in _originalText
        /// for the substring</param>
        /// <returns>the substring modified to displayed properly in RTL</returns>
        private string FixRTLText(int startIndex, int endIndex)
        {
            _stringBuilder.Clear();
            string textSubstr = _originalText.Substring(startIndex, (endIndex - startIndex) + 1);

            for (int i = 0; i < textSubstr.Length; i++)
            {
                char ch = textSubstr[i];

                if (_indexTagDictionary.ContainsKey(startIndex + i))
                {
                    string textSoFar = _stringBuilder.ToString();
                    _stringBuilder.SetValue(
                        _isRightToLeftText ?
                            textSoFar + _indexTagDictionary[startIndex + i].Str :
                            _indexTagDictionary[startIndex + i].AltStr + textSoFar);
                    i += _tagIndexOffsetsDictionary[startIndex + i];
                    continue;
                }

                if (TextUtils.IsRTLCharacter(ch) && (char.IsLetter(ch) || IsTashkeelAccent(ch)))
                {
                    int j = i + 1;
                    while (j < textSubstr.Length &&
                           TextUtils.IsRTLCharacter(textSubstr[j]) &&
                           (char.IsLetter(textSubstr[j]) || IsTashkeelAccent(textSubstr[j])))
                    {
                        j++;
                    }
                    j--;

                    if (j <= i)
                    {
                        if (_isRightToLeftText)
                        {
                            _stringBuilder.Append(ch);
                        }
                        else
                        {
                            _stringBuilder.Insert(0, ch);
                        }
                        continue;
                    }

                    string textToFix = textSubstr.Substring(i, (j - i) + 1);

                    _rtlStringBuilder.SetValue(textToFix);
                    TashkeelFixer.RemoveTashkeel(_rtlStringBuilder);
                    string textToFixWithoutTashkeel = _rtlStringBuilder.ToString();
                    _rtlStringBuilder.Clear();
                    GlyphFixer.Fix(textToFixWithoutTashkeel,
                                   _rtlStringBuilder,
                                   PRESERVE_NUMBERS,
                                   FARSI,
                                   FIX_TAGS);
                    TashkeelFixer.RestoreTashkeel(_rtlStringBuilder);
                    string fixedLigatureText = _rtlStringBuilder.ToString();
                    _rtlStringBuilder.Clear();
                    LigatureFixer.Fix(fixedLigatureText, _rtlStringBuilder, FARSI, FIX_TAGS, PRESERVE_NUMBERS);

                    FixArabicAccents();

                    AdjustArabicAccentForLTR();

                    _stringBuilder.SetValue(
                        _isRightToLeftText ?
                            _stringBuilder.ToString() + _rtlStringBuilder.ToString() :
                            _rtlStringBuilder.ToString() + _stringBuilder.ToString());
                    i = j;
                }
                else if (char.IsPunctuation(ch))
                {
                    if (_isRightToLeftText)
                    {
                        _stringBuilder.Append(ch);
                    }
                    else
                    {
                        _stringBuilder.Insert(0, ch);
                    }
                }
                else if (char.IsWhiteSpace(ch) || char.IsSymbol(ch) || ch == ZERO_WIDTH_CHAR)
                {
                    if (_isRightToLeftText)
                    {
                        _stringBuilder.Append(ch);
                    }
                    else
                    {
                        if (i + 1 < textSubstr.Length &&
                        (ch == '<' && textSubstr[i + 1] == ZERO_WIDTH_CHAR) ||
                        (ch == ZERO_WIDTH_CHAR && textSubstr[i + 1] == '>'))
                        {
                            _stringBuilder.SetValue(
                                textSubstr.Substring(i, 2) + _stringBuilder.ToString());
                            i++;
                        }
                        else
                        {
                            _stringBuilder.Insert(0, ch);
                        }
                    }
                }
                else
                {
                    int ltrCharOffsetIndex = 1;
                    if (_isRightToLeftText)
                    {
                        _stringBuilder.Append(ch);
                        ltrCharOffsetIndex = _stringBuilder.Length - 1;
                    }
                    else
                    {
                        _stringBuilder.Insert(0, ch);
                    }

                    int j = i + 1;

                    while (j < textSubstr.Length &&
                           (char.IsNumber(textSubstr[j]) ||
                           _indexTagDictionary.ContainsKey(startIndex + j)))
                    {
                        if (_indexTagDictionary.ContainsKey(startIndex + j))
                        {
                            string prevStr = _stringBuilder.ToString();
                            string tagText = _isRightToLeftText ?
                                                _indexTagDictionary[startIndex + j].AltStr :
                                                _indexTagDictionary[startIndex + j].Str;
                            _stringBuilder.SetValue(prevStr.Substring(0, ltrCharOffsetIndex) +
                                                    tagText +
                                                    prevStr.Substring(ltrCharOffsetIndex));
                            if (!_isRightToLeftText)
                            {
                                ltrCharOffsetIndex += tagText.Length;
                            }
                            j += _tagIndexOffsetsDictionary[startIndex + j] + 1;
                            continue;
                        }

                        _stringBuilder.Insert(ltrCharOffsetIndex, textSubstr[j]);
                        if (!_isRightToLeftText)
                        {
                            ltrCharOffsetIndex++;
                        }
                        j++;
                    }
                    j--;

                    i = j;
                }
            }

            if (!_isRightToLeftText)
            {
                FixWhiteSpace(_stringBuilder.ToString());
            }

            return _stringBuilder.ToString();
        }

        /// <summary>
        /// Adjust substring of text to be displayed properly in LTR
        /// </summary>
        /// <param name="startIndex">the index of the first character in _originalText
        /// for the substring</param>
        /// <param name="endIndex">the index of the first character in _originalText
        /// for the substring</param>
        /// <returns>the substring modified to displayed properly in LTR</returns>
        private string FixLTRText(int startIndex, int endIndex)
        {
            _stringBuilder.Clear();
            string textSubstr = _originalText.Substring(startIndex, (endIndex - startIndex) + 1);

            for (int i = 0; i < textSubstr.Length; i++)
            {
                char ch = textSubstr[i];

                if (_indexTagDictionary.ContainsKey(startIndex + i))
                {
                    string textSoFar = _stringBuilder.ToString();
                    _stringBuilder.SetValue(
                        !_isRightToLeftText ?
                            textSoFar + _indexTagDictionary[startIndex + i].Str :
                            _indexTagDictionary[startIndex + i].AltStr + textSoFar);
                    i += _tagIndexOffsetsDictionary[startIndex + i];
                    continue;
                }

                if (!_isRightToLeftText)
                {
                    _stringBuilder.Append(ch);
                }
                else
                {
                    if (i + 1 < textSubstr.Length &&
                        (ch == '<' && textSubstr[i + 1] == ZERO_WIDTH_CHAR) ||
                        (ch == ZERO_WIDTH_CHAR && textSubstr[i + 1] == '>'))
                    {
                        _stringBuilder.SetValue(
                            textSubstr.Substring(i, 2) + _stringBuilder.ToString());
                        i++;
                    }
                    else
                    {
                        _stringBuilder.Insert(0, ch);
                    }
                }
            }

            if (_isRightToLeftText)
            {
                FixWhiteSpace(_stringBuilder.ToString());
            }

            return _stringBuilder.ToString();
        }

        /// <summary>
        /// Swaps placement of whitespace on the outer edges of the text.
        /// </summary>
        /// <param name="text">the text that needs to fix the placement of whitespace on</param>
        private void FixWhiteSpace(string text)
        {
            int whitespaceOnRightCount = 0;
            int whitespaceOnLeftCount = 0;
            for (int i = text.Length - 1; i >= 0 && char.IsWhiteSpace(text[i]); i--)
            {
                whitespaceOnRightCount++;
            }
            int lengthWithNoSpacesOnRight = text.Length - whitespaceOnRightCount;
            for (int i = 0; i < lengthWithNoSpacesOnRight && char.IsWhiteSpace(text[i]); i++)
            {
                whitespaceOnLeftCount++;
            }

            if (whitespaceOnRightCount + whitespaceOnLeftCount > 0 &&
                whitespaceOnRightCount + whitespaceOnLeftCount < text.Length)
            {
                int lengthWithNoOuterSpaces =
                    text.Length - (whitespaceOnRightCount + whitespaceOnLeftCount);
                _stringBuilder.SetValue(
                    text.Substring(text.Length - whitespaceOnRightCount) +
                    text.Substring(whitespaceOnLeftCount, lengthWithNoOuterSpaces) +
                    text.Substring(0, whitespaceOnLeftCount));
            }
        }

        private void FixArabicAccents()
        {
            bool kIsAccent = false;
            bool foundAccent = false;
            int accentDictionarySize = 0;
            string lowerAccentStr = "";
            string remainingCharsStr = "";
            string accentString = "";
            char lastLowerAccentChar = '\0';
            int numOfDisplayedTopAccents = 0;
            int numOfDisplayedBottomAccents = 0;
            for (int k = 0; k < _rtlStringBuilder.Length; k++)
            {
                char charAtK = (char)_rtlStringBuilder.Get(k);
                kIsAccent = IsTashkeelAccent(charAtK);
                if (!kIsAccent && !foundAccent)
                {
                    continue;
                }

                foundAccent = kIsAccent || foundAccent;

                if (kIsAccent)
                {
                    switch (charAtK)
                    {
                        case (char)TashkeelCharacters.Shadda:
                        case (char)TashkeelCharacters.Fathan:
                        case (char)TashkeelCharacters.Fatha:
                        case (char)TashkeelCharacters.Damma:
                        case (char)TashkeelCharacters.Dammatan:
                        case (char)TashkeelCharacters.Sukun:
                        case (char)TashkeelCharacters.MaddahAbove:
                        case (char)TashkeelCharacters.SuperscriptAlef:
                            if (numOfDisplayedTopAccents < MAX_TOP_ACCENT_CHAR_COUNT)
                            {
                                numOfDisplayedTopAccents++;
                            }
                            _accentCountDictionary[charAtK]++;
                            accentDictionarySize++;
                            break;
                        case (char)TashkeelCharacters.Kasratan:
                            if (numOfDisplayedBottomAccents < MAX_BOTTOM_ACCENT_CHAR_COUNT)
                            {
                                numOfDisplayedBottomAccents++;
                            }
                            lastLowerAccentChar = charAtK;
                            lowerAccentStr += charAtK;
                            break;
                        case (char)TashkeelCharacters.Kasra:
                            if (numOfDisplayedBottomAccents < MAX_BOTTOM_ACCENT_CHAR_COUNT)
                            {
                                numOfDisplayedBottomAccents++;
                            }
                            lastLowerAccentChar = charAtK;
                            if (lowerAccentStr.Length == 0)
                            {
                                lowerAccentStr += charAtK;
                            }
                            else
                            {
                                lowerAccentStr = "" + charAtK + lowerAccentStr;
                            }
                            break;
                        default:
                            if (numOfDisplayedTopAccents < MAX_TOP_ACCENT_CHAR_COUNT)
                            {
                                numOfDisplayedTopAccents++;
                            }
                            remainingCharsStr = "" + charAtK + remainingCharsStr;
                            break;
                    }
                }

                if (kIsAccent && k != _rtlStringBuilder.Length - 1)
                {
                    continue;
                }

                if (accentDictionarySize == 1 &&
                    _accentCountDictionary[(char)TashkeelCharacters.Shadda] == 1 &&
                    remainingCharsStr.Length == 0)
                {
                    if (lowerAccentStr.Length > 0)
                    {
                        if (lastLowerAccentChar == (char)TashkeelCharacters.Kasra)
                        {
                            lowerAccentStr = lowerAccentStr.Substring(1) + lastLowerAccentChar;
                        }
                        accentString = "" +
                                       lowerAccentStr +
                                       (char)TashkeelCharacters.Shadda;
                        numOfDisplayedBottomAccents++;
                    }
                    else
                    {
                        accentString = "" + (char)TashkeelCharacters.Shadda;
                    }
                    _accentCountDictionary[(char)TashkeelCharacters.Shadda] = 0;
                }
                else
                {
                    accentString = lowerAccentStr;
                    accentString += remainingCharsStr;
                    accentString += BuildAccentString((char)TashkeelCharacters.MaddahAbove);
                    accentString += BuildAccentString((char)TashkeelCharacters.SuperscriptAlef);
                    accentString += BuildAccentString((char)TashkeelCharacters.Sukun);
                    accentString += BuildAccentString((char)TashkeelCharacters.Damma);
                    accentString += BuildAccentString((char)TashkeelCharacters.Fatha);
                    accentString += BuildAccentString((char)TashkeelCharacters.Dammatan);
                    accentString += BuildAccentString((char)TashkeelCharacters.Fathan);
                    accentString += BuildAccentString((char)TashkeelCharacters.Shadda);
                }

                int numOfBottomCharsToEdit =
                    Mathf.Min(lowerAccentStr.Length, numOfDisplayedBottomAccents);
                int bottomIndex = 0;
                while (bottomIndex < numOfBottomCharsToEdit)
                {
                    _rtlStringBuilder.Set(kIsAccent ?
                                            (k - (accentString.Length - 1)) + bottomIndex :
                                            (k - accentString.Length) + bottomIndex,
                                          accentString[(lowerAccentStr.Length -
                                                       numOfBottomCharsToEdit) +
                                                       bottomIndex]);
                    bottomIndex++;
                }

                int numOfTopCharsToEdit =
                    Mathf.Min(accentString.Length - numOfBottomCharsToEdit,
                              numOfDisplayedTopAccents);
                int topIndex = accentString.Length - 1;
                while (topIndex > (accentString.Length - 1) - numOfTopCharsToEdit)
                {
                    _rtlStringBuilder.Set(kIsAccent ?
                                            (k - (accentString.Length - 1)) + topIndex :
                                            (k - accentString.Length) + topIndex,
                                          accentString[topIndex]);
                    topIndex--;
                }

                int displayedChars = numOfTopCharsToEdit + numOfBottomCharsToEdit;
                _rtlStringBuilder.Remove(kIsAccent ?
                                            (k - (accentString.Length - 1)) + numOfBottomCharsToEdit :
                                            (k - accentString.Length) + numOfBottomCharsToEdit,
                                         accentString.Length - displayedChars);
                k -= accentString.Length - displayedChars;

                lowerAccentStr = "";
                remainingCharsStr = "";
                accentDictionarySize = 0;
                lastLowerAccentChar = '\0';
                foundAccent = false;
                numOfDisplayedTopAccents = 0;
                numOfDisplayedBottomAccents = 0;
            }

            _rtlStringBuilder.Reverse();
        }

        private string BuildAccentString(char ch)
        {
            if (!_accentCountDictionary.ContainsKey(ch))
            {
                Debug.LogError("ERROR: did accentCountDictionary did not contain char");
                return "";
            }

            string result = "";
            while (_accentCountDictionary[ch] > 0)
            {
                result += ch;
                _accentCountDictionary[ch]--;
            }

            return result;
        }

        private void AdjustArabicAccentForLTR()
        {
            if (_isRightToLeftText)
            {
                return;
            }

            _rtlStringBuilder.Reverse();

            int flipAccentsEndIdx = _rtlStringBuilder.Length - 1;
            bool foundAccentChar = false;
            bool kIsAnAccentChar = false;
            for (int k = _rtlStringBuilder.Length - 1; k >= 0; k--)
            {
                kIsAnAccentChar = IsTashkeelAccent((char)_rtlStringBuilder.Get(k));

                if (!kIsAnAccentChar && !foundAccentChar)
                {
                    flipAccentsEndIdx = k;
                    continue;
                }

                foundAccentChar = kIsAnAccentChar || foundAccentChar;

                if (kIsAnAccentChar && k != 0)
                {
                    continue;
                }

                int lengthOfCharsToFlip = (flipAccentsEndIdx - k) +
                                          (kIsAnAccentChar ? 1 : 0);

                _rtlStringBuilder.Reverse(k + (kIsAnAccentChar ? 0 : 1),
                                          lengthOfCharsToFlip);

                flipAccentsEndIdx = k;
                foundAccentChar = false;
            }
        }
        #endregion Private Methods
    }
}
