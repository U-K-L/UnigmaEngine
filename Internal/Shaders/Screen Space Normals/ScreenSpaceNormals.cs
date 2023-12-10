using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class ScreenSpaceNormals : MonoBehaviour
{
    // Start is called before the first frame update
    public Material mat;
    public Material dmat;
    public Material customOutput;
    RenderTexture camDepthTexture;
    RenderTexture customDepthTexture;

    RenderTexture _drawObjsTextureNormals;
    void Start()
    {
        camDepthTexture = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        camDepthTexture.name = "camDepthTextureUnigma";
        camDepthTexture.enableRandomWrite = true;
        camDepthTexture.Create();

        customDepthTexture = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        customDepthTexture.name = "customDepthTextureUnigma";
        customDepthTexture.enableRandomWrite = true;
        customDepthTexture.Create();

        _drawObjsTextureNormals = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        _drawObjsTextureNormals.name = "_drawObjsTextureNormals";
        _drawObjsTextureNormals.enableRandomWrite = true;
        _drawObjsTextureNormals.Create();

        CommandBuffer screenSpaceNormals = new CommandBuffer();
        screenSpaceNormals.name = "Screen Space Normals";
        screenSpaceNormals.SetRenderTarget(_drawObjsTextureNormals);
        screenSpaceNormals.ClearRenderTarget(true, true, new Vector4(0, 0, 0, 0));
        GameObject[] objs = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject obj in objs)
        {
            screenSpaceNormals.DrawRenderer(obj.GetComponent<Renderer>(), obj.GetComponent<Renderer>().material);
        }

        screenSpaceNormals.SetGlobalTexture("_drawObjsTextureNormals", _drawObjsTextureNormals);
        GetComponent<Camera>().AddCommandBuffer(CameraEvent.AfterForwardOpaque, screenSpaceNormals);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(source, camDepthTexture, dmat);
        Shader.SetGlobalTexture("camDepthTextureUnigma", camDepthTexture);
        Graphics.Blit(source, customDepthTexture, customOutput);
        Shader.SetGlobalTexture("customDepthTextureUnigma", customDepthTexture);
        Graphics.Blit(source, destination, mat);
    }
}
