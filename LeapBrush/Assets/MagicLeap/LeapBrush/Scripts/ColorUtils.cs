using System;
using UnityEngine;

namespace MagicLeap
{
    /// <summary>
    /// Utilities for working with Colors.
    /// </summary>
    public class ColorUtils
    {
        public static Color32 FromRgbaUint(uint rgbaUint)
        {
            Color32 c = new Color32();
            c.r = (byte)((rgbaUint >> 24) & 0xff);
            c.g = (byte)((rgbaUint >> 16) & 0xff);
            c.b = (byte)((rgbaUint >> 8) & 0xff);
            c.a = (byte)((rgbaUint) & 0xff);
            return c;
        }

        public static uint ToRgbaUint(Color32 color)
        {
            return (uint) ((color.r << 24) + (color.g << 16) + (color.b << 8) + color.a);
        }
    }
}