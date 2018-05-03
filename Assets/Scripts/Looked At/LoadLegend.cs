#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

/**
 * This script is responsible for loading the legend dataset. 
 * It writes an ouput vol tex data file that can then be set in material.
 * 
 * The legend is binary data that specifies which parts of the cadaver should be rendered.
 * It is sent to the shader along with the cadaver data
 */
public class LoadLegend : MonoBehaviour
{

    [SerializeField] private ImageSegmentationHandler2 m_SegmentationHandler;		// holds settings information
    [SerializeField] private DataContainer m_Data;									// holds segment and Texture dimensions info

	//// settings for loading segments from a file
	// indices
	[SerializeField] private int startIndex;										// starting layer
	[SerializeField] private int endIndex;											// ending layer
	[SerializeField] private int indexIncrement;									// load every X images
	// segment file location
	[SerializeField] private string segmentName;									// name of segment if loading the segment from a file
	private static string LegendSystemPath = "";
	private static string folderPath = "segment/";
	private int systemID = 1;
	private int subsystemID = 1;
	private int bodyPartID = 1;

    private List<Color> imageColors;												// intermediately stores colors before converting to Texture3D


    // Use this for initialization
    void Start()
    {
		
        //LegendSystemPath = Application.dataPath + "/Resources/Legend/segment";
        imageColors = new List<Color>();

        //Precalc the tex dimensions by loading the first image.
        //TODO
        //string folderPath = LegendSystemPath + startIndex.ToString().PadLeft(4, '0');
        //folderPath = "Legend/segment0010.png";

		// load segment from a file
        if (m_SegmentationHandler.loadSegmentOnStart)
        {
            Debug.Log("Loading legend from " + folderPath + segmentName);
            Texture2D first = Resources.Load(folderPath + segmentName + startIndex.ToString().PadLeft(4, '0')) as Texture2D;
            int width = (int)System.Math.Pow(2, System.Math.Ceiling(System.Math.Log(first.width) / System.Math.Log(2)));
            int height = (int)System.Math.Pow(2, System.Math.Ceiling(System.Math.Log(first.height) / System.Math.Log(2)));
            int numImages = (endIndex - startIndex + 1) / indexIncrement;

            for (int i = startIndex; i <= endIndex; i += indexIncrement)
            {
                Texture2D anImage = new Texture2D(width, height);
                Texture2D temp = Resources.Load(folderPath + segmentName + i.ToString().PadLeft(4, '0')) as Texture2D;

                anImage.SetPixels(temp.GetPixels());
                addImageColorToList(anImage);
            }

            //Convert volTex dimensions to nearest power of two
            int numSlices = (int)System.Math.Pow(2, System.Math.Ceiling(System.Math.Log(numImages) / System.Math.Log(2)));

            //Need to pad the 3D tex along the z axis with empty data due to the round off.
            Texture2D emptyTex = new Texture2D(width, height);
            Color[] emptyTexColors = emptyTex.GetPixels();
            for (int a = 0; a < emptyTexColors.Length; a++)
            {
                emptyTexColors[a] = Color.white;
            }
            emptyTex.SetPixels(emptyTexColors);
            emptyTex.Apply();
            for (int j = numImages; j < numSlices; j++)
            {
                addImageColorToList(emptyTex);
            }

            //Allocate the texture memory.
            Texture3D legendVolume = new Texture3D(width, height, numSlices, TextureFormat.ARGB32, false);

            //Copy the data to the 3D texture.
            Color[] allColors = imageColors.ToArray();
            legendVolume.SetPixels(allColors);
            legendVolume.Apply();

            // assign it to the material of the parent object
            GetComponent<Renderer>().material.SetTexture("Legend_Data", legendVolume);

            //Update the shader slice axes;
            setShaderSliceAxes(numImages, numSlices);

            //As a sanity check, don't actually write out files if running from a compiled binary.
#if UNITY_EDITOR
            writeLoadedAssetsToFile(legendVolume);
#endif
        }
    }

	// Update the 3D legend texture and send it to the shader
    void LoadSegments()
    {
        //LegendSystemPath = Application.dataPath + "/Resources/Legend/segment";
        imageColors = new List<Color>();

        //Precalc the tex dimensions by loading the first image.
        //TODO
        //string folderPath = LegendSystemPath + startIndex.ToString().PadLeft(4, '0');
        //folderPath = "Legend/segment0010.png";
        //Debug.Log("Loading legend from " + folderPath);

		// get the most recent segment
        bool[,,] segments = m_Data.GetSegment();
        
		// Texture3D dimensions
		// fancy math rounds width and height UP to the next highest power of 2 (257->512, 513->1024, etc.)
        int width = (int) System.Math.Pow( 2, System.Math.Ceiling( System.Math.Log( m_Data.getWidth() ) / System.Math.Log( 2 ) ) );
        int height = (int) System.Math.Pow( 2, System.Math.Ceiling( System.Math.Log( m_Data.getHeight() ) / System.Math.Log( 2 ) ) );
        int numImages = m_Data.getNumLayers();

        addSegmentColorsToList( segments );
        //for( int i = 0; i < numImages; i += 1)
        //{
        //    Texture2D anImage = new Texture2D(width, height);
        //    Texture2D temp = textures[i];

        //    anImage.SetPixels(temp.GetPixels());
        //    addImageColorToList(anImage);
        //}

        //Convert volTex dimensions to nearest power of two
        int numSlices = (int)System.Math.Pow(2, System.Math.Ceiling(System.Math.Log(numImages) / System.Math.Log(2)));

        //Need to pad the 3D tex along the z axis with empty data due to the round off.
        Texture2D emptyTex = new Texture2D(width, height);
        Color[] emptyTexColors = emptyTex.GetPixels();
        for (int a = 0; a < emptyTexColors.Length; a++)
        {
            emptyTexColors[a] = Color.white;
        }
        emptyTex.SetPixels(emptyTexColors);
        emptyTex.Apply();
        for (int j = numImages; j < numSlices; j++)
        {
            addImageColorToList(emptyTex);
        }

        //Allocate the texture memory.
        Texture3D legendVolume = new Texture3D(width, height, numSlices, TextureFormat.ARGB32, false);

        //Copy the data to the 3D texture.
        Color[] allColors = imageColors.ToArray();
		legendVolume.SetPixels(allColors);
        legendVolume.Apply();

        // assign it to the material of the parent object
        GetComponent<Renderer>().material.SetTexture("Legend_Data", legendVolume);

        //Update the shader slice axes;
        setShaderSliceAxes(numImages, numSlices);

        //As a sanity check, don't actually write out files if running from a compiled binary.
//#if UNITY_EDITOR
//        writeLoadedAssetsToFile(legendVolume);
//#endif
    }
    

	// Update the 3D legend texture and send it to the shader
    public void LoadLegendFromSegmentationHandler() {
        LoadSegments();
    }

    /*
     * Copy the pixel colors of the given image to the master array.
     */
    private void addImageColorToList(Texture2D anImage) {
        Color[] tempColors = anImage.GetPixels();
        for (int i = 0; i < tempColors.Length; i++) {
            imageColors.Add(tempColors[i]);
        }
    }

	// convert segment boolean array into legend 0 or 1 data
    private void addSegmentColorsToList( bool[,,] segments ) {
        for( int z = 0; z < segments.GetLength(2); z++ ) {
            for( int y = segments.GetLength( 1 ) - 1; y >= 0; y-- ) {
                for( int x = segments.GetLength( 0 ) - 1; x >= 0; x-- ) {
                    // Part of segment is black and part of the background is white
                    if( segments[x, y, z] ) {
                        imageColors.Add( Color.black );
                    } else {
                        imageColors.Add( Color.white );
                    }
                }
            }
        }
    }

    /**
     * Function that constructs the final path that corresponds the user selected IDs. 
     * Use 0 to signify that a given system does not have any subsytems and/or body parts/
     * Eg. In order to show the Parietal bone:
     *      systemID = 1
     *      subsystemID = 1
     *      bodyPartID = 2
     */
    string constructPathForSelectedIDs() {
        string path = LegendSystemPath;

        //Error check, there must be a system selected.
        if (systemID <= 0)
            systemID = 1; //Default to skeletal system.
        
        //Error check, selecting body part necessitates selecting a subsystem.
        if (bodyPartID > 0 && subsystemID <= 0)
            throw new System.Exception("Illegal Arguments: Cannot select body part without first selecting system.");

        string systemFolder = findSystemFolderWithID(systemID);
        path += systemFolder;

        if(subsystemID > 0)
        {
            string subsystemFolder = findFolderWithIDFromParent(subsystemID, path);
            path += subsystemFolder;
        }

        if( bodyPartID > 0)
        {
            string bodyPartFolder = findFolderWithIDFromParent(bodyPartID, path);
            path += bodyPartFolder;
        }

        //Trim the path to only be relative to the Resources folder
        string[] folders = path.Split('/');
        int i;
        for(i = 0; i < folders.Length; i++)
        {
            if(folders[i] == "Legend")
            {
                break;
            }
        }

        string finalPath = "";
        for(int j = i; j < folders.Length - 1; j++) //Length - 1 to avoid extra slash on the end.
        {
            finalPath += folders[j] + "/";
        }

        return finalPath;

    }

    /**
     * Function to find the corresponding System data folder for the given legend ID
     * Eg. systems are Skeletal, Articular.
     */
    string findSystemFolderWithID( int anId )
    {
        string[] legendDirs = Directory.GetDirectories(LegendSystemPath, "(" + anId.ToString() + ")*");

        //Assuming unique dir and system ids, get the first.
        string folderPath = legendDirs[0];

        //Extract just the last directory.
        string[] folders = folderPath.Split('/');

        //The last element should be the folder name.
        return folders[folders.Length - 1] + "/";
    }

    /**
     * Finds the folder corresponding to the given subsystem/body part ID in the given parent folder.
     * Eg. anID = 1 for subsystem Cranium in parent system Skeletal.
     * Eg. anID = 2 for body part Parietal bone in parent subsystem Cranium
     */
    string findFolderWithIDFromParent( int anId, string parentSystemPath )
    {
        if (anId <= 0)
            return "";

        //Get the list of subsystem folders
        string[] subsystemFolders = Directory.GetDirectories(parentSystemPath, "(" + anId + ")*");

        //Assuming unique IDs, first match should be folder.
        string folderPath = subsystemFolders[0];

        //Extract just the last dir name from the path
        string[] folders = folderPath.Split('/');

        return folders[folders.Length - 1] + "/";
    }


    void setShaderSliceAxes(float numImages, float numSlices)
    {
        //get the material
        Material mat = GetComponent<Renderer>().material;

        //Set 2nd slice axis to numImages cutoff, as don't want to sample empty voxels.
        mat.SetFloat("_SliceAxis2Max",  numImages / numSlices);
        //Getting a weird artifact when the min Y-slice axis is not 0, this is a quick fix.
        //TODO: FIGURE THIS SHIT OUT.
        mat.SetFloat("_SliceAxis2Min", 0.0001f);
    }

#if UNITY_EDITOR
    /**
     * Write the loaded legend files to disk as a .asset.
     * File is written with name of format systemID_subSystemID_bodyPartID-Asset.asset
     */
    void writeLoadedAssetsToFile(Texture3D legendVolume)
    {
        //Load an instance of the base CadaverBody Material.
        string filename = systemID + "_" + subsystemID + "_" + bodyPartID + "-Asset.asset";
        AssetDatabase.CreateAsset(legendVolume, "Assets/Resources/Legend Assets/" + filename);
    }
#endif


    public int getSystemID() { return systemID; }
    public int getSubSystemID() { return subsystemID; }
    public int getBodyPartID() { return bodyPartID; }
}


