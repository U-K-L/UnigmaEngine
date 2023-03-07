using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrightContrast : MonoBehaviour
{
    public Material material;
    // Start is called before the first frame update
    void Start()
    {
        Camera cam = GetComponent<Camera>();
        cam.depthTextureMode = cam.depthTextureMode | DepthTextureMode.Depth;
        cam.depthTextureMode |= DepthTextureMode.MotionVectors;
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(source, destination, material);
    }
}
