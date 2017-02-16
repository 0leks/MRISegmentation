using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrabScript : MonoBehaviour {

    public string moveButtonName; // the name of the button in the input manager
    public string rotateButtonName; // the name of the button in the input manager
    public GameObject cube;   // the object which is used to render segments
    public GameObject grabbingSphere;

    private bool grabMoveState;
    private bool grabRotateState;
    private Vector3 offset;
    private Quaternion rotation;

	void Start () {
        grabMoveState = false;
        grabRotateState = false;
    }
	
	void Update () {
        //Debug.Log(Input.GetAxis(buttonName));
		if(!grabMoveState && Input.GetAxis(moveButtonName) >= 1 )
        {
            //Debug.LogError("Grabbing! " + moveButtonName + " at " + Time.time);
            grabMoveState = true;
            offset = cube.transform.position - grabbingSphere.transform.position;
        }

        if (grabMoveState && Input.GetAxis(moveButtonName) < 1)
        {
            //Debug.LogError("Releasing! " + moveButtonName + " at " + Time.time);
            grabMoveState = false;
        }

        if (!grabRotateState && Input.GetAxis(rotateButtonName) >= 1)
        {
            //Debug.LogError("Grabbing! " + rotateButtonName + " at " + Time.time);
            grabRotateState = true;
            rotation = Quaternion.Inverse(grabbingSphere.transform.rotation) * cube.transform.rotation;
        }

        if (grabRotateState && Input.GetAxis(rotateButtonName) < 1)
        {
            //Debug.LogError("Releasing! " + rotateButtonName + " at " + Time.time);
            grabRotateState = false;
        }

        if (grabMoveState)
        {
            cube.transform.position = grabbingSphere.transform.position + offset;
        }
        if(grabRotateState)
        {
            cube.transform.rotation = grabbingSphere.transform.rotation * rotation;
        }
    }

    public void ReleaseObject()
    {
        grabMoveState = false;
        grabRotateState = false;
        //if (cube.transform.parent == grabbingSphere.transform)
        //{
        //    cube.transform.parent = null;
        //}
    }
}
