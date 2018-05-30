using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RenderData;

public class LegendShaderInterface : MonoBehaviour {

    [SerializeField] private Renderer LegendRenderer;

	public void SendScanVolumeToShader (ScanVolume volume)
	{
		if (volume != null) 
		{
			LegendRenderer.material.SetTexture ("Cadaver_Data", volume.GetVolume ());
		}
	}

	public void SendLegendToShader (LegendVolume volume) 
	{
		if (volume != null)
		{
        	LegendRenderer.material.SetTexture("Legend_Data", volume.GetVolume());
		}
	}

}
