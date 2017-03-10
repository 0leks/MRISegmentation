using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SeedScript : MonoBehaviour {

    public string addObjectButtonName;
    public string addBackgroundButtonName;

    public GameObject tipOfWand;
    public GameObject renderCube;

    public ImageSegmentationHandler2 segmentationScript;

    private bool AddObjectActive;
    private bool AddBackgroundActive;

    // Use this for initialization
    void Start () {
    }
	
	// Update is called once per frame
	void Update () {
        if (AddObjectActive && Input.GetAxis(addObjectButtonName) == 1) {
            Vector3 asdf = renderCube.transform.InverseTransformPoint(tipOfWand.transform.position);
            segmentationScript.AddSeed(asdf.x, asdf.y, asdf.z, true, tipOfWand.transform.position);
            AddObjectActive = false;
        }
        if(Input.GetAxis(addObjectButtonName) < 0.1)
        {
            AddObjectActive = true;
        }

        if (AddBackgroundActive && Input.GetAxis(addBackgroundButtonName) == 1)
        {
            Vector3 asdf = renderCube.transform.InverseTransformPoint(tipOfWand.transform.position);
            segmentationScript.AddSeed(asdf.x, asdf.y, asdf.z, false, tipOfWand.transform.position);
            AddBackgroundActive = false;
        }
        if (Input.GetAxis(addBackgroundButtonName) < 0.1)
        {
            AddBackgroundActive = true;
        }
    }
}
