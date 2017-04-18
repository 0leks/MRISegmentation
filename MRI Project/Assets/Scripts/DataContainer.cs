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
    
    void Start () {

    }

    void Update() {

    }

    public void loadMedicalData( string folderName, string filePrefix, int startLayer, int numLayers) {

        m_layerWidth = -1;
        m_layerHeight = -1;

        for (int layerIndex = 0; layerIndex < m_numLayers; layerIndex++) {
            string filePath = "Assets/Resources/scans/" + folderName + "/" + filePrefix + layerIndex.ToString().PadLeft(3, '0') + ".png";
            Texture2D layerTexture = LoadLayer(filePath);
            originalTextures[layerIndex] = layerTexture;
            if (m_layerWidth == -1) // m_scanWidth is -1 until it is initialized from the first texture read in
            {
                m_layerWidth = layerTexture.width;
                m_layerHeight = layerTexture.height;
                originalFloatData = new float[m_numLayers, m_layerWidth, m_layerHeight];
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

    public int getWidth() {
        return m_layerWidth;
    }

    public int getHeight() {
        return m_layerHeight;
    }

    public int getNumLayers() {
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
        Texture2D tex = null;
        byte[] fileData;
        if (File.Exists(filePath)) {
            fileData = File.ReadAllBytes(filePath);
            tex = new Texture2D(2, 2);
            tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
        }
        else {
            Debug.LogErrorFormat("The file at %s could not be found.", filePath);
            throw new System.IO.FileNotFoundException(filePath);
        }
        return tex;
    }

    public void saveSegmentToFileAsImages(bool[,,] segmentArray, string fileName) {

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
