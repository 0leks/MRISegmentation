using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
///     Struct for storing and retrieving segment data.
///     Segment data is represented as a 3D boolean array, where
///     true denotes that index is part of a segment, and false
///     denotes that it is not.
/// </summary>
namespace SegmentationData
{
    public class LegendBooleans
    {
        public bool[,,] booleans;
        public int width, height, depth;

        public LegendBooleans(int width, int height, int numSlices)
        {
            booleans = new bool[width, height, numSlices];
            this.width = width;
            this.height = height;
            depth = numSlices;
        }

        public LegendBooleans(bool[,,] data)
        {
            this.booleans = (bool[,,])data.Clone();
            width = data.GetLength(0);
            height = data.GetLength(1);
            depth = data.GetLength(2);
        }

        // return a deep copy
        public LegendBooleans Clone()
        {
            return new LegendBooleans((bool[,,])booleans.Clone());
        }

        // override [,,] operator for getting and setting
        public bool this[int x, int y, int sliceIndex]
        {
            get { return booleans[x, y, sliceIndex]; }
            set { booleans[x, y, sliceIndex] = value; }
        }


        // invert all booleans in the segment
        public void Invert()
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
        public static LegendBooleans Invert(LegendBooleans segment)
        {
            LegendBooleans result = segment.Clone();
            result.Invert();
            return result;
        }

        // TODO: learn what this function is doing (it separates a segment into multiple, but in what way and why?
        public List<LegendBooleans> Separate(int maxSegments)
        {
            // do DFS from each point unless its already visited. Each iteration of DFS increment group counter
            // finally for each group create a new bool[,,] and assign values to true where the group is.
            LegendBooleans segmentCopy = Clone();
            List<LegendBooleans> separatedSegments = new List<LegendBooleans>();
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
                            LegendBooleans group = new LegendBooleans(width, height, depth);
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
        public static List<LegendBooleans> Separate(LegendBooleans segment, int maxSegments)
        {
            return segment.Separate(maxSegments);
        }
    }
}