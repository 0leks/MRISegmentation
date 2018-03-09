using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeCut : MonoBehaviour {

    public GameObject MiniCube;
    public GameObject Pointer;
    public string Vertical;

    public bool cutOn = false;
    public bool moveOn = false;
    public bool grabbing = false;
    public Vector3 Offset;


    void Start () {
        Offset = new Vector3(0, 0, 0);
	}


    void Update()
    {
        if (cutOn)
        {
            //Update Position
            if (moveOn && grabbing)
            {
                MiniCube.transform.position = Pointer.transform.position + Offset;
                
                //scale 
                if ((Input.GetAxis(Vertical) >= 0.25))
                {
                    MiniCube.transform.localScale = (1.01f) * (MiniCube.transform.localScale);
                }

                else if ((Input.GetAxis(Vertical) <= -0.25))
                {
                    MiniCube.transform.localScale = (0.99f) * (MiniCube.transform.localScale);
                }
                

            }

            //Set shader
            Vector3 Position = MiniCube.transform.localPosition;
            Vector3 Scale = MiniCube.transform.localScale;

            float xMin = Position.x - (Scale.x / 2.0f);
            float xMax = Position.x + (Scale.x / 2.0f);
            float yMin = Position.y - (Scale.y / 2.0f);
            float yMax = Position.y + (Scale.y / 2.0f);
            float zMin = Position.z - (Scale.z / 2.0f);
            float zMax = Position.z + (Scale.z / 2.0f);

            float xMinLarge = -0.5f;
            float xMaxLarge = 0.5f;
            float yMinLarge = -0.5f;
            float yMaxLarge = 0.5f;
            float zMinLarge = -0.5f;
            float zMaxLarge = 0.5f;

            if (xMin > xMinLarge)
            {
                xMin = (xMin - xMinLarge) / (xMaxLarge - xMinLarge);
                this.GetComponent<Renderer>().material.SetFloat("_SliceAxis1Min", xMin);
            }
            else
            {
                this.GetComponent<Renderer>().material.SetFloat("_SliceAxis1Min", 0.0f);
            }
            if (xMax < xMaxLarge)
            {
                xMax = (xMax - xMinLarge) / (xMaxLarge - xMinLarge);
                this.GetComponent<Renderer>().material.SetFloat("_SliceAxis1Max", xMax);
            }
            else
            {
                this.GetComponent<Renderer>().material.SetFloat("_SliceAxis1Max", 1.0f);
            }

            if (yMin > yMinLarge)
            {
                yMin = (yMin - yMinLarge) / (yMaxLarge - yMinLarge);
                this.GetComponent<Renderer>().material.SetFloat("_SliceAxis2Min", yMin);
            }
            else
            {
                this.GetComponent<Renderer>().material.SetFloat("_SliceAxis2Min", 0.0f);
            }
            if (yMax < yMaxLarge)
            {
                yMax = (yMax - yMinLarge) / (yMaxLarge - yMinLarge);
                this.GetComponent<Renderer>().material.SetFloat("_SliceAxis2Max", yMax);
            }
            else
            {
                this.GetComponent<Renderer>().material.SetFloat("_SliceAxis2Max", 1.0f);
            }

            if (zMin > zMinLarge)
            {
                zMin = (zMin - zMinLarge) / (zMaxLarge - zMinLarge);
                this.GetComponent<Renderer>().material.SetFloat("_SliceAxis3Min", zMin);
            }
            else
            {
                this.GetComponent<Renderer>().material.SetFloat("_SliceAxis3Min", 0.0f);
            }
            if (zMax < zMaxLarge)
            {
                zMax = (zMax - zMinLarge) / (zMaxLarge - zMinLarge);
                this.GetComponent<Renderer>().material.SetFloat("_SliceAxis3Max", zMax);
            }
            else
            {
                this.GetComponent<Renderer>().material.SetFloat("_SliceAxis3Max", 1.0f);
            }

        }
    }


    public void ToggleCut()
    {
        if (cutOn)
        {
            cutOn = false;
            moveOn = false;
            grabbing = false;
            MiniCube.SetActive(false);
        }
        else
        {
            cutOn = true;
            MiniCube.transform.localPosition = Vector3.zero;
            MiniCube.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            MiniCube.SetActive(true);

        }
    }

    public void ResetCut()
    {
        if (cutOn)
        {
            ToggleCut();
        }

        this.GetComponent<Renderer>().material.SetFloat("_SliceAxis1Min", 0.0f);
        this.GetComponent<Renderer>().material.SetFloat("_SliceAxis1Max", 1.0f);
        this.GetComponent<Renderer>().material.SetFloat("_SliceAxis2Min", 0.0f);
        this.GetComponent<Renderer>().material.SetFloat("_SliceAxis2Max", 1.0f);
        this.GetComponent<Renderer>().material.SetFloat("_SliceAxis3Min", 0.0f);
        this.GetComponent<Renderer>().material.SetFloat("_SliceAxis3Max", 1.0f);
    }

}
