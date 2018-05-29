using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ScanData
{
    public class ScanIntensities
    {
        private float[,,] intensities;       // [slice, x, y] 0..1 intensity where 0 is black, 1 is white
        public int width { get { return intensities.GetLength(0); } }
        public int height { get { return intensities.GetLength(1); } }
        public int depth { get { return intensities.GetLength(2); } }

        public ScanIntensities (ScanSlices sliceData)
        {
            GenerateIntensities(sliceData);
        }

        public float[,,] GetIntensities()
        {
            return intensities;
        }

        // override [,,] operator for getting and setting
        public float this[int x, int y, int layer]
        {
            get { return intensities[x, y, layer]; }
            set { intensities[x, y, layer] = value; }
        }

        /// <summary>
        ///     Generate intensities from scan slices.
        ///     TODO: optimize intensity format
        /// </summary>
        /// <param name="scanTextures"></param>
        public void GenerateIntensities(ScanSlices sliceData)
        {
            if (sliceData == null || sliceData.Count < 1)
            {
                throw new Exception("There are no slices to load from.");
            }

            intensities = new float[sliceData.width, sliceData.height, sliceData.Count];

            // convert each layer into floats
            for (int layer = 0; layer < depth; layer++)
            {
                Color[] pixels = sliceData.GetPixels(layer);
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        // TODO: adjust this format so it works with the algorithms
                        intensities[x, y, layer] = pixels[(y*width) + x].r;
                    }
                }
            }
        }
    }
}