using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;
using System;

public class ScanLoaderScript2 : MonoBehaviour {

    public struct Point
    {
        public int x;
        public int y;
        public float from;
        public Point(int p1, int p2, float fro)
        {
            x = p1;
            y = p2;
            from = fro;
        }
        public override string ToString()
        {
            return "( " + x + " , " + y + " , " + from + " )";
        }
    }
    public Slider mainSlider;

    public Boolean DEBUG;
    public Boolean DISABLESEGMENTATION;
    public int m_N_histogram;

    public int numScans;

    private GameObject[] planes;
    private Texture2D[] textures;
    private Texture2D[] texturesCopy;
    private Texture2D[] texturesGaussianFilter;
    
    private int currentActive = 0;

    public Camera cam;

    //private Material fakeMaterial;


    private bool[,] visited;
    private int visitedwidth;
    private int visitedheight;
    private Texture2D segment;
    private float threshold;

    Stack<Point> searchArea;

    void saveHistogram()
    {
        int[] amounts = new int[m_N_histogram + 1];
        
        for (int x = 0; x < textures[0].width; x++)
        {
            for (int y = 0; y < textures[0].height; y++)
            {
                int slot = (int)(textures[0].GetPixel(-x, -y).r*m_N_histogram);
                amounts[slot]++;
            }
        }
        using (System.IO.StreamWriter file = new System.IO.StreamWriter(Application.dataPath + "/textHistogram.txt"))
        {
            file.WriteLine("Hello");
            for( int a = 0; a < m_N_histogram + 1; a++ )
            {
                file.WriteLine(a + "\t" + amounts[a]);
            }
        }
    }

    static int counter = 0;
    public Texture2D applyGaussianFilter(Texture2D im)
    {
        int[,] filter = { { 1, 4, 7, 4, 1 }, { 4, 16, 26, 16, 4 }, { 7, 26, 41, 26, 7 }, { 4, 16, 26, 16, 4 },
        { 1, 4, 7, 4, 1 } };
        int filterSize = 5;
        int filterOffset = filterSize / 2;

        Texture2D temp = new Texture2D(im.width, im.height);

        for (int x = 0; x < im.height - 5; x++)
        {
            for (int y = 0; y < im.width - 5; y++)
            {
                float sum = 0f;
                for (int fx = 0; fx < filterSize; fx++)
                {
                    for (int fy = 0; fy < filterSize; fy++)
                    {
                        float intensity = im.GetPixel(x + fx, y + fy).r;
                        sum += filter[fx,fy] * intensity;
                    }
                }
                float result = sum * 1.0f / 273.0f;
                temp.SetPixel(x+filterOffset, y + filterOffset, new Color(result, result, result));
            }
        }
        saveImage(temp, "TestGaussianFilter" + counter++);
        return temp;
    }

    // Use this for initialization
    void Start () {
        Debug.Log("Hello!");
        searchArea = new Stack<Point>();
        planes = new GameObject[numScans];
        textures = new Texture2D[numScans];
        texturesCopy = new Texture2D[numScans];
        texturesGaussianFilter = new Texture2D[numScans];



        //fakeMaterial = new Material(Shader.Find(" Diffuse"));

        for (int scanIndex = 0; scanIndex < numScans; scanIndex++)
        {
            GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Plane);
            panel.transform.position = new Vector3(0, scanIndex*0.0f, 0);

            int ones = scanIndex % 10;
            int tens = ((scanIndex - ones) % 100)/10;
            int hundreds = ((scanIndex - ones - 10*tens ) % 1000 ) / 100;

            string filePath = "Assets/Scans/png-0" + hundreds + tens + ones + ".png";
            Texture2D scanone = LoadImage(filePath);


            Texture2D temp = new Texture2D(2, 2);
            temp.LoadRawTextureData(scanone.GetRawTextureData());

            panel.GetComponent<Renderer>().material = new Material(Shader.Find(" Diffuse"));
            panel.GetComponent<Renderer>().material.SetTextureScale("_MainTex", new Vector2(1f, 1f));
            //floor.GetComponent<Renderer>().material.mainTexture = Resources.Load("Scans/Floor.jpg") as Texture2D;
            panel.GetComponent<Renderer>().material.mainTexture = scanone;
            textures[scanIndex] = scanone;
            planes[scanIndex] = panel;

            temp = new Texture2D(textures[scanIndex].width, textures[scanIndex].height);
            for (int x = 0; x < textures[scanIndex].width; x++)
            {
                for (int y = 0; y < textures[scanIndex].height; y++)
                {
                    temp.SetPixel(x, y, textures[scanIndex].GetPixel(x - textures[scanIndex].width + 1, y - textures[scanIndex].height + 1));
                }
            }
            texturesCopy[scanIndex] = temp;
            texturesGaussianFilter[scanIndex] = applyGaussianFilter(texturesCopy[scanIndex]);


            //byte[] bytes = scanone.EncodeToPNG();
            //File.WriteAllBytes(Application.dataPath + "/Scans/png-0" + hundreds + tens + ones + ".png", bytes);
        }


        mainSlider.onValueChanged.AddListener(delegate { ValueChangeCheck(); });

        for (int scanIndex = 0; scanIndex < numScans; scanIndex++)
        {
            planes[scanIndex].SetActive(false);
        }
        SwitchActivePanel(currentActive);


        saveHistogram();
    }

    public void ValueChangeCheck()
    {
        //Debug.Log(mainSlider.value);

        float ratio = mainSlider.value * (numScans-1);

        SwitchActivePanel((int)ratio);
    }

    private void SwitchActivePanel(int scanIndex)
    {
        planes[currentActive].SetActive(false);
        planes[scanIndex].SetActive(true);
        currentActive = scanIndex;
    }

    void Update()
    {

        //Check if user clicked on a spot on the mri scan, this will be the seed to start segmentation from
        if (Input.GetMouseButtonDown(0) && !DISABLESEGMENTATION)
        { // if left button pressed...
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                // position on plan can be anywhere from -5 to 5
                float xRatio = (hit.point.x + 5.0f ) / 10.0f;
                float yRatio = (hit.point.z + 5.0f) / 10.0f;
                int xPixel = (int)(textures[currentActive].width * -xRatio);
                int yPixel = (int)(textures[currentActive].height * -yRatio);

                //planes[currentActive].GetComponent<Renderer>().material.mainTexture = null;
                //Color color = textures[currentActive].GetPixel(xPixel, yPixel);
                //textures[currentActive].SetPixel(xPixel, yPixel, Color.red);
                //Color color2 = textures[currentActive].GetPixel(xPixel, yPixel);
                //Debug.Log("Changing pixel to red:" + xPixel + "," + yPixel + " Color was " + color + " changed to " + color2);
                //planes[currentActive].GetComponent<Renderer>().material.mainTexture = textures[currentActive];

                BeginPrimitiveSegmentation(xPixel, yPixel, currentActive, texturesGaussianFilter[currentActive], "gaussian", true);
                BeginPrimitiveSegmentation(xPixel, yPixel, currentActive, texturesCopy[currentActive]);
                //textures[currentActive].Apply();
                //UpdateTexturesOnScreen();
            }
        }
    }

    public void UpdateTexturesOnScreen()
    {
        planes[currentActive].GetComponent<Renderer>().material = new Material(Shader.Find(" Diffuse"));
        planes[currentActive].GetComponent<Renderer>().material.SetTextureScale("_MainTex", new Vector2(1f, 1f));
        planes[currentActive].GetComponent<Renderer>().material.SetTexture("_MainTexture", textures[currentActive]);
        planes[currentActive].GetComponent<Renderer>().material.mainTexture = textures[currentActive];
    }

    int counts = 0;
    public void BeginPrimitiveSegmentation(int xPixel, int yPixel, int layer, Texture2D texture, String tag = "", bool useBlue = false)
    {

        threshold = 0.005f;
        counts = 0;
        visited = new bool[textures[layer].width,textures[layer].height];
        visitedwidth = textures[layer].width;
        visitedheight = textures[layer].height;
        if( DEBUG )
            Debug.Log("Created visited table of size " + visitedwidth + "," + visitedheight);

        if (DEBUG)
            Debug.Log("Emptying search area stack");
        searchArea.Clear();

        Point seed = new Point(-xPixel, -yPixel, textures[layer].GetPixel(xPixel, yPixel).r);
        if (DEBUG)
            Debug.Log("Adding seed to stack: " + seed);
        searchArea.Push(seed);

        if (DEBUG)
            Debug.Log("Entering search area loop");
        while ( searchArea.Count > 0 )
        {
            counts++;
            if (DEBUG)
                Debug.Log("Search area stack size is " + searchArea.Count);
            Point point = searchArea.Pop();
            if (DEBUG)
                Debug.Log("Popped Point " + point);

            if( point.x >= 0 && point.x < visitedwidth && point.y >= 0 && point.y < visitedheight )
            {
                if (!visited[point.x, point.y]) // if this pixel has not been visited yet
                {

                    float color = texture.GetPixel(-point.x, -point.y).r;
                    float diff = Mathf.Abs(point.from - color);
                    if (DEBUG)
                        Debug.Log("The color of point " + point + " is " + color);
                    if (diff <= threshold)
                    {

                        

                        //Debug.Log("Point is marked as part of segment");
                        // mark pixel as visited
                        visited[point.x, point.y] = true;
                        // spread to adjacent pixels
                        Point p1 = new Point(point.x + 1, point.y, color);
                        Point p2 = new Point(point.x, point.y + 1, color);
                        //Debug.Log("adding to stack nearby points " + p1 + "\t" + p2);
                        searchArea.Push(p1);
                        searchArea.Push(new Point(point.x - 1, point.y, color));
                        searchArea.Push(p2);
                        searchArea.Push(new Point(point.x, point.y - 1, color));
                    }
                    else
                    {
                        if (DEBUG)
                            Debug.Log("Color difference: " + diff + " is more than the threshold " + threshold);
                    }
                }
                else
                {
                    if (DEBUG)
                        Debug.Log("Point " + point + " is already part of the segment");
                }
            }
            else
            {
                if (DEBUG)
                    Debug.Log("Point " + point + " is not in range 0-" + visitedwidth + " and 0-" + visitedheight);
            }
        }
        if (DEBUG)
            Debug.Log("Search area stack is empty, ending algorithm");
        Debug.Log("Ran " + counts + " points");
        Texture2D visImage = new Texture2D(textures[layer].width, textures[layer].height);
        for (int x = 0; x < visImage.width; x++)
        {
            for (int y = 0; y < visImage.height; y++)
            {
                if (visited[x, y])
                {
                    visImage.SetPixel(x, y, Color.red);
                    //planes[currentActive].GetComponent<Renderer>().material.mainTexture = null;
                    if (!useBlue)
                    {
                        textures[currentActive].SetPixel(-x, -y, Color.red);
                    }
                    else
                    {
                        textures[currentActive].SetPixel(-x, -y, Color.blue);
                    }
                    //planes[currentActive].GetComponent<Renderer>().material.mainTexture = textures[currentActive];
                }
                else
                {
                    visImage.SetPixel(x, y, Color.black);
                }
            }
        }
        textures[currentActive].Apply();
        UpdateTexturesOnScreen();

        saveImage(visImage, "/segment" + tag + currentActive + ".png");
    }

    public void saveImage(Texture2D texture, String filename)
    {

        byte[] bytes = texture.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/" + filename + ".png", bytes);
    }

    public void PrimitiveRegionGrowing(int x, int y, Texture2D texture, float from)
    {
        if( counts -- < 0 )
        {
            return;
        }
        Debug.Log("Running primitive region growing on pixel:" + x + "," + y);
        if (x <= 0 && x > -texture.width && y <= 0 && y > -texture.height)
        {
            if (!visited[-x,-y]) // if this pixel has not been visited yet
            {
                Debug.Log("This pixel has not been visited yet");
                float color = texture.GetPixel(x, y).r;
                float diff = from - color;
                Debug.Log("Color difference: " + diff);
                if (Mathf.Abs(diff) <= threshold)
                {
                    Debug.Log("propogating to nearby pixels");
                    segment.SetPixel(-x, -y, Color.red);
                    // mark pixel as visited
                    visited[-x, -y] = true;
                    // spread to adjacent pixels
                    PrimitiveRegionGrowing(x + 1, y, texture, color);
                    PrimitiveRegionGrowing(x, y + 1, texture, color);
                    //PrimitiveRegionGrowing(x + 1, y, texture, color);
                    //PrimitiveRegionGrowing(x - 1, y, texture, color);
                    //PrimitiveRegionGrowing(x, y + 1, texture, color);
                    //PrimitiveRegionGrowing(x, y - 1, texture, color);
                }
            }
        }
        
    }

    public static Texture2D LoadImage(string filePath)
    {

        Texture2D tex = null;
        byte[] fileData;

        if (File.Exists(filePath))
        {
            fileData = File.ReadAllBytes(filePath);
            tex = new Texture2D(2, 2);
            tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
        }
        return tex;
    }
}
