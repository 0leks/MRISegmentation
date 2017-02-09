
using UnityEngine.UI;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class ImageSegmentationHandler : MonoBehaviour
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

    // Use this for initialization
    void Start()
    {

        Debug.Log("Hello! I will start by loading in all of the mri scans and displaying them as 2D sprites");
        Debug.Log("THIS IS THE NUMBER 1 VERSION");
        displayMode2D = true;
        originalScanTextures = new Texture2D[m_numScans];
        originalScans = new float[m_numScans, m_scanWidth, m_scanHeight];
        segmentedTextures = new Texture2D[m_numScans];

        for (int scanIndex = 0; scanIndex < m_numScans; scanIndex++)
        {
            int ones = scanIndex % 10;
            int tens = ((scanIndex - ones) % 100) / 10;
            int hundreds = ((scanIndex - ones - 10 * tens) % 1000) / 100;
            string filePath = "Assets/Scans/png-0" + hundreds + tens + ones + ".png";
            Texture2D scanTexture = LoadScan(filePath);

            for (int x = 0; x < scanTexture.width; x++)
            {
                for (int y = 0; y < scanTexture.height; y++)
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
            segmentedTextures[indexInArray] = new Texture2D(segmentArray.GetLength(0), segmentArray.GetLength(1));

            for (int x = 0; x < segmentedTextures[indexInArray].width; x++)
            {
                for (int y = 0; y < segmentedTextures[indexInArray].height; y++)
                {
                    if (segmentArray[x, y, z])
                    {
                        segmentedTextures[indexInArray].SetPixel(segmentedTextures[indexInArray].width - 1 - x, segmentedTextures[indexInArray].height - 1 - y, new Color(0, 0, 0));
                    }
                    else
                    {
                        segmentedTextures[indexInArray].SetPixel(segmentedTextures[indexInArray].width - 1 - x, segmentedTextures[indexInArray].height - 1 - y, new Color(1, 1, 1));
                        //texture.SetPixel(texture.width - 1 - x, texture.height - 1 - y, new Color(texturesArray[indexInArray, x, y], texturesArray[indexInArray, x, y], texturesArray[indexInArray, x, y]));
                    }
                }
            }

            byte[] bytes = segmentedTextures[indexInArray].EncodeToPNG();
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

        if (Input.GetMouseButtonDown(0))
        { // if left button pressed...
            int x = (int)((guiWidth - Input.mousePosition.x) * m_scanWidth / guiWidth);
            int y = (int)((Screen.height - Input.mousePosition.y + 3) * m_scanHeight / guiHeight);
            Debug.LogFormat("Clicked on {0}, {1}", x, y);
            if (x >= 0 && x < m_scanWidth && y >= 0 && y < m_scanHeight)
            {
                RunSegmentation(x, y, selectedScan);
                Debug.Log("finished segmenting");

                SwitchToDisplaySegment();
            }
        }
    }

    void SwitchToDisplaySegment()
    {
        m_Renderer.enabled = true;
        displayMode2D = false;
        m_sliderCanvas.SetActive(false);
        m_legendScript.LoadLegendFrom("segment");

    }

    int count = 0;
    public void RunSegmentation(int mousex, int mousey, int scanIndex)
    {
        float threshold = 0.005f;
        bool[,,] visited = new bool[m_scanWidth, m_scanHeight, m_numScans];
        Stack<Point> searchArea = new Stack<Point>();
        Point seed = new Point(mousex, mousey, scanIndex, originalScans[scanIndex, mousex, mousey]);
        searchArea.Push(seed);

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
