using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuScript : MonoBehaviour {

	public string menuButtonName;			// button to show the menu (Unity input manager)

	// state tracking
    private bool menuActive;				
    private bool menuButtonReady;

	// Use this for initialization
	void Start () {
        menuButtonReady = true;
        menuActive = false;
    }

    // Update is called once per frame
    void Update () {
		handleInput ();
    }

	// toggle the menu
	private void handleInput () {
		if( menuButtonReady && Input.GetAxis( menuButtonName ) >= 1 ) {
			menuButtonReady = false;
			menuActive = !menuActive;
			activateMenu( menuActive );
		}
		else if( !menuButtonReady && Input.GetAxis( menuButtonName ) < 0.1 ) {
			menuButtonReady = true;
		}
	}

    private void activateMenu(bool active) {
        foreach( Transform menuItemTransform in gameObject.transform ) {
            menuItemTransform.gameObject.SetActive( active );
        }
    }
}
