using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using ScanData;
using SegmentationData;

// Runs the flood fill / region grow

public class RegionGrow : ThreadedJob
{

    private ScanIntensities m_ScanIntensities;
    private List<Vector3Int> m_SeedPoints;
    private int m_Width;
    private int m_Height;
    private int m_Depth;
    private float m_Threshold; // max difference between pixel intensities allowed for neighbor to be included in foreground

    private LegendBooleans m_LegendBooleans;
    public Action onFinished; 

    public RegionGrow(ScanIntensities scanIntensities, List<Vector3Int> seedPoints, int width, int height, int depth, float threshold)
    {
        m_ScanIntensities = scanIntensities;
        m_SeedPoints = seedPoints;
        m_Width = width;
        m_Height = height;
        m_Depth = depth;
        m_Threshold = threshold;
    }

    public LegendBooleans GetLegendBooleans()
    {
        return m_LegendBooleans;
    }

    // this runs in the new thread
    protected override void ThreadFunction()
    {
        Debug.LogError("beginning flood fill segmenting");
        RegionGrowAlgorithm();
        Debug.LogError("finished flood fill segmenting");
    }

    // This runs in the main thread
    protected override void OnFinished()
    {
        Debug.LogError("entered OnFinished for flood fill");
        onFinished();
    }

    private void RegionGrowAlgorithm()
    {

        m_LegendBooleans = new LegendBooleans(m_Width, m_Height, m_Depth);

        Stack<Vector3Int> searchArea = new Stack<Vector3Int>();
        Stack<float> searchAreaFrom = new Stack<float>();

        foreach (Vector3Int seed in m_SeedPoints)
        {
            searchArea.Push(new Vector3Int(seed.x, seed.y, seed.z));
            searchAreaFrom.Push(m_ScanIntensities[seed.x, seed.y, seed.z]);
        }

        while (searchArea.Count > 0)
        {
            Vector3Int point = searchArea.Pop();
            float from = searchAreaFrom.Pop();

            if (point.x >= 0 && point.x < m_Width && point.y >= 0 && point.y < m_Height && point.z >= 0 && point.z < m_Depth)
            {
                if (!m_LegendBooleans[point.x, point.y, point.z])
                {
                    float color = m_ScanIntensities[point.x, point.y, point.z];
                    float diff = Mathf.Abs(from - color);
                    if (diff <= m_Threshold)
                    {
                        m_LegendBooleans[point.x, point.y, point.z] = true;
                        searchArea.Push(new Vector3Int(point.x - 1, point.y, point.z));
                        searchArea.Push(new Vector3Int(point.x + 1, point.y, point.z));
                        searchArea.Push(new Vector3Int(point.x, point.y - 1, point.z));
                        searchArea.Push(new Vector3Int(point.x, point.y + 1, point.z));
                        searchArea.Push(new Vector3Int(point.x, point.y, point.z + 1));
                        searchArea.Push(new Vector3Int(point.x, point.y, point.z - 1));
                        searchAreaFrom.Push(color);
                        searchAreaFrom.Push(color);
                        searchAreaFrom.Push(color);
                        searchAreaFrom.Push(color);
                        searchAreaFrom.Push(color);
                        searchAreaFrom.Push(color);
                    }
                }
            }
        }

    }



}
