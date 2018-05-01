using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuButtonScript : MonoBehaviour {

	private float countdown;											// length button shows active when pressed (green highlight)
	private bool countingDown;											// whether or not button was just hit (prevent double-presses)
    private bool canceled;												// button press canceled or not
    private const float MAX_COUNTDOWN = 2.0f;							// delay before non-canceled button press takes the next input

	// button state colors
    private Color restingColor = new Color( 0.7f, 0.7f, 0.7f, 0.7f );
    private Color activeColor = new Color( 1, 1, 1, 1 );
    private Color activatedColor = new Color( 0, 1, 0, 1 );
    private Color canceledColor = new Color( 1, 0, 0, 1 );

    // Use this for initialization
    void Start () {
        gameObject.GetComponent<Renderer>().material.color = restingColor;
    }
	
	// Update is called once per frame
	void Update () {
        if( countdown >= 0 ) {
            countdown -= Time.deltaTime;
        } else if( countingDown ) {
            gameObject.GetComponent<Renderer>().material.color = restingColor;
            countingDown = false;
            canceled = false;
        }
	}

    void OnTriggerEnter( Collider col ) {
        if( !countingDown || canceled ) {
            Debug.Log( "Entered collision with " + col.gameObject.name );
            gameObject.GetComponent<Renderer>().material.color = activeColor;
            countingDown = false;
            canceled = false;
        }
    }
    void OnTriggerStay( Collider col ) {
        if( !countingDown || canceled ) {
            OnTriggerEnter( col );
        }
    }

    void OnTriggerExit( Collider col ) {
        Debug.Log( "Exited collision with " + col.gameObject.name );
        // Check if exited thru the bottom
        if( !countingDown ) {
            if( gameObject.transform.position.y - col.gameObject.transform.position.y > 0 ) {
                this.GetComponent<Button>().onClick.Invoke();
                Debug.Log( "calling func" );
                gameObject.GetComponent<Renderer>().material.color = activatedColor;
                countdown = MAX_COUNTDOWN/2;
            }
            else {
                gameObject.GetComponent<Renderer>().material.color = canceledColor;
                countdown = MAX_COUNTDOWN;
                canceled = true;
            }
            countingDown = true;
        }
    }
}
