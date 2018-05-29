using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ScanData;
using SegmentationData;

public class SimpleMRIViewer : MonoBehaviour {

    [SerializeField] private int sliceCount;
    [SerializeField] private float regionGrowThreshold;
    [SerializeField] private Renderer LegendRenderer;

    private ScanSlices m_ScanSlices;
    private ScanVolume m_ScanVolume;
    private ScanIntensities m_ScanIntensities;

    private LegendBooleans m_LegendBooleans;
    private LegendVolume m_LegendVolume;

    private int lastSliceCount;

    private ThreadManager m_ThreadManager;
    private SegmentationManager m_SegmentationManager;

    // Use this for initialization
    void Start()
    {
        m_ScanSlices = new ScanSlices(
            Application.dataPath + "/Resources/scans/colon/pgm-",
            ScanSlices.SliceFormat.jpg,
            sliceCount);

        m_ScanIntensities = new ScanIntensities(m_ScanSlices);
        m_ScanVolume = new ScanVolume(m_ScanSlices);
        LegendRenderer.material.SetTexture("Cadaver_Data", m_ScanVolume.GetVolume());

        m_LegendBooleans = new LegendBooleans(m_ScanIntensities.width, m_ScanIntensities.height, m_ScanIntensities.depth);
        m_LegendBooleans.Invert();
        m_LegendVolume = new LegendVolume(m_LegendBooleans);
        LegendRenderer.material.SetTexture("Legend_Data", m_LegendVolume.GetVolume());

        m_ThreadManager = new ThreadManager();
        m_SegmentationManager = new SegmentationManager(LegendRenderer, m_LegendVolume, m_ThreadManager, m_ScanIntensities);

        m_SegmentationManager.AddSeedPoint(0f, 0f, 0f, true);
        m_SegmentationManager.RegionGrow(regionGrowThreshold);
    }

    void Update()
    {
        m_ThreadManager.Update();
    }
}
