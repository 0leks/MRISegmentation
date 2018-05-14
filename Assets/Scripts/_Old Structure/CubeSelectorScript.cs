using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// This class tracks when the hand is inside or outside of the cube
public class CubeSelectorScript : MonoBehaviour {

    public GrabScript grabScript1;

    public bool RightHand;
    public GameObject Cube;

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    void OnTriggerEnter( Collider col ) {
        //Debug.Log( "Entered collision with " + col.gameObject.name );

        if (RightHand && (col.gameObject.CompareTag("CubeCut")))
        {
            Cube.GetComponent<CubeCut>().moveOn = true;
        }
        if (RightHand && (col.gameObject.CompareTag("CutoffPlane")))
        {
            Cube.GetComponent<CubeCut>().planeMoveOn = true;
            Cube.GetComponent<CubeCut>().SelectedPlane = col.gameObject;
        }
        
        grabScript1.SelectedGameObject( col.gameObject, true );
    }

    void OnTriggerExit( Collider col ) {
        //Debug.Log( "Exited collision with " + col.gameObject.name );

        if (RightHand && (col.gameObject.CompareTag("CubeCut")))
        {
            Cube.GetComponent<CubeCut>().moveOn = false;
        }
        if (RightHand && (col.gameObject.CompareTag("CutoffPlane")))
        {
            Cube.GetComponent<CubeCut>().planeMoveOn = false;
            Cube.GetComponent<CubeCut>().SelectedPlane = null;
        }
        grabScript1.SelectedGameObject( col.gameObject, false );

    }
}
