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

    private Renderer m_LegendRenderer;
    private LegendVolume m_LegendVolume;
    private ThreadManager m_ThreadManager;
    private ScanIntensities m_ScanItensities;
    private int m_Width;
    private int m_Height;
    private int m_Depth;

    private RegionGrow m_RegionGrowJob;

    private List<Vector3Int> m_ForegroundSeedPoints;
    private List<Vector3Int> m_BackgroundSeedPoints;


    public SegmentationManager(Renderer legendRenderer, LegendVolume legendVolume, ThreadManager programThreadManager, ScanIntensities scanIntensities)
    {
        m_LegendRenderer = legendRenderer;
        m_LegendVolume = legendVolume;
        m_ThreadManager = programThreadManager;
        m_ScanItensities = scanIntensities;
        m_Width = scanIntensities.width;
        m_Height = scanIntensities.height;
        m_Depth = scanIntensities.depth;
    }

    public void RegionGrow(float threshold)
    {
        if (m_RegionGrowJob != null) return;
        m_RegionGrowJob = new RegionGrow(m_ScanItensities, m_ForegroundSeedPoints, m_Width, m_Height, m_Depth, threshold);
        m_RegionGrowJob.onFinished += FinishedRegionGrow;
        m_ThreadManager.AddThread(m_RegionGrowJob);
    }

    public void MaxFlow()
    {
        //
    }

    // gets called in OnFinished of RegionGrowingJob
    private void FinishedRegionGrow()
    {
        UpdateShader(m_RegionGrowJob.GetLegendBooleans());
        m_RegionGrowJob = null;
    }

    private void UpdateShader(LegendBooleans legendBooleans)
    {
        m_LegendVolume = new LegendVolume(legendBooleans);
        m_LegendRenderer.material.SetTexture("Legend_Data", m_LegendVolume.GetVolume());
    }


    public void AddSeedPoint(float x, float y, float z, bool foreground)
    {
        int xCoord = (int)((0.5f - x) * m_Width);
        int yCoord = (int)((0.5f - z) * m_Height);
        int zCoord = (int)((y + 0.5f) * m_Depth);
        
        if (foreground)
        {
            m_ForegroundSeedPoints.Add(new Vector3Int(xCoord, yCoord, zCoord));
        }
        else
        {
            m_BackgroundSeedPoints.Add(new Vector3Int(xCoord, yCoord, zCoord));
        }
    }










    /// <summary>
    /// Generate a segment of all pixels below the threshold intensity
    /// </summary>
    /// <param name="intensities"></param>
    /// <param name="threshold"></param>
    /// <returns></returns>
    public static LegendBooleans GetBlackFromPixelIntensities(float[,,] intensities, float threshold)
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
