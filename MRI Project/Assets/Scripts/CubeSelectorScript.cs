using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeSelectorScript : MonoBehaviour {

    public GrabScript grabScript1;

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    void OnTriggerEnter( Collider col ) {
        Debug.Log( "Entered collision with " + col.gameObject.name );
        grabScript1.SelectedGameObject( col.gameObject, true );
    }

    void OnTriggerExit( Collider col ) {
        Debug.Log( "Exited collision with " + col.gameObject.name );
        grabScript1.SelectedGameObject( col.gameObject, false );

    }
}
