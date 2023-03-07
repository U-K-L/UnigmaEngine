using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class SSNormalColors : MonoBehaviour
{
    // Start is called before the first frame update
    public Material material;
    Camera cam;
    void Start()
    {
        cam = GetComponent<Camera>();
        cam.depthTextureMode |= DepthTextureMode.DepthNormals;
        cam.depthTextureMode |= DepthTextureMode.MotionVectors;
    }


    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Matrix4x4 _CamToWorld = cam.cameraToWorldMatrix;
        material.SetMatrix("_CamToWorld", _CamToWorld);
        Graphics.Blit(source, destination, material);
    }
}
