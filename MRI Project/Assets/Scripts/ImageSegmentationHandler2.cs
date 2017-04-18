
using UnityEngine.UI;
using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

public class ImageSegmentationHandler2 : MonoBehaviour {

    [SerializeField] private TwoDDisplay m_twoDDisplay;

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

    public bool m_viewCopiedTextures;
    public bool loadSegmentOnStart;
    public bool m_useAlpha;

    private int guiWidth;
    private int guiHeight;
    
    private int edgesCounter;

    private float SOURCESINKFLOW = 2.0f;
    private float SIGMASQUARED = 0.0001f;
    
    private bool[,,] segmentedTextures;

    private List<GameObject> objectSeeds;
    private List<GameObject> backgroundSeeds;
    
    [SerializeField]
    private DataContainer data;

    StreamWriter file;
    
    private RegionGrowingJob regionGrowingJob;

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
    }

    public bool[,,] GetSegments() {
        return segmentedTextures;
    }

    public void loadMedicalData(string folderName, string filePrefix, int startLayer, int numLayers) {
        data.loadMedicalData( folderName, filePrefix, startLayer, numLayers );
        segmentedTextures = new bool[data.getWidth(), data.getHeight(), data.getNumLayers()];
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
            int xCoord = (int)((0.5f - x) * data.getWidth());
            int zCoord = (int)((y + 0.5f) * data.getNumLayers());
            int yCoord = (int)((0.5f - z) * data.getHeight());
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
        int xCoord = (int) ( x * data.getWidth() );
        int yCoord = (int) ( y * data.getHeight() );
        Debug.Log( " clicked on = (" + x + "," + y + ")" );
        Debug.Log( " Coord of new seed = (" + xCoord + "," + yCoord + ")" );
        if( xCoord >= 0 && xCoord < data.getWidth() && yCoord >= 0 && yCoord < data.getHeight() ) {
            AddSeed( xCoord, yCoord, z, objectSeed );
            float visualX = 0.5f - x;
            float visualZ = 0.5f - y;
            float visualY = ( 1.0f * z / data.getNumLayers() ) - 0.5f;
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
        data.AddSeed( x, y, z, objectSeed );
    }

    public void StartFloodFillSegmentationThread() {
        regionGrowingJob = new RegionGrowingJob( this, data, m_threshold );
        regionGrowingJob.StartThread();
    }

    public void FinishedSegmentationCallback() {
        m_twoDDisplay.disableTwoDDisplay();
        m_legendScript.LoadLegendFrom( "segment" );
    }

    public void StartMaxFlowSegmentationThread() {
        Debug.LogError( "beginning max flow segmenting" );
        file = new StreamWriter( "debugLog.txt" );
        RunMaxFlowSegmentation();
        file.Close();
        GetComponent<AudioSource>().PlayOneShot( GetComponent<AudioSource>().clip, 1 );
        Debug.LogError( "finished max flow segmenting" );
        m_twoDDisplay.disableTwoDDisplay();
        m_legendScript.LoadLegendFrom( "segment" );
    }

    public void StartMaxFlowSegmentationTimedThread() {
        Debug.LogError( "beginning timed max flow segmenting" );
        string filename = data.getNumLayers() + " " + data.getWidth() + " debugLog" + Time.time + ".txt";
        file = new StreamWriter( filename );
        Debug.LogError( " filename = " + filename );
        RunMaxFlowSegmentationTimed();
        file.Close();
        GetComponent<AudioSource>().PlayOneShot( GetComponent<AudioSource>().clip, 1 );
        Debug.LogError( "finished timed max flow segmenting" );
        m_twoDDisplay.disableTwoDDisplay();
        m_legendScript.LoadLegendFrom( "segment" );
    }
    
    public void ClearSeeds() {
        Debug.LogError( "Reset object and background seed list" );
        data.ClearSeeds();
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
        AddSeedTwoD( 155.0f * M / data.getWidth(), 149.0f * M / data.getHeight(), 0, true );
        AddSeedTwoD( 155.0f * M / data.getWidth(), 149.0f * M / data.getHeight(), 0, true );
        AddSeedTwoD( 144.0f * M / data.getWidth(), 160.0f * M / data.getHeight(), 0, true );
        AddSeedTwoD( 132.0f * M / data.getWidth(), 176.0f * M / data.getHeight(), 0, true );
        AddSeedTwoD( 131.0f * M / data.getWidth(), 197.0f * M / data.getHeight(), 0, true );
        AddSeedTwoD( 115.0f * M / data.getWidth(), 202.0f * M / data.getHeight(), 0, true );
        AddSeedTwoD( 115.0f * M / data.getWidth(), 179.0f * M / data.getHeight(), 0, true );
        AddSeedTwoD( 125.0f * M / data.getWidth(), 160.0f * M / data.getHeight(), 0, true );
        AddSeedTwoD( 135.0f * M / data.getWidth(), 145.0f * M / data.getHeight(), 0, true );
        AddSeedTwoD( 151.0f * M / data.getWidth(), 132.0f * M / data.getHeight(), 0, true );
        AddSeedTwoD( 159.0f * M / data.getWidth(), 139.0f * M / data.getHeight(), 0, true );

        AddSeedTwoD( 154.0f * M / data.getWidth(), 171.0f * M / data.getHeight(), 0, false );
        AddSeedTwoD( 144.0f * M / data.getWidth(), 197.0f * M / data.getHeight(), 0, false );
        AddSeedTwoD( 132.0f * M / data.getWidth(), 217.0f * M / data.getHeight(), 0, false );
        AddSeedTwoD( 103.0f * M / data.getWidth(), 213.0f * M / data.getHeight(), 0, false );
        AddSeedTwoD(  98.0f * M / data.getWidth(), 188.0f * M / data.getHeight(), 0, false );
        AddSeedTwoD( 111.0f * M / data.getWidth(), 161.0f * M / data.getHeight(), 0, false );
        AddSeedTwoD( 123.0f * M / data.getWidth(), 141.0f * M / data.getHeight(), 0, false );
        AddSeedTwoD( 141.0f * M / data.getWidth(), 124.0f * M / data.getHeight(), 0, false );
        AddSeedTwoD( 163.0f * M / data.getWidth(), 124.0f * M / data.getHeight(), 0, false );
        AddSeedTwoD( 172.0f * M / data.getWidth(), 146.0f * M / data.getHeight(), 0, false );
    }

    void Update() {
        if( Input.GetKeyDown( "g" ) ) {
            loadMedicalData( "heart", "heart-", 0, 10 );
        }
        if (Input.GetKeyDown("r")) {
            segmentedTextures = new bool[ data.getWidth(), data.getHeight(), data.getNumLayers() ];
            ClearSeeds();
        }
        if( regionGrowingJob != null ) {
            if( regionGrowingJob.Update() ) {
                regionGrowingJob = null;
            }
        }
    }

    public class Vertex {
        public Vertex[] neighbors;
        public float[] flows;
        public bool visited;
        public Vertex from;
        public float value;

        public Vertex(int numNeighbors, float svalue) {
            neighbors = new Vertex[numNeighbors];
            flows = new float[numNeighbors];
            visited = false;
            from = null;
            value = svalue;
        }
    }
    private float ComputeFlowBetween(Vertex one, Vertex two ) {
        float delta = (one.value - two.value);
        float sigmaSquared = SIGMASQUARED;
        float flow = (float)Math.Exp(-(delta * delta) / sigmaSquared);
        return flow;
    }
    private float MaxFlowBetweenPoints(int x1, int y1, int z1, int x2, int y2, int z2) {
        //float delta = (originalScans[z1, x1, y1] - originalScans[z2, x2, y2]);
        float delta = ( data.getOriginalPixelFloat(x1, y1, z1 ) - data.getOriginalPixelFloat( x2, y2, z2 ) );
        float sigmaSquared = SIGMASQUARED;
        float flow = (float)Math.Exp(-(delta * delta) / sigmaSquared);
        //file.WriteLine("\tPoint {0},{1} has intensity = {2}, delta = {3}, and flow = {4}", x2, y2, originalScans[z2, x2, y2], delta, flow);
        return flow;
    }
    private float ComputeAverageValue(int x1, int y1, int z1, int xfreq, int yfreq, int zfreq) {
        float sum1 = 0.0f;
        int x, y, z;
        int sx1 = x1 * xfreq, sy1 = y1 * yfreq, sz1 = z1 * zfreq;
        for (x = 0; x < xfreq; x++) {
            for (y = 0; y < yfreq; y++) {
                for (z = 0; z < zfreq; z++) {
                    //sum1 += originalScans[sz1 + z, sx1 + x, sy1 + y];
                    sum1 += data.getOriginalPixelFloat( sx1 + x, sy1 + y, sz1 + z );
                }
            }
        }
        return sum1 / (xfreq * yfreq * zfreq);
    }
    private float MaxFlowBetweenPointsSampling(int x1, int y1, int z1, int x2, int y2, int z2, int xfreq, int yfreq, int zfreq) {
        float average1 = ComputeAverageValue( x1, y1, z1, xfreq, yfreq, zfreq );
        float average2 = ComputeAverageValue( x2, y2, z2, xfreq, yfreq, zfreq );
        float delta = average1 - average2;
        float sigmaSquared = SIGMASQUARED;
        float flow = (float) Math.Exp ( - (delta * delta) / sigmaSquared); // e ^ ( delta^2 / sigma^2 )
        //file.WriteLine("\tPoint {0},{1} has intensity = {2}, delta = {3}, and flow = {4}", x2, y2, originalScans[z2, x2, y2], delta, flow);
        return flow;
    }

    private void MaxFlowSetup(Vertex[,,] vertices, Vertex sink, Vertex source) {
        int numNeighbors = 7;
        //float startTime = Time.time;
        for (int x = 0; x < data.getWidth(); x++) {
            for (int y = 0; y < data.getHeight(); y++) {
                for (int z = 0; z < data.getNumLayers(); z++) {
                    if (m_3DFlow) {
                        numNeighbors = 7;
                    }
                    else {
                        numNeighbors = 5;
                    }
                    if (x == 0 || x == data.getWidth() - 1) {
                        numNeighbors--;
                    }
                    if (y == 0 || y == data.getHeight() - 1) {
                        numNeighbors--;
                    }
                    if (m_3DFlow && (z == 0 || z == data.getNumLayers() - 1)) {
                        numNeighbors--;
                    }
                    vertices[x, y, z] = new Vertex(numNeighbors, data.getOriginalPixelFloat(x, y, z));
                }
            }
        }
        float maximumFlow = 0.0f;
        for (int x = 0; x < data.getWidth(); x++) {
            for (int y = 0; y < data.getHeight(); y++) {
                for (int z = 0; z < data.getNumLayers(); z++) {
                    source.neighbors[(x * data.getHeight() + y) * data.getNumLayers() + z] = vertices[x, y, z]; // the source is connected to each pixel
                    int n_i = 0;
                    vertices[x, y, z].neighbors[n_i++] = sink; // each vertex is connected to the sink
                    //file.WriteLine("Point {0},{1},{2} has intensity {3}", x, y, z, originalScans[scanIndex, x, y]);
                    //file.WriteLine("Neighbors:");
                    if (x != 0) { // Add the four neighbors of each pixel but check edge cases.
                        vertices[x, y, z].flows[n_i] = MaxFlowBetweenPoints(x, y, z, x - 1, y, z);
                        maximumFlow = Mathf.Max(maximumFlow, vertices[x, y, z].flows[n_i]);
                        vertices[x, y, z].neighbors[n_i++] = vertices[x - 1, y, z];

                    }
                    if (x != data.getWidth() - 1) {
                        vertices[x, y, z].flows[n_i] = MaxFlowBetweenPoints(x, y, z, x + 1, y, z);
                        maximumFlow = Mathf.Max(maximumFlow, vertices[x, y, z].flows[n_i]);
                        vertices[x, y, z].neighbors[n_i++] = vertices[x + 1, y, z];
                    }
                    if (y != 0) {
                        vertices[x, y, z].flows[n_i] = MaxFlowBetweenPoints(x, y, z, x, y - 1, z);
                        maximumFlow = Mathf.Max(maximumFlow, vertices[x, y, z].flows[n_i]);
                        vertices[x, y, z].neighbors[n_i++] = vertices[x, y - 1, z];
                    }
                    if (y != data.getHeight() - 1) {
                        vertices[x, y, z].flows[n_i] = MaxFlowBetweenPoints(x, y, z, x, y + 1, z);
                        maximumFlow = Mathf.Max(maximumFlow, vertices[x, y, z].flows[n_i]);
                        vertices[x, y, z].neighbors[n_i++] = vertices[x, y + 1, z];
                    }
                    if (m_3DFlow) {
                        if (z != 0) {
                            vertices[x, y, z].flows[n_i] = MaxFlowBetweenPoints(x, y, z, x, y, z - 1);
                            maximumFlow = Mathf.Max(maximumFlow, vertices[x, y, z].flows[n_i]);
                            vertices[x, y, z].neighbors[n_i++] = vertices[x, y, z - 1];
                        }
                        if (z != data.getNumLayers() - 1) {
                            vertices[x, y, z].flows[n_i] = MaxFlowBetweenPoints(x, y, z, x, y, z + 1);
                            maximumFlow = Mathf.Max(maximumFlow, vertices[x, y, z].flows[n_i]);
                            vertices[x, y, z].neighbors[n_i++] = vertices[x, y, z + 1];
                        }
                    }
                }
            }
        }
        //file.WriteLine("The maximum flow between any two pixels was: {0}", maximumFlow);

        for (int x = 0; x < data.getWidth(); x++) {
            for (int y = 0; y < data.getHeight(); y++) {
                for (int z = 0; z < data.getNumLayers(); z++) {
                    source.flows[(x * data.getHeight() + y) * data.getNumLayers() + z] = 0.0f; // need to figure out what to put here 0 seems to be working just fine for now
                    vertices[x, y, z].flows[0] = 0.0f; // need to figure out what to put here
                }
            }
        }
        foreach ( DataContainer.Point obj in data.GetObjectSeeds()) {
            source.flows[(obj.x * data.getHeight() + obj.y) * data.getNumLayers() + obj.z] = 1.0f + maximumFlow; // the flow from the source to a seed is 1 + maxflow
            vertices[obj.x, obj.y, obj.z].flows[0] = 0.0f; // the flow to the sink from a seed is 0

            // file.WriteLine("source to pixel {0},{1},{2}  flow: {3}", obj.x, obj.y, obj.z, source.flows[(obj.x * data.getHeight() + obj.y) * data.getNumLayers() + obj.z]);
        }
        foreach ( DataContainer.Point obj in data.GetBackgroundSeeds() ) {
            source.flows[(obj.x * data.getHeight() + obj.y) * data.getNumLayers() + obj.z] = 0.0f;
            vertices[obj.x, obj.y, obj.z].flows[0] = 1.0f + maximumFlow;
            //file.WriteLine("pixel to background {0},{1},{2}  flow: {3}", obj.x, obj.y, obj.z, vertices[obj.x, obj.y, obj.z].flows[0]);
        }
    }

    public Vertex[,,] MaxFlowSetupBetter(Vertex sink, Vertex source, int xfreq, int yfreq, int zfreq) {
        int numNeighbors;
        Vertex[,,] vertices = new Vertex[data.getWidth(), data.getHeight(), data.getNumLayers() ];
        foreach ( DataContainer.Point obj in data.GetBackgroundSeeds() ) {
            int x = obj.x; int y = obj.y; int z = obj.z;
            if (m_3DFlow) {
                numNeighbors = 7;
            }
            else {
                numNeighbors = 5;
            }
            if (x == 0 || x == data.getWidth() - 1) {
                numNeighbors--;
            }
            if (y == 0 || y == data.getHeight() - 1) {
                numNeighbors--;
            }
            if (m_3DFlow && (z == 0 || z == data.getNumLayers() - 1)) {
                numNeighbors--;
            }
            edgesCounter += numNeighbors;
            vertices[x, y, z] = new Vertex(numNeighbors, data.getOriginalPixelFloat(x, y, z));
            vertices[x, y, z].neighbors[numNeighbors - 1] = sink;
        }
        //float startTime = Time.time;
        for (int x = 0; x < data.getWidth(); x++) {
            for (int y = 0; y < data.getHeight(); y++) {
                for (int z = 0; z < data.getNumLayers(); z++) {
                    if (vertices[x, y, z] == null) {
                        if (m_3DFlow) {
                            numNeighbors = 6;
                        }
                        else {
                            numNeighbors = 4;
                        }
                        if (x == 0 || x == data.getWidth() - 1) {
                            numNeighbors--;
                        }
                        if (y == 0 || y == data.getHeight() - 1) {
                            numNeighbors--;
                        }
                        if (m_3DFlow && (z == 0 || z == data.getNumLayers() - 1)) {
                            numNeighbors--;
                        }
                        edgesCounter += numNeighbors;
                        vertices[x, y, z] = new Vertex(numNeighbors, data.getOriginalPixelFloat( x, y, z ) );
                    }
                }
            }
        }
        float maximumFlow = 0.0f;
        for (int x = 0; x < data.getWidth(); x++) {
            for (int y = 0; y < data.getHeight(); y++) {
                for (int z = 0; z < data.getNumLayers(); z++) {
                    //source.neighbors[(x * data.getHeight() + y) * data.getNumLayers() + z] = vertices[x, y, z]; // the source is connected to each pixel
                    int n_i = 0;
                    if (x != 0) { // Add the four neighbors of each pixel but check edge cases.
                        vertices[x, y, z].flows[n_i] = MaxFlowBetweenPoints(x, y, z, x - 1, y, z);
                        maximumFlow = Mathf.Max(maximumFlow, vertices[x, y, z].flows[n_i]);
                        vertices[x, y, z].neighbors[n_i++] = vertices[x - 1, y, z];

                    }
                    if (x != data.getWidth() - 1) {
                        vertices[x, y, z].flows[n_i] = MaxFlowBetweenPoints(x, y, z, x + 1, y, z);
                        maximumFlow = Mathf.Max(maximumFlow, vertices[x, y, z].flows[n_i]);
                        vertices[x, y, z].neighbors[n_i++] = vertices[x + 1, y, z];
                    }
                    if (y != 0) {
                        vertices[x, y, z].flows[n_i] = MaxFlowBetweenPoints(x, y, z, x, y - 1, z);
                        maximumFlow = Mathf.Max(maximumFlow, vertices[x, y, z].flows[n_i]);
                        vertices[x, y, z].neighbors[n_i++] = vertices[x, y - 1, z];
                    }
                    if (y != data.getHeight() - 1) {
                        vertices[x, y, z].flows[n_i] = MaxFlowBetweenPoints(x, y, z, x, y + 1, z);
                        maximumFlow = Mathf.Max(maximumFlow, vertices[x, y, z].flows[n_i]);
                        vertices[x, y, z].neighbors[n_i++] = vertices[x, y + 1, z];
                    }
                    if (m_3DFlow) {
                        if (z != 0) {
                            vertices[x, y, z].flows[n_i] = MaxFlowBetweenPoints(x, y, z, x, y, z - 1);
                            maximumFlow = Mathf.Max(maximumFlow, vertices[x, y, z].flows[n_i]);
                            vertices[x, y, z].neighbors[n_i++] = vertices[x, y, z - 1];
                        }
                        if (z != data.getNumLayers() - 1) {
                            vertices[x, y, z].flows[n_i] = MaxFlowBetweenPoints(x, y, z, x, y, z + 1);
                            maximumFlow = Mathf.Max(maximumFlow, vertices[x, y, z].flows[n_i]);
                            vertices[x, y, z].neighbors[n_i++] = vertices[x, y, z + 1];
                        }
                    }
                }
            }
        }

        //for (int x = 0; x < data.getWidth(); x++) {
        //    for (int y = 0; y < data.getHeight(); y++) {
        //        for (int z = 0; z < data.getNumLayers(); z++) {
        //            source.flows[(x * data.getHeight() + y) * data.getNumLayers() + z] = 0.0f; // need to figure out what to put here 0 seems to be working just fine for now
        //            vertices[x, y, z].flows[0] = 0.0f; // need to figure out what to put here
        //        }
        //    }
        //}
        int index = 0;
        foreach ( DataContainer.Point obj in data.GetObjectSeeds()) {
            source.neighbors[index] = vertices[obj.x, obj.y, obj.z];
            source.flows[index] = 1.0f + maximumFlow; // the flow from the source to a seed is 1 + maxflow
            index++;
            //vertices[obj.x, obj.y, obj.z].flows[0] = 0.0f; // the flow to the sink from a seed is 0

            // file.WriteLine("source to pixel {0},{1},{2}  flow: {3}", obj.x, obj.y, obj.z, source.flows[(obj.x * data.getHeight() + obj.y) * data.getNumLayers() + obj.z]);
        }
        foreach ( DataContainer.Point obj in data.GetBackgroundSeeds()) {
            //source.flows[(obj.x * data.getHeight() + obj.y) * data.getNumLayers() + obj.z] = 0.0f;
            vertices[obj.x, obj.y, obj.z].flows[vertices[obj.x, obj.y, obj.z].flows.Length - 1] = 1.0f + maximumFlow;
            //file.WriteLine("pixel to background {0},{1},{2}  flow: {3}", obj.x, obj.y, obj.z, vertices[obj.x, obj.y, obj.z].flows[0]);
        }
        return vertices;
    }

    public Vertex[,,] MaxFlowSetupBetterSampling(Vertex sink, Vertex source, int xfreq, int yfreq, int zfreq) {
        int numNeighbors;
        int numEdges = 0;
        int scanWidth = data.getWidth() / xfreq;
        int scanHeight = data.getHeight() / yfreq;
        int numScans = data.getNumLayers() / zfreq;
        Vertex[,,] vertices = new Vertex[scanWidth, scanHeight, numScans];
        foreach ( DataContainer.Point obj in data.GetBackgroundSeeds()) {
            int x = obj.x/xfreq; int y = obj.y/yfreq; int z = obj.z/zfreq;
            if (m_3DFlow) {
                numNeighbors = 7;
            }
            else {
                numNeighbors = 5;
            }
            if (x == 0 || x == scanWidth - 1) {
                numNeighbors--;
            }
            if (y == 0 || y == scanHeight - 1) {
                numNeighbors--;
            }
            if (m_3DFlow && (z == 0 || z == numScans - 1)) {
                numNeighbors--;
            }
            numEdges += numNeighbors;
            //Debug.Log(x + ", " + y + ",  " + z);
            //Debug.Log(scanWidth + ", " + scanHeight + ",  " + numScans);
            vertices[x, y, z] = new Vertex(numNeighbors, data.getOriginalPixelFloat(obj.x, obj.y, obj.z ) );
            vertices[x, y, z].neighbors[numNeighbors - 1] = sink;
        }
        //float startTime = Time.time;
        for (int x = 0; x < scanWidth; x++) {
            for (int y = 0; y < scanHeight; y++) {
                for (int z = 0; z < numScans; z++) {
                    if (vertices[x, y, z] == null) {
                        if (m_3DFlow) {
                            numNeighbors = 6;
                        }
                        else {
                            numNeighbors = 4;
                        }
                        if (x == 0 || x == scanWidth - 1) {
                            numNeighbors--;
                        }
                        if (y == 0 || y == scanHeight - 1) {
                            numNeighbors--;
                        }
                        if (m_3DFlow && (z == 0 || z == numScans - 1)) {
                            numNeighbors--;
                        }
                        numEdges += numNeighbors;
                        vertices[x, y, z] = new Vertex(numNeighbors, ComputeAverageValue(x, y, z, xfreq, yfreq, zfreq));
                    }
                }
            }
        }
        float maximumFlow = 0.0f;
        for (int x = 0; x < scanWidth; x++) {
            for (int y = 0; y < scanHeight; y++) {
                for (int z = 0; z < numScans; z++) {
                    //source.neighbors[(x * data.getHeight() + y) * data.getNumLayers() + z] = vertices[x, y, z]; // the source is connected to each pixel
                    int n_i = 0;
                    if (x != 0) { // Add the four neighbors of each pixel but check edge cases.
                        vertices[x, y, z].flows[n_i] = MaxFlowBetweenPointsSampling(x, y, z, x - 1, y, z, xfreq, yfreq, zfreq);
                        maximumFlow = Mathf.Max(maximumFlow, vertices[x, y, z].flows[n_i]);
                        vertices[x, y, z].neighbors[n_i++] = vertices[x - 1, y, z];
                    }
                    if (x != scanWidth - 1) {
                        vertices[x, y, z].flows[n_i] = MaxFlowBetweenPointsSampling(x, y, z, x + 1, y, z, xfreq, yfreq, zfreq);
                        maximumFlow = Mathf.Max(maximumFlow, vertices[x, y, z].flows[n_i]);
                        vertices[x, y, z].neighbors[n_i++] = vertices[x + 1, y, z];
                    }
                    if (y != 0) {
                        vertices[x, y, z].flows[n_i] = MaxFlowBetweenPointsSampling(x, y, z, x, y - 1, z, xfreq, yfreq, zfreq);
                        maximumFlow = Mathf.Max(maximumFlow, vertices[x, y, z].flows[n_i]);
                        vertices[x, y, z].neighbors[n_i++] = vertices[x, y - 1, z];
                    }
                    if (y != scanHeight - 1) {
                        vertices[x, y, z].flows[n_i] = MaxFlowBetweenPointsSampling(x, y, z, x, y + 1, z, xfreq, yfreq, zfreq);
                        maximumFlow = Mathf.Max(maximumFlow, vertices[x, y, z].flows[n_i]);
                        vertices[x, y, z].neighbors[n_i++] = vertices[x, y + 1, z];
                    }
                    if (m_3DFlow) {
                        if (z != 0) {
                            vertices[x, y, z].flows[n_i] = MaxFlowBetweenPointsSampling(x, y, z, x, y, z - 1, xfreq, yfreq, zfreq);
                            maximumFlow = Mathf.Max(maximumFlow, vertices[x, y, z].flows[n_i]);
                            vertices[x, y, z].neighbors[n_i++] = vertices[x, y, z - 1];
                        }
                        if (z != numScans - 1) {
                            vertices[x, y, z].flows[n_i] = MaxFlowBetweenPointsSampling(x, y, z, x, y, z + 1, xfreq, yfreq, zfreq);
                            maximumFlow = Mathf.Max(maximumFlow, vertices[x, y, z].flows[n_i]);
                            vertices[x, y, z].neighbors[n_i++] = vertices[x, y, z + 1];
                        }
                    }
                }
            }
        }

        //for (int x = 0; x < data.getWidth(); x++) {
        //    for (int y = 0; y < data.getHeight(); y++) {
        //        for (int z = 0; z < data.getNumLayers(); z++) {
        //            source.flows[(x * data.getHeight() + y) * data.getNumLayers() + z] = 0.0f; // need to figure out what to put here 0 seems to be working just fine for now
        //            vertices[x, y, z].flows[0] = 0.0f; // need to figure out what to put here
        //        }
        //    }
        //}
        int index = 0;
        foreach ( DataContainer.Point obj in data.GetObjectSeeds()) {
            source.neighbors[index] = vertices[obj.x/xfreq, obj.y/yfreq, obj.z/zfreq];
            source.flows[index] = 1.0f + maximumFlow; // the flow from the source to a seed is 1 + maxflow
            index++;
            //vertices[obj.x, obj.y, obj.z].flows[0] = 0.0f; // the flow to the sink from a seed is 0

            // file.WriteLine("source to pixel {0},{1},{2}  flow: {3}", obj.x, obj.y, obj.z, source.flows[(obj.x * data.getHeight() + obj.y) * data.getNumLayers() + obj.z]);
        }
        foreach ( DataContainer.Point obj in data.GetBackgroundSeeds()) {
            //source.flows[(obj.x * data.getHeight() + obj.y) * data.getNumLayers() + obj.z] = 0.0f;
            vertices[obj.x/xfreq, obj.y/yfreq, obj.z/zfreq].flows[vertices[obj.x / xfreq, obj.y / yfreq, obj.z / zfreq].flows.Length - 1] = 1.0f + maximumFlow;
            //file.WriteLine("pixel to background {0},{1},{2}  flow: {3}", obj.x, obj.y, obj.z, vertices[obj.x, obj.y, obj.z].flows[0]);
        }
        edgesCounter += numEdges;
        return vertices;
    }

    public Vertex[,,] MaxFlowSetupBetterSamplingRound2(Vertex sink, Vertex source, int xfreq, int yfreq, int zfreq) {
        int numNeighbors;
        int numEdges = 0;
        int scanWidth = data.getWidth() / xfreq;
        int scanHeight = data.getHeight() / yfreq;
        int numScans = data.getNumLayers() / zfreq;
        Vertex[,,] vertices = new Vertex[scanWidth, scanHeight, numScans];
        foreach ( DataContainer.Point obj in data.GetBackgroundSeeds()) {
            int x = obj.x / xfreq; int y = obj.y / yfreq; int z = obj.z / zfreq;
            if (m_3DFlow) {
                numNeighbors = 7;
            }
            else {
                numNeighbors = 5;
            }
            if (x == 0 || x == scanWidth - 1) {
                numNeighbors--;
            }
            if (y == 0 || y == scanHeight - 1) {
                numNeighbors--;
            }
            if (m_3DFlow && (z == 0 || z == numScans - 1)) {
                numNeighbors--;
            }
            numEdges += numNeighbors;
            //Debug.Log(x + ", " + y + ",  " + z);
            //Debug.Log(scanWidth + ", " + scanHeight + ",  " + numScans);
            vertices[x, y, z] = new Vertex(numNeighbors, data.getOriginalPixelFloat(x, y, z));
            vertices[x, y, z].neighbors[numNeighbors - 1] = sink;
        }
        //float startTime = Time.time;
        for (int x = 0; x < scanWidth; x++) {
            for (int y = 0; y < scanHeight; y++) {
                for (int z = 0; z < numScans; z++) {
                    if (vertices[x, y, z] == null) {
                        if (m_3DFlow) {
                            numNeighbors = 6;
                        }
                        else {
                            numNeighbors = 4;
                        }
                        if (x == 0 || x == scanWidth - 1) {
                            numNeighbors--;
                        }
                        if (y == 0 || y == scanHeight - 1) {
                            numNeighbors--;
                        }
                        if (m_3DFlow && (z == 0 || z == numScans - 1)) {
                            numNeighbors--;
                        }
                        numEdges += numNeighbors;
                        vertices[x, y, z] = new Vertex(numNeighbors, ComputeAverageValue(x, y, z, xfreq, yfreq, zfreq));
                    }
                }
            }
        }
        float maximumFlow = 0.0f;
        for (int x = 0; x < scanWidth; x++) {
            for (int y = 0; y < scanHeight; y++) {
                for (int z = 0; z < numScans; z++) {
                    //source.neighbors[(x * data.getHeight() + y) * data.getNumLayers() + z] = vertices[x, y, z]; // the source is connected to each pixel
                    int n_i = 0;
                    if (x != 0) { // Add the four neighbors of each pixel but check edge cases.
                        vertices[x, y, z].flows[n_i] = xfreq*MaxFlowBetweenPointsSampling(x, y, z, x - 1, y, z, xfreq, yfreq, zfreq);
                        maximumFlow = Mathf.Max(maximumFlow, vertices[x, y, z].flows[n_i]);
                        vertices[x, y, z].neighbors[n_i++] = vertices[x - 1, y, z];
                    }
                    if (x != scanWidth - 1) {
                        vertices[x, y, z].flows[n_i] = xfreq * MaxFlowBetweenPointsSampling(x, y, z, x + 1, y, z, xfreq, yfreq, zfreq);
                        maximumFlow = Mathf.Max(maximumFlow, vertices[x, y, z].flows[n_i]);
                        vertices[x, y, z].neighbors[n_i++] = vertices[x + 1, y, z];
                    }
                    if (y != 0) {
                        vertices[x, y, z].flows[n_i] = yfreq * MaxFlowBetweenPointsSampling(x, y, z, x, y - 1, z, xfreq, yfreq, zfreq);
                        maximumFlow = Mathf.Max(maximumFlow, vertices[x, y, z].flows[n_i]);
                        vertices[x, y, z].neighbors[n_i++] = vertices[x, y - 1, z];
                    }
                    if (y != scanHeight - 1) {
                        vertices[x, y, z].flows[n_i] = yfreq * MaxFlowBetweenPointsSampling(x, y, z, x, y + 1, z, xfreq, yfreq, zfreq);
                        maximumFlow = Mathf.Max(maximumFlow, vertices[x, y, z].flows[n_i]);
                        vertices[x, y, z].neighbors[n_i++] = vertices[x, y + 1, z];
                    }
                    if (m_3DFlow) {
                        if (z != 0) {
                            vertices[x, y, z].flows[n_i] = zfreq * MaxFlowBetweenPointsSampling(x, y, z, x, y, z - 1, xfreq, yfreq, zfreq);
                            maximumFlow = Mathf.Max(maximumFlow, vertices[x, y, z].flows[n_i]);
                            vertices[x, y, z].neighbors[n_i++] = vertices[x, y, z - 1];
                        }
                        if (z != numScans - 1) {
                            vertices[x, y, z].flows[n_i] = zfreq * MaxFlowBetweenPointsSampling(x, y, z, x, y, z + 1, xfreq, yfreq, zfreq);
                            maximumFlow = Mathf.Max(maximumFlow, vertices[x, y, z].flows[n_i]);
                            vertices[x, y, z].neighbors[n_i++] = vertices[x, y, z + 1];
                        }
                    }
                }
            }
        }

        //for (int x = 0; x < data.getWidth(); x++) {
        //    for (int y = 0; y < data.getHeight(); y++) {
        //        for (int z = 0; z < data.getNumLayers(); z++) {
        //            source.flows[(x * data.getHeight() + y) * data.getNumLayers() + z] = 0.0f; // need to figure out what to put here 0 seems to be working just fine for now
        //            vertices[x, y, z].flows[0] = 0.0f; // need to figure out what to put here
        //        }
        //    }
        //}
        int index = 0;
        foreach ( DataContainer.Point obj in data.GetObjectSeeds()) {
            source.neighbors[index] = vertices[obj.x / xfreq, obj.y / yfreq, obj.z / zfreq];
            source.flows[index] = 1.0f + maximumFlow; // the flow from the source to a seed is 1 + maxflow
            index++;
            //vertices[obj.x, obj.y, obj.z].flows[0] = 0.0f; // the flow to the sink from a seed is 0

            // file.WriteLine("source to pixel {0},{1},{2}  flow: {3}", obj.x, obj.y, obj.z, source.flows[(obj.x * data.getHeight() + obj.y) * data.getNumLayers() + obj.z]);
        }
        foreach ( DataContainer.Point obj in data.GetBackgroundSeeds()) {
            //source.flows[(obj.x * data.getHeight() + obj.y) * data.getNumLayers() + obj.z] = 0.0f;
            vertices[obj.x / xfreq, obj.y / yfreq, obj.z / zfreq].flows[vertices[obj.x / xfreq, obj.y / yfreq, obj.z / zfreq].flows.Length - 1] = 1.0f + maximumFlow;
            //file.WriteLine("pixel to background {0},{1},{2}  flow: {3}", obj.x, obj.y, obj.z, vertices[obj.x, obj.y, obj.z].flows[0]);
        }
        edgesCounter += numEdges;
        return vertices;
    }

    public void RunMaxFlowSegmentation() {
        bool[,,] visited = new bool[data.getWidth(), data.getHeight(), data.getNumLayers()];
        // create array of width by height vertices, one for each pixel on the image
        long startTime = DateTime.Now.Ticks;


        Vertex[,,] vertices = new Vertex[data.getWidth(), data.getHeight(), data.getNumLayers()];
        Vertex sink = new Vertex(0, 0);
        //Vertex source = new Vertex(data.getWidth() * data.getHeight() * data.getNumLayers());
        Vertex source = new Vertex(data.GetObjectSeeds().Count, 1);
        
        int scanWidth = data.getWidth() / m_xfreq;
        int scanHeight = data.getHeight() / m_yfreq;
        int numScans = data.getNumLayers() / m_zfreq;

        vertices = MaxFlowSetupBetterSampling(sink, source, m_xfreq, m_yfreq, m_zfreq);

        long endTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        //float endTime = Time.time;
        file.WriteLine("Initalization took {0} ms", (endTime - startTime));
        file.Flush();

        // now that total flows have been set up, run breadth first search and each time reach sink, update flows and run again.
        // Upon parsing every accesible vertex and not having visited sink yet, that is the end and the max flow has been found.

        // For BFS use a queue
        Queue<Vertex> searchArea = new Queue<Vertex>();

        long time1 = 0;
        long time2 = 0;
        int iterations = 0;
        while (true) // this is the loop that adds augmenting paths to the flow.
        {
            startTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            searchArea.Clear(); // reset the queue
            sink.visited = false; // reset all of the visited values
            for (int x = 0; x < scanWidth; x++) {
                for (int y = 0; y < scanHeight; y++) {
                    for (int z = 0; z < numScans; z++) {
                        vertices[x, y, z].visited = false;
                    }
                }
            }

            // First enqueue the source node
            searchArea.Enqueue(source);
            source.visited = true;

            Vertex v = null;
            Vertex n = null;
            while (searchArea.Count > 0) // visit all possible from source 
            {                            // This loop is Breadth first search until finding the sink, or visiting every possible node
                v = searchArea.Dequeue();
                if (v == sink) {
                    break;
                }
                for (int i = 0; i < v.neighbors.Length; i++) {
                    n = v.neighbors[i];
                    if (!n.visited && v.flows[i] > 0) {
                        n.visited = true;
                        n.from = v;
                        searchArea.Enqueue(n);
                    }
                }
            }
            endTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            time1 += endTime - startTime;
            // Now if reached the sink need to update flows based on the path taken, then run again
            if (v == sink) {
                iterations++;
                float minFlow = 10000.0f;
                //file.Write("Found path from source to sink ");
                while (v.from != null) {
                    for (int i = 0; i < v.from.neighbors.Length; i++) {
                        if (v.from.neighbors[i] == v) {
                            minFlow = Math.Min(v.from.flows[i], minFlow);
                            //file.Write("{0} ", v.from.flows[i]);
                            break;
                        }
                    }
                    v = v.from;
                }
                v = sink;
                //file.WriteLine();
                //file.Write("Update flows: ");
                while (v.from != null) {
                    for (int i = 0; i < v.from.neighbors.Length; i++) {
                        if (v.from.neighbors[i] == v) {
                            v.from.flows[i] -= minFlow;
                            //file.Write("{0} ", v.from.flows[i]);
                            break;
                        }
                    }
                    v = v.from;
                }
                //file.WriteLine();
                //file.Flush();
            }
            else // otherwise if didnt reach sink the max-flow has been found and move on to next step
            {
                break;
            }
            startTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            time2 += startTime - endTime;
        }
        startTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        //file.WriteLine("max flow took {0} ms", (startTime - endTime));
        file.WriteLine("time1 = {0} ms, time2 = {1} ms", time1, time2);
        file.WriteLine("Iterations of flow reduction = {0}", iterations);
        Debug.Log("Iterations of flow reduction = " + iterations);
        //file.WriteLine("Final results:");
        for (int x = 0; x < data.getWidth(); x++) {
            for (int y = 0; y < data.getHeight(); y++) {
                for (int z = 0; z < data.getNumLayers(); z++) {
                    if (vertices[x/m_xfreq, y/m_yfreq, z/m_zfreq].visited) {
                        //file.Write("#");
                        visited[x, y, z] = true;
                    }
                    else {
                        //file.Write("-");
                    }
                }
            }
            //file.WriteLine("");
        }
        data.saveSegmentToFileAsImages( visited, "Resources/segment/segment");
    }

    public void ReattachVertexes(Vertex oldOne, Vertex oldTwo, Vertex[,,] vertices, int x1, int x2, int y1, int y2, int z1, int z2, int xx1, int xx2, int yy1, int yy2, int zz1, int zz2) {
        float oldFlowOneTwo = 0;
        float oldFlowTwoOne = 0;
        int numOne = (x2 - x1) * (y2 - y1) * (z2 - z1);
        int numTwo = (xx2 - xx1) * (yy2 - yy1) * (zz2 - zz1);
        for ( int i = 0; i < oldOne.neighbors.Length; i++ ) {
            if( oldOne.neighbors[i] == oldTwo ) {
                oldFlowOneTwo = oldOne.flows[i];
                break;
            }
        }
        for (int i = 0; i < oldTwo.neighbors.Length; i++) {
            if (oldTwo.neighbors[i] == oldOne) {
                oldFlowTwoOne = oldTwo.flows[i];
                break;
            }
        }
        float totalFlow = ComputeFlowBetween(oldOne, oldTwo);
        float deductOneTwo = (totalFlow - oldFlowOneTwo) / numOne;
        float deductTwoOne = (totalFlow - oldFlowTwoOne) / numTwo;

        int xx = xx1;
        int yy, zz;
        for( int x = x1; x < x2; x++ ) {
            yy = yy1;
            for( int y = y1; y < y2; y++ ) {
                zz = zz1;
                for( int z = z1; z < z2; z++ ) {
                    Vertex first = vertices[x, y, z];
                    Vertex second = vertices[xx, yy, zz];
                    float flowFirstToSecond = ComputeFlowBetween(first, second);
                    float flowSecondToFirst = flowFirstToSecond;
                    if( flowFirstToSecond >= deductOneTwo ) {
                        flowFirstToSecond -= deductOneTwo;
                    } else {
                        flowFirstToSecond = 0;
                    }
                    if( flowSecondToFirst >= deductTwoOne ) {
                        flowSecondToFirst -= deductTwoOne;
                    } else {
                        flowSecondToFirst = 0;
                    }
                    for ( int i = 0; i < first.neighbors.Length; i++ ) {
                        if( first.neighbors[i] == null ) {
                            first.neighbors[i] = second;
                            first.flows[i] = flowFirstToSecond;
                            break;
                        }
                    }
                    for (int i = 0; i < second.neighbors.Length; i++) {
                        if (second.neighbors[i] == null) {
                            second.neighbors[i] = first;
                            second.flows[i] = flowSecondToFirst;
                            break;
                        }
                    }
                    zz++;
                }
                yy++;
            }
            xx++;
        }
    }

    public void ReattachVertexes(Vertex replace, Vertex attach, Vertex[,,] with , int x1, int x2, int y1, int y2, int z1, int z2) {

        //int numVerts = with.GetLength(0) * with.GetLength(1); // number of vertices that the single vertex attach is going to be attaching to
        int numVerts = (x2 - x1) * (y2 - y1) * (z2 - z1);

        int newNumNeighbors = attach.neighbors.Length - 1 + numVerts; // the number of niegbors attach will have after detaching replace and attaching all of the replacements

        Vertex[] newNeighbors = new Vertex[newNumNeighbors];
        float[] newFlows = new float[newNumNeighbors];

        //float oldFlowOut = 0; // OldFlowOut is the current flow from attach to replace.
        //float oldFlowIn = 0; // oldFlowIn is the current flow from replace to attach.
        int counter = 0; // This for loop adds all of the neighbors that will stay with attach to the new neighbor array
        for ( int i = 0; i < attach.neighbors.Length; i++ ) {
            if (attach.neighbors[i] != replace ) {
                newNeighbors[counter] = attach.neighbors[i];
                newFlows[counter] = attach.flows[i];
                counter++;
            } else {
                //oldFlowOut = attach.flows[i];
            }
        }
        // This for loop looks through replace's neighbors to find the flow from replace to attach.
        for (int i2 = 0; i2 < replace.neighbors.Length; i2++) { 
            if (replace.neighbors[i2] == attach) {
                //oldFlowIn = replace.flows[i2];
            }
        }
        //float oldFlowTotalOut = ComputeFlowBetween(replace, attach);
        //float oldFlowTotalIn = oldFlowTotalOut;
        //float eachDeduct = (oldFlowTotalOut - oldFlowOut) / numVerts;
        // This for loop appends the new neighbors that are replacing replace to attach's new neighbor array
        for( int x = x1; x < x2; x++ ) {
            for( int y = y1; y < y2; y++ ) {
                for( int z = z1; z < z2; z++ ) {
                    newNeighbors[counter] = with[x, y, z];
                    newFlows[counter] = ComputeFlowBetween(attach, with[x, y, z]);
                    //Debug.Log("attach = " + attach.value + ", with[" + x + ", " + y + ", " + z + "] = " + with[x, y, z].value);
                    //Debug.Log("Set flow from attach to other " + x + "," + y + "," + z + " to " + newFlows[counter]);
                    //if (newFlows[counter] >= eachDeduct) { // reduce the flows to the new vertexes by the amount computed in the previous max flow calculation
                    //    newFlows[counter] -= eachDeduct;
                    //    oldFlowTotalOut -= eachDeduct;
                    //}
                    //else {
                    //    oldFlowTotalOut -= newFlows[counter];
                    //    newFlows[counter] = 0;
                    //}
                    counter++;

                    // This part of the for loop appends attach to the new neighbors neighbor arrays
                    for (int k = 0; k < with[x, y, z].neighbors.Length; k++) {
                        if (with[x, y, z].neighbors[k] == null) {
                            with[x, y, z].neighbors[k] = attach;
                            with[x, y, z].flows[k] = ComputeFlowBetween(with[x, y, z], attach);
                            //Debug.Log("Set flow from other " + x + "," + y + "," + z + " to attach to " + with[x, y, z].flows[k]);
                            //if (with[x, y, z].flows[k] >= eachDeduct) {
                            //    with[x, y, z].flows[k] -= eachDeduct;
                            //    oldFlowTotalIn -= eachDeduct;
                            //}
                            //else {
                            //    oldFlowTotalIn -= with[x, y, z].flows[k];
                            //    with[x, y, z].flows[k] = 0;
                            //}
                            break;
                        }
                    }
                }
            }
        }
        attach.neighbors = newNeighbors;
        attach.flows = newFlows;

    }

    public Vertex[,,] ResetGraph( Vertex[,,] verticesBefore, Vertex[,,] verticesAfter , int scanWidth, int scanHeight, int numScans, Vertex source, Vertex sink, float sourceSinkFlow) {
        bool[,,] edgeVertexes = new bool[scanWidth, scanHeight, numScans];
        Vertex[,,] fullRezVertices = new Vertex[data.getWidth(), data.getHeight(), data.getNumLayers()];
        for (int x = 0; x < scanWidth; x++) {
            for (int y = 0; y < scanHeight; y++) {
                for (int z = 0; z < numScans; z++) {
                    if (verticesBefore[x, y, z].visited) { // Any visited vertex adjacent to an unvisited vertex is part of the edge that needs to be magnified
                        if (x != 0) {
                            if( !verticesBefore[x-1, y, z].visited ) {
                                edgeVertexes[x, y, z] = true;
                            }
                        }
                        if (x != scanWidth - 1) {
                            if (!verticesBefore[x + 1, y, z].visited) {
                                edgeVertexes[x, y, z] = true;
                            }
                        }
                        if (y != 0) {
                            if (!verticesBefore[x, y - 1, z].visited) {
                                edgeVertexes[x, y, z] = true;
                            }
                        }
                        if (y != scanHeight - 1) {
                            if (!verticesBefore[x, y + 1, z].visited) {
                                edgeVertexes[x, y, z] = true;
                            }
                        }
                        if (m_3DFlow) {
                            if (z != 0) {
                                if (!verticesBefore[x, y, z - 1].visited) {
                                    edgeVertexes[x, y, z] = true;
                                }
                            }
                            if (z != numScans - 1) {
                                if (!verticesBefore[x, y, z + 1].visited) {
                                    edgeVertexes[x, y, z] = true;
                                }
                            }
                        }
                    }
                    //edgeVertexes[x, y, z] = false;
                    //for (int i = 0; i < vertices[x, y, z].flows.Length; i++) {
                    //    if (vertices[x, y, z].flows[i] == 0) {
                    //        edgeVertexes[x, y, z] = true;
                    //        break;
                    //    }
                    //}
                }
            }
        }
        for (int x = 0; x < scanWidth; x++) {
            for (int y = 0; y < scanHeight; y++) {
                if (edgeVertexes[x, y, 0]) {
                    file.Write("#");
                }
                else {
                    file.Write(" ");
                }
            }
            file.WriteLine();
        }
        int numNeighbors;
        int finalX, finalY, finalZ;
        for (int x = 0; x < scanWidth; x++) {
            for (int y = 0; y < scanHeight; y++) {
                for (int z = 0; z < numScans; z++) {
                    if (edgeVertexes[x, y, z]) {
                        for (int xOffset = 0; xOffset < m_xfreq; xOffset++) {
                            for (int yOffset = 0; yOffset < m_yfreq; yOffset++) {
                                for (int zOffset = 0; zOffset < m_zfreq; zOffset++) {
                                    finalX = x * m_xfreq + xOffset;
                                    finalY = y * m_yfreq + yOffset;
                                    finalZ = z * m_zfreq + zOffset;
                                    if (m_3DFlow) {
                                        numNeighbors = 6;
                                    } else {
                                        numNeighbors = 4;
                                    }
                                    if (finalX == 0 || finalX == data.getWidth() - 1) {
                                        numNeighbors--;
                                    }
                                    if (finalY == 0 || finalY == data.getHeight() - 1) {
                                        numNeighbors--;
                                    }
                                    if (m_3DFlow && (finalZ == 0 || finalZ == data.getNumLayers() - 1)) {
                                        numNeighbors--;
                                    }
                                    fullRezVertices[finalX, finalY, finalZ] = new Vertex(numNeighbors, data.getOriginalPixelFloat(finalX, finalY, finalZ));
                                    if (xOffset > 0) {
                                        float flowBetween = ComputeFlowBetween(fullRezVertices[finalX - 1, finalY, finalZ], fullRezVertices[finalX, finalY, finalZ]);
                                        for ( int i = 0; i < fullRezVertices[finalX - 1, finalY, finalZ].neighbors.Length; i++ ) {
                                            if(fullRezVertices[finalX - 1, finalY, finalZ].neighbors[i] == null ) {
                                                fullRezVertices[finalX - 1, finalY, finalZ].neighbors[i] = fullRezVertices[finalX, finalY, finalZ];
                                                fullRezVertices[finalX - 1, finalY, finalZ].flows[i] = flowBetween;
                                                break;
                                            }
                                        }
                                        for( int i = 0; i < fullRezVertices[finalX, finalY, finalZ].neighbors.Length; i ++ ) {
                                            if(fullRezVertices[finalX, finalY, finalZ].neighbors[i] == null ) {
                                                fullRezVertices[finalX, finalY, finalZ].neighbors[i] = fullRezVertices[finalX - 1, finalY, finalZ];
                                                fullRezVertices[finalX, finalY, finalZ].flows[i] = flowBetween;
                                                break;
                                            }
                                        }
                                    }
                                    if (yOffset > 0) {
                                        float flowBetween = ComputeFlowBetween(fullRezVertices[finalX, finalY - 1, finalZ], fullRezVertices[finalX, finalY, finalZ]);
                                        for (int i = 0; i < fullRezVertices[finalX, finalY - 1, finalZ].neighbors.Length; i++) {
                                            if (fullRezVertices[finalX, finalY - 1, finalZ].neighbors[i] == null) {
                                                fullRezVertices[finalX, finalY - 1, finalZ].neighbors[i] = fullRezVertices[finalX, finalY, finalZ];
                                                fullRezVertices[finalX, finalY - 1, finalZ].flows[i] = flowBetween;
                                                break;
                                            }
                                        }
                                        for (int i = 0; i < fullRezVertices[finalX, finalY, finalZ].neighbors.Length; i++) {
                                            if (fullRezVertices[finalX, finalY, finalZ].neighbors[i] == null) {
                                                fullRezVertices[finalX, finalY, finalZ].neighbors[i] = fullRezVertices[finalX, finalY - 1, finalZ];
                                                fullRezVertices[finalX, finalY, finalZ].flows[i] = flowBetween;
                                                break;
                                            }
                                        }
                                    }
                                    if (zOffset > 0) {
                                        float flowBetween = ComputeFlowBetween(fullRezVertices[finalX, finalY, finalZ - 1], fullRezVertices[finalX, finalY, finalZ]);
                                        for (int i = 0; i < fullRezVertices[finalX, finalY, finalZ - 1].neighbors.Length; i++) {
                                            if (fullRezVertices[finalX, finalY, finalZ - 1].neighbors[i] == null) {
                                                fullRezVertices[finalX, finalY, finalZ - 1].neighbors[i] = fullRezVertices[finalX, finalY, finalZ];
                                                fullRezVertices[finalX, finalY, finalZ - 1].flows[i] = flowBetween;
                                                break;
                                            }
                                        }
                                        for (int i = 0; i < fullRezVertices[finalX, finalY, finalZ].neighbors.Length; i++) {
                                            if (fullRezVertices[finalX, finalY, finalZ].neighbors[i] == null) {
                                                fullRezVertices[finalX, finalY, finalZ].neighbors[i] = fullRezVertices[finalX, finalY, finalZ - 1];
                                                fullRezVertices[finalX, finalY, finalZ].flows[i] = flowBetween;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        //for (int xOffset = 0; xOffset < m_xfreq; xOffset++) {
                        //    for (int yOffset = 0; yOffset < m_yfreq; yOffset++) {
                        //        for (int zOffset = 0; zOffset < m_zfreq; zOffset++) {
                        //            finalX = x * m_xfreq + xOffset;
                        //            finalY = y * m_yfreq + yOffset;
                        //            finalZ = z * m_zfreq + zOffset;

                        //        }
                        //    }
                        //}

                        if ( x != 0 ) {
                            // connect to left
                            if (edgeVertexes[x - 1, y, z]) {
                                // if to the left is also along the edge
                                Vertex left = verticesAfter[x - 1, y, z];
                                Vertex current = verticesAfter[x, y, z];
                                ReattachVertexes(current, left, fullRezVertices, x * m_xfreq, x * m_xfreq + 1, y * m_yfreq, (y + 1) * m_yfreq, z * m_zfreq, (z + 1) * m_zfreq, x * m_xfreq - 1, x * m_xfreq, y * m_yfreq, (y + 1) * m_yfreq, z * m_zfreq, (z + 1) * m_zfreq);
                            }
                            else {
                                Vertex left = verticesAfter[x - 1, y, z];
                                Vertex current = verticesAfter[x, y, z];
                                ReattachVertexes(current, left, fullRezVertices, x * m_xfreq, x * m_xfreq + 1, y * m_yfreq, y * m_yfreq + m_yfreq, z * m_zfreq, z * m_zfreq + m_zfreq);
                            }
                        }
                        if ( y != 0 ) {
                            // connect to up
                            if (edgeVertexes[x, y - 1, z]) {
                                // if up is also along the edge
                                Vertex up = verticesAfter[x, y - 1, z];
                                Vertex current = verticesAfter[x, y, z];
                                ReattachVertexes(current, up, fullRezVertices, x * m_xfreq, x * m_xfreq + m_xfreq, y * m_yfreq, y * m_yfreq + 1, z * m_zfreq, z * m_zfreq + m_zfreq, x * m_xfreq, x * m_xfreq + m_xfreq, y * m_yfreq - 1, y * m_yfreq, z * m_zfreq, z * m_zfreq + m_zfreq);
                            }
                            else {
                                Vertex up = verticesAfter[x, y - 1, z];
                                Vertex current = verticesAfter[x, y, z];
                                ReattachVertexes(current, up, fullRezVertices, x * m_xfreq, x * m_xfreq + m_xfreq, y * m_yfreq, y * m_yfreq + 1, z * m_zfreq, z * m_zfreq + m_zfreq);
                            }
                        }
                        if ( z != 0 ) {
                            if (edgeVertexes[x, y, z - 1]) {
                                Vertex up = verticesAfter[x, y, z - 1];
                                Vertex current = verticesAfter[x, y, z];
                                ReattachVertexes(current, up, fullRezVertices, x * m_xfreq, x * m_xfreq + m_xfreq, y * m_yfreq, y * m_yfreq + m_yfreq, z * m_zfreq, z * m_zfreq + 1, x * m_xfreq, x * m_xfreq + m_xfreq, y * m_yfreq, y * m_yfreq + m_yfreq, z * m_zfreq - 1, z * m_zfreq);
                            }
                            else {
                                Vertex up = verticesAfter[x, y, z - 1];
                                Vertex current = verticesAfter[x, y, z];
                                ReattachVertexes(current, up, fullRezVertices, x * m_xfreq, x * m_xfreq + m_xfreq, y * m_yfreq, y * m_yfreq + m_yfreq, z * m_zfreq, z * m_zfreq + 1);
                            }
                        }
                    }
                }
            }
        }
        for (int x = 0; x < scanWidth; x++) {
            for (int y = 0; y < scanHeight; y++) {
                for (int z = 0; z < numScans; z++) {
                    if (edgeVertexes[x, y, z]) {
                        if (x < scanWidth - 1) {
                            // connect to right
                            if (edgeVertexes[x + 1, y, z]) {
                                // if to the right is also along the edge
                                Vertex right = verticesAfter[x + 1, y, z];
                                Vertex current = verticesAfter[x, y, z];
                                ReattachVertexes(current, right, fullRezVertices, x * m_xfreq + m_xfreq - 1, x * m_xfreq + m_xfreq, y * m_yfreq, y * m_yfreq + m_yfreq, z * m_zfreq, z * m_zfreq + m_zfreq, x * m_xfreq + m_xfreq, x * m_xfreq + m_xfreq + 1, y * m_yfreq, y * m_yfreq + m_yfreq, z * m_zfreq, z * m_zfreq + m_zfreq);
                            }
                            else {
                                Vertex right = verticesAfter[x + 1, y, z];
                                Vertex current = verticesAfter[x, y, z];
                                ReattachVertexes(current, right, fullRezVertices, x * m_xfreq + m_xfreq - 1, x * m_xfreq + m_xfreq, y * m_yfreq, y * m_yfreq + m_yfreq, z * m_zfreq, z * m_zfreq + m_zfreq);
                            }
                        }
                        if (y < scanHeight - 1) {
                            // connect to down
                            if (edgeVertexes[x, y + 1, z]) {
                                // if down is also along the edge
                                Vertex down = verticesAfter[x, y + 1, z];
                                Vertex current = verticesAfter[x, y, z];
                                ReattachVertexes(current, down, fullRezVertices, x * m_xfreq, x * m_xfreq + m_xfreq, y * m_yfreq + m_yfreq - 1, y * m_yfreq + m_yfreq, z * m_zfreq, z * m_zfreq + m_zfreq, x * m_xfreq, x * m_xfreq + m_xfreq, y * m_yfreq + m_yfreq, y * m_yfreq + m_yfreq + 1, z * m_zfreq, z * m_zfreq + m_zfreq);
                            }
                            else {
                                Vertex down = verticesAfter[x, y + 1, z];
                                Vertex current = verticesAfter[x, y, z];
                                ReattachVertexes(current, down, fullRezVertices, x * m_xfreq, x * m_xfreq + m_xfreq, y * m_yfreq + m_yfreq - 1, y * m_yfreq + m_yfreq, z * m_zfreq, z * m_zfreq + m_zfreq);
                            }
                        }
                        if (m_3DFlow)
                        {
                            if (z < numScans - 1)
                            {
                                if (edgeVertexes[x, y, z + 1])
                                {
                                    Vertex up = verticesAfter[x, y, z + 1];
                                    Vertex current = verticesAfter[x, y, z];
                                    ReattachVertexes(current, up, fullRezVertices, x * m_xfreq, x * m_xfreq + m_xfreq, y * m_yfreq, y * m_yfreq + m_yfreq, z * m_zfreq + m_zfreq - 1, z * m_zfreq + m_zfreq, x * m_xfreq, x * m_xfreq + m_xfreq, y * m_yfreq, y * m_yfreq + m_yfreq, z * m_zfreq + m_zfreq, z * m_zfreq + m_zfreq + 1);
                                }
                                else
                                {
                                    Vertex up = verticesAfter[x, y, z + 1];
                                    Vertex current = verticesAfter[x, y, z];
                                    ReattachVertexes(current, up, fullRezVertices, x * m_xfreq, x * m_xfreq + m_xfreq, y * m_yfreq, y * m_yfreq + m_yfreq, z * m_zfreq + m_zfreq - 1, z * m_zfreq + m_zfreq);
                                }
                            }
                        }
                    }
                }
            }
        }
        int numNull = 0;
        int numTotal = 0;
        for( int x = 0; x < data.getWidth(); x++ ) {
            for( int y = 0; y < data.getHeight(); y++ ) {
                for( int z = 0; z < data.getNumLayers(); z++ ) {
                    if( fullRezVertices[x, y, z] != null ) {
                        for( int i = 0; i < fullRezVertices[x, y, z].neighbors.Length; i++ ) {
                            if(fullRezVertices[x, y, z].neighbors[i] == null ) {
                                numNull++;
                            }
                            numTotal++;
                        }
                    }
                }
            }
        }
        Debug.LogError("Number of nulls after graph set up before source and sink set up = " + numNull);
        Debug.LogError("Number of total new edges = " + numTotal);
        foreach ( DataContainer.Point obj in data.GetObjectSeeds()) {
            if( edgeVertexes[obj.x / m_xfreq, obj.y / m_yfreq, obj.z / m_zfreq] ) {
                Debug.LogError("Encountered edge object");
                Vertex oldVertex = verticesAfter[obj.x / m_xfreq, obj.y / m_yfreq, obj.z / m_zfreq];
                Vertex newVertex = fullRezVertices[obj.x, obj.y, obj.z];
                for( int i = 0; i < source.neighbors.Length; i++ ) {
                    if( source.neighbors[i] == oldVertex ) {
                        source.neighbors[i] = newVertex;
                        break;
                    }
                }
            }
            else {

            }
        }
        foreach ( DataContainer.Point obj in data.GetBackgroundSeeds()) {
            if (edgeVertexes[obj.x / m_xfreq, obj.y / m_yfreq, obj.z / m_zfreq]) {
                Debug.LogError("Encountered edge background");
                Vertex v = fullRezVertices[obj.x, obj.y, obj.z];
                Vertex[] newNeighbors = new Vertex[v.neighbors.Length + 1];
                float[] newFlows = new float[v.flows.Length + 1];
                for (int i = 0; i < v.neighbors.Length; i++) {
                    newNeighbors[i] = v.neighbors[i];
                    newFlows[i] = v.flows[i];
                }
                newNeighbors[newNeighbors.Length - 1] = sink;
                newFlows[newNeighbors.Length - 1] = sourceSinkFlow;
                v.neighbors = newNeighbors;
                v.flows = newFlows;
            }
            else {

            }
        }
        numNull = 0;
        numTotal = 0;
        for (int x = 0; x < data.getWidth(); x++) {
            for (int y = 0; y < data.getHeight(); y++) {
                for (int z = 0; z < data.getNumLayers(); z++) {
                    if (fullRezVertices[x, y, z] != null) {
                        for (int i = 0; i < fullRezVertices[x, y, z].neighbors.Length; i++) {
                            if (fullRezVertices[x, y, z].neighbors[i] == null) {
                                numNull++;
                            }
                            numTotal++;
                        }
                    }
                }
            }
        }
        Debug.LogError("Number of nulls after graph set up after source and sink set up = " + numNull);
        Debug.LogError("Number of total new edges = " + numTotal);
        return fullRezVertices;
    }

    public int VertexCompareLamda(Vertex one, Vertex two )
    {
        if( one.value < two.value )
        {
            return -1;
        }
        else
        {
            return 1;
        }
    }

    public Vertex[,,] ResetGraphHistogramWay(Vertex[,,] verticesBefore, Vertex[,,] verticesAfter, int scanWidth, int scanHeight, int numScans, Vertex source, Vertex sink, float sourceSinkFlow)
    {
        bool[,,] edgeVertexes = new bool[scanWidth, scanHeight, numScans];
        Vertex[,,] fullRezVertices = new Vertex[data.getWidth(), data.getHeight(), data.getNumLayers()];
        for (int x = 0; x < scanWidth; x++)
        {
            for (int y = 0; y < scanHeight; y++)
            {
                for (int z = 0; z < numScans; z++)
                {
                    if (verticesBefore[x, y, z].visited)
                    { // Any visited vertex adjacent to an unvisited vertex is part of the edge that needs to be magnified
                        if (x != 0)
                        {
                            if (!verticesBefore[x - 1, y, z].visited)
                            {
                                edgeVertexes[x, y, z] = true;
                            }
                        }
                        if (x != scanWidth - 1)
                        {
                            if (!verticesBefore[x + 1, y, z].visited)
                            {
                                edgeVertexes[x, y, z] = true;
                            }
                        }
                        if (y != 0)
                        {
                            if (!verticesBefore[x, y - 1, z].visited)
                            {
                                edgeVertexes[x, y, z] = true;
                            }
                        }
                        if (y != scanHeight - 1)
                        {
                            if (!verticesBefore[x, y + 1, z].visited)
                            {
                                edgeVertexes[x, y, z] = true;
                            }
                        }
                        if (m_3DFlow)
                        {
                            if (z != 0)
                            {
                                if (!verticesBefore[x, y, z - 1].visited)
                                {
                                    edgeVertexes[x, y, z] = true;
                                }
                            }
                            if (z != numScans - 1)
                            {
                                if (!verticesBefore[x, y, z + 1].visited)
                                {
                                    edgeVertexes[x, y, z] = true;
                                }
                            }
                        }
                    }
                    //edgeVertexes[x, y, z] = false;
                    //for (int i = 0; i < vertices[x, y, z].flows.Length; i++) {
                    //    if (vertices[x, y, z].flows[i] == 0) {
                    //        edgeVertexes[x, y, z] = true;
                    //        break;
                    //    }
                    //}
                }
            }
        }
        for (int x = 0; x < scanWidth; x++)
        {
            for (int y = 0; y < scanHeight; y++)
            {
                if (edgeVertexes[x, y, 0])
                {
                    file.Write("#");
                }
                else
                {
                    file.Write(" ");
                }
            }
            file.WriteLine();
        }
        int numNeighbors;
        int finalX, finalY, finalZ;
        for (int x = 0; x < scanWidth; x++)
        {
            for (int y = 0; y < scanHeight; y++)
            {
                for (int z = 0; z < numScans; z++)
                {
                    if (edgeVertexes[x, y, z])
                    {
                        List<Vertex> newvertices = new List<Vertex>();
                        for (int xOffset = 0; xOffset < m_xfreq; xOffset++)
                        {
                            for (int yOffset = 0; yOffset < m_yfreq; yOffset++)
                            {
                                for (int zOffset = 0; zOffset < m_zfreq; zOffset++)
                                {
                                    finalX = x * m_xfreq + xOffset;
                                    finalY = y * m_yfreq + yOffset;
                                    finalZ = z * m_zfreq + zOffset;
                                    if (m_3DFlow)
                                    {
                                        numNeighbors = 6;
                                    }
                                    else
                                    {
                                        numNeighbors = 4;
                                    }
                                    if (finalX == 0 || finalX == data.getWidth() - 1)
                                    {
                                        numNeighbors--;
                                    }
                                    if (finalY == 0 || finalY == data.getHeight() - 1)
                                    {
                                        numNeighbors--;
                                    }
                                    if (m_3DFlow && (finalZ == 0 || finalZ == data.getNumLayers() - 1))
                                    {
                                        numNeighbors--;
                                    }
                                    fullRezVertices[finalX, finalY, finalZ] = new Vertex(numNeighbors, data.getOriginalPixelFloat(finalX, finalY, finalZ));
                                    newvertices.Add(fullRezVertices[finalX, finalY, finalZ]);
                                }

                            }
                        }

                        newvertices.Sort((i, j) => VertexCompareLamda(i, j));
                        file.Write("Sorted Values: ");
                        float maxDelta = -1;
                        float delta = 0;
                        int indexStop = -1;
                        for (int i = 1; i < newvertices.Count; i++ ) {
                            //Debug.LogError("vertex " + i + " value: " + newvertices[i].value);
                            delta = newvertices[i].value - newvertices[i - 1].value;
                            if( delta > maxDelta ) {
                                maxDelta = delta;
                                indexStop = i;
                            }
                            file.Write("(" + newvertices[i].value + "," + delta + ")");
                        }
                        file.WriteLine(" Cut off at index " + indexStop);
                        for (int i = indexStop; i < newvertices.Count; i++ ) {
                            newvertices[i].visited = true;
                        }

                    }
                }
            }
        }
        return fullRezVertices;
        //foreach (Point obj in partOfObject)
        //{
        //    if (edgeVertexes[obj.x / m_xfreq, obj.y / m_yfreq, obj.z / m_zfreq])
        //    {
        //        Debug.LogError("Encountered edge object");
        //        Vertex oldVertex = verticesAfter[obj.x / m_xfreq, obj.y / m_yfreq, obj.z / m_zfreq];
        //        Vertex newVertex = fullRezVertices[obj.x, obj.y, obj.z];
        //        for (int i = 0; i < source.neighbors.Length; i++)
        //        {
        //            if (source.neighbors[i] == oldVertex)
        //            {
        //                source.neighbors[i] = newVertex;
        //                break;
        //            }
        //        }
        //    }
        //    else
        //    {

        //    }
        //}
        //foreach (Point obj in partOfBackground)
        //{
        //    if (edgeVertexes[obj.x / m_xfreq, obj.y / m_yfreq, obj.z / m_zfreq])
        //    {
        //        Debug.LogError("Encountered edge background");
        //        Vertex v = fullRezVertices[obj.x, obj.y, obj.z];
        //        Vertex[] newNeighbors = new Vertex[v.neighbors.Length + 1];
        //        float[] newFlows = new float[v.flows.Length + 1];
        //        for (int i = 0; i < v.neighbors.Length; i++)
        //        {
        //            newNeighbors[i] = v.neighbors[i];
        //            newFlows[i] = v.flows[i];
        //        }
        //        newNeighbors[newNeighbors.Length - 1] = sink;
        //        newFlows[newNeighbors.Length - 1] = sourceSinkFlow;
        //        v.neighbors = newNeighbors;
        //        v.flows = newFlows;
        //    }
        //    else
        //    {

        //    }
        //}
        //return fullRezVertices;
    }

    public void RunMaxFlowSegmentationTimed() {

        long numEdges = 0;
        long time1 = DateTime.Now.Ticks;

        bool[,,] visited = new bool[data.getWidth(), data.getHeight(), data.getNumLayers()];
        // create array of 512 by 512 vertices, one for each pixel on the image
        Vertex[,,] vertices = new Vertex[data.getWidth(), data.getHeight(), data.getNumLayers()];
        Vertex sink = new Vertex(0, 0);
        //Vertex source = new Vertex(data.getWidth() * data.getHeight() * data.getNumLayers());

        Vertex source = new Vertex(data.GetObjectSeeds().Count, 1);

        //numEdges += data.getWidth() * data.getHeight() * data.getNumLayers();
        numEdges += data.GetObjectSeeds().Count;

        edgesCounter = 0;
        
        int scanWidth = data.getWidth() / m_xfreq;
        int scanHeight = data.getHeight() / m_yfreq;
        int numScans = data.getNumLayers() / m_zfreq;

        vertices = MaxFlowSetupBetterSampling(sink, source, m_xfreq, m_yfreq, m_zfreq);

        float initialFlow = 0.0f;
        for (int i = 0; i < source.flows.Length; i++) {
            initialFlow += source.flows[i];
        }
        numEdges += edgesCounter;

        long time2 = DateTime.Now.Ticks;
        //float endTime = Time.time;
        //file.WriteLine("Initalization took {0} ms", (endTime - startTime));
        //file.Flush();

        // now that total flows have been set up, run breadth first search and each time reach sink, update flows and run again.
        // Upon parsing every accesible vertex and not having visited sink yet, that is the end and the max flow has been found.

        // For BFS use a queue
        Queue<Vertex> searchArea = new Queue<Vertex>();

        int flowIterations = 0;
        int bfsIterations = 0;
        long maximumQueue = 0;
        long resetTime = 0;
        long bfsTime = 0;
        long flowTime = 0;
        long time3;
        long time4;
        long time5;
        long time6;
        while (true) // this is the loop that adds augmenting paths to the flow.
        {
            time3 = DateTime.Now.Ticks;
            searchArea.Clear(); // reset the queue
            sink.visited = false; // reset all of the visited values
            for (int x = 0; x < scanWidth; x++) {
                for (int y = 0; y < scanHeight; y++) {
                    for (int z = 0; z < numScans; z++) {
                        vertices[x, y, z].visited = false;
                    }
                }
            }

            // First enqueue the source node
            searchArea.Enqueue(source);
            source.visited = true;

            Vertex v = null;
            Vertex n = null;
            time4 = DateTime.Now.Ticks;
            resetTime += time4 - time3;
            bfsIterations++;
            Debug.Log("Incrementing bfs Iterations");
            while (searchArea.Count > 0) // visit all possible from source 
            {                            // This loop is Breadth first search until finding the sink, or visiting every possible node
                v = searchArea.Dequeue();
                if (v == sink) {
                    break;
                }
                for (int i = 0; i < v.neighbors.Length; i++) {
                    n = v.neighbors[i];
                    if (!n.visited && v.flows[i] > 0) {
                        n.visited = true;
                        n.from = v;
                        searchArea.Enqueue(n);
                    }
                }
                maximumQueue = Math.Max(maximumQueue, searchArea.Count);
            }
            time5 = DateTime.Now.Ticks;
            bfsTime += time5 - time4;
            // Now if reached the sink need to update flows based on the path taken, then run again
            if (v == sink) {
                flowIterations++;
                float minFlow = 10000.0f;
                //file.Write("Found path from source to sink ");
                while (v.from != null) {
                    for (int i = 0; i < v.from.neighbors.Length; i++) {
                        if (v.from.neighbors[i] == v) {
                            minFlow = Math.Min(v.from.flows[i], minFlow);
                            //file.Write("{0} ", v.from.flows[i]);
                            break;
                        }
                    }
                    v = v.from;
                }
                v = sink;
                //file.WriteLine();
                //file.Write("Update flows: ");
                while (v.from != null) {
                    for (int i = 0; i < v.from.neighbors.Length; i++) {
                        if (v.from.neighbors[i] == v) {
                            v.from.flows[i] -= minFlow;
                            //file.Write("{0} ", v.from.flows[i]);
                            break;
                        }
                    }
                    v = v.from;
                }
                //file.WriteLine();
                //file.Flush();
            }
            else // otherwise if didnt reach sink the max-flow has been found and move on to next step
            {
                break;
            }
            time6 = DateTime.Now.Ticks;
            flowTime += time6 - time5;
        }

        float leftoverFlow = 0.0f;
        for (int i = 0; i < source.flows.Length; i++) {
            leftoverFlow += source.flows[i];
        }
        long setupTime = time2 - time1;
        int numVertexes = numScans * scanHeight * scanWidth + 2;
        file.WriteLine("Running Timed Max-flow Segmentation");
        file.WriteLine("numScans = {0}, scanWidth = {1}, scanHeight = {2}", numScans, data.getWidth(), data.getHeight());
        file.WriteLine("xfreq, yfreq, zfreq = {0}, {1}, {2}", m_xfreq, m_yfreq, m_zfreq);
        file.WriteLine("Setup Time            = {0}", setupTime / TimeSpan.TicksPerMillisecond);
        file.WriteLine("Reset Time            = {0}", resetTime / TimeSpan.TicksPerMillisecond);
        file.WriteLine("BFS Time              = {0}", bfsTime / TimeSpan.TicksPerMillisecond);
        file.WriteLine("Computing flow Time   = {0}", flowTime / TimeSpan.TicksPerMillisecond);
        file.WriteLine("Total number of vertexes in graph = {0}", numVertexes);
        file.WriteLine("Total number of edges in graph    = {0}", numEdges);
        file.WriteLine("Maximum size of queue             = {0}", maximumQueue);
        file.WriteLine("Iterations of bfs            = {0}", bfsIterations);
        file.WriteLine("Iterations of flow reduction = {0}", flowIterations);
        file.WriteLine("Leftover flow from source = {0}", leftoverFlow);
        file.WriteLine("Initial flow from source = {0}", initialFlow);
        file.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}", data.getWidth(), data.getNumLayers(), setupTime / TimeSpan.TicksPerMillisecond, resetTime / TimeSpan.TicksPerMillisecond,
            bfsTime / TimeSpan.TicksPerMillisecond, flowTime / TimeSpan.TicksPerMillisecond, numVertexes, numEdges, maximumQueue, bfsIterations, flowIterations);

        vertices[0, 0, 0].visited = true;
        //file.WriteLine("Final results:");
        for (int x = 0; x < data.getWidth(); x++) {
            for (int y = 0; y < data.getHeight(); y++) {
                for (int z = 0; z < data.getNumLayers(); z++) {
                    if (vertices[x/ m_xfreq, y/ m_yfreq, z/ m_zfreq].visited) {
                        //file.Write("#");
                        visited[x, y, z] = true;
                    }
                    else {
                        //file.Write("-");
                    }
                }
            }
            //file.WriteLine("");
        }
        //saveSegmentToFile(visited, originalScans, "Resources/segment/segment");
        //return;

        Vertex[,,] verticesAfter = MaxFlowSetupBetterSamplingRound2(sink, source, m_xfreq, m_yfreq, m_zfreq);
        Vertex[,,] vertices2 = ResetGraphHistogramWay(vertices, verticesAfter, scanWidth, scanHeight, numScans, source, sink, SOURCESINKFLOW);

        flowIterations = 0;
        bfsIterations = 0;
        maximumQueue = 0;
        resetTime = 0;
        bfsTime = 0;
        flowTime = 0;
        int limit = 100;
        while (false) // this is the loop that adds augmenting paths to the flow.
        {
            if (limit-- < 0) {
                //Debug.LogError("Reached limit of " + 100 + " iterations");
                //break;
            }
            time3 = DateTime.Now.Ticks;
            searchArea.Clear(); // reset the queue
            sink.visited = false; // reset all of the visited values
            for (int x = 0; x < scanWidth; x++) {
                for (int y = 0; y < scanHeight; y++) {
                    for (int z = 0; z < numScans; z++) {
                        verticesAfter[x, y, z].visited = false;
                    }
                }
            }
            for (int x = 0; x < data.getWidth(); x++) {
                for (int y = 0; y < data.getHeight(); y++) {
                    for (int z = 0; z < data.getNumLayers(); z++) {
                        if (vertices2[x, y, z] != null) {
                            //if(vertices2[x, y, z].visited ) {
                            //    Debug.LogError("OMG something in vertices2 was visited");
                            //}
                            vertices2[x, y, z].visited = false;
                        }
                    }
                }
            }
            // First enqueue the source node
            searchArea.Enqueue(source);
            source.visited = true;

            Vertex v = null;
            Vertex n = null;
            time4 = DateTime.Now.Ticks;
            resetTime += time4 - time3;
            bfsIterations++;
            Debug.Log("Incrementing bfs Iterations");
            while (searchArea.Count > 0) // visit all possible from source 
            {                            // This loop is Breadth first search until finding the sink, or visiting every possible node
                v = searchArea.Dequeue();
                if (v == sink) {
                    break;
                }
                for (int i = 0; i < v.neighbors.Length; i++) {
                    n = v.neighbors[i];
                    if (!n.visited && v.flows[i] > 0) {
                        n.visited = true;
                        n.from = v;
                        searchArea.Enqueue(n);
                    }
                }
                maximumQueue = Math.Max(maximumQueue, searchArea.Count);
            }
            time5 = DateTime.Now.Ticks;
            bfsTime += time5 - time4;
            // Now if reached the sink need to update flows based on the path taken, then run again
            if (v == sink) {
                flowIterations++;
                float minFlow = 10000.0f;
                //file.Write("Found path from source to sink ");
                while (v.from != null) {
                    for (int i = 0; i < v.from.neighbors.Length; i++) {
                        if (v.from.neighbors[i] == v) {
                            minFlow = Math.Min(v.from.flows[i], minFlow);
                            //file.Write("{0} ", v.from.flows[i]);
                            break;
                        }
                    }
                    v = v.from;
                }
                v = sink;
                //file.WriteLine();
                //file.Write("Update flows: ");
                while (v.from != null) {
                    for (int i = 0; i < v.from.neighbors.Length; i++) {
                        if (v.from.neighbors[i] == v) {
                            v.from.flows[i] -= minFlow;
                            //file.Write("{0} ", v.from.flows[i]);
                            break;
                        }
                    }
                    v = v.from;
                }
                //file.WriteLine();
                //file.Flush();
            }
            else // otherwise if didnt reach sink the max-flow has been found and move on to next step
            {
                break;
            }
            time6 = DateTime.Now.Ticks;
            flowTime += time6 - time5;
        }
        file.WriteLine("Running Second Round of Max-flow Segmentation");
        file.WriteLine("numScans = {0}, scanWidth = {1}, scanHeight = {2}", numScans, data.getWidth(), data.getHeight());
        file.WriteLine("xfreq, yfreq, zfreq = {0}, {1}, {2}", m_xfreq, m_yfreq, m_zfreq);
        file.WriteLine("Setup Time            = {0}", setupTime / TimeSpan.TicksPerMillisecond);
        file.WriteLine("Reset Time            = {0}", resetTime / TimeSpan.TicksPerMillisecond);
        file.WriteLine("BFS Time              = {0}", bfsTime / TimeSpan.TicksPerMillisecond);
        file.WriteLine("Computing flow Time   = {0}", flowTime / TimeSpan.TicksPerMillisecond);
        file.WriteLine("Total number of vertexes in graph = {0}", numVertexes);
        file.WriteLine("Total number of edges in graph    = {0}", numEdges);
        file.WriteLine("Maximum size of queue             = {0}", maximumQueue);
        file.WriteLine("Iterations of bfs            = {0}", bfsIterations);
        file.WriteLine("Iterations of flow reduction = {0}", flowIterations);

        //saveSegmentToFile(visited, originalScans, "Resources/segment/segmentLowRez");
        data.saveSegmentToFileAsImages(visited, "Resources/segment/" + segmentName);
        //for (int x = 0; x < data.getWidth(); x++) {
        //    for (int y = 0; y < data.getHeight(); y++) {
        //        for (int z = 0; z < data.getNumLayers(); z++) {
        //            if( vertices[x / m_xfreq, y / m_yfreq, z / m_zfreq].neighbors.Length > 5 ) {
        //                Debug.LogError(" Vertex  has " + vertices[x / m_xfreq, y / m_yfreq, z / m_zfreq].neighbors.Length + " neighbors");
        //            }
        //        }
        //    }
        //}
        for (int x = 0; x < data.getWidth(); x++) {
            for (int y = 0; y < data.getHeight(); y++) {
                for (int z = 0; z < data.getNumLayers(); z++) {
                    //if (vertices2[x, y, z] != null && vertices2[x, y, z].visited ) {
                    //if (vertices[x / m_xfreq, y / m_yfreq, z / m_zfreq].visited || (vertices2[x, y, z] != null && vertices2[x, y, z].visited)) {
                    visited[x, y, z] = false;
                    if (vertices2[x, y, z] != null) {
                        if (vertices2[x, y, z].visited ) {
                            visited[x, y, z] = true;
                        }
                        //file.Write("#");
                    }
                    else {
                        //file.Write("-");
                        if(vertices[x / m_xfreq, y / m_yfreq, z / m_zfreq].visited ) {
                            visited[x, y, z] = true;
                        }
                    }
                }
            }
            //file.WriteLine("");
        }
        data.saveSegmentToFileAsImages( visited, "Resources/segment/segment" );
    }
}
