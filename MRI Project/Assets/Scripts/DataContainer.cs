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


    public int m_numLayers;
    private int m_layerWidth;
    private int m_layerHeight;

    private Texture2D[] originalTextures;
    private float[,,] originalFloatData;
    private byte[,,] originalByteData;

    private List<bool[,,]> segments;
    
    private List<Point> partOfObject;
    private List<Point> partOfBackground;

    void Start () {
        partOfObject = new List<Point>();
        partOfBackground = new List<Point>();

        segments = new List<bool[,,]>();
    }

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

    public void loadMedicalData( string folderName, string filePrefix, int startLayer, int numLayers) {

        m_layerWidth = -1;
        m_layerHeight = -1;
        m_numLayers = numLayers;
        originalTextures = new Texture2D[ getNumLayers() ];

        for (int layerIndex = 0; layerIndex < getNumLayers(); layerIndex++) {
            string filePath = "Assets/Resources/scans/" + folderName + "/" + filePrefix + layerIndex.ToString().PadLeft(3, '0') + ".png";
            Texture2D layerTexture = LoadLayer(filePath);
            originalTextures[layerIndex] = layerTexture;
            if (m_layerWidth == -1) // m_scanWidth is -1 until it is initialized from the first texture read in
            {
                m_layerWidth = layerTexture.width;
                m_layerHeight = layerTexture.height;
                originalFloatData = new float[m_layerWidth, m_layerHeight, m_numLayers ];
                originalByteData = new byte[m_layerWidth, m_layerHeight, m_numLayers];
            }
            for (int x = 0; x < m_layerWidth; x++) {
                for (int y = 0; y < m_layerHeight; y++) {
                    originalFloatData[ x, y, layerIndex] = layerTexture.GetPixel(-x, -y).r;
                    originalByteData[ x, y, layerIndex ] = (byte) ( originalFloatData[ x, y, layerIndex ] * 255); // Assuming the values are from 0 to 1 initially
                }
            }
            //saveTextureToFile(originalScans, scanIndex, "Saved Texture " + scanIndex + ".png");
        }
    }

    public bool[,,] GetSegment() {
        return segments[ segments.Count - 1 ];
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
        return partOfObject;
    }
    public List<Point> GetBackgroundSeeds() {
        return partOfBackground;
    }
    public void ClearSeeds() {
        partOfObject.Clear();
        partOfBackground.Clear();
    }
    public void AddSeed(int x, int y, int z, bool isPartOfObject) {
        if( x >= 0 && x < getWidth() && y >= 0 && y < getHeight() && z >= 0 && z < getNumLayers() ) {
            if( isPartOfObject ) {
                Debug.LogErrorFormat( "Added point to Object {0}, {1}, {2}", x, y, z );
                partOfObject.Add( new Point( x, y, z) );
            }
            else {
                Debug.LogErrorFormat( "Added point to Background {0}, {1}, {2}", x, y, z );
                partOfBackground.Add( new Point( x, y, z) );
            }
        }
        else {
            Debug.LogErrorFormat( "Seed was out of range = {0}, {1}, {2}", x, y, z );
        }
    }
    public int getWidth() {     return m_layerWidth;    }
    public int getHeight() {    return m_layerHeight;   }
    public int getNumLayers() { return m_numLayers;     }

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
        Texture2D tex = null;
        byte[] fileData;
        if (File.Exists(filePath)) {
            fileData = File.ReadAllBytes(filePath);
            tex = new Texture2D(2, 2);
            tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
        }
        else {
            Debug.LogError("The file at " + filePath + " could not be found.");
            throw new System.IO.FileNotFoundException(filePath);
        }
        return tex;
    }

    public bool[,,] loadSegmentFromTextFile( string fileName ) {
        StreamReader file = new StreamReader( Application.dataPath + "/" + fileName );
        Debug.LogError( "Loading segment from " + Application.dataPath + "/" + fileName );
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
        StreamWriter file = new StreamWriter( Application.dataPath + "/" + fileName );
        Debug.LogError( "Saving segment to " + Application.dataPath + "/" + fileName );
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
            File.WriteAllBytes(Application.dataPath + "/" + fileName + z.ToString().PadLeft(4, '0') + ".png", bytes);
        }
    }

}
