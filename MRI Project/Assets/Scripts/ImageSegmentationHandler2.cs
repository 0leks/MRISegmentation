
using UnityEngine.UI;
using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

public class ImageSegmentationHandler2 : MonoBehaviour
{

    public LoadLegend m_legendScript;
    public GameObject m_sliderCanvas;
    public Slider m_mainSlider;
    public int m_numScans;
    public int m_scanWidth;
    public int m_scanHeight;

    public MeshRenderer m_Renderer;

    public bool m_viewCopiedTextures;

    private bool displayMode2D;

    private int selectedScan;

    private int guiWidth;
    private int guiHeight;

    private Texture2D displayedTexture;
    private Texture2D[] originalScanTextures;
    private float[,,] originalScans; // this is an array of all of the original Scans stored as 2d float arrays

    private Texture2D[] segmentedTextures;

    private List<Point> partOfObject;
    private List<Point> partOfBackground;

    StreamWriter file;

    // Use this for initialization
    void Start()
    {
        Debug.Log("Hello! I will start by loading in all of the mri scans and displaying them as 2D sprites");
        Debug.Log("THIS IS THE NUMBER 2 VERSION");
        displayMode2D = true;
        originalScanTextures = new Texture2D[m_numScans];
        originalScans = new float[m_numScans, m_scanWidth, m_scanHeight];
        segmentedTextures = new Texture2D[m_numScans];
        partOfObject = new List<Point>();
        partOfBackground = new List<Point>();

        for (int scanIndex = 0; scanIndex < m_numScans; scanIndex++)
        {
            int ones = scanIndex % 10;
            int tens = ((scanIndex - ones) % 100) / 10;
            int hundreds = ((scanIndex - ones - 10 * tens) % 1000) / 100;
            string filePath = "Assets/Scans/png-0" + hundreds + tens + ones + ".png";
            Texture2D scanTexture = LoadScan(filePath);

            for (int x = 0; x < m_scanWidth; x++)
            {
                for (int y = 0; y < m_scanHeight; y++)
                {
                    originalScans[scanIndex, x, y] = scanTexture.GetPixel(-x, -y).r;
                }
            }
            originalScanTextures[scanIndex] = scanTexture;
            //saveTextureToFile(originalScans, scanIndex, "Saved Texture " + scanIndex + ".png");
        }

        ValueChangeCheck();

        m_mainSlider.onValueChanged.AddListener(delegate { ValueChangeCheck(); });
    }

    /**
     * Populates the passed in texture with color values from the array of floats.
     */
    public void saveTextureToFile(float[,,] texturesArray, int indexInArray, string fileName)
    {
        Texture2D texture = new Texture2D(texturesArray.GetLength(1), texturesArray.GetLength(2));

        for (int x = 0; x < texture.width; x++)
        {
            for (int y = 0; y < texture.height; y++)
            {
                texture.SetPixel(texture.width - 1 - x, texture.height - 1 - y, new Color(texturesArray[indexInArray, x, y], texturesArray[indexInArray, x, y], texturesArray[indexInArray, x, y]));
            }
        }
        byte[] bytes = texture.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/" + fileName + ".png", bytes);
    }

    public void saveSegmentToFile(bool[,,] segmentArray, float[,,] texturesArray, int indexInArray, string fileName)
    {

        for (int z = 0; z < m_numScans; z++)
        {
            if (segmentedTextures[z] == null)   // only create new texture when no textures yet, otherwise keep adding to the same segmented texture to allow
            {                                   // multiple segments to be rendered 
                segmentedTextures[z] = new Texture2D(segmentArray.GetLength(0), segmentArray.GetLength(1));
                Debug.LogError("Creating new texture");
                for (int x = 0; x < segmentedTextures[z].width; x++)
                {
                    for (int y = 0; y < segmentedTextures[z].height; y++)
                    {
                        segmentedTextures[z].SetPixel(segmentedTextures[z].width - 1 - x, segmentedTextures[z].height - 1 - y, new Color(1, 1, 1));
                    }
                }
            }

            for (int x = 0; x < segmentedTextures[z].width; x++)
            {
                for (int y = 0; y < segmentedTextures[z].height; y++)
                {
                    if (segmentArray[x, y, z])
                    {
                        segmentedTextures[z].SetPixel(segmentedTextures[z].width - 1 - x, segmentedTextures[z].height - 1 - y, new Color(0, 0, 0));
                    }
                }
            }

            byte[] bytes = segmentedTextures[z].EncodeToPNG();
            Debug.Log("segmentedTextures[" + z + "] = " + segmentedTextures[z]);
            File.WriteAllBytes(Application.dataPath + "/" + fileName + z.ToString().PadLeft(4, '0') + ".png", bytes);
        }
    }

    public Texture2D[] getSegments()
    {
        return segmentedTextures;
    }

    public void ValueChangeCheck()
    {
        // select a scan beased on position of the slider. the min is to prevent selecting past the array size
        selectedScan = Mathf.Min((int)(m_mainSlider.value * m_numScans), originalScanTextures.Length - 1);
        displayedTexture = originalScanTextures[selectedScan];
    }

    void OnGUI()
    {
        if (displayMode2D == true)
        {
            guiWidth = Mathf.Max(Screen.height - 100, m_scanWidth);
            guiHeight = Mathf.Max(Screen.height - 100, m_scanHeight);
            GUI.DrawTexture(new Rect(0, 0, guiWidth, guiHeight), displayedTexture, ScaleMode.ScaleToFit, true, 1.0F);
        }
    }

    void Update()
    {
        if ((Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)) && displayMode2D)
        { // if left button pressed...
            int x = (int)((guiWidth - Input.mousePosition.x) * m_scanWidth / guiWidth);
            int y = (int)((Screen.height - Input.mousePosition.y + 3) * m_scanHeight / guiHeight);
            if (x >= 0 && x < m_scanWidth && y >= 0 && y < m_scanHeight)
            {
                if (Input.GetMouseButtonDown(0) )
                {
                    Debug.LogErrorFormat("Added point to Object {0}, {1}", x, y);
                    partOfObject.Add(new Point(x, y, selectedScan, 0));
                }
                else
                {
                    Debug.LogErrorFormat("Adding point to Background {0}, {1}", x, y);
                    partOfBackground.Add(new Point(x, y, selectedScan, 0));
                }
            }
        }
        if (Input.GetKeyDown("f"))
        {
            Debug.LogError("beginning flood fill segmenting");
            RunSegmentation(selectedScan);
            Debug.LogError("finished flood fill segmenting");
            SwitchToDisplaySegment();
            m_legendScript.LoadLegendFrom("segment");
        }
        if (Input.GetKeyDown("s"))
        {
            Debug.LogError("beginning max flow segmenting");
            file = new StreamWriter("debugLog.txt");
            RunMaxFlowSegmentation(selectedScan);
            file.Close();
            Debug.LogError("finished max flow segmenting");
            SwitchToDisplaySegment();
            m_legendScript.LoadLegendFrom("segment");
        }
        if (Input.GetKeyDown("space"))
        {
            if (!displayMode2D)
            {
                SwitchToDisplayScans();
            }
            else
            {
                SwitchToDisplaySegment();
            }
        }

        if (Input.GetKeyDown("r"))
        {
            segmentedTextures = new Texture2D[m_numScans];
            partOfObject.Clear();
        }
    }

    void SwitchToDisplaySegment()
    {
        m_Renderer.enabled = true;
        displayMode2D = false;
        m_sliderCanvas.SetActive(false);
    }

    void SwitchToDisplayScans()
    {
        m_Renderer.enabled = false;
        displayMode2D = true;
        m_sliderCanvas.SetActive(true);
    }

    public struct Vertex
    {
        public Vertex[] neighbors;
        public float[] flows;

        public Vertex(int numNeighbors)
        {
            neighbors = new Vertex[numNeighbors];
            flows = new float[numNeighbors];
        }
    }

    public float MaxFlowBetweenPoints(int x1, int y1, int x2, int y2, int scanIndex)
    {
        float delta = (originalScans[scanIndex, x1, y1] - originalScans[scanIndex, x2, y2]);
        float sigmaSquared = 0.01f * 0.01f;
        float flow = (float)Math.Exp(-(delta * delta) / sigmaSquared);
        file.WriteLine("\tPoint {0},{1} has intensity {2}, the delta is {3}, and the flow is {4}", x2, y2, originalScans[scanIndex, x2, y2], delta, flow);

        return flow;
    }

    public void RunMaxFlowSegmentation(int scanIndex)
    {
        // create array of 512 by 512 vertices, one for each pixel on the image
        Vertex[,] vertices = new Vertex[m_scanWidth, m_scanHeight];
        Vertex sink = new Vertex(0);
        Vertex source = new Vertex(m_scanWidth * m_scanHeight);
        for (int x = 0; x < m_scanWidth; x++)
        {
            for (int y = 0; y < m_scanHeight; y++)
            {
                if (x == 0 || x == m_scanWidth - 1 || y == 0 || y == m_scanHeight - 1)
                {
                    if ((x == 0 && y == 0) || (x == 0 && y == m_scanHeight - 1) || (x == m_scanWidth - 1 && y == 0) || (x == m_scanWidth - 1 && y == m_scanHeight - 1))
                    {
                        vertices[x, y] = new Vertex(3); // the corners have two pixel neighbors and the sink
                    }
                    else
                    {
                        vertices[x, y] = new Vertex(4); // the edges have 3 pixel neighbors and the sink
                    }
                }
                else
                {
                    vertices[x, y] = new Vertex(5); // everything else has 4 pixel neighbors and the sink
                }
            }
        }
        float maximumFlow = 0.0f;
        for (int x = 0; x < m_scanWidth; x++)
        {
            for (int y = 0; y < m_scanHeight; y++)
            {
                source.neighbors[x * m_scanHeight + y] = vertices[x, y]; // the source is connected to each pixel
                int n_i = 0;
                vertices[x, y].neighbors[n_i++] = sink; // each vertex is connected to the sink
                file.WriteLine("Point {0},{1} has intensity {2}", x, y, originalScans[scanIndex, x, y]);
                file.WriteLine("Neighbors:");
                if (x != 0)
                { // Add the four neighbors of each pixel but check edge cases.
                    vertices[x, y].flows[n_i] = MaxFlowBetweenPoints(x, y, x - 1, y, scanIndex);
                    maximumFlow = Mathf.Max(maximumFlow, vertices[x, y].flows[n_i]);
                    vertices[x, y].neighbors[n_i++] = vertices[x - 1, y];

                }
                if (x != m_scanWidth - 1)
                {
                    vertices[x, y].flows[n_i] = MaxFlowBetweenPoints(x, y, x + 1, y, scanIndex);
                    maximumFlow = Mathf.Max(maximumFlow, vertices[x, y].flows[n_i]);
                    vertices[x, y].neighbors[n_i++] = vertices[x + 1, y];
                }
                if (y != 0)
                {
                    vertices[x, y].flows[n_i] = MaxFlowBetweenPoints(x, y, x, y - 1, scanIndex);
                    maximumFlow = Mathf.Max(maximumFlow, vertices[x, y].flows[n_i]);
                    vertices[x, y].neighbors[n_i++] = vertices[x, y - 1];
                }
                if (y != m_scanHeight - 1)
                {
                    vertices[x, y].flows[n_i] = MaxFlowBetweenPoints(x, y, x, y + 1, scanIndex);
                    maximumFlow = Mathf.Max(maximumFlow, vertices[x, y].flows[n_i]);
                    vertices[x, y].neighbors[n_i++] = vertices[x, y + 1];
                }
            }
        }
        file.WriteLine("The maximum flow between any two pixels was: {0}", maximumFlow);

        for (int x = 0; x < m_scanWidth; x++)
        {
            for (int y = 0; y < m_scanHeight; y++)
            {
                source.flows[x * m_scanHeight + y] = 0.0f; // need to figure out what to put here
            }
        }
        foreach (Point obj in partOfObject)
        {
            source.flows[obj.x * m_scanHeight + obj.y] = 1.0f + maximumFlow;

            file.WriteLine("source to pixel {0},{1}  flow: {2}", obj.x, obj.y, source.flows[obj.x * m_scanHeight + obj.y]);
        }
        foreach (Point obj in partOfBackground)
        {
            source.flows[obj.x * m_scanHeight + obj.y] = 0.0f;
        }

        // now that total flows have been set up, run breadth first search and each time reach sink, update flows and run again.
        // Upon parsing every accesible vertex and not having visited sink yet, that is the end and the max flow has been found.

    }

    int count = 0;
    public void RunSegmentation(int scanIndex)
    {
        float threshold = 0.005f;
        bool[,,] visited = new bool[m_scanWidth, m_scanHeight, m_numScans];
        Stack<Point> searchArea = new Stack<Point>();
        foreach( Point obj in partOfObject)
        {
            Point seed = new Point(obj.x, obj.y, obj.z, originalScans[obj.z, obj.x, obj.y]);
            searchArea.Push(seed);
        }
        partOfObject.Clear();

        while (searchArea.Count > 0)
        {
            Point point = searchArea.Pop();

            if (point.x >= 0 && point.x < m_scanWidth && point.y >= 0 && point.y < m_scanHeight && point.z >= 0 && point.z < m_numScans)
            {
                if (!visited[point.x, point.y, point.z])
                {
                    float color = originalScans[point.z, point.x, point.y];
                    float diff = Mathf.Abs(point.from - color);
                    if (diff <= threshold)
                    {
                        visited[point.x, point.y, point.z] = true;
                        searchArea.Push(new Point(point.x - 1, point.y, point.z, color));
                        searchArea.Push(new Point(point.x + 1, point.y, point.z, color));
                        searchArea.Push(new Point(point.x, point.y - 1, point.z, color));
                        searchArea.Push(new Point(point.x, point.y + 1, point.z, color));
                        searchArea.Push(new Point(point.x, point.y, point.z + 1, color));
                        searchArea.Push(new Point(point.x, point.y, point.z - 1, color));
                    }
                }
            }
        }
        saveSegmentToFile(visited, originalScans, scanIndex, "Resources/segment/segment");

    }

    public struct Point
    {
        public int x;
        public int y;
        public int z;
        public float from;
        public Point(int p1, int p2, int p3, float fro)
        {
            x = p1;
            y = p2;
            z = p3;
            from = fro;
        }
        public override string ToString()
        {
            return "( " + x + " , " + y + " , " + from + " )";
        }
    }


    /** 
     * This function loads the image located at filePath and returns it as a Texture2D.
     * If the file at the specified path doesn't exist, it returns null.
     */
    public static Texture2D LoadScan(string filePath)
    {
        Texture2D tex = null;
        byte[] fileData;
        if (File.Exists(filePath))
        {
            fileData = File.ReadAllBytes(filePath);
            tex = new Texture2D(2, 2);
            tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
        }
        else
        {
            Debug.LogErrorFormat("The file at %s could not be found.", filePath);
        }
        return tex;
    }
}
