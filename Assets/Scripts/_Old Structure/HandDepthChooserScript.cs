using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Attach to the hand. Creates a volume around the hand that cuts through the
// render cube.
public class HandDepthChooserScript : MonoBehaviour {

    public GameObject renderCube;			// MRI data render cube
    public Transform _camera;				// camera position

    private bool doCut = true;				// enabled/disabled

	public float overlap = 0.03f;			// hand-camera distance offset (TODO what does this do?)

    // Use this for initialization
    void Start () {
    }
	
	// Update is called once per frame
	void Update () {
        float distance = Vector3.Magnitude(GetComponent<Transform>().position - _camera.position);
        distance -= overlap;

        if (!doCut) {
            distance = 0;
        }

		// send distance to the shader
        renderCube.GetComponent<Renderer>().material.SetFloat("_DepthMult", distance);

        //Debug.Log("hand to camera distance:" + distance);
        //Debug.Log("depth mult: " + renderCube.GetComponent<Renderer>().material.GetFloat("_DepthMult"));
    }

    public void toggleCut() {
        doCut = !doCut;
    }

}
