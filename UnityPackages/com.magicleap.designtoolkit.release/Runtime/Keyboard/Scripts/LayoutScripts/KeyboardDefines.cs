// Copyright (c) 2022 Magic Leap, Inc. All Rights Reserved.
// Please see the top-level LICENSE.md in this distribution
// for terms and conditions governing this file.
using System.Collections;
using System.Collections.Generic;
using System;
using System.Text;
using UnityEngine;

namespace MagicLeap.DesignToolkit.Keyboard
{
    public enum Code : int
    {
        kEn_US_Unity,
        kJp_JP_Unity,
        kAr_AR_Unity,
        kDe_DE_Unity,
        kFr_FR_Unity,
        kEs_ES_Unity,
        kPt_PT_Unity,
        kIt_IT_Unity,

        kAll_Unity = -1,
    }

    public enum PageCode
    {
        kLowerLetters,
        kUpperLetters,
        kCapsLock,
        kNumericSymbols,
        kHiragana,
        kKatakana,
        kAltLowerLetters,
        kAltUpperLetters,
    }

    public enum KeyType
    {
        kNone,
        kCharacter,
        kBackspace,
        kShift,
        kCapsLock,
        kPageNumericSymbols,
        kCancel,
        kSubmit,
        kClear,
        kClose,
        kEnter,
        kChangeLocale,
        kPageHiragana,
        kPageKatakana,
        kAltCharToggle,
        kSpacebar,
        kTab,
        kCharacterSpecial,
        kSuggestion,
        kHiragana,
        kKatakana,
        kBlank,
        kJPNumSym,
        kJPEnter,
        kAccents,
        kJPSpacebar,
        kJPNewLine,
        kRayDrumstickSwitch,
        kHideKeyboard,
        kJPNextSuggestion,
        kChangeLocaleEn,
        kChangeLocaleJP,
        kChangeLocaleAR,
        kChangeLocaleDE,
        kAccent,
        kPageDownExpanded,
        kPageUpExpanded,
        kPageDownCollapsed,
        kPageUpCollapsed,
        kExpandUp,
        kCollapseDown,
        kCharacter2Labels,
        kSubPanel,
        kTashkeel,
        kAltGr,
        kChangeLocaleFR,
        kChangeLocaleES,
        kChangeLocalePT,
        kChangeLocaleIT
    }

    public enum KeyButtonStyle
    {
        kDefault,
        kLight,
        kMedium,
        kDark,
        kReturn,
        kSpaceBar
    }

    public enum LayoutType
    {
        kFull,
        kEmail,
        kBasic,
        kNumeric,
        kNumericSymbols,
        kURL
    }

    // Enum representing the type of a given character in the typed content
    public enum CharType : int
    {
        // used to determine if a char is RTL or not
        RTL = 1,

        // char types
        LETTER = 2,
        NUMBER = 4,
        PUNCTUATION = 8,
        SYMBOL = 16,
        SPACE = 32,

        // some punctuation is exclusively RTL, so we this to keep track of it.
        RTL_PUNCTUATION = 64,

        // used exclusively for tashkeel accents in arabic
        TASHKEEL = 128,
    }

    public enum AudioAssetType
    {
        kDefault,
        kHover,
        kKeyDown,
        kAltChar,
        kClear,
        kDelete,
        kShift,
        kSpace,
        kSubmit,
        kSwitchLayout,
        kTab,
        kSelectEntered,
        kSelectExited,
        kNav,
        kGrab,
        kRelease,
        kExpandOption,
        kNone
    }

    // String data for rich text tags
    public class TagStringData
    {
        // The rich text tag as it appears when typed
        public string Str;

        // The rich text tag that should be placed instead of the
        // actual tag in the text. Typically used for tags that
        // need to be balanced when switching from RTL to LTR or vice versa.
        // (ie. if we start typing in LTR and then move to RTL and
        // we have rich text tags "<mark>ab</mark>", AltStr would be "</mark>".
        // If it is instead "<mark>ab", AltStr would be "<mark>")
        public string AltStr;
    }

    // Used to group a sequence of character together by text flow
    public class RTLTextPoint
    {
        // The index of the first character in the sequence of characters
        public int StartIndex;

        // The type of the first non-space character in the sequence
        public CharType CharType;

        // Returns true if the text flow of the characters is RTL. Defined
        // by the text flow of the first non-space character in the sequence
        public bool IsRTL
        {
            get
            {
                return (CharType & CharType.RTL) != 0;
            }
        }
    }

    public struct U32string
    {
        public Char32_t[] Data;

        // Copy constructor
        public U32string(U32string a_U32string)
        {
            Data = new Char32_t[a_U32string.Data.Length];
            for (int i = 0; i < Data.Length; i++)
            {
                Data[i] = a_U32string.Data[i];
            }
        }

        // Can be constructed from a regular string
        public U32string(string a_string)
        {
            var enc = new UTF32Encoding(true, false, false);
            var byteLen = enc.GetByteCount(a_string);
            if (byteLen % 4 != 0)
            {
                Debug.LogWarning("Converting string of " + a_string +
                                 " to Bytes resulted in irregular number of bytes");
            }
            Data = new Char32_t[byteLen / 4];
            Byte[] encodedBytes = enc.GetBytes(a_string);
            for (int i = 0; i < (byteLen / 4); i++)
            {
                Data[i] = new Char32_t(
                    encodedBytes[i * 4],
                    encodedBytes[i * 4 + 1],
                    encodedBytes[i * 4 + 2],
                    encodedBytes[i * 4 + 3]
                );
            }
        }

        public override bool Equals(object obj) => obj is U32string other && this.Equals(other);

        public bool Equals(U32string other)
        {
            if (this.Data == null && other.Data == null)
            {
                return true; // both null, equal
            }
            if (this.Data == null || other.Data == null)
            {
                return false; // only one is null, not equal
            }
            if (this.Data.Length != other.Data.Length)
            {
                return false; // length not euqal
            }
            for (int i = 0; i < this.Data.Length; i++)
            {
                if (this.Data[i] != other.Data[i])
                {
                    return false;
                }
            }
            return true;
        }

        public override int GetHashCode() => base.GetHashCode();
        public static bool operator ==(U32string lhs, U32string rhs) => lhs.Equals(rhs);
        public static bool operator !=(U32string lhs, U32string rhs) => !lhs.Equals(rhs);

        public override string ToString()
        {
            if (Data == null)
            {
                return "NULL";
            }
            Byte[] bytes = new Byte[Data.Length * 4];
            for (int i = 0; i < Data.Length; i++)
            {
                bytes[i * 4] = Data[i].Byte0;
                bytes[i * 4 + 1] = Data[i].Byte1;
                bytes[i * 4 + 2] = Data[i].Byte2;
                bytes[i * 4 + 3] = Data[i].Byte3;
            }
            var enc = new UTF32Encoding(true, false, false);
            return enc.GetString(bytes);
        }
    }

    [Serializable]
    public struct Char32_t
    {
        public Byte Byte0;
        public Byte Byte1;
        public Byte Byte2;
        public Byte Byte3;

        public Char32_t(Byte b0, Byte b1, Byte b2, Byte b3)
        {
            Byte0 = b0;
            Byte1 = b1;
            Byte2 = b2;
            Byte3 = b3;
        }

        // Overload the quals operator
        public override bool Equals(object obj) => obj is Char32_t && this.Equals(obj);

        public bool Equals(Char32_t other) => Byte0 == other.Byte0 && Byte1 == other.Byte1 &&
                                              Byte2 == other.Byte2 && Byte3 == other.Byte3;

        public override int GetHashCode() => (Byte0, Byte1, Byte2, Byte3).GetHashCode();
        public static bool operator ==(Char32_t lhs, Char32_t rhs) => lhs.Equals(rhs);
        public static bool operator !=(Char32_t lhs, Char32_t rhs) => !(lhs == rhs);

        public override string ToString()
        {
            var enc = new UTF32Encoding(true, false, false);
            Byte[] bytes = new Byte[] {Byte0, Byte1, Byte2, Byte3};
            return enc.GetString(bytes);
        }

        public char ToChar()
        {
            return ToString()[0];
        }
    }

    public enum OverrideFontSizeOptions : int
    {
        NONE = 0,
        OVERRIDE_MIN = 1 << 0,
        OVERRIDE_MAX = 1 << 1,
        OVERRIDE_BOTH = OVERRIDE_MIN | OVERRIDE_MAX
    }

    public struct LabeledKey
    {
        public Char32_t CharacterCode;
        public U32string Label;

        public LabeledKey(Char32_t a_CharacterCode, string a_label)
        {
            CharacterCode = a_CharacterCode;
            Label = new U32string(a_label);
        }

        public LabeledKey(Char32_t a_characgterCode, U32string a_U32string)
        {
            CharacterCode = a_characgterCode;
            Label = new U32string(a_U32string);
        }

        public override string ToString()
        {
            return ("LabeledKey {CharacterCode: " + CharacterCode.ToString() +
                    "; Label: " + Label.ToString() + "} ");
        }
    }

    public class MultiLabeledKey
    {
        public Char32_t CharacterCode;
        public List<U32string> Labels;

        public MultiLabeledKey()
        {
            CharacterCode = new Char32_t();
            Labels = new List<U32string>();
        }

        public override string ToString()
        {
            string labelsStr = "Labels: ";
            foreach (U32string label in Labels)
            {
                labelsStr += (label.ToString() + ", ");
            }
            return "MultiLabeledKey {CharacterCode: " + CharacterCode.ToString() +
                   "; Labels: " + labelsStr + "} ";
        }
    }

    public struct KeyDesc
    {
        public KeyType KeyType;
        public LabeledKey DefaultLabeledKey;
        public List<LabeledKey> AlternativeLabeledKeys;

        public KeyDesc(KeyType aKeyType,
                       LabeledKey aDefaultLabeledKey,
                       List<LabeledKey> anAlternativeLabeledKeys)
        {
            KeyType = aKeyType;
            DefaultLabeledKey = aDefaultLabeledKey;
            AlternativeLabeledKeys = anAlternativeLabeledKeys;
        }

        public override string ToString()
        {
            string alternativeLabedKeysStr = "AlternativeLabeledKeys: ";
            foreach (LabeledKey labeledKey in AlternativeLabeledKeys)
            {
                alternativeLabedKeysStr += (labeledKey.ToString() + ", ");
            }
            return "KeyDesc {KeyType: " + KeyType +
                   "; DefaultLabeledKey: " + DefaultLabeledKey.ToString() +
                   "; AlternativeLabeledKeys: " + alternativeLabedKeysStr + "} ";
        }
    }
}
