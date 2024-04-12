using RTLTMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MagicLeap.DesignToolkit.Keyboard
{
    public class RTLTextPointSetter
    {
        #region [Constant] Private Members
        private const char ZERO_WIDTH_CHAR = '\u200B';
        #endregion [Constant] Private Members

        #region Public Members
        public bool IsRightToLeftText = false;
        #endregion Public Members

        #region Public Methods
        /// <summary>
        /// Responsible for breaking the typed text into groups based on common text flow
        /// </summary>
        /// <param name="originalText">The text to break up into groups of characters</param>
        /// <param name="rtlTextPointList">The list of RTLTextPoints to update</param>
        /// <param name="tagIdxDictionary">A Dictionary containing the starting indexes of tags in originalText</param>
        /// <param name="tagOffsetsDictionary">A Dictionary containing the lengths of a tag at a index in originalText</param>
        public void SetRTLTextPoints(string originalText,
                                      List<RTLTextPoint> rtlTextPointList,
                                      Dictionary<int, TagStringData> tagIdxDictionary,
                                      Dictionary<int, int> tagOffsetsDictionary)
        {
            // The index of the first space char we encounter after processing a non-space char.
            int firstSpaceIdx = -1;

            // The index of the first number char we encounter after processing a non-number char.
            int firstNumberIdx = -1;

            // The type of the last character that was typed.
            CharType lastCharType = 0;

            // Whether or not to write LTR numbers as RTL numbers.
            bool writeNumbersRTL = false;

            // The number of chars from the beginning of a tag to the end of a tag.
            // (used to help group tags with the correct text flow group)
            int tagOffset = 0;
            for (int i = 0; i < originalText.Length; i++)
            {
                // If the char at index i is a tag, find the first non tag character.
                tagOffset = 0;
                while (tagIdxDictionary.ContainsKey(i + tagOffset))
                {
                    if (originalText[i + tagOffset + 1] != '/')
                    {
                        tagOffset += tagOffsetsDictionary[i + tagOffset] + 1;
                    }
                    else
                    {
                        i = i + tagOffset + tagOffsetsDictionary[i + tagOffset] + 1;
                        tagOffset = 0;
                    }
                }

                // Exit loop if there are no more characters left to process.
                if (i + tagOffset >= originalText.Length)
                {
                    break;
                }

                char ch = originalText[i + tagOffset];

                CharType charType = GetCharType(ch);

                // If we haven't placed any RTLTextpoints yet...
                RTLTextPoint newTextPoint;
                if (rtlTextPointList.Count == 0)
                {
                    // Skips spaces but stash the index of the first one we encounter.
                    if ((charType & CharType.SPACE) != 0)
                    {
                        firstSpaceIdx = firstSpaceIdx == -1 ? i : firstSpaceIdx;
                        i = i + tagOffset;
                        lastCharType = charType;
                        continue;
                    }

                    // Place a new RTLTextPoint and update variables.
                    newTextPoint = new RTLTextPoint()
                    {
                        StartIndex = firstSpaceIdx != -1 ? firstSpaceIdx : i,
                        CharType = charType
                    };
                    rtlTextPointList.Add(newTextPoint);

                    firstNumberIdx =
                        SetFirstNumberIndex(charType, newTextPoint.StartIndex, firstNumberIdx);

                    i = i + tagOffset;
                    firstSpaceIdx = -1;
                    writeNumbersRTL = newTextPoint.IsRTL;
                    lastCharType = charType;
                    continue;
                }

                // If we have placed a RTLTextpoints already...
                RTLTextPoint textPoint = rtlTextPointList[rtlTextPointList.Count - 1];

                // Skips spaces but stash the index of the first one we encounter after processing
                // a non-space character.
                if ((charType & CharType.SPACE) != 0)
                {
                    firstSpaceIdx = firstSpaceIdx == -1 ? i : firstSpaceIdx;
                    i = i + tagOffset;
                    firstNumberIdx = -1;
                    lastCharType = charType;
                    continue;
                }

                // Don't place punctuation marks and symbols in the opposite text flow if they're
                // bewtwen two chars that are in the opposite text flow if we began the string
                // typing in RTL and we just typed an RTL punctuation mark.
                if (IsRightToLeftText &&
                    (charType & CharType.RTL_PUNCTUATION) != 0 &&
                    (textPoint.CharType & (CharType.PUNCTUATION | CharType.SYMBOL)) != 0)
                {
                    textPoint.CharType = charType;
                    i = i + tagOffset;
                    firstNumberIdx = -1;
                    firstSpaceIdx = -1;
                    lastCharType = charType;
                    continue;
                }

                // Place punctuation marks and symbols to the opposite text flow if they're
                // between two chars that are in the opposite text flow.
                // (ie. if we are typing in RTL and the user types a sequence of symbols and
                // punctuation marks in between two LTR chars, the symbols and punctuation marks
                // should be processed as LTR instead of RTL)
                if (CanFlipPunctuation(rtlTextPointList, textPoint, charType))
                {
                    rtlTextPointList.Remove(textPoint);
                    textPoint = rtlTextPointList[rtlTextPointList.Count - 1];
                    writeNumbersRTL = WriteNumbersRTL(charType, writeNumbersRTL);
                }

                if ((charType & CharType.NUMBER) != 0)
                {
                    // Process all LTR numbers as RTL Numbers if it comes after a non-number, RTL char.
                    // Also, always group rtl numbers together with the previous textpoint if the last
                    // character in the group was a number or letter when we started typing in RTL.
                    if ((!CharTypeIsRTL(charType) && writeNumbersRTL) ||
                        (IsRightToLeftText && CharTypeIsRTL(charType) && firstSpaceIdx == -1 &&
                        (lastCharType & (CharType.LETTER | CharType.NUMBER)) != 0))
                    {
                        firstNumberIdx =
                            SetFirstNumberIndex(charType,
                                                firstSpaceIdx != -1 ? firstSpaceIdx : i,
                                                firstNumberIdx);
                        i = i + tagOffset;
                        firstSpaceIdx = -1;
                        lastCharType = charType;
                        continue;
                    }
                }

                // Regroups a sequence of RTL chars that are only numbers into a LTR group if a
                // LTR letter comes immediately after it. Also regroups them if we are typing numbers
                // in LTR and a LTR number comes after it. (used only when typing in RTL)
                if (IsRightToLeftText &&
                    textPoint.IsRTL &&
                    ((charType & CharType.LETTER) != 0 ||
                    (charType & CharType.NUMBER) != 0 && !writeNumbersRTL) &&
                    !CharTypeIsRTL(charType) &&
                    firstNumberIdx != -1)
                {
                    // Update top existing textpoint if it is at the index of the first number
                    // char after processing a non-number char.
                    if (textPoint.StartIndex == firstNumberIdx)
                    {
                        textPoint.CharType = charType;
                        firstNumberIdx = SetFirstNumberIndex(charType, textPoint.StartIndex, firstNumberIdx);
                    }
                    else
                    {
                        // Otherwise, create a new one.
                        newTextPoint = new RTLTextPoint()
                        {
                            StartIndex = firstNumberIdx,
                            CharType = charType
                        };
                        rtlTextPointList.Add(newTextPoint);
                        firstNumberIdx = SetFirstNumberIndex(charType, newTextPoint.StartIndex, firstNumberIdx);
                    }

                    i = i + tagOffset;
                    firstSpaceIdx = -1;
                    writeNumbersRTL = false;
                    lastCharType = charType;
                    continue;
                }

                // Don't create a new if the current top textpoint is in the same text flow
                // as the character currently being processed & if said character is not a
                // letter or an number and the char at the texpoint isn't a symbol or
                // punctuation mark.
                if (textPoint.IsRTL == CharTypeIsRTL(charType) &&
                    ((charType & (CharType.LETTER | CharType.NUMBER)) == 0 ||
                    (textPoint.CharType & CharType.RTL_PUNCTUATION) != 0 ||
                    (textPoint.CharType & (CharType.PUNCTUATION | CharType.SYMBOL)) == 0))
                {
                    firstNumberIdx =
                        SetFirstNumberIndex(charType,
                                            firstSpaceIdx != -1 ? firstSpaceIdx : i,
                                            firstNumberIdx);
                    i = i + tagOffset;
                    firstSpaceIdx = -1;
                    lastCharType = charType;
                    continue;
                }

                // Place a new TextPoint and update variables.
                newTextPoint = new RTLTextPoint()
                {
                    StartIndex = firstSpaceIdx != -1 ? firstSpaceIdx : i,
                    CharType = charType
                };
                rtlTextPointList.Add(newTextPoint);

                firstNumberIdx =
                    SetFirstNumberIndex(charType, newTextPoint.StartIndex, firstNumberIdx);

                i = i + tagOffset;
                firstSpaceIdx = -1;
                writeNumbersRTL = WriteNumbersRTL(charType, writeNumbersRTL);
                lastCharType = charType;
            }
        }
        #endregion Public Methods

        #region Private Methods
        /// <summary>
        /// Gets a CharType bit mask for a given char
        /// </summary>
        /// <param name="ch">The character to convert</param>
        /// <returns>The CharType bit mask for the given character</returns>
        private CharType GetCharType(char ch)
        {
            if (char.IsWhiteSpace(ch))
            {
                return CharType.SPACE;
            }

            CharType charType = 0;

            if (TextUtils.IsRTLCharacter(ch))
            {
                charType |= CharType.RTL;
            }

            if (IsTashkeelAccent(ch))
            {
                charType |= CharType.TASHKEEL;
            }
            else if (char.IsPunctuation(ch))
            {
                if (TextUtils.IsRTLCharacter(ch))
                {
                    charType |= CharType.RTL_PUNCTUATION;
                }

                // Process all Punctuation chars as RTL chars if we are typing in RTL
                charType |= !IsRightToLeftText ? CharType.PUNCTUATION :
                                                 (CharType.PUNCTUATION | CharType.RTL);
            }
            else if (char.IsSymbol(ch) || ch == ZERO_WIDTH_CHAR)
            {
                // Process all symbols as RTL chars if we are typing in RTL
                charType |= !IsRightToLeftText ? CharType.SYMBOL :
                                                 (CharType.SYMBOL | CharType.RTL);
            }
            else if (char.IsNumber(ch))
            {
                charType |= CharType.NUMBER;
            }
            else
            {
                charType |= CharType.LETTER;
            }

            return charType;
        }

        /// <summary>
        /// Returns true if a given CharType is in a RTL text flow.
        /// </summary>
        /// <param name="charType">The CharType to check against</param>
        /// <returns>Returns true whether or not the CharType was in a RTL text flow or not</returns>
        private bool CharTypeIsRTL(CharType charType)
        {
            return (charType & CharType.RTL) != 0;
        }

        /// <summary>
        /// Helper method for setting the firstNumberIdx in the SetRTLTextPoints() method
        /// </summary>
        /// <param name="charType">The CharType of the character</param>
        /// <param name="idx">The index of the character in the string being modified</param>
        /// <param name="firstNumberIdx">The current value of firstNumberIdx in SetRTLTextPoints()</param>
        /// <returns>The new value of firsNumberIdx</returns>
        private int SetFirstNumberIndex(CharType charType, int idx, int firstNumberIdx)
        {
            if ((charType & (CharType.NUMBER)) == 0)
            {
                return -1;
            }

            return firstNumberIdx != -1 ? firstNumberIdx : idx;
        }

        /// <summary>
        /// Helper for determining if a group of punctuation marks needs to be flipped to the
        /// opposite different text flow group
        /// </summary>
        /// <param name="rtlTextPointList">The current running list of RTLTextPoints</param>
        /// <param name="textPoint">The current RTLTextPoint</param>
        /// <param name="charType">The CharType of the character</param>
        /// <returns>Whether or not the group of punctuation marks should be flipped</returns>
        private bool CanFlipPunctuation(
            List<RTLTextPoint> rtlTextPointList, RTLTextPoint textPoint, CharType charType)
        {
            if (rtlTextPointList.Count < 2)
            {
                return false;
            }

            if ((rtlTextPointList[rtlTextPointList.Count - 2].CharType & CharType.LETTER) == 0 ||
                (textPoint.CharType & (CharType.PUNCTUATION | CharType.SYMBOL)) == 0 ||
                (textPoint.CharType & CharType.RTL_PUNCTUATION) != 0 ||
                IsRightToLeftText != textPoint.IsRTL)
            {
                return false;
            }

            if ((!IsRightToLeftText && (charType & CharType.TASHKEEL) != 0))
            {
                return false;
            }

            return (CharTypeIsRTL(charType) != IsRightToLeftText ||
                   !IsRightToLeftText && (charType & CharType.NUMBER) != 0);
        }

        /// <summary>
        /// Helper method for determining if LTR numbers should be written in RTL form
        /// instead of LTR in the SetRTLTextPoints() method
        /// </summary>
        /// <param name="charType">The CharType of the character</param>
        /// <param name="writeNumbersRTL">The current value of writeNumbersRTL in SetRTLTextPoints()</param>
        /// <returns>The new value of writeNumbersRTL</returns>
        private bool WriteNumbersRTL(CharType charType, bool writeNumbersRTL)
        {
            if ((charType & CharType.LETTER) == 0)
            {
                return writeNumbersRTL;
            }

            return CharTypeIsRTL(charType);
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
            char superscriptAlef = (char)TashkeelCharacters.SuperscriptAlef;
            char dammatanIsolated = (char)TashkeelCharacters.ShaddaWithDammatanIsolatedForm;
            char superscriptAlefIsolated =
                (char)TashkeelCharacters.ShaddaWithSuperscriptAlefIsolatedForm;

            return (ch >= fathan && ch <= superscriptAlef) ||
                   (ch >= dammatanIsolated && ch <= superscriptAlefIsolated);
        }
        #endregion Private Methods
    }
}
