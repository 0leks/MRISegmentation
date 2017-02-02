using UnityEngine;
using UnityEngine.UI;
using System.IO;
using UnityEditor;

public class ScanLoaderScript : MonoBehaviour {

    public Slider mainSlider;

    public int numScans;

    private GameObject[] planes;
    private Texture2D[] textures;
    private Texture2D[] texturesCopy;
    private int currentActive = 0;

    public Camera camera;

    private Material fakeMaterial;


    private bool[,] visited;
    private Texture2D segment;
    private float threshold;

    // Use this for initialization
    void Start () {
        Debug.Log("Hello!");
        planes = new GameObject[numScans];
        textures = new Texture2D[numScans];

        fakeMaterial = new Material(Shader.Find(" Diffuse"));

        for (int scanIndex = 0; scanIndex < numScans; scanIndex++)
        {
            GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Plane);
            panel.transform.position = new Vector3(0, scanIndex*0.0f, 0);

            int ones = scanIndex % 10;
            int tens = ((scanIndex - ones) % 100)/10;
            int hundreds = ((scanIndex - ones - 10*tens ) % 1000 ) / 100;

            string filePath = "Assets/Scans/pgm-0" + hundreds + tens + ones + ".jpg";
            Debug.Log(filePath, panel);
            TextureImporter imp = AssetImporter.GetAtPath(filePath) as TextureImporter;
            imp.isReadable = true;
            imp.textureType = TextureImporterType.Default;
            AssetDatabase.ImportAsset(filePath, ImportAssetOptions.ForceUpdate);
            //Texture2D scanone = (Texture2D)AssetDatabase.LoadAssetAtPath(filePath, typeof(Texture2D));
            Texture2D scanone = LoadImage(filePath);


            Texture2D temp = new Texture2D(2, 2);
            temp.LoadRawTextureData(scanone.GetRawTextureData());

            panel.GetComponent<Renderer>().material = new Material(Shader.Find(" Diffuse"));
            panel.GetComponent<Renderer>().material.SetTextureScale("_MainTex", new Vector2(1f, 1f));
            //floor.GetComponent<Renderer>().material.mainTexture = Resources.Load("Scans/Floor.jpg") as Texture2D;
            panel.GetComponent<Renderer>().material.mainTexture = scanone;
            textures[scanIndex] = scanone;
            planes[scanIndex] = panel;
        }


        mainSlider.onValueChanged.AddListener(delegate { ValueChangeCheck(); });

        for (int scanIndex = 0; scanIndex < numScans; scanIndex++)
        {
            planes[scanIndex].SetActive(false);
        }
        SwitchActivePanel(currentActive);
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
        if (Input.GetMouseButtonDown(0))
        { // if left button pressed...
            Ray ray = camera.ScreenPointToRay(Input.mousePosition);
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
                planes[currentActive].GetComponent<Renderer>().material.mainTexture = textures[currentActive];

                BeginPrimitiveSegmentation(xPixel, yPixel, currentActive);
                textures[currentActive].Apply();
                UpdateTexturesOnScreen();
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
    public void BeginPrimitiveSegmentation(int xPixel, int yPixel, int layer)
    {

        threshold = 0.05f;
        visited = new bool[textures[layer].width,textures[layer].height];
        segment = new Texture2D(textures[layer].width, textures[layer].height);
        for( int x = 0; x < segment.width; x++)
        {
            for( int y = 0; y < segment.height; y++ )
            {
                segment.SetPixel(x, y, Color.black);
            }
        }

        PrimitiveRegionGrowing(xPixel, yPixel, textures[layer], textures[layer].GetPixel(xPixel, yPixel).r);

        Texture2D visImage = new Texture2D(textures[layer].width, textures[layer].height);
        for (int x = 0; x < visImage.width; x++)
        {
            for (int y = 0; y < visImage.height; y++)
            {
                if (visited[x, y])
                {
                    visImage.SetPixel(x, y, Color.red);
                }
            }
        }

        byte[] bytes = visImage.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/Visited.png", bytes);
        bytes = segment.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/Segment.png", bytes);
    }

    public void PrimitiveRegionGrowing(int x, int y, Texture2D texture, float from)
    {
        if (x <= 0 && x > -texture.width && y <= 0 && y > -texture.height)
        {
            if (!visited[-x,-y]) // if this pixel has not been visited yet
            {
                float color = texture.GetPixel(x, y).r;
                float diff = from - color;
                Debug.Log("Color difference: " + diff);
                if (Mathf.Abs(diff) < threshold)
                {
                    segment.SetPixel(x, y, Color.red);
                    // mark pixel as visited
                    visited[-x, -y] = true;
                    // spread to adjacent pixels
                    PrimitiveRegionGrowing(x, y, texture, color);
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
