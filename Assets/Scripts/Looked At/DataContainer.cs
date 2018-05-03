using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class DataContainer : MonoBehaviour {

    // So i think the plan is to load in the textures and take the pixels 
    // and store them as 1 byte each in an array. Then get rid of the textures.
    // This will drastically reduce memory usage.

    // also for saving the resulting segments, maybe save them as a text file instead of images.
    // Then would need to modify the rendering code to be able to read in the new segments.
    // Create a SegmentHandler that handles both saving and loading segments, give it array of bools or something, and it saves to file.
    // and it can read from file and create a texture

    // the SegmentHandler will also be able to save and load different textures in different places. If they are 1 txt file each, then it'll be pretty easy to store them.


    // MRI scan dimensions (width, height, layer)
    private int m_numLayers;                                // number of MRI scan layers loaded
    private bool loadedSettings = false;                    // keep track of if the settings have been loaded
    private int m_layerWidth;                               // width of each layer
    private int m_layerHeight;                              // height of each layer

    // Data
	private Texture2D[] originalTextures;                   // keep track of original textures (used for 2D MRI data display)
    private float[,,] originalFloatData;                    // used by segmentation algorithms
    private byte[,,] originalByteData;                      // used to select black pixels.. TODO float data can be used instead
    private List<bool[,,]> segments;                        // keeps track of pixels part of a segment [x,y,layer]
    
    // seed points
	private List<Point> foregroundSeedPoints;                  // keep track of seed points that are part of the object
    private List<Point> backgroundSeedPoints;               // keep track of seed points that are part of the background

    void Start () {
        foregroundSeedPoints = new List<Point>();
        backgroundSeedPoints = new List<Point>();
        segments = new List<bool[,,]>();
        loadNumberScans();
    }

    private void loadNumberScans() {
        string path = "settings.txt";

        //Read the text from directly from the test.txt file
        StreamReader reader = new StreamReader(path);
        string text = reader.ReadToEnd();
        reader.Close();

        int value = m_numLayers;
        int.TryParse(text, out value);

        m_numLayers = value;
        Debug.Log("Loading " + m_numLayers + " layers");
    }


	// [x, y] in texture, z is the layer
    public struct Point {
        public int x;
        public int y;
        public int z;
        public Point( int p1, int p2, int p3) {
            x = p1;
            y = p2;
            z = p3;
        }
        public override string ToString() {
            return "( " + x + " , " + y + " , " + z + " )";
        }
    }


	// load textures, byte, and float data from MRI files
    public void loadMedicalData( string folderName, string filePrefix, int startLayer, int numLayers) {
        Debug.Log("Loading medical data " + numLayers + " layers");
        m_layerWidth = -1;
        m_layerHeight = -1;
        m_numLayers = numLayers;
        originalTextures = new Texture2D[ getNumLayers() ];

        //int startIndex = 0;
        //string folderPath = "scans/" + folderName + "/" + filePrefix;
        //string folderPath2 = folderPath + startIndex.ToString().PadLeft(3, '0');
        //Texture2D first = Resources.Load(folderPath2) as Texture2D;
        //Debug.Log("Loaded " + folderPath2 + ", w" + first.width);


        //int count = 0;
        for (int layerIndex = 0; layerIndex < getNumLayers(); layerIndex++) {
            //string filePath = "Assets/Resources/scans/" + folderName + "/" + filePrefix + layerIndex.ToString().PadLeft(3, '0') + ".png";
            string filePath = "scans/" + folderName + "/" + filePrefix + layerIndex.ToString().PadLeft(3, '0');
            // Texture2D layerTexture = LoadLayer(filePath);
            Texture2D temp = Resources.Load(filePath) as Texture2D;
            Debug.Log("Loaded " + filePath);
            Texture2D layerTexture = new Texture2D(temp.width, temp.height);
            layerTexture.SetPixels(temp.GetPixels());
            originalTextures[layerIndex] = layerTexture;

			// m_scanWidth is -1 until it is initialized from the first texture read in
            if (m_layerWidth == -1)
            {
                m_layerWidth = layerTexture.width;
                m_layerHeight = layerTexture.height;
                originalFloatData = new float[m_layerWidth, m_layerHeight, m_numLayers ];
                originalByteData = new byte[m_layerWidth, m_layerHeight, m_numLayers];
            }
            for (int x = 1; x <= m_layerWidth; x++) {
                //string row = "";
                for (int y = 1; y <= m_layerHeight; y++) {
                    originalFloatData[ x-1, y-1, layerIndex] = layerTexture.GetPixel(-x, -y).r;
                    //row += originalFloatData[ x-1, y-1, layerIndex ] + ", " ;
                    originalByteData[ x-1, y-1, layerIndex ] = (byte) ( originalFloatData[ x-1, y-1, layerIndex ] * 255); // Assuming the values are from 0 to 1 initially
                }
                //Debug.Log( row  + layerIndex + count++);
            }
            //saveTextureToFile(originalScans, scanIndex, "Saved Texture " + scanIndex + ".png");
        }
    }

    public List<bool[,,]> GetSegments() {
        return segments;
    }
		
    public bool[,,] GetSegment() {
        return segments[ segments.Count - 1 ];
    }

    public bool[,,] selectBlackPixels(byte threshold) {
        bool[,,] black = new bool[ m_layerWidth, m_layerHeight, m_numLayers ];
        for( int a = 0; a < black.GetLength( 0 ); a++ ) {
            for( int b = 0; b < black.GetLength( 1 ); b++ ) {
                for( int c = 0; c < black.GetLength( 2 ); c++ ) {
                    black[ a, b, c ] = this.getOriginalPixelByte( a, b, c ) < threshold;
                }
            }
        }
        return black;
    }

    public bool[,,] InvertSegment( bool[,,] segment ) {
        bool[,,] inverted = (bool[,,]) segment.Clone();
        for( int a = 0; a < inverted.GetLength(0); a++ ) {
            for( int b = 0; b < inverted.GetLength( 1 ); b++ ) {
                for( int c = 0; c < inverted.GetLength( 2 ); c++ ) {
                    inverted[ a, b, c ] = !inverted[ a, b, c ];
                }
            }
        }
        return inverted;
    }
    public List<bool[,,]> SeparateSegment( bool[,,] segment, int maxSegments ) {
        // do DFS from each point unless its already visited. Each iteration of DFS increment group counter
        // finally for each group create a new bool[,,] and assign values to true where the group is.
        bool[,,] segmentCopy = (bool[,,]) segment.Clone();
        List<bool[,,]> segments = new List<bool[,,]>();
        List<int> segmentVolumes = new List<int>();
        Stack<Point> searchArea = new Stack<Point>();

        int smallestVolume = -1;
        for( int a = 0; a < segment.GetLength( 0 ); a++ ) {
            for( int b = 0; b < segment.GetLength( 1 ); b++ ) {
                for( int c = 0; c < segment.GetLength( 2 ); c++ ) {
                    if( segmentCopy[a, b, c] ) {
                        int volume = 0;
                        bool[,,] group = new bool[ segment.GetLength( 0 ), segment.GetLength( 1 ), segment.GetLength( 2 ) ];
                        searchArea.Push( new Point( a, b, c ) );
                        while( searchArea.Count > 0 ) {
                            Point point = searchArea.Pop();

                            if( point.x >= 0 && point.x < segment.GetLength(0) && point.y >= 0 && point.y < segment.GetLength( 1 ) && point.z >= 0 && point.z < segment.GetLength( 2 ) ) {
                                if( segmentCopy[ point.x, point.y, point.z ] ) {
                                    segmentCopy[ point.x, point.y, point.z ] = false;
                                    group[ point.x, point.y, point.z ] = true;
                                    volume++;
                                    searchArea.Push( new Point( point.x - 1, point.y, point.z ) );
                                    searchArea.Push( new Point( point.x + 1, point.y, point.z ) );
                                    searchArea.Push( new Point( point.x, point.y - 1, point.z ) );
                                    searchArea.Push( new Point( point.x, point.y + 1, point.z ) );
                                    searchArea.Push( new Point( point.x, point.y, point.z + 1 ) );
                                    searchArea.Push( new Point( point.x, point.y, point.z - 1 ) );
                                }
                            }
                        }
                        if( volume > smallestVolume && segments.Count >= maxSegments ) {
                            int minVolume = segmentVolumes[ 0 ];
                            int minIndex = 0;
                            for( int index = 1; index < segments.Count; index++ ) {
                                if( segmentVolumes[ index ] < minVolume ) {
                                    minVolume = segmentVolumes[ index ];
                                    minIndex = index;
                                }
                            }
                            segments.RemoveAt( minIndex );
                            segmentVolumes.RemoveAt( minIndex );
                            segments.Add( group );
                            segmentVolumes.Add( volume );
                            smallestVolume = Math.Min( smallestVolume, volume );
                        }
                        else {
                            smallestVolume = Math.Min( smallestVolume, volume );
                            segments.Add( group );
                            segmentVolumes.Add( volume );
                        }
                    }
                }
            }
        }
        return segments;
    }

    public void ClearSegments() {
        segments.Clear();
    }
    public void AddSegment(bool[,,] segment ) {
        segments.Add( segment );
        Debug.Log( "Adding segment" );
        //for( int x = 0; x < segment.GetLength( 0 ); x++ ) {
        //    for( int y = 0; y < segment.GetLength( 1 ); y++ ) {
        //        for( int z = 0; z < segment.GetLength( 2 ); z++ ) {
        //            segmentedTextures[ x, y, z ] = segmentedTextures[ x, y, z ] || segment[ x, y, z ];
        //        }
        //    }
        //}
    }

    public List<Point> GetObjectSeeds() {
        return foregroundSeedPoints;
    }
    public List<Point> GetBackgroundSeeds() {
        return backgroundSeedPoints;
    }
    public void ClearSeeds() {
        foregroundSeedPoints.Clear();
        backgroundSeedPoints.Clear();
    }
    public void AddSeed(int x, int y, int z, bool isPartOfObject) {
        if( x >= 0 && x < getWidth() && y >= 0 && y < getHeight() && z >= 0 && z < getNumLayers() ) {
            if( isPartOfObject ) {
                Debug.LogErrorFormat( "Added point to Object {0}, {1}, {2}", x, y, z );
                foregroundSeedPoints.Add( new Point( x, y, z) );
            }
            else {
                Debug.LogErrorFormat( "Added point to Background {0}, {1}, {2}", x, y, z );
                backgroundSeedPoints.Add( new Point( x, y, z) );
            }
        }
        else {
            Debug.LogErrorFormat( "Seed was out of range = {0}, {1}, {2}", x, y, z );
        }
    }
    public int getWidth() {     return m_layerWidth;    }
    public int getHeight() {    return m_layerHeight;   }
    public int getNumLayers() {
        if( !loadedSettings ) {
            loadNumberScans();
            loadedSettings = true;
        }
        return m_numLayers;
    }

    public float getOriginalPixelFloat(int x, int y, int z) {
        return originalFloatData[ x, y, z ];
    }
    public byte getOriginalPixelByte(int x, int y, int z) {
        return originalByteData[ x, y, z ];
    }

    /** Used for the 2D display,    float ratio is a number from 0 to 1 */
    public int getSelectedLayer( float ratio ) {
        // select a scan based on position of the slider. 
        // the max and min is to prevent selecting past the array size
        return (int) ( Mathf.Max( Mathf.Min( ratio * getNumLayers(), originalTextures.Length - 1 ), 0 ) );
    }
    /** Used for the 2D display,    float ratio is a number from 0 to 1 */
    public Texture2D getSelectedTexture(float ratio) {
        return originalTextures[ getSelectedLayer(ratio) ];
    }

    /** 
     * This function loads the image located at filePath and returns it as a Texture2D.
     * If the file at the specified path doesn't exist, it returns null.
     */
    public static Texture2D LoadLayer(string filePath) {

        Texture2D first = Resources.Load(filePath) as Texture2D;
        return first;
        //Texture2D tex = null;
        //byte[] fileData;
        //if (File.Exists(filePath)) {
        //    fileData = File.ReadAllBytes(filePath);
        //    tex = new Texture2D(2, 2);
        //    tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
        //}
        //else {
        //    Debug.LogError("The file at " + filePath + " could not be found.");
        //    throw new System.IO.FileNotFoundException(filePath);
        //}
        //return tex;
    }

    public bool[,,] loadSegmentFromTextFile( string fileName ) {
		StreamReader file = new StreamReader( Application.dataPath + "/Output/" + fileName );
		Debug.LogError( "Loading segment from " + Application.dataPath + "/Output/" + fileName );
        string dimensionString = file.ReadLine();
        string[] dimensionStrings = dimensionString.Split( ',' );
        int xDim = Int32.Parse( dimensionStrings[ 0 ] );
        int yDim = Int32.Parse( dimensionStrings[ 1 ] );
        int zDim = Int32.Parse( dimensionStrings[ 2 ] );
        bool[,,] segmentArray = new bool[ xDim, yDim, zDim ];
        int num = 0;
        Debug.LogError( "Array size is " + xDim + "," + yDim + "," + zDim );
        for( int a = 0; a < segmentArray.GetLength( 2 ); a++ ) {
            for( int b = 0; b < segmentArray.GetLength( 0 ); b++ ) {
                string line = file.ReadLine();
                for( int c = 0; c < segmentArray.GetLength( 1 ); c++ ) {
                    if( line[c] == '#' ) {
                        segmentArray[ b, c, a ] = true;
                        num++;
                    }
                    else {
                        segmentArray[ b, c, a ] = false;
                    }
                }
            }
            file.ReadLine(); // Skip line of ~~~~~~~~~~~~~~ in between every layer.
        }
        Debug.LogError( "Number of pixels = " + num );
        return segmentArray;
    }

    public void saveSegmentToFileAsText( bool[,,] segmentArray, string fileName ) {
        StreamWriter file = new StreamWriter( Application.dataPath + "/Output/" + fileName );
		Debug.Log( "Saving segment to " + Application.dataPath + "/Output/" + fileName );
        file.WriteLine(segmentArray.GetLength( 0 ) + "," + segmentArray.GetLength( 1 ) + "," + segmentArray.GetLength( 2 ) );
        for( int a = 0; a < segmentArray.GetLength( 2 ); a++ ) {
            for( int b = 0; b < segmentArray.GetLength( 0 ); b++ ) {
                for( int c = 0; c < segmentArray.GetLength( 1 ); c++ ) {
                    if( segmentArray[ b, c, a ] ) {
                        file.Write( "#" );
                    }
                    else {
                        file.Write( "-" );
                    }
                }
                file.WriteLine();
            }
            for( int c = 0; c < segmentArray.GetLength( 1 ); c++ ) {
                file.Write( "~" );
            }
            file.WriteLine();
        }
        file.Close();
    }

    public void saveSegmentToFileAsImages( bool[,,] segmentArray, string fileName) {

        for (int z = 0; z < getNumLayers(); z++) {
            Texture2D texture = new Texture2D(getWidth(), getHeight());
            for ( int x = 0; x < getWidth(); x++ ) {
                for( int y = 0; y < getHeight(); y++ ) {
                    if (segmentArray[x, y, z]) {
                        texture.SetPixel(getWidth() - 1 - x, getHeight() - 1 - y, new Color(0, 0, 0));
                    }
                }
            }
            byte[] bytes = texture.EncodeToPNG();
			File.WriteAllBytes(Application.dataPath + "/Output/" + fileName + z.ToString().PadLeft(4, '0') + ".png", bytes);
        }
    }

}
