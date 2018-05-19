using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ScanData;

public class SimpleMRIViewer : MonoBehaviour {

    [SerializeField] private int sliceCount;
    [SerializeField] private Renderer LegendRenderer;

    private ScanSlices m_ScanSlices;
    private ScanVolume m_ScanVolume;

    private int lastSliceCount;

	// Use this for initialization
	void Start () {
        m_ScanSlices = new ScanSlices(
            Application.dataPath + "/Resources/scans/colon/pgm-",
            ScanSlices.SliceFormat.jpg,
            sliceCount);

        m_ScanVolume = new ScanVolume(m_ScanSlices);
        LegendRenderer.material.SetTexture("Cadaver_Data", m_ScanVolume.GetVolume());
    }
}
