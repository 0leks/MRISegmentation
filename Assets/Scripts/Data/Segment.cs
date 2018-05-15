using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Segment {

    /// <summary>
    ///     Struct for storing and retrieving 3D volume segment data.
    ///     Segment data is represented as a 3D boolean array, where
    ///         true denotes that index is part of a segment, and false
    ///         denotes that it is not.
    /// </summary>
    public struct SegmentData
    {
        public bool[,,] data;
        public int width, height, depth;

        public SegmentData(int width, int height, int numLayers)
        {
            data = new bool[width, height, numLayers];
            this.width = width;
            this.height = height;
            depth = numLayers;
        }

        public SegmentData(bool[,,] data)
        {
            this.data = (bool[,,])data.Clone();
            width = data.GetLength(0);
            height = data.GetLength(1);
            depth = data.GetLength(2);
        }

        // return a deep copy
        public SegmentData Clone()
        {
            return new SegmentData((bool[,,])data.Clone());
        }

        // override [,,] operator for getting and setting
        public bool this[int x, int y, int layer]
        {
            get { return data[x, y, layer]; }
            set { data[x, y, layer] = value; }
        }


        // invert all booleans in the segment
        public void Invert ()
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        this[x, y, z] = !this[x, y, z];
                    }
                }
            }
        }
        public static SegmentData Invert(SegmentData segment)
        {
            SegmentData result = segment.Clone();
            result.Invert();
            return result;
        }

        // TODO: learn what this function is doing (it separates a segment into multiple, but in what way and why?
        public List<SegmentData> Separate( int maxSegments)
        {
            // do DFS from each point unless its already visited. Each iteration of DFS increment group counter
            // finally for each group create a new bool[,,] and assign values to true where the group is.
            SegmentData segmentCopy = Clone();
            List<SegmentData> separatedSegments = new List<SegmentData>();
            List<int> segmentVolumes = new List<int>();
            Stack<DataContainer.Point> searchArea = new Stack<DataContainer.Point>();

            int smallestVolume = -1;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        if (segmentCopy[x, y, z])
                        {
                            int volume = 0;
                            SegmentData group = new SegmentData(width, height, depth);
                            // TODO: replace with a Point struct from the refactored code
                            searchArea.Push(new DataContainer.Point(x, y, z));
                            while (searchArea.Count > 0)
                            {
                                DataContainer.Point point = searchArea.Pop();

                                if (point.x >= 0 && point.x < width
                                    && point.y >= 0 && point.y < height
                                    && point.z >= 0 && point.z < depth)
                                {
                                    if (segmentCopy[point.x, point.y, point.z])
                                    {
                                        segmentCopy[point.x, point.y, point.z] = false;
                                        group[point.x, point.y, point.z] = true;
                                        volume++;
                                        searchArea.Push(new DataContainer.Point(point.x - 1, point.y, point.z));
                                        searchArea.Push(new DataContainer.Point(point.x + 1, point.y, point.z));
                                        searchArea.Push(new DataContainer.Point(point.x, point.y - 1, point.z));
                                        searchArea.Push(new DataContainer.Point(point.x, point.y + 1, point.z));
                                        searchArea.Push(new DataContainer.Point(point.x, point.y, point.z + 1));
                                        searchArea.Push(new DataContainer.Point(point.x, point.y, point.z - 1));
                                    }
                                }
                            }
                            if (volume > smallestVolume && separatedSegments.Count >= maxSegments)
                            {
                                int minVolume = segmentVolumes[0];
                                int minIndex = 0;
                                for (int index = 1; index < separatedSegments.Count; index++)
                                {
                                    if (segmentVolumes[index] < minVolume)
                                    {
                                        minVolume = segmentVolumes[index];
                                        minIndex = index;
                                    }
                                }
                                separatedSegments.RemoveAt(minIndex);
                                segmentVolumes.RemoveAt(minIndex);
                                separatedSegments.Add(group);
                                segmentVolumes.Add(volume);
                                smallestVolume = Math.Min(smallestVolume, volume);
                            }
                            else
                            {
                                smallestVolume = Math.Min(smallestVolume, volume);
                                separatedSegments.Add(group);
                                segmentVolumes.Add(volume);
                            }
                        }
                    }
                }
            }
            return separatedSegments;
        }
        public static List<SegmentData> Separate (SegmentData segment, int maxSegments)
        {
            return segment.Separate(maxSegments);
        }



    }

    // Generate a segment of all pixels below the threshold intensity
    public static SegmentData GetBlackFromPixelIntensities (float[,,] intensities, float threshold)
    {
        SegmentData segment = new SegmentData(intensities.GetLength(0), intensities.GetLength(1), intensities.GetLength(2));
        for (int x = 0; x < segment.width; x++)
        {
            for (int y = 0; y < segment.height; y++)
            {
                for (int layer = 0; layer < segment.depth; layer++)
                {
                    segment[x, y, layer] = intensities[x, y, layer] < threshold;
                }
            }
        }
        return segment;
    }

    public static Texture3D GetPaddedTexture3DFromSegment (SegmentData segment)
    {
        List<Color> colors = new List<Color>();

        // add a black pixel if part of segment, white if not
        for (int x = 0; x < segment.width; x++)
        {
            for (int y = 0; y < segment.height; y++)
            {
                for (int layer = 0; layer < segment.depth; layer++)
                {
                    colors.Add(segment[x, y, layer] ? Color.black : Color.white);
                }
            }
        }


        int paddedWidth = Mathf.NextPowerOfTwo(segment.width);
        int paddedHeight = Mathf.NextPowerOfTwo(segment.height);
        int paddedDepth = Mathf.NextPowerOfTwo(segment.depth);
        Texture3D result = new Texture3D(paddedWidth, paddedHeight, paddedDepth, TextureFormat.ARGB32, false);

        return result;
    }
}
