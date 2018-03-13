using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeCut : MonoBehaviour
{

    public GameObject MiniCube;
    public GameObject Pointer;
    public string Vertical;
    public GameObject SelectedPlane;

    public bool cutOn = false;
    public bool moveOn = false;
    public bool grabbing = false;
    public Vector3 Offset;
    public bool planeMoveOn = false;
    public bool planeGrabbing = false;
    public Vector3 PlaneOffset;
    private int PlaneNum;

    public GameObject RightPlane;
    public GameObject LeftPlane;
    public GameObject UpPlane;
    public GameObject DownPlane;
    public GameObject ForwardPlane;
    public GameObject BackwardPlane;

    private Vector3 oldCubePos;
    private Vector3 oldPlanePos;



    void Start()
    {
        Offset = new Vector3(0, 0, 0);
    }


    void Update()
    {
        if (cutOn)
        {

            //move cutoff plane
            if (planeMoveOn && planeGrabbing)
            {

                oldCubePos = MiniCube.transform.position;
                oldPlanePos = SelectedPlane.transform.position;

                if (SelectedPlane == RightPlane)
                {
                    float displacement = Vector3.Dot(Pointer.transform.position - oldPlanePos, MiniCube.transform.right.normalized);
                    SelectedPlane.transform.position += ((displacement) * (MiniCube.transform.right));
                    AdaptMiniCubeRight(true);
                }
                else if (SelectedPlane == LeftPlane)
                {
                    float displacement = Vector3.Dot(Pointer.transform.position - oldPlanePos, MiniCube.transform.right.normalized);
                    SelectedPlane.transform.position += ((displacement) * (MiniCube.transform.right));
                    AdaptMiniCubeRight(false);
                }
                else if (SelectedPlane == UpPlane)
                {
                    float displacement = Vector3.Dot(Pointer.transform.position - oldPlanePos, MiniCube.transform.up.normalized);
                    SelectedPlane.transform.position += ((displacement) * (MiniCube.transform.up));
                    AdaptMiniCubeUp(true);
                }
                else if (SelectedPlane == DownPlane)
                {
                    float displacement = Vector3.Dot(Pointer.transform.position - oldPlanePos, MiniCube.transform.up.normalized);
                    SelectedPlane.transform.position += ((displacement) * (MiniCube.transform.up));
                    AdaptMiniCubeUp(false);
                }
                else if (SelectedPlane == ForwardPlane)
                {
                    float displacement = Vector3.Dot(Pointer.transform.position - oldPlanePos, MiniCube.transform.forward.normalized);
                    SelectedPlane.transform.position += ((displacement) * (MiniCube.transform.forward));
                    AdaptMiniCubeForward(true);
                }
                else if (SelectedPlane == BackwardPlane)
                {
                    float displacement = Vector3.Dot(Pointer.transform.position - oldPlanePos, MiniCube.transform.forward.normalized);
                    SelectedPlane.transform.position += ((displacement) * (MiniCube.transform.forward));
                    AdaptMiniCubeForward(false);
                }





            }

            //Update Position
            else if (moveOn && grabbing)
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


            SetPlaneLocations();
            SetPlaneScales();


            SetShader();

        }
    }

    public void AdaptMiniCubeRight(bool dir)
    {

        float diff = (SelectedPlane.transform.position - oldPlanePos).magnitude;
        if (Vector3.Dot(SelectedPlane.transform.position - oldPlanePos, MiniCube.transform.right) < 0)
        {
            diff = -diff;
        }

        if (dir)
        {
            Vector3 newScale = new Vector3(MiniCube.transform.localScale.x + diff, MiniCube.transform.localScale.y, MiniCube.transform.localScale.z);
            MiniCube.transform.localScale = newScale;
            MiniCube.transform.position = (LeftPlane.transform.position + SelectedPlane.transform.position) / 2.0f;
        }
        else
        {
            Vector3 newScale = new Vector3(MiniCube.transform.localScale.x - diff, MiniCube.transform.localScale.y, MiniCube.transform.localScale.z);
            MiniCube.transform.localScale = newScale;
            MiniCube.transform.position = (RightPlane.transform.position + SelectedPlane.transform.position) / 2.0f;
        }
    }

    public void AdaptMiniCubeUp(bool dir)
    {

        float diff = (SelectedPlane.transform.position - oldPlanePos).magnitude;
        if (Vector3.Dot(SelectedPlane.transform.position - oldPlanePos, MiniCube.transform.up) < 0)
        {
            diff = -diff;
        }

        if (dir)
        {
            Vector3 newScale = new Vector3(MiniCube.transform.localScale.x, MiniCube.transform.localScale.y + diff, MiniCube.transform.localScale.z);
            MiniCube.transform.localScale = newScale;
            MiniCube.transform.position = (DownPlane.transform.position + SelectedPlane.transform.position) / 2.0f;
        }
        else
        {
            Vector3 newScale = new Vector3(MiniCube.transform.localScale.x, MiniCube.transform.localScale.y - diff, MiniCube.transform.localScale.z);
            MiniCube.transform.localScale = newScale;
            MiniCube.transform.position = (UpPlane.transform.position + SelectedPlane.transform.position) / 2.0f;
        }
    }

    public void AdaptMiniCubeForward(bool dir)
    {

        float diff = (SelectedPlane.transform.position - oldPlanePos).magnitude;
        if (Vector3.Dot(SelectedPlane.transform.position - oldPlanePos, MiniCube.transform.forward) < 0)
        {
            diff = -diff;
        }

        if (dir)
        {
            Vector3 newScale = new Vector3(MiniCube.transform.localScale.x, MiniCube.transform.localScale.y, MiniCube.transform.localScale.z + diff);
            MiniCube.transform.localScale = newScale;
            MiniCube.transform.position = (BackwardPlane.transform.position + SelectedPlane.transform.position) / 2.0f;
        }
        else
        {
            Vector3 newScale = new Vector3(MiniCube.transform.localScale.x, MiniCube.transform.localScale.y, MiniCube.transform.localScale.z - diff);
            MiniCube.transform.localScale = newScale;
            MiniCube.transform.position = (ForwardPlane.transform.position + SelectedPlane.transform.position) / 2.0f;
        }
    }


    public void SetShader()
    {

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



    public void SetPlaneScales()
    {
        //Change scales of the planes to reflect new minicube
        Vector3 scaleVec = new Vector3(RightPlane.transform.localScale.x, (0.8f) * MiniCube.transform.localScale.y, (0.8f) * MiniCube.transform.localScale.z);
        RightPlane.transform.localScale = scaleVec;
        LeftPlane.transform.localScale = scaleVec;

        scaleVec = new Vector3(UpPlane.transform.localScale.x, (0.8f) * MiniCube.transform.localScale.x, (0.8f) * MiniCube.transform.localScale.z);
        UpPlane.transform.localScale = scaleVec;
        DownPlane.transform.localScale = scaleVec;

        scaleVec = new Vector3(ForwardPlane.transform.localScale.x, (0.8f) * MiniCube.transform.localScale.y, (0.8f) * MiniCube.transform.localScale.x);
        ForwardPlane.transform.localScale = scaleVec;
        BackwardPlane.transform.localScale = scaleVec;
    }

    public void SetPlaneLocations()
    {
        Vector3 scaleOffset;

        scaleOffset = new Vector3(MiniCube.transform.localScale.x / 2.0f, 0.0f, 0.0f);
        RightPlane.transform.localPosition = MiniCube.transform.localPosition + scaleOffset;
        LeftPlane.transform.localPosition = MiniCube.transform.localPosition - scaleOffset;

        scaleOffset = new Vector3(0.0f, MiniCube.transform.localScale.y / 2.0f, 0.0f);
        UpPlane.transform.localPosition = MiniCube.transform.localPosition + scaleOffset;
        DownPlane.transform.localPosition = MiniCube.transform.localPosition - scaleOffset;

        scaleOffset = new Vector3(0.0f, 0.0f, MiniCube.transform.localScale.z / 2.0f);
        ForwardPlane.transform.localPosition = MiniCube.transform.localPosition + scaleOffset;
        BackwardPlane.transform.localPosition = MiniCube.transform.localPosition - scaleOffset;
    }


    public void ToggleCut()
    {
        if (cutOn)
        {
            cutOn = false;
            moveOn = false;
            grabbing = false;
            MiniCube.SetActive(false);
            RightPlane.SetActive(false);
            LeftPlane.SetActive(false);
            UpPlane.SetActive(false);
            DownPlane.SetActive(false);
            ForwardPlane.SetActive(false);
            BackwardPlane.SetActive(false);
        }
        else
        {
            cutOn = true;
            MiniCube.transform.localPosition = Vector3.zero;
            MiniCube.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            MiniCube.SetActive(true);
            RightPlane.SetActive(true);
            LeftPlane.SetActive(true);
            UpPlane.SetActive(true);
            DownPlane.SetActive(true);
            ForwardPlane.SetActive(true);
            BackwardPlane.SetActive(true);


            /*
            RightPlane.transform.localPosition = new Vector3(0.25f,0.0f,0.0f);
            LeftPlane.transform.localPosition = new Vector3(-0.25f, 0.0f, 0.0f);
            UpPlane.transform.localPosition = new Vector3(0.0f, 0.25f, 0.0f);
            DownPlane.transform.localPosition = new Vector3(0.0f, -0.25f, 0.0f);
            ForwardPlane.transform.localPosition = new Vector3(0.0f, 0.0f, 0.25f);
            BackwardPlane.transform.localPosition = new Vector3(0.0f, 0.0f, -0.25f);
            */

            SetPlaneLocations();
            SetPlaneScales();
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
