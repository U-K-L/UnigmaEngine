using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SaveTexture
{
    public static void SaveTexture2D(Texture2D texture, string path)
    {
        byte[] bytes = texture.EncodeToPNG();
        System.IO.File.WriteAllBytes("../" + path, bytes);

        Debug.Log("Saved texture to: " + path);
    }

    public static void SaveRenderTexture2D(RenderTexture renderTexture, string path)
    {
        Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
        RenderTexture.active = renderTexture;
        texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture.Apply();
        RenderTexture.active = null;

        SaveTexture2D(texture, path);
    }

    public static void TakeScreenshot(string path)
    {
        ScreenCapture.CaptureScreenshot("../" + path);
    }
}
