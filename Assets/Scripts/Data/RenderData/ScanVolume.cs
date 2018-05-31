using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ScanData;

namespace RenderData
{
    public class ScanVolume
    {
        private Texture3D volume;            // slices stored in a 3D volume ray marching
        public int width { get { return volume.width; } }
        public int height { get { return volume.height; } }
        public int depth { get { return volume.depth; } }

        public ScanVolume(ScanSlices sliceData)
        {
            volume = null;
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
