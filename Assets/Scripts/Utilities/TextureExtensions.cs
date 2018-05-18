using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ExtensionMethods
{
    public static class TextureExtensions
    {
        /// <summary>
        /// Sets all pixels in the texture to a single color.
        /// </summary>
        /// <param name="tex"></param>
        /// <param name="color"></param>
        public static void FillSingleColor (this Texture2D tex, Color color)
        {
            Color[] colors = tex.GetPixels();
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = color;
            }
            tex.SetPixels(colors);
            tex.Apply();
        }
    }
}
