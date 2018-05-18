using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ExtensionMethods;
using SegmentationData;

/// <summary>
/// This class provides static methods for basic segment generation.
/// </summary>
public class SimpleSegmentation {

    /// <summary>
    /// Generate a segment of all pixels below the threshold intensity
    /// </summary>
    /// <param name="intensities"></param>
    /// <param name="threshold"></param>
    /// <returns></returns>
    public static LegendBooleans GetBlackFromPixelIntensities (float[,,] intensities, float threshold)
    {
        LegendBooleans segment = new LegendBooleans(intensities.GetLength(0), intensities.GetLength(1), intensities.GetLength(2));
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
}
