using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;



namespace ScanData
{
    /// <summary>
    /// This class holds raw slice data and provides basic methods to generate
    /// the scan data from appropriate sources. </summary>
    public class ScanSlices
    {
        private Texture2DArray slices;
        public int width { get { return slices.width; } }
        public int height { get { return slices.height; } }
        public int Count { get { return slices.depth; } }
        public enum SliceFormat {
            jpg3,   // 000.jpg
            jpg4,   // 0000.jpg
            png3
        };

        public ScanSlices(Texture2DArray sliceArray)
        {
            slices = sliceArray;
        }

        /// <summary> </summary>
        /// <param name="filepath">Path to load scans from</param>
        /// <param name="numSlices">Number of slices to load from the scan</param>
        public ScanSlices(string filepath, SliceFormat format, int numSlices) 
            : this(filepath, format, 0, numSlices, 1)
        {

        }

        /// <summary></summary>
        /// <param name="filepathInResources">Path to load slices from.</param>
        /// <param name="startIndex">Starting slice # to load.</param>
        /// <param name="endIndex">Ending slice # to load.</param>
        /// <param name="increment">Slice increment (2 is every other slices, 3 is every 3 slices, and so on).</param>
        public ScanSlices(string filepathInResources, SliceFormat format, 
            int startIndex, int endIndex, int increment)
        {
            LoadFromFile(filepathInResources, format, startIndex, endIndex, increment);
        }

        public Texture2DArray GetSlices()
        {
            return slices;
        }

        public Color[] GetPixels(int index)
        {
            return slices.GetPixels(index);
        }

        /// <summary>
        /// Load multiple scans as Texture2D's from files. Starting from
        /// file numbered 0000, up to the specified number of layers. </summary>
        /// <param name="path">Path to the file minus the numbered suffix.</param>
        /// <param name="numLayers">Number of files to load.</param>
        public void LoadFromFile(string path, SliceFormat format, int numSlices)
        {
            LoadFromFile(path, format, 0, numSlices, 1);
        }

        public void LoadFromFile(string path, SliceFormat format, int startIndex, int endIndex, int increment)
        {
            List<Texture2D> temp = new List<Texture2D>();
            for (int i = startIndex; i < endIndex; i += increment)
            {
                Texture2D tex = LoadSingleSliceFromFile(path, format, i);
                if (tex != null)
                    temp.Add(tex);
            }

            slices = TextureUtils.ArrayFromList(temp);
        }

        public Texture2D LoadSingleSliceFromFile(string path, SliceFormat format, int index)
        {
            Texture2D result = new Texture2D(2, 2, TextureFormat.RGB24, false);
            byte[] fileData = File.ReadAllBytes(path + DigitFileSuffix(index, format));
            result.LoadImage(fileData);

            // Conversion to handle alternate file formats such as ARGB
            Texture2D temp = new Texture2D(result.width, result.height);
            temp.SetPixels(result.GetPixels());

            return temp;
        }

        /// <summary>
        /// Convert an index to the appropriate file suffix. e.g. Format 'jpg' and
        /// index '0' returns "0000.jpg", index 1 returns "0001.jpg", and so on.</summary>
        private string DigitFileSuffix(int index, SliceFormat format)
        {

            if (format == SliceFormat.jpg3)
            {
                string digits = index.ToString().PadLeft(3, '0');
                return digits + ".jpg";
            }
            else if (format == SliceFormat.jpg4)
            {
                string digits = index.ToString().PadLeft(4, '0');
                return digits + ".jpg";
            }
            else if (format == SliceFormat.png3)
            {
                string digits = index.ToString().PadLeft(3, '0');
                return digits + ".png";
            }
            else
                return "";
        }
    }

}