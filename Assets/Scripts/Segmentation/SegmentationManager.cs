using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ExtensionMethods;
using ScanData;
using SegmentationData;

/// <summary>
/// This class provides static methods for segment generation.
/// </summary>
public class SegmentationManager
{

    private ThreadManager m_ThreadManager;
    private ScanIntensities m_ScanItensities;
    private SeedPoints m_SeedPoints;

    public Action<LegendBooleans> onSegmentationFinished;


    public SegmentationManager(ThreadManager threadManager, ScanIntensities scanIntensities, SeedPoints seedPoints)
    {
        m_ThreadManager = threadManager;
        m_ScanItensities = scanIntensities;
        m_SeedPoints = seedPoints;
    }

    public void RegionGrow(float threshold)
    {
        RegionGrow regionGrowJob = new RegionGrow(m_ScanItensities, m_SeedPoints.GetForeground(), threshold);
        regionGrowJob.onFinished += FinishedSegmentation;
        m_ThreadManager.AddThread(regionGrowJob);
    }

    public void MaxFlow()
    {
        //
    }

    private void FinishedSegmentation(LegendBooleans legendBooleans)
    {
        onSegmentationFinished(legendBooleans);
    }
}
