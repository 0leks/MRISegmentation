using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WireFrameScript : MonoBehaviour {

    public GameObject meshCube;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    private void OnTriggerEnter(Collider other) {
        Debug.Log("entered trigger");
        GetComponent<MeshRenderer>().enabled = true;
        //this.gameObject.SetActive(false);
        //wireFrameCube.SetActive(true);
    }
    private void OnTriggerExit(Collider other) {
        GetComponent<MeshRenderer>().enabled = false;
        //this.gameObject.SetActive(false);
        //wireFrameCube.SetActive(false);
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(GetComponent<BoxCollider>().center, GetComponent<BoxCollider>().size);
    }
}
