using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ScanData
{
    public class ScanVolume
    {
        private Texture3D volume;            // slices stored in a 3D volume ray marching
        public int width, height, depth;

        public ScanVolume(ScanSlices sliceData)
        {
            volume = null;
            width = height = depth = 0;
            GeneratePaddedVolume(sliceData);
        }

        public Texture3D GetVolume()
        {
            return volume;
        }

        /// <summary>
        ///     Generate a padded Texture3D from scan textures.
        ///     (width, height, and depth will all be padded to the nearest power of 2)
        /// </summary>
        public void GeneratePaddedVolume(ScanSlices sliceData)
        {
            List<Texture2D> slices = sliceData.GetSlices();

            if (slices == null || slices.Count < 1)
            {
                throw new Exception("Scan slices must be loaded before intensities can be set up.");
            }

            // pad to the nearest power of 2 for Texture3D performance improvments
            width = Mathf.NextPowerOfTwo(sliceData.width);
            height = Mathf.NextPowerOfTwo(sliceData.height);
            depth = Mathf.NextPowerOfTwo(slices.Count);

            Texture2D paddedTex2D = new Texture2D(width, height);

            // add pixels from the Texture2D's
            List<Color> allPixels = new List<Color>();
            for (int i = 0; i < slices.Count; i++)
            {
                paddedTex2D.SetPixels(slices[i].GetPixels());
                allPixels.AddRange(paddedTex2D.GetPixels());
            }

            volume = new Texture3D(width, height, depth, TextureFormat.ARGB32, false);
            volume.SetPixels(allPixels.ToArray());
            volume.Apply();
        }
    }

}
