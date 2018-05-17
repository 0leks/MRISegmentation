using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RenderManager : MonoBehaviour {

    Renderer m_LegendRenderer;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    void UpdateCadaver (Texture3D scanVolume)
    {
        m_LegendRenderer.material.SetTexture("Cadaver_Data", scanVolume);
    }

    void UpdateLegend (Texture3D legendVolume)
    {
        m_LegendRenderer.material.SetTexture("Legend_Data", legendVolume);
    }
}
