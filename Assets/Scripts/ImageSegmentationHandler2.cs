
using UnityEngine.UI;
using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

public class ImageSegmentationHandler2 : MonoBehaviour {

	// UI
    [SerializeField] private TwoDDisplay m_twoDDisplay;             // reference to 2D UI
    
	// DataContainer
	[SerializeField] private DataContainer m_data;					// Data for segmentation
    
	// RegionGrowingJob
	public float m_threshold;										// RegionGrowingJob sensitivity threshold

	// MaxFlowJob
    public bool m_3DFlow;											// MaxFlowJob 2D or 3D approach					
    public int m_xfreq, m_yfreq, m_zfreq;							// X, Y, and Z frequency for MaxFlowJob

	// MarchingCubes
	public GameObject cubePrefab;									// prefab used for marching cubes
	public GameObject wireFrameCubePrefab;							// prefab used for marching cubes
    public float zStretch;											// zStretch for MarchingCubes

	// MRI data files info
    public string folderName;										// name from which to load files
    public string filePrefix;										// prefix of numbered MRI files

	// GameObjects for cube and seeds
    [SerializeField] private GameObject renderCube;
	[SerializeField] private GameObject foregroundSeed;
	[SerializeField] private GameObject backgroundSeed;

	// Grab scripts
	[SerializeField] private GrabScript LGrabScript;
	[SerializeField] private GrabScript RGrabScript;

	// LoadLegend
    public bool loadSegmentOnStart;									// setting to load segment on start for LoadLegend
	public LoadLegend m_legendScript;								// TODO look at loadlegend first
    
	// seeds
	private List<GameObject> foregroundSeeds;						// keeps track of seed GameObjects
	private List<GameObject> backgroundSeeds;						// keeps track of seed GameObjects

	// threads
    private List<ThreadedJob> m_runningThreads;						// list of running threads

    // Use this for initialization
    void Start() {
        Debug.Log("Hello! I will start by loading in all of the mri scans and displaying them as 2D sprites");
        Debug.Log("THIS IS THE NUMBER 2 VERSION");
        foregroundSeeds = new List<GameObject>();
        backgroundSeeds = new List<GameObject>();
        m_runningThreads = new List<ThreadedJob>();
        loadMedicalData( folderName, filePrefix, 0, m_data.getNumLayers() );
    }


    public void loadMedicalData(string folderName, string filePrefix, int startLayer, int numLayers) {
        m_data.loadMedicalData( folderName, filePrefix, startLayer, numLayers );
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
		File.WriteAllBytes(Application.dataPath + "/Output/" + fileName + ".png", bytes);
    }

    /** This function is called from a click on the 3D cube. */
    public void AddSeedThreeD(float x, float y, float z, bool foregroundSeed, Vector3 originalSpot) {
        if (x > -0.5 && x < 0.5 && y > -0.5 && y < 0.5 && z > -0.5 && z < 0.5) {
            int xCoord = (int)((0.5f - x) * m_data.getWidth());
            int zCoord = (int)((y + 0.5f) * m_data.getNumLayers());
            int yCoord = (int)((0.5f - z) * m_data.getHeight());
            AddSeed(xCoord, yCoord, zCoord, foregroundSeed);
            GameObject seed;
            if (foregroundSeed) {
                seed = Instantiate(foregroundSeed);
                foregroundSeeds.Add(seed);
            }
            else {
                seed = Instantiate(backgroundSeed);
                backgroundSeeds.Add(seed);
            }
            seed.transform.position = originalSpot;
            seed.transform.parent = renderCube.transform;
            seed.transform.localScale = 0.02f * new Vector3(1, 1, 1); //  renderCube.transform.localScale;
        }
    }
    /**
     * This function is called from the 2D Display x and y are between 0 and 1 
     *      Converts the 0 to 1 to -0.5 to 0.5 in order to add visual feedback just like 3D seeds.
     */
    public void AddSeedTwoD(float x, float y, int z, bool foregroundSeed) {
        int xCoord = (int) ( x * m_data.getWidth() );
        int yCoord = (int) ( y * m_data.getHeight() );
        Debug.Log( " clicked on = (" + x + "," + y + ")" );
        Debug.Log( " Coord of new seed = (" + xCoord + "," + yCoord + ")" );
        if( xCoord >= 0 && xCoord < m_data.getWidth() && yCoord >= 0 && yCoord < m_data.getHeight() ) {
            AddSeed( xCoord, yCoord, z, foregroundSeed );
            float visualX = 0.5f - x;
            float visualZ = 0.5f - y;
            float visualY = ( 1.0f * z / m_data.getNumLayers() ) - 0.5f;
            GameObject seed;
            if( foregroundSeed ) {
                seed = Instantiate( foregroundSeed );
                foregroundSeeds.Add( seed );
            }
            else {
                seed = Instantiate( backgroundSeed );
                backgroundSeeds.Add( seed );
            }
            seed.transform.parent = renderCube.transform;
            seed.transform.localPosition = new Vector3( visualX, visualY, visualZ);
            seed.transform.localScale = 0.02f * new Vector3( 1, 1, 1 );
        }
    }

	private void AddSeed(int x, int y, int z, bool foregroundSeed) {
        m_data.AddSeed( x, y, z, foregroundSeed );
    }

    public void ClearSeeds() {
        Debug.LogError( "Reset object and background seed list" );
        m_data.ClearSeeds();
        foreach( GameObject obj in foregroundSeeds ) {
            Destroy( obj );
        }
        foregroundSeeds.Clear();
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

    // gets called in OnFinished of RegionGrowingJob and MaxFlowJob to have LoadLegend run its LoadSegments method
    public void FinishedSegmentationCallback() {
        m_twoDDisplay.disableTwoDDisplay();
        m_legendScript.LoadLegendFromSegmentationHandler();
        //GetComponent<AudioSource>().PlayOneShot( GetComponent<AudioSource>().clip, 1 );
    }
    int index = 0;
    public void StartMarchingCubesThread2() {
        List<bool[,,]> segments = m_data.GetSegments();
        Debug.LogError( "currently " + segments.Count + " segments" );
        if( index < segments.Count ) {
            Debug.LogError( "Running marching cubes on segment " + index );
            MarchingCubesJob marchingCubesJob = new MarchingCubesJob( this, m_data, segments[ index ] );
            m_runningThreads.Add( marchingCubesJob );
            marchingCubesJob.StartThread();
            index++;
        }
        else {
            index = 0;
        }
    }
    public void StartMarchingCubesThread() {
        List<bool[,,]> segments = m_data.GetSegments();
        Debug.LogError( "currently " + segments.Count + " segments" );
        foreach(bool[,,] segment in segments ) {
            Debug.LogError( "Running marching cubes" );
            MarchingCubesJob marchingCubesJob = new MarchingCubesJob( this, m_data, segment );
            m_runningThreads.Add( marchingCubesJob );
            marchingCubesJob.StartThread();
        }
    }
    public void MarchingCubesFinished( List<Vector3> vertices, List<int> triangles ) {
        GameObject newCube = Instantiate<GameObject>( cubePrefab );
        Vector3[] vert = vertices.ToArray();
        
        Vector3 minimum = vert[0];
        Vector3 maximum = vert[0];
        for ( int i = 1; i < vert.Length; i++ ) {
            maximum.x = Math.Max(maximum.x, vert[i].x);
            maximum.y = Math.Max(maximum.y, vert[i].y);
            maximum.z = Math.Max(maximum.z, vert[i].z);

            minimum.x = Math.Min(minimum.x, vert[i].x);
            minimum.y = Math.Min(minimum.y, vert[i].y);
            minimum.z = Math.Min(minimum.z, vert[i].z);
        }
        Vector3 center = (minimum + maximum) / 2;
        Vector3 size = (maximum - minimum);
        for (int i = 0; i < vert.Length; i++) {
            vert[i] = vert[i] - center;
        }
        Mesh mesh = new Mesh();
        mesh.vertices = vert;
        mesh.triangles = triangles.ToArray();
        if (true) {
            mesh.normals = computeNormals(mesh.vertices, mesh.triangles);
            Color32[] col = new Color32[mesh.vertices.Length];
            Color dullColor = new Color(UnityEngine.Random.value / 2 + 0.5f, UnityEngine.Random.value / 2 + 0.5f, UnityEngine.Random.value / 2 + 0.5f);
            for (int i = 0; i < mesh.normals.Length; i++) {
                //col[ i ] = new Color( mesh.normals[ i ].x, mesh.normals[ i ].y, mesh.normals[ i ].z, 1.0f );
                //col[ i ] = new Color( Mathf.Abs( 0.0f - mesh.normals[ i ].x ), Mathf.Abs( 0.0f - mesh.normals[ i ].y ), Mathf.Abs( 0.0f - mesh.normals[ i ].z ), 1.0f );
                col[i] = dullColor;
            }
            mesh.colors32 = col;
        }
        newCube.GetComponent<MeshFilter>().mesh = mesh;



        //newCube.GetComponent<BoxCollider>().center = center;
        //newCube.GetComponent<BoxCollider>().size = size;

        GameObject wireframe = Instantiate<GameObject>(wireFrameCubePrefab);
        wireframe.transform.parent = newCube.transform;
        wireframe.transform.localScale = size + new Vector3(0.001f, 0.001f, 0.001f);
        wireframe.GetComponent<MeshRenderer>().enabled = false;
        //wireframe.SetActive(true);

        //newCube.GetComponent<WireFrameScript>().meshCube = wireframe;
        newCube.transform.Translate(center);

        LGrabScript.AddGrabbable(wireframe);
        RGrabScript.AddGrabbable(wireframe);
        StartMarchingCubesThread2();
    }

    public void StartMaxFlowSegmentationTimedThread() {
        Debug.LogError( "beginning timed max flow segmenting, currently doesnt do anything" );
        string filename = m_data.getNumLayers() + " " + m_data.getWidth() + " debugLog" + Time.time + ".txt";
		StreamWriter file = new StreamWriter( Application.dataPath + "/Output/" + filename );
        Debug.LogError( " filename = " + filename );
        //RunMaxFlowSegmentationTimed();
        file.Close();
        GetComponent<AudioSource>().PlayOneShot( GetComponent<AudioSource>().clip, 1 );
        Debug.LogError( "finished timed max flow segmenting" );
        m_twoDDisplay.disableTwoDDisplay();
        m_legendScript.LoadLegendFromSegmentationHandler();
    }
		

    int threshold = 30;
    public void SelectBackground() {
        bool[,,] black = m_data.selectBlackPixels( (byte) threshold );
        //threshold = threshold - 20;
        m_data.AddSegment( black );
        m_legendScript.LoadLegendFromSegmentationHandler();
    }

    public void SelectAll() {
        m_data.AddSegment( m_data.InvertSegment( new bool[ m_data.getWidth(), m_data.getHeight(), m_data.getNumLayers() ] ) );
        m_legendScript.LoadLegendFromSegmentationHandler();
    }

    public void InvertSelection() {
        bool[,,] inverted = m_data.InvertSegment( m_data.GetSegment() );
        m_data.AddSegment( inverted );
        m_legendScript.LoadLegendFromSegmentationHandler();
    }

    public void SeparateSegments() {
        bool[,,] inverted = m_data.GetSegment();
        List<bool[,,]> segments = m_data.SeparateSegment( inverted, 20 );
        //m_data.saveSegmentToFileAsText( inverted, "segments/invert.txt" );
        Debug.LogError( "separated " + segments.Count + " segments" );
        ClearSeeds();
        m_data.ClearSegments();
        int count = 0;
        foreach( bool[,,] segment in segments ) {
            int segmentVolume = 0;
            for( int a = 0; a < segment.GetLength( 0 ); a++ ) {
                for( int b = 0; b < segment.GetLength( 1 ); b++ ) {
                    for( int c = 0; c < segment.GetLength( 2 ); c++ ) {
                        if( segment[ a, b, c ] ) {
                            segmentVolume++;
                        }
                    }
                }
            }
            //m_data.saveSegmentToFileAsText( segment, "segments/" +  ++count + "after" + segmentVolume + ".txt" );
            m_data.AddSegment( segment );
        }
    }

    public void ClearSegmentsAndSeeds() {
        ClearSeeds();
        m_data.ClearSegments();
    }

    void Update() {

        if (Input.GetKeyDown("c"))
        {
            Debug.Log("HAPPENED");
            renderCube.GetComponent<CubeCut>().ToggleCut();
        }
        if (Input.GetKeyDown("u"))
        {
            renderCube.GetComponent<CubeCut>().ResetCut();
        }
        if ( Input.GetKeyDown( "p" ) ) {
            SelectBackground();
            InvertSelection();
            //m_data.saveSegmentToFileAsText( m_data.GetSegment(), "segments/before.txt" );
            SeparateSegments();
            //GetComponent<AudioSource>().PlayOneShot( GetComponent<AudioSource>().clip, 1 );
            //m_legendScript.LoadLegendFromSegmentationHandler();
        }
        if( Input.GetKeyDown( "g" ) ) {
            loadMedicalData( folderName, filePrefix, 0, m_data.getNumLayers());
        }
        if( Input.GetKeyDown( "b" ) ) {
            m_data.ClearSegments();
        }
        if (Input.GetKeyDown("r")) {
            m_data.saveSegmentToFileAsText( m_data.GetSegment(), "testSegment.txt" );
            ClearSeeds();
            SelectAll();
        }
        if( Input.GetKeyDown( "m" ) ) {
            StartMarchingCubesThread2();
        }
        // Update each thread to see if it is done yet.
        for( int index = m_runningThreads.Count - 1; index >= 0; index-- ) {
            if( m_runningThreads[index].Update() ) {
                m_runningThreads.Remove( m_runningThreads[ index ] );
            }
        }
    }


    public Vector3[] computeNormals( Vector3[] vertices, int[] triangles ) {

        Debug.Log( "Has " + triangles.Length + " triangles" );
        int numTriangles = triangles.Length / 3 /2; // only look through first half of triangles, not the backwards facing ones.
        Vector3[] normals = new Vector3[ vertices.Length ];
        for( int t = 0; t < numTriangles; t++ ) {
            Vector3 vec1 = vertices[ triangles[ t * 3 + 1 ] ] - vertices[ triangles[ t * 3 ] ];
            Vector3 vec2 = vertices[ triangles[ t * 3 + 2 ] ] - vertices[ triangles[ t * 3 ] ];
            vec1 = Vector3.Normalize( vec1 );
            vec2 = Vector3.Normalize( vec2 );
            Vector3 normal = Vector3.Cross( vec1, vec2);
            normal = Vector3.Normalize( normal );
            for( int i = 0; i < 3; i++ ) {
                normals[ triangles[ t * 3 + i ] ] = normal;
            }
        }
        //for( int i = 0; i < normals.Length; i++ ) {
        //    if( normals[i] == null ) {
        //        Debug.Log("Normal is null");
        //    }
        //    if( Vector3.Magnitude(normals[i]) < 0.999999999 ) {
        //        Debug.Log( "normal is " + Vector3.Magnitude( normals[ i ] ) );
        //    }
        //}
        return normals;
    }
}
