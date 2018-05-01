using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// This class handles adding seed points to the scene. It communicates with ImageSegmentationHandler to place the points.
public class SeedScript : MonoBehaviour {

	public string addForegroundButtonName;					// foreground seed button name (Unity input manager)
	public string addBackgroundButtonName;					// background seed button name (Unity input manager)

    public GameObject tipOfWand;							// for placing seed points
    public GameObject renderCube;							// MRI data render cube

	public ImageSegmentationHandler2 segmentationScript;

	// state tracking
	private bool AddForegroundActive;
    private bool AddBackgroundActive;

    // Use this for initialization
    void Start () {
    }
	
	// Update is called once per frame
	void Update () {
        if (AddForegroundActive && Input.GetAxis(addForegroundButtonName) == 1) {
            Vector3 tipOfWandInCubeCoords = renderCube.transform.InverseTransformPoint(tipOfWand.transform.position);
            segmentationScript.AddSeedThreeD( tipOfWandInCubeCoords.x, tipOfWandInCubeCoords.y, tipOfWandInCubeCoords.z, true, tipOfWand.transform.position);
            AddForegroundActive = false;
        }
        if(Input.GetAxis(addForegroundButtonName) < 0.1) {
            AddForegroundActive = true;
        }

        if (AddBackgroundActive && Input.GetAxis(addBackgroundButtonName) == 1) {
            Vector3 tipOfWandInCubeCoords = renderCube.transform.InverseTransformPoint(tipOfWand.transform.position);
            segmentationScript.AddSeedThreeD( tipOfWandInCubeCoords.x, tipOfWandInCubeCoords.y, tipOfWandInCubeCoords.z, false, tipOfWand.transform.position);
            AddBackgroundActive = false;
        }
        if (Input.GetAxis(addBackgroundButtonName) < 0.1) {
            AddBackgroundActive = true;
        }
    }
}
