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

        public LegendVolume(LegendBooleans booleans)
        {
            volume = null;
            loadFromBooleans(booleans);
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
        public void loadFromBooleans(LegendBooleans booleans)
        {
            List<Color> pixels = new List<Color>();
            int paddedWidth = Mathf.NextPowerOfTwo(booleans.width);
            int paddedHeight = Mathf.NextPowerOfTwo(booleans.height);
            int paddedDepth = Mathf.NextPowerOfTwo(booleans.depth);

            // add a black pixel if part of segment, white if not
            // TODO: I added padding since the empty textures below have padding. Will this break anything?
            for (int x = 0; x < paddedWidth; x++)
            {
                for (int y = 0; y < paddedHeight; y++)
                {
                    for (int layer = 0; layer < paddedDepth; layer++)
                    {
                        if (x > booleans.width || y > booleans.height || layer > booleans.depth)
                        {
                            pixels.Add(Color.white);        // padded indices are not part of the segment
                        }
                        else
                        {
                            pixels.Add(booleans[x, y, layer] ? Color.black : Color.white);
                        }
                    }
                }
            }

            // pad the end with white pixels
            Texture2D texturePadding = new Texture2D(paddedWidth, paddedHeight);
            texturePadding.FillSingleColor(Color.white);
            for (int layer = booleans.depth; layer < paddedDepth; layer++)
            {
                pixels.AddRange(texturePadding.GetPixels());
            }

            volume = new Texture3D(paddedWidth, paddedHeight, paddedDepth, TextureFormat.ARGB32, false);
            volume.SetPixels(pixels.ToArray());
            volume.Apply();
        }
    }
}