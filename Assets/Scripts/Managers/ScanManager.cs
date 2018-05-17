using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This class manages creation, storing and modification of Scan Data.
/// </summary>
public class ScanManager : MonoBehaviour {

    public Scans.ScansData m_ScanData;

	// Use this for initialization
	void Start () {
        m_ScanData = new Scans.ScansData();
	}
	
    /// <summary>
    ///     Load textures, intensities, and a scan volume
    /// </summary>
    /// <param name="filepath"></param>
    /// <param name="numLayers"></param>
    public void LoadScanData (string filepath, int numLayers)
    {
        LoadScanData(Scans.LoadTexturesFromFiles(filepath, numLayers));
    }

    public void LoadScanData (List<Texture2D> scanTextures)
    {
        m_ScanData.slices = new List<Texture2D>(scanTextures);
        m_ScanData.intensities = Scans.GetIntensitiesFromTextures(m_ScanData.slices);
        m_ScanData.scanVolume = Scans.GetPaddedTexture3DFromTextures(m_ScanData.slices);
    }

}
