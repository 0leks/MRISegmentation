using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraScript : MonoBehaviour {

    void Start() {
        GetComponent<Camera>().depthTextureMode = DepthTextureMode.Depth;
    }
}
