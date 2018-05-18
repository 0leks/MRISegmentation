using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ScanData;

public class SimpleMRIViewer : MonoBehaviour {

    [SerializeField] private int sliceCount;
    [SerializeField] private Renderer LegendRenderer;

    private ScanSlices m_Slices;
    private ScanVolume m_Volume;

    private int lastSliceCount;

	// Use this for initialization
	void Start () {
        m_Slices = new ScanSlices("scans/colon/pgm-", sliceCount);
        m_Volume = new ScanVolume(m_Slices);
        SendCadaverToShader(m_Volume);

        lastSliceCount = sliceCount;
    }

    void Update()
    {
        DynamicSliceCount();
    }

    void DynamicSliceCount ()
    {
        // clamp
        if (sliceCount < 0)
            sliceCount = 0;
        if (sliceCount > m_Slices.GetSlices().Count)
            sliceCount = m_Slices.GetSlices().Count;

        // if it's changed, generate a new volume for the shader
        if (sliceCount != lastSliceCount)
        {
            m_Volume = new ScanVolume(m_Slices.GetRange(0, sliceCount));
            SendCadaverToShader(m_Volume);
        }

        lastSliceCount = sliceCount;
    }

    void SendCadaverToShader (ScanVolume scanVolume)
    {
        LegendRenderer.material.SetTexture("Cadaver_Data", scanVolume.GetVolume());
    }
}
