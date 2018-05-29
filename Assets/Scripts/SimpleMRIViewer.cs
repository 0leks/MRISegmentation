using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ScanData;
using SegmentationData;

public class SimpleMRIViewer : MonoBehaviour {

    private enum ScanName{ heart, colon};
    [SerializeField] private ScanName m_ScanName;
    [SerializeField] private int sliceCount;
    [SerializeField] private float regionGrowThreshold;
    [SerializeField] private Renderer LegendRenderer;

    private ScanSlices m_ScanSlices;
    private ScanVolume m_ScanVolume;
    private ScanIntensities m_ScanIntensities;
    private SeedPoints m_SeedPoints;

    private int lastSliceCount;

    private ThreadManager m_ThreadManager;
    private SegmentationManager m_SegmentationManager;

    // Use this for initialization
    void Start()
    {
        string path;
        ScanSlices.SliceFormat sliceFormat;
        if (m_ScanName == ScanName.heart)
        {
            path = "/Resources/scans/heart/heart-";
            sliceFormat = ScanSlices.SliceFormat.png3;
        }
        // m_ScanName == ScanName.colon
        else
        {
            path = "/Resources/scans/colon/pgm-";
            sliceFormat = ScanSlices.SliceFormat.jpg4;
        }

        m_ScanSlices = new ScanSlices(
            Application.dataPath + path,
            sliceFormat,
            sliceCount);

        m_ScanVolume = new ScanVolume(m_ScanSlices);
        m_ScanIntensities = new ScanIntensities(m_ScanSlices);
        m_SeedPoints = new SeedPoints(m_ScanIntensities.width, m_ScanIntensities.height, m_ScanIntensities.depth);
        LegendRenderer.material.SetTexture("Cadaver_Data", m_ScanVolume.GetVolume());

        LegendBooleans legendBooleans = new LegendBooleans(m_ScanIntensities.width, m_ScanIntensities.height, m_ScanIntensities.depth);
        legendBooleans.Invert();
        LegendVolume legendVolume = new LegendVolume(legendBooleans);
        LegendRenderer.material.SetTexture("Legend_Data", legendVolume.GetVolume());

        m_ThreadManager = new ThreadManager();
        // TODO: Currently not correctly updating m_LegendBooleans
        m_SegmentationManager = new SegmentationManager(m_ThreadManager, m_ScanIntensities, m_SeedPoints);
        m_SegmentationManager.onSegmentationFinished += SegmentationFinished;

    }

    void Update()
    {
        m_ThreadManager.Update();

        if (Input.GetKeyDown("r"))
        {
            m_SeedPoints.AddSeedPoint(new Vector3(0f, 0f, 0f), true);
            m_SegmentationManager.RegionGrow(regionGrowThreshold);
        }

    }

    private void SegmentationFinished(LegendBooleans legendBooleans)
    {
        UpdateLegendShader(legendBooleans);
    }

    private void UpdateLegendShader(LegendBooleans legendBooleans)
    {
        LegendVolume legendVolume = new LegendVolume(legendBooleans);
        LegendRenderer.material.SetTexture("Legend_Data", legendVolume.GetVolume());
    }
}
