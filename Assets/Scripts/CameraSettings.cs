using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// Settings for the camera
public class CameraSettings : MonoBehaviour {

    void Start() {
        GetComponent<Camera>().depthTextureMode = DepthTextureMode.Depth;
    }
}
