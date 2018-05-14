using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
///     This class holds raw scan data and provides basic methods to generate
///     the scan data from appropriate sources.
/// </summary>
public class Scans {

    public struct ScansData
    {
        public List<Texture2D> scanTextures;
        public Texture3D scanVolume;            // 3D volume for ray marching
        public float[,,] scanIntensities;       // [x,y,layer] 0..1 grayscale
                                                //       values of each pixel
    }

    /// <summary>
    ///     Load multiple scans as Texture2D's from files. Starting from
    ///     file numbered 0000, up to the specified number of layers.
    /// </summary>
    /// <param name="path">Path to the file minus the numbered suffix.</param>
    /// <param name="numLayers">Number of files to load.</param>
    public static List<Texture2D> LoadTexturesFromFiles(string path, int numLayers)
    {
        return LoadTexturesFromFiles(path, 0, numLayers, 1);
    }

    public static List<Texture2D> LoadTexturesFromFiles(string path, int startIndex, int endIndex, int increment)
    {
        List<Texture2D> scanTextures = new List<Texture2D>();
        
        for (int i = startIndex; i < endIndex; i += increment)
        {
            scanTextures.Add(
                Resources.Load(path + IndexToFileSuffix(i)) as Texture2D);
        }

        return scanTextures;
    }

    /// <summary>
    ///     Generate pixel intensities from scan textures.
    /// </summary>
    public static float[,,] GetIntensitiesFromTextures(List<Texture2D> scanTextures)
    {
        if (scanTextures.Count < 1)
        {
            return null;
        }

        int width = scanTextures[0].width;
        int height = scanTextures[0].height;

        float[,,] pixelIntensities = new float[
            width,
            height,
            scanTextures.Count];

        // convert each layer into floats
        for (int layer = 0; layer < scanTextures.Count; layer++)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // TODO: what are the negative indices doing?
                    pixelIntensities[x,y,layer] = scanTextures[layer].GetPixel(-(x+1), -(y+1)).r;
                }
            }
        }

        return pixelIntensities;
    }

    /// <summary>
    ///     Generate a padded Texture3D from scan textures.
    ///     (width, height, and depth will all be padded to the nearest power of 2)
    /// </summary>
    public static Texture3D GetPaddedTexture3DFromTextures (List<Texture2D> scanTextures)
    {
        if (scanTextures.Count < 1)
            return null;

        List<Color> allPixels = new List<Color>();

        // pad to the nearest power of 2 for Texture3D performance improvments
        int paddedWidth = Mathf.NextPowerOfTwo(scanTextures[0].width);
        int paddedHeight = Mathf.NextPowerOfTwo(scanTextures[0].height);
        int paddedDepth = Mathf.NextPowerOfTwo(scanTextures.Count);

        Texture2D paddedTex2D = new Texture2D(paddedWidth, paddedHeight);
        Texture3D tex3D = new Texture3D(paddedWidth, paddedHeight, paddedDepth, TextureFormat.ARGB32, false);

        // add pixels from the Texture2D's
        foreach (Texture2D tex in scanTextures)
        {
            paddedTex2D.SetPixels(tex.GetPixels());
            allPixels.AddRange(paddedTex2D.GetPixels());
        }

        tex3D.SetPixels(allPixels.ToArray());
        tex3D.Apply();
        return tex3D;
    }

    /// <summary>
    ///     Convert an index to the appropriate file suffix
    ///     for files with the format "{path}-####"
    /// </summary>
    private static string IndexToFileSuffix (int index)
    {
        return index.ToString().PadLeft(3, '0');
    }

}
