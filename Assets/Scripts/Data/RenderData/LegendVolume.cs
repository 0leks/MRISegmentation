using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ExtensionMethods;
using SegmentationData;

namespace RenderData
{
    public class LegendVolume
    {
        private Texture3D volume;
        private static Color OnColor = Color.black;
        private static Color OffColor = Color.white;
        public int width { get { return volume.width; } }
        public int height { get { return volume.height; } }
        public int depth { get { return volume.depth; } }

        public LegendVolume(LegendBooleans booleans, bool loadInverted = false)
        {
            volume = null;
            loadFromBooleans(booleans, loadInverted);
        }

        public Texture3D GetVolume ()
        {
            return volume;
        }

        /// <summary>
        ///     Convert a segment into a Texture3D equivalent where black pixels are part of the
        ///     segment, and white pixels are not. The Texture3D's dimensions are rounded to the
        ///     next nearest power of 2, any extra pixels are set to white.
        /// </summary>
        public void loadFromBooleans(LegendBooleans booleans, bool loadInverted)
        {
            List<Color> pixels = new List<Color>();

            // add a black pixel if part of segment, white if not
            for (int x = 0; x < booleans.width; x++)
            {
                for (int y = 0; y < booleans.height; y++)
                {
                    for (int layer = 0; layer < booleans.depth; layer++)
                    {
                        if (loadInverted)
                            pixels.Add(booleans[x, y, layer] ? OffColor : OnColor);
                        else
                            pixels.Add(booleans[x, y, layer] ? OnColor : OffColor);
                    }
                }
            }

            volume = new Texture3D(booleans.width, booleans.height, booleans.depth, TextureFormat.ARGB32, false);
            volume.SetPixels(pixels.ToArray());
            volume.Apply();
        }
    }
}