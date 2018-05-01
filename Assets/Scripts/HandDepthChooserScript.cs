using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandDepthChooserScript : MonoBehaviour {

    public GameObject renderCube;			// MRI data render cube
    public Transform _camera;

    private bool inside = false;
    private bool doCut = true;

    public float overlap = 0.05f;

    // Use this for initialization
    void Start () {
    }
	
	// Update is called once per frame
	void Update () {
        float distance = Vector3.Magnitude(GetComponent<Transform>().position - _camera.position);
        distance -= overlap;
        // I think shouldnt suddenly stop when you go outside of the cube.
        //if ( !inside || !doCut) {
        if (!doCut) {
            distance = 0;
        }
        renderCube.GetComponent<Renderer>().material.SetFloat("_DepthMult", distance);
        //Debug.Log("hand to camera distance:" + distance);
        //Debug.Log("depth mult: " + renderCube.GetComponent<Renderer>().material.GetFloat("_DepthMult"));
    }

    public void toggleCut() {
        doCut = !doCut;
    }
    private void OnTriggerEnter(Collider other) {
        if( other.gameObject.name == renderCube.name ) {
            inside = true;
        }
    }
    private void OnTriggerExit(Collider other) {
        if (other.gameObject.name == renderCube.name) {
            inside = false;
        }
    }
}
