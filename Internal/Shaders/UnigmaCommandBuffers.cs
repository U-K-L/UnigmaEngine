using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class UnigmaCommandBuffers : MonoBehaviour
{
    private int buffersAdded = 0;
    private List<UnigmaPostProcessingObjects> _OutlineRenderObjects; //Objects part of this render. 
    private List<Renderer> _OutlineNullObjects = default; //Objects not part of this render.
    
    public Material _depthNormalsMaterial;
    public Material _nullMaterial;
    // Start is called before the first frame update
    void Start()
    {
        Camera cam = GetComponent<Camera>();
        cam.depthTextureMode = cam.depthTextureMode | DepthTextureMode.Depth;
        cam.depthTextureMode = cam.depthTextureMode | DepthTextureMode.DepthNormals;
    }

    // Update is called once per frame
    void Update()
    {
        
        if (buffersAdded < 1)
        {
            //Create isometric depth normals.
            _OutlineRenderObjects = new List<UnigmaPostProcessingObjects>();
            _OutlineNullObjects = new List<Renderer>();
            FindObjects("IsometricDepthNormalObject");
            CreateDepthNormalBuffers();
            buffersAdded += 1;
        }

        if (buffersAdded < 2)
        {
            //CreateOutlineColorBuffers();
            buffersAdded += 1;
        }
        
    }

    void CreateDepthNormalBuffers()
    {
        CommandBuffer outlineDepthBuffer = new CommandBuffer();
        RenderTexture rt = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
        outlineDepthBuffer.SetGlobalTexture("_IsometricDepthNormal", rt);

        outlineDepthBuffer.SetRenderTarget(rt);

        outlineDepthBuffer.ClearRenderTarget(true, true, Color.black);
        DrawIsometricDepthNormals(outlineDepthBuffer);
        GetComponent<Camera>().AddCommandBuffer(CameraEvent.AfterForwardOpaque, outlineDepthBuffer);
    }

    void CreateOutlineColorBuffers()
    {
        CommandBuffer outlineColorBuffer = new CommandBuffer();
        RenderTexture rt = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
        outlineColorBuffer.SetGlobalTexture("_IsometricDepthNormal", rt);

        outlineColorBuffer.SetRenderTarget(rt);

        outlineColorBuffer.ClearRenderTarget(true, true, Color.black);
        //DrawIsometricDepthNormals();
        GetComponent<Camera>().AddCommandBuffer(CameraEvent.AfterForwardOpaque, outlineColorBuffer);
    }

    void DrawIsometricDepthNormals(CommandBuffer outlineDepthBuffer)
    {
        foreach (UnigmaPostProcessingObjects r in _OutlineRenderObjects)
        {

            if(r.enabled == true)
                outlineDepthBuffer.DrawRenderer(r.renderer, r.material, 0, -1);
        }

        foreach (Renderer r in _OutlineNullObjects)
        {
            if (r.enabled == true)
                outlineDepthBuffer.DrawRenderer(r, _nullMaterial, 0, -1);
        }
    }

    // Find and store visible renderers to a list
    void FindObjects(string component)
    {
        // Retrieve all renderers in scene
        Renderer[] sceneRenderers = FindObjectsOfType<Renderer>();

        // Store only visible renderers
        _OutlineRenderObjects.Clear();
        _OutlineNullObjects.Clear();
        for (int i = 0; i < sceneRenderers.Length; i++)
            if (sceneRenderers[i].GetComponent(component))
                _OutlineRenderObjects.Add(sceneRenderers[i].gameObject.GetComponent<UnigmaPostProcessingObjects>());
            else
                _OutlineNullObjects.Add(sceneRenderers[i]);
    }
}
