using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ScanData;
using SegmentationData;

public class SimpleMRIViewer : MonoBehaviour {

    [SerializeField] private int sliceCount;
    [SerializeField] private Renderer LegendRenderer;

    private ScanSlices m_ScanSlices;
    private ScanVolume m_ScanVolume;
    private ScanIntensities m_ScanIntensities;

    private LegendBooleans m_LegendBooleans;
    private LegendVolume m_LegendVolume;

    private int lastSliceCount;

	// Use this for initialization
	void Start () {
        m_ScanSlices = new ScanSlices(
            Application.dataPath + "/Resources/scans/colon/pgm-",
            ScanSlices.SliceFormat.jpg,
            sliceCount);

        m_ScanVolume = new ScanVolume(m_ScanSlices);
        LegendRenderer.material.SetTexture("Cadaver_Data", m_ScanVolume.GetVolume());


        m_ScanIntensities = new ScanIntensities(m_ScanSlices);
        // TODO: implement RegionGrow:
        // m_LegendBoolean = RegionGrow (m_ScanIntensities);
        m_LegendVolume = new LegendVolume(m_LegendBooleans);
        LegendRenderer.material.SetTexture("Legend_Data", m_LegendVolume.GetVolume());
    }
}
