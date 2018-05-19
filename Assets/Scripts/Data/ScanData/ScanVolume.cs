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
            GenerateVolume(sliceData);
        }

        public Texture3D GetVolume()
        {
            return volume;
        }

        public void GenerateVolume(ScanSlices slices)
        {
            volume = TextureUtils.Texture3DFromArray(slices.GetSlices());
        }
    }
}
