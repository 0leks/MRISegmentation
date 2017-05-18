
using UnityEngine.UI;
using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

public class ImageSegmentationHandler2 : MonoBehaviour {

    [SerializeField] private TwoDDisplay m_twoDDisplay;
    [SerializeField] private MeshReduction m_meshReduction;
    [SerializeField] private DataContainer m_data;
    
    public GameObject tempCube;
    public GameObject cubePrefab;

    public LoadLegend m_legendScript;
    public Slider m_mainSlider;
    public float m_threshold;
    public bool m_3DFlow;

    public AudioSource m_ErrorAudio;

    public int m_xfreq, m_yfreq, m_zfreq;

    public string folderName;
    public string filePrefix;
    public string segmentName;

    public MeshRenderer m_Renderer;
    public GameObject renderCube;
    public GameObject seedObject;
    public GameObject seedBackground;

    public GrabScript grabScript1;
    public GrabScript grabScript2;

    public bool m_viewCopiedTextures;
    public bool loadSegmentOnStart;
    public bool m_useAlpha;

    private int guiWidth;
    private int guiHeight;
    
    private int edgesCounter;
    
    private bool[,,] segmentedTextures;

    private List<GameObject> objectSeeds;
    private List<GameObject> backgroundSeeds;

    public string dataPath;
    StreamWriter file;

    private List<ThreadedJob> m_runningThreads;

    // Use this for initialization
    void Start() {
        Debug.Log("Hello! I will start by loading in all of the mri scans and displaying them as 2D sprites");
        Debug.Log("THIS IS THE NUMBER 2 VERSION");
        if( m_useAlpha ) {
            m_Renderer.material.SetVector("_UseAlpha", new Vector4(1, 0, 0, 0));
        }
        else {
            m_Renderer.material.SetVector("_UseAlpha", new Vector4(0, 0, 0, 1));
        }
        objectSeeds = new List<GameObject>();
        backgroundSeeds = new List<GameObject>();
        m_runningThreads = new List<ThreadedJob>();
        dataPath = Application.dataPath;
        loadMedicalData( folderName, filePrefix, 0, m_data.m_numLayers );
    }

    public bool[,,] GetSegments() {
        return segmentedTextures;
    }

    public void loadMedicalData(string folderName, string filePrefix, int startLayer, int numLayers) {
        m_data.loadMedicalData( folderName, filePrefix, startLayer, numLayers );
        segmentedTextures = new bool[m_data.getWidth(), m_data.getHeight(), m_data.getNumLayers()];
    }

    /**
     * Populates a texture with color values from the array of floats.
     */
    public void saveTextureToFile(float[,,] texturesArray, int indexInArray, string fileName) {
        Texture2D texture = new Texture2D(texturesArray.GetLength(1), texturesArray.GetLength(2));
        for (int x = 0; x < texture.width; x++) {
            for (int y = 0; y < texture.height; y++) {
                texture.SetPixel(texture.width - 1 - x, texture.height - 1 - y, new Color(texturesArray[indexInArray, x, y], texturesArray[indexInArray, x, y], texturesArray[indexInArray, x, y]));
            }
        }
        byte[] bytes = texture.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/" + fileName + ".png", bytes);
    }

    /** This function is called from a click on the 3D cube. */
    public void AddSeedThreeD(float x, float y, float z, bool objectSeed, Vector3 originalSpot) {
        if (x > -0.5 && x < 0.5 && y > -0.5 && y < 0.5 && z > -0.5 && z < 0.5) {
            int xCoord = (int)((0.5f - x) * m_data.getWidth());
            int zCoord = (int)((y + 0.5f) * m_data.getNumLayers());
            int yCoord = (int)((0.5f - z) * m_data.getHeight());
            AddSeed(xCoord, yCoord, zCoord, objectSeed);
            GameObject seed;
            if (objectSeed) {
                seed = Instantiate(seedObject);
                objectSeeds.Add(seed);
            }
            else {
                seed = Instantiate(seedBackground);
                backgroundSeeds.Add(seed);
            }
            seed.transform.position = originalSpot;
            seed.transform.parent = renderCube.transform;
            seed.transform.localScale = 0.02f * new Vector3(1, 1, 1); //  renderCube.transform.localScale;
        }
        else {
            m_ErrorAudio.Play(); // If user tried to put seed outside of the cube, make an error sound
        }
    }
    /**
     * This function is called from the 2D Display x and y are between 0 and 1 
     *      Converts the 0 to 1 to -0.5 to 0.5 in order to add visual feedback just like 3D seeds.
     */
    public void AddSeedTwoD(float x, float y, int z, bool objectSeed) {
        int xCoord = (int) ( x * m_data.getWidth() );
        int yCoord = (int) ( y * m_data.getHeight() );
        Debug.Log( " clicked on = (" + x + "," + y + ")" );
        Debug.Log( " Coord of new seed = (" + xCoord + "," + yCoord + ")" );
        if( xCoord >= 0 && xCoord < m_data.getWidth() && yCoord >= 0 && yCoord < m_data.getHeight() ) {
            AddSeed( xCoord, yCoord, z, objectSeed );
            float visualX = 0.5f - x;
            float visualZ = 0.5f - y;
            float visualY = ( 1.0f * z / m_data.getNumLayers() ) - 0.5f;
            GameObject seed;
            if( objectSeed ) {
                seed = Instantiate( seedObject );
                objectSeeds.Add( seed );
            }
            else {
                seed = Instantiate( seedBackground );
                backgroundSeeds.Add( seed );
            }
            seed.transform.parent = renderCube.transform;
            seed.transform.localPosition = new Vector3( visualX, visualY, visualZ);
            seed.transform.localScale = 0.02f * new Vector3( 1, 1, 1 );
        }
        else {
            //m_ErrorAudio.Play(); // If user tried to put seed outside of the cube, make an error sound
        }
    }

    private void AddSeed(int x, int y, int z, bool objectSeed) {
        m_data.AddSeed( x, y, z, objectSeed );
    }

    public void ClearSeeds() {
        Debug.LogError( "Reset object and background seed list" );
        m_data.ClearSeeds();
        foreach( GameObject obj in objectSeeds ) {
            Destroy( obj );
        }
        objectSeeds.Clear();
        foreach( GameObject obj in backgroundSeeds ) {
            Destroy( obj );
        }
        backgroundSeeds.Clear();
    }

    public void SeedPresetOne() {
        int M = 2;
        AddSeedTwoD( 155.0f * M / m_data.getWidth(), 149.0f * M / m_data.getHeight(), 0, true );
        AddSeedTwoD( 155.0f * M / m_data.getWidth(), 149.0f * M / m_data.getHeight(), 0, true );
        AddSeedTwoD( 144.0f * M / m_data.getWidth(), 160.0f * M / m_data.getHeight(), 0, true );
        AddSeedTwoD( 132.0f * M / m_data.getWidth(), 176.0f * M / m_data.getHeight(), 0, true );
        AddSeedTwoD( 131.0f * M / m_data.getWidth(), 197.0f * M / m_data.getHeight(), 0, true );
        AddSeedTwoD( 115.0f * M / m_data.getWidth(), 202.0f * M / m_data.getHeight(), 0, true );
        AddSeedTwoD( 115.0f * M / m_data.getWidth(), 179.0f * M / m_data.getHeight(), 0, true );
        AddSeedTwoD( 125.0f * M / m_data.getWidth(), 160.0f * M / m_data.getHeight(), 0, true );
        AddSeedTwoD( 135.0f * M / m_data.getWidth(), 145.0f * M / m_data.getHeight(), 0, true );
        AddSeedTwoD( 151.0f * M / m_data.getWidth(), 132.0f * M / m_data.getHeight(), 0, true );
        AddSeedTwoD( 159.0f * M / m_data.getWidth(), 139.0f * M / m_data.getHeight(), 0, true );

        AddSeedTwoD( 154.0f * M / m_data.getWidth(), 171.0f * M / m_data.getHeight(), 0, false );
        AddSeedTwoD( 144.0f * M / m_data.getWidth(), 197.0f * M / m_data.getHeight(), 0, false );
        AddSeedTwoD( 132.0f * M / m_data.getWidth(), 217.0f * M / m_data.getHeight(), 0, false );
        AddSeedTwoD( 103.0f * M / m_data.getWidth(), 213.0f * M / m_data.getHeight(), 0, false );
        AddSeedTwoD( 98.0f * M / m_data.getWidth(), 188.0f * M / m_data.getHeight(), 0, false );
        AddSeedTwoD( 111.0f * M / m_data.getWidth(), 161.0f * M / m_data.getHeight(), 0, false );
        AddSeedTwoD( 123.0f * M / m_data.getWidth(), 141.0f * M / m_data.getHeight(), 0, false );
        AddSeedTwoD( 141.0f * M / m_data.getWidth(), 124.0f * M / m_data.getHeight(), 0, false );
        AddSeedTwoD( 163.0f * M / m_data.getWidth(), 124.0f * M / m_data.getHeight(), 0, false );
        AddSeedTwoD( 172.0f * M / m_data.getWidth(), 146.0f * M / m_data.getHeight(), 0, false );
    }
    public void StartFloodFillSegmentationThread() {
        RegionGrowingJob regionGrowingJob = new RegionGrowingJob( this, m_data, m_threshold );
        regionGrowingJob.StartThread();
        m_runningThreads.Add( regionGrowingJob );
    }
    public void StartMaxFlowSegmentationThread() {
        MaxFlowJob maxFlowJob = new MaxFlowJob( this, m_data, m_xfreq, m_zfreq, m_3DFlow);
        maxFlowJob.StartThread();
        m_runningThreads.Add( maxFlowJob );
    }
    public void FinishedSegmentationCallback() {
        m_twoDDisplay.disableTwoDDisplay();
        m_legendScript.LoadLegendFromSegmentationHandler();
        GetComponent<AudioSource>().PlayOneShot( GetComponent<AudioSource>().clip, 1 );
    }
    public void StartMarchingCubesThread() {
        MarchingCubesJob marchingCubesJob = new MarchingCubesJob(this, m_data );
        marchingCubesJob.StartThread();
        m_runningThreads.Add( marchingCubesJob );
    }
    public void MarchingCubesFinished( List<Vector3> vertices, List<int> triangles ) {
        GameObject newCube = Instantiate<GameObject>( cubePrefab );
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        if( true ) {
            mesh.normals = computeNormals( mesh.vertices, mesh.triangles );
            Color32[] col = new Color32[ mesh.vertices.Length ];
            for( int i = 0; i < mesh.normals.Length; i++ ) {
                //col[ i ] = new Color( mesh.normals[ i ].x, mesh.normals[ i ].y, mesh.normals[ i ].z, 1.0f );
                col[ i ] = new Color( Mathf.Abs( 0.0f - mesh.normals[ i ].x ), Mathf.Abs( 0.0f - mesh.normals[ i ].y ), Mathf.Abs( 0.0f - mesh.normals[ i ].z ), 1.0f );
            }
            mesh.colors32 = col;
        }

        newCube.GetComponent<MeshFilter>().mesh = mesh;

        grabScript1.AddGrabbable( newCube );
        grabScript2.AddGrabbable( newCube );
    }

    public void StartMaxFlowSegmentationTimedThread() {
        Debug.LogError( "beginning timed max flow segmenting, currently doesnt do anything" );
        string filename = m_data.getNumLayers() + " " + m_data.getWidth() + " debugLog" + Time.time + ".txt";
        file = new StreamWriter( filename );
        Debug.LogError( " filename = " + filename );
        //RunMaxFlowSegmentationTimed();
        file.Close();
        GetComponent<AudioSource>().PlayOneShot( GetComponent<AudioSource>().clip, 1 );
        Debug.LogError( "finished timed max flow segmenting" );
        m_twoDDisplay.disableTwoDDisplay();
        m_legendScript.LoadLegendFromSegmentationHandler();
    }


    void Update() {
        if( Input.GetKeyDown( "g" ) ) {
            loadMedicalData( folderName, filePrefix, 0, m_data.m_numLayers );
        }
        if (Input.GetKeyDown("r")) {
            m_data.saveSegmentToFileAsText( m_data.GetSegment(), "testSegment.txt" );
            segmentedTextures = new bool[ m_data.getWidth(), m_data.getHeight(), m_data.getNumLayers() ];
            ClearSeeds();
        }
        if( Input.GetKeyDown( "q" ) ) {
            segmentedTextures = m_data.loadSegmentFromTextFile( "testSegment.txt" );
            m_data.AddSegment( segmentedTextures );
            m_legendScript.LoadLegendFromSegmentationHandler();
            m_twoDDisplay.disableTwoDDisplay();
        }
        if( Input.GetKeyDown( "m" ) ) {
            StartMarchingCubesThread();
        }
        // Update each thread to see if it is done yet.
        for( int index = m_runningThreads.Count - 1; index >= 0; index-- ) {
            if( m_runningThreads[index].Update() ) {
                m_runningThreads.Remove( m_runningThreads[ index ] );
            }
        }
    }


    public Vector3[] computeNormals( Vector3[] vertices, int[] triangles ) {

        int numTriangles = triangles.Length / 3;
        Vector3[] normals = new Vector3[ vertices.Length ];
        for( int t = 0; t < numTriangles; t++ ) {
            Vector3 normal = Vector3.Cross( vertices[ triangles[ t * 3 + 1 ] ] - vertices[ triangles[ t * 3 ] ],
                                                vertices[ triangles[ t * 3 + 2 ] ] - vertices[ triangles[ t * 3 ] ] );
            normal = Vector3.Normalize( normal );
            for( int i = 0; i < 3; i++ ) {
                normals[ triangles[ t * 3 + i ] ] = normal;
            }
        }
        return normals;
    }
}
