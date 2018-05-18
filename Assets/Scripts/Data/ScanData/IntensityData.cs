using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ScanData
{
    public class ScanIntensities
    {
        private float[,,] intensities;       // [x,y,layer] 0..1 intensity where 0 is black, 1 is white
        public int width, height, depth;

        public ScanIntensities (ScanSlices sliceData)
        {
            intensities = null;
            width = height = depth = 0;
            GenerateIntensities(sliceData);
        }

        public float[,,] GetIntensities()
        {
            return intensities;
        }

        /// <summary>
        ///     Generate intensities from scan slices.
        ///     TODO: optimize intensity format
        /// </summary>
        /// <param name="scanTextures"></param>
        public void GenerateIntensities(ScanSlices sliceData)
        {
            List<Texture2D> slices = sliceData.GetSlices();
            if (slices.Count == null || slices.Count < 1)
            {
                throw new Exception("Scan slices must be loaded before intensities can be set up.");
            }

            width = sliceData.width;
            height = sliceData.height;
            depth = slices.Count;

            intensities = new float[
                width,
                height,
                depth];

            // convert each layer into floats
            // TODO: this way is REALLY slow, using getpixel. We want to use GetPixels() instead.
            for (int layer = 0; layer < depth; layer++)
            {
                Color[] pixels = slices[layer].GetPixels();
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        // TODO: what are the negative indices doing?
                        intensities[x, y, layer] = slices[layer].GetPixel(-(x + 1), -(y + 1)).r;
                    }
                }
            }
        }
    }

}