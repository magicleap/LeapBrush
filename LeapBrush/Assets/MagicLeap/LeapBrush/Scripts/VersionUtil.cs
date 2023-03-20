using System;

namespace MagicLeap.LeapBrush
{
    /// <summary>
    /// Utility for working with version strings.
    /// </summary>
    public class VersionUtil
    {
        public static bool IsGreatorOrEqual(string version1, string version2)
        {
            string[] version1Pieces = version1.Split(".");
            string[] version2Pieces = version2.Split(".");

            try
            {
                for (int i = 0; i < Math.Max(version1Pieces.Length, version2Pieces.Length); ++i)
                {
                    if (i >= version1Pieces.Length)
                    {
                        return false;
                    }
                    if (i >= version2Pieces.Length)
                    {
                        return true;
                    }

                    int version1PieceValue =
                        version1Pieces[i].Length > 0 ? Int32.Parse(version1Pieces[i]) : 0;
                    int version2PieceValue =
                        version2Pieces[i].Length > 0 ? Int32.Parse(version2Pieces[i]) : 0;

                    if (version1PieceValue > version2PieceValue)
                    {
                        return true;
                    }

                    if (version2PieceValue > version1PieceValue)
                    {
                        return false;
                    }
                }
            }
            catch (FormatException e)
            {
                return false;
            }

            return true;
        }
    }
}