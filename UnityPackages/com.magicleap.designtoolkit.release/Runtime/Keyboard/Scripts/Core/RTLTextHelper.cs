using RTLTMPro;

namespace MagicLeap.DesignToolkit.Keyboard
{
    /// <summary>
    /// Helper Class for RTL text
    /// </summary>
    public class RTLTextHelper
    {
        /// <summary>
        /// Checks to see if a string is a RTL string or not
        /// </summary>
        /// <param name="text">the string being checked</param>
        /// <returns>True if a RTL Character appears before any LTR Letter or number in the
        /// string</returns>
        public static bool IsRTLString(string text)
        {
            foreach (char ch in text)
            {
                if (TextUtils.IsRTLCharacter(ch))
                {
                    return true;
                }

                if (char.IsLetter(ch) || char.IsNumber(ch))
                {
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks to see a string contains any Non-punction marks at all
        /// </summary>
        /// <param name="text">the string being checked</param>
        /// <returns>True if the string contains a char that is not a punctuation mark</returns>
        public static bool ContainsNonPunctuationChar(string text)
        {
            foreach (char ch in text)
            {
                if (char.IsLetter(ch) || char.IsNumber(ch) || char.IsSymbol(ch))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
