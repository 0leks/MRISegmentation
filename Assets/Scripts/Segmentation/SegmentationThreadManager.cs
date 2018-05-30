using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ExtensionMethods;
using ScanData;
using SegmentationData;

/// <summary>
/// This class provides methods for segment generation.
/// </summary>
public class SegmentationThreadManager : ThreadManager
{
    public Action<LegendBooleans> onSegmentationFinished;

	public void StartRegionGrow(ScanIntensities intensities, SeedPoints seedPoints, float threshold)
    {
        RegionGrow regionGrowJob = new RegionGrow(intensities, seedPoints.GetForeground(), threshold);
        regionGrowJob.onFinished += SegmentationFinished;
        AddThread(regionGrowJob);
    }

	/// <summary>
	/// Called with the result of a finished segmentation algorithm. </summary>
	/// <param name="legendBooleans">Result of the segmentation.</param>
    private void SegmentationFinished(LegendBooleans legendBooleans)
    {
        onSegmentationFinished(legendBooleans);
    }
}
