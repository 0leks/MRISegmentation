using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        Debug.Log( "Entered collision with " + col.gameObject.name );

        if (RightHand && (col.gameObject.CompareTag("CubeCut")))
        {
            Cube.GetComponent<CubeCut>().moveOn = true;
        }
        grabScript1.SelectedGameObject( col.gameObject, true );
    }

    void OnTriggerExit( Collider col ) {
        Debug.Log( "Exited collision with " + col.gameObject.name );

        if (RightHand && (col.gameObject.CompareTag("CubeCut")))
        {
            Cube.GetComponent<CubeCut>().moveOn = false;
        }
        grabScript1.SelectedGameObject( col.gameObject, false );

    }
}
