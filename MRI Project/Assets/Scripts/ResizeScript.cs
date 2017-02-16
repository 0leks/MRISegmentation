using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResizeScript : MonoBehaviour {

    // the names of the buttons in the input manager
    public string LButtonName;
    public string RButtonName;

    // the two scripts which handle grabbing
    public GrabScript LScript;
    public GrabScript RScript;
    
    // the two controllers, for getting their positions
    public OVRInput.Controller LController;
    public OVRInput.Controller RController;

    public GameObject cube;   // the object which is used to render segments

    private bool grabState; //  keeps track of whether object is currently grabbed
    private float initialDistance;

    // Use this for initialization
    void Start () {
        grabState = false;
	}
	
	// Update is called once per frame
	void Update () {
		
        if(!grabState && Input.GetAxis(LButtonName) >= 1 && Input.GetAxis(RButtonName) >= 1)
        {
            grabState = true;
            LScript.ReleaseObject();
            RScript.ReleaseObject();
            LScript.enabled = false;
            RScript.enabled = false;

            initialDistance = Vector3.Distance(OVRInput.GetLocalControllerPosition(LController), OVRInput.GetLocalControllerPosition(RController));


        }
        if(grabState && (Input.GetAxis(LButtonName) < 1 || Input.GetAxis(RButtonName) < 1))
        {
            grabState = false;
            LScript.enabled = true;
            RScript.enabled = true;
        }

        if( grabState )
        {
            float currentDistance = Vector3.Distance(OVRInput.GetLocalControllerPosition(LController), OVRInput.GetLocalControllerPosition(RController));
            float ratio = cube.transform.localScale.x * currentDistance / initialDistance;
            initialDistance = currentDistance;
            cube.transform.localScale = new Vector3(ratio, ratio, ratio);
        }
	}
}
