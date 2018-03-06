﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandDepthChooserScript : MonoBehaviour {



    public GameObject renderCube;
    public GameObject _camera;
    private bool inside = false;

    // Use this for initialization
    void Start () {
    }
	
	// Update is called once per frame
	void Update () {
        float distance = Vector3.Magnitude(GetComponent<Transform>().position - _camera.GetComponent<Transform>().position);
        if( !inside ) {
            distance = 0;
        }
        renderCube.GetComponent<Renderer>().material.SetFloat("_DepthMult", distance);
        //Debug.Log("hand to camera distance:" + distance);
        //Debug.Log("depth mult: " + renderCube.GetComponent<Renderer>().material.GetFloat("_DepthMult"));
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
