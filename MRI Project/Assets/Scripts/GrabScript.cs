using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrabScript : MonoBehaviour {

    public string moveButtonName; // the name of the button in the input manager
    public string rotateButtonName; // the name of the button in the input manager
    public GameObject grabbingSphere;

    public GameObject displayCube;

    public GameObject CubeCut;
    public bool RightHand;
    private bool FirstGrab;

    private bool grabMoveState;
    private bool grabRotateState;

    private List<GameObject> cubes;   // the object which is used to render segments
    private List<Vector3> offsets;
    private List<Quaternion> rotations;
    private List<bool> grabbedMove;
    private List<bool> grabbedRotate;
    private List<bool> selected;

    public void SelectedGameObject( GameObject obj, bool select ) {
        for( int index = 0; index < cubes.Count; index++ ) {
            if( cubes[index] == obj ) {
                selected[ index ] = select;
            }
        }
    }

    public void AddGrabbable(GameObject obj ) {
        cubes.Add( obj );
        offsets.Add( new Vector3( 0, 0, 0 ) );
        rotations.Add( new Quaternion() );
        grabbedMove.Add( false );
        grabbedRotate.Add( false );
        selected.Add( false );
    }

	void Start () {
        grabMoveState = false;
        grabRotateState = false;
        cubes = new List<GameObject>();
        offsets = new List<Vector3>();
        rotations = new List<Quaternion>();
        grabbedMove = new List<bool>();
        grabbedRotate = new List<bool>();
        selected = new List<bool>();
        AddGrabbable( displayCube );
    }
	
	void Update () {

        if (RightHand)
        {
            if (displayCube.GetComponent<CubeCut>().moveOn == true)
            {
                if (Input.GetAxis(moveButtonName) >= 1)
                {
                    if (FirstGrab)
                    {
                        displayCube.GetComponent<CubeCut>().Offset = CubeCut.transform.position - grabbingSphere.transform.position;
                        FirstGrab = false;
                    }
                    displayCube.GetComponent<CubeCut>().grabbing = true;
                }
                else
                {
                    displayCube.GetComponent<CubeCut>().grabbing = false;
                }
                return;
            }
        }


        //Debug.Log(Input.GetAxis(buttonName));
		if(!grabMoveState && Input.GetAxis(moveButtonName) >= 1 )
        {
            //Debug.LogError("Grabbing! " + moveButtonName + " at " + Time.time);
            grabMoveState = true;
            for( int index = 0; index < cubes.Count; index++ ) {
                if( selected[index] ) {
                    offsets[ index ] = cubes[ index ].transform.position - grabbingSphere.transform.position;
                    grabbedMove[ index ] = true;
                }
            }
        }

        if (grabMoveState && Input.GetAxis(moveButtonName) < 1)
        {
            //Debug.LogError("Releasing! " + moveButtonName + " at " + Time.time);
            grabMoveState = false;
            for( int index = 0; index < cubes.Count; index++ ) {
                grabbedMove[ index ] = false;
            }
        }

        if (!grabRotateState && Input.GetAxis(rotateButtonName) >= 1)
        {
            //Debug.LogError("Grabbing! " + rotateButtonName + " at " + Time.time);
            grabRotateState = true;
            for( int index = 0; index < cubes.Count; index++ ) {
                if( selected[ index ] ) {
                    rotations[ index ] = Quaternion.Inverse( grabbingSphere.transform.rotation ) * cubes[ index ].transform.rotation;
                    grabbedRotate[ index ] = true;
                }
            }
        }

        if (grabRotateState && Input.GetAxis(rotateButtonName) < 1)
        {
            //Debug.LogError("Releasing! " + rotateButtonName + " at " + Time.time);
            grabRotateState = false;
            for( int index = 0; index < cubes.Count; index++ ) {
                grabbedRotate[ index ] = false;
            }
        }

        if (grabMoveState) {
            for( int index = 0; index < cubes.Count; index++ ) {
                if( grabbedMove[index] ) {
                    cubes[ index ].transform.position = grabbingSphere.transform.position + offsets[ index ];
                }
            }
        }
        if(grabRotateState) {
            for( int index = 0; index < cubes.Count; index++ ) {
                if( grabbedRotate[index] ) {
                    cubes[ index ].transform.rotation = grabbingSphere.transform.rotation * rotations[ index ];
                }
            }
        }
    }

    public void ReleaseObject()
    {
        if (RightHand)
        {
            FirstGrab = false;
        }

        grabMoveState = false;
        grabRotateState = false;
    }
}
