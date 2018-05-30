using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ScanData;
using SegmentationData;
using RenderData;

public class SimpleMRIViewer : MonoBehaviour {

	[SerializeField] private SegmentationThreadManager m_SegmentationThreadManager = new SegmentationThreadManager();
	[SerializeField] private LegendShaderInterface m_LegendShaderInterface;

	// Settings
    private enum ScanName{ heart, colon};
    [SerializeField] private ScanName m_ScanName;		// scan preset to load on start
    [SerializeField] private int sliceCount;			// number of slices to load
    [SerializeField] private float regionGrowThreshold;	// sensitivity threshold for region grow

	// Data
    private ScanSlices m_ScanSlices;
    private ScanVolume m_ScanVolume;
    private ScanIntensities m_ScanIntensities;
	private SeedPoints m_SeedPoints;


    // Use this for initialization
    void Start()
    {
		m_SegmentationThreadManager.onSegmentationFinished += UpdateShaderWithLegend;	// segmentation finished callback
		LoadScanByName (m_ScanName);													// load scans
		m_LegendShaderInterface.SendScanVolumeToShader (m_ScanVolume);					// send the scans to the shader for rendering
    }

    void Update()
    {
		m_SegmentationThreadManager.Update ();

        if (Input.GetKeyDown("r"))
        {
            m_SeedPoints.AddSeedPoint(new Vector3(0f, 0f, 0f), true);
			m_SegmentationThreadManager.StartRegionGrow(m_ScanIntensities, m_SeedPoints, regionGrowThreshold);
        }
    }
		
	private void LoadScanByName (ScanName scanName) {
        if (m_ScanName == ScanName.heart)
			LoadScan ("/Resources/scans/heart/heart-", ScanSlices.SliceFormat.png3);
        else
			LoadScan ("/Resources/scans/colon/pgm-", ScanSlices.SliceFormat.jpg4);
	}

	private void LoadScan (string path, ScanSlices.SliceFormat sliceFormat)
	{
        m_ScanSlices = new ScanSlices(Application.dataPath + path, sliceFormat, sliceCount);
        m_ScanVolume = new ScanVolume(m_ScanSlices);
        m_ScanIntensities = new ScanIntensities(m_ScanSlices);
		m_SeedPoints = new SeedPoints (m_ScanSlices.width, m_ScanSlices.height, m_ScanSlices.Count);
	}

    private void UpdateShaderWithLegend(LegendBooleans legendBooleans)
    {
		m_LegendShaderInterface.SendLegendToShader (new LegendVolume(legendBooleans));
    }
}
