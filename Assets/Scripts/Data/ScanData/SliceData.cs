using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



namespace ScanData
{
    /// <summary>
    ///     This class holds raw slice data and provides basic methods to generate
    ///     the scan data from appropriate sources.
    /// </summary>
    public class ScanSlices
    {
        private List<Texture2D> slices;      // each slice of the scan
        public int width, height;

        public ScanSlices(List<Texture2D> sliceTextures)
        {
            slices = sliceTextures;

            if (slices.Count > 0)
            {
                width = slices[0].width;
                height = slices[0].height;
            }
            else
            {
                width = 0;
                height = 0;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filepath">Path to load scans from</param>
        /// <param name="numSlices">Number of slices to load from the scan</param>
        public ScanSlices(string filepath, int numSlices) : this(filepath, 0, numSlices, 1)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filepathInResources">Path to load slices from.</param>
        /// <param name="startIndex">Starting slice # to load.</param>
        /// <param name="endIndex">Ending slice # to load.</param>
        /// <param name="increment">Slice increment (2 is every other slices, 3 is every 3 slices, and so on).</param>
        public ScanSlices(string filepathInResources, int startIndex, int endIndex, int increment)
        {
            LoadSlicesFromFile(filepathInResources, startIndex, endIndex, increment);
        }

        public List<Texture2D> GetSlices ()
        {
            return slices;
        }

        // If nextPowerOfTwo, will attempt to round count up to the nearest power of
        // two, if it will not go out of range
        public ScanSlices GetRange(int index, int count)
        {
            return new ScanSlices(slices.GetRange(index, count));
        }

        /// <summary>
        ///     Load multiple scans as Texture2D's from files. Starting from
        ///     file numbered 0000, up to the specified number of layers.
        /// </summary>
        /// <param name="path">Path to the file minus the numbered suffix.</param>
        /// <param name="numLayers">Number of files to load.</param>
        public void LoadSlicesFromFile(string path, int numSlices)
        {
            LoadSlicesFromFile(path, 0, numSlices, 1);
        }

        public void LoadSlicesFromFile(string path, int startIndex, int endIndex, int increment)
        {
            slices = new List<Texture2D>();

            for (int i = startIndex; i < endIndex; i += increment)
            {
                slices.Add(Resources.Load(path + FourDigitFileSuffix(i)) as Texture2D);
            }

            if (slices.Count > 0)
            {
                width = slices[0].width;
                height = slices[0].height;
            }
        }

        /// <summary>
        ///     Convert an index to the appropriate file suffix
        ///     for files with the format "{path}-####"
        /// </summary>
        private string FourDigitFileSuffix(int index)
        {
            return index.ToString().PadLeft(4, '0');
        }
    }

}