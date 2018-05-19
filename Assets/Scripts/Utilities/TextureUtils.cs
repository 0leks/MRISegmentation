using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextureUtils {

    public static List<Texture2D> ListFromArray(Texture2DArray texArr)
    {
        List<Texture2D> result = new List<Texture2D>();
        for (int i = 0; i < texArr.depth; i++)
        {
            Texture2D temp = new Texture2D(texArr.width, texArr.height, texArr.format, false);
            Graphics.CopyTexture(texArr, i, temp, 0);
            result.Add(temp);
        }

        return result;
    }

    public static List<Texture2D> ListFromTexture3D(Texture3D tex3D)
    {
        List<Texture2D> result = new List<Texture2D>();
        for (int i = 0; i < tex3D.depth; i++)
        {
            Texture2D temp = new Texture2D(tex3D.width, tex3D.height, tex3D.format, false);
            Graphics.CopyTexture(tex3D, i, temp, 0);
            result.Add(temp);
        }

        return result;
    }

    public static Texture2DArray ArrayFromList(List<Texture2D> texList)
    {
        if (texList == null)
            throw new Exception("Texture list cannot be null.");
        if (texList.Count < 1)
            throw new Exception("Texture list cannot be empty.");

        Texture2DArray result = new Texture2DArray(
            texList[0].width, texList[0].height, texList.Count, texList[0].format, false);

        for (int i = 0; i < texList.Count; i++)
        {
            Graphics.CopyTexture(texList[i], 0, result, i);
        }

        return result;
    }

    public static Texture3D Texture3DFromList(List<Texture2D> texList)
    {
        if (texList == null)
            throw new Exception("Texture list cannot be null.");
        if (texList.Count < 1)
            throw new Exception("Texture list cannot be empty.");

        Texture3D result = new Texture3D(
            texList[0].width, texList[0].width, texList.Count, texList[0].format, false);

        List<Color> colors = new List<Color>();
        for (int i = 0; i < texList.Count; i++)
        {
            colors.AddRange(texList[i].GetPixels(i));
        }

        result.SetPixels(colors.ToArray());
        result.Apply();
        return result;
    }

    public static Texture3D Texture3DFromArray(Texture2DArray texArr)
    {
        if (texArr == null)
            throw new Exception("Texture2DArray cannot be null.");

        Texture3D result = new Texture3D(
            texArr.width, texArr.height, texArr.depth, texArr.format, false);

        List<Color> colors = new List<Color>();
        for (int i = 0; i < texArr.depth; i++)
        {
            colors.AddRange(texArr.GetPixels(i));
        }

        result.SetPixels(colors.ToArray());
        result.Apply();
        return result;
    }
}
