using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class UnigmaCommandBuffers : MonoBehaviour
{
    private int buffersAdded = 0;
    private List<UnigmaPostProcessingObjects> _OutlineRenderObjects; //Objects part of this render.
    private List<Renderer> _OutlineNullObjects = default; //Objects not part of this render.

    private ComputeShader computeOutlineColors;
    private Material _nullMaterial = default;

    private bool BuffersReady = false;
    // Start is called before the first frame update
    void Start()
    {
        _nullMaterial = new Material(Shader.Find("Unigma/IsometricNull"));
        computeOutlineColors = Resources.Load("OutlineColorsBoxBlur") as ComputeShader;
        Camera cam = GetComponent<Camera>();
        cam.depthTextureMode = cam.depthTextureMode | DepthTextureMode.Depth;
        cam.depthTextureMode = cam.depthTextureMode | DepthTextureMode.DepthNormals;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (BuffersReady == false)
        {
            BuffersReady = true;
            return;
        }
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
            CreateOutlineColorBuffers();
            buffersAdded += 1;
        }

    }

    void CreateDepthNormalBuffers()
    {
        CommandBuffer outlineDepthBuffer = new CommandBuffer();
        RenderTexture rt = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
        RenderTexture posRT = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
        outlineDepthBuffer.SetGlobalTexture("_IsometricDepthNormal", rt);
        outlineDepthBuffer.SetGlobalTexture("_IsometricPositions", posRT);

        outlineDepthBuffer.SetRenderTarget(rt);

        outlineDepthBuffer.ClearRenderTarget(true, true, Color.black);
        DrawIsometricDepthNormals(outlineDepthBuffer, 0);

        //Second pass
        outlineDepthBuffer.SetRenderTarget(posRT);
        outlineDepthBuffer.ClearRenderTarget(true, true, Color.black);
        DrawIsometricDepthNormals(outlineDepthBuffer, 1);
        GetComponent<Camera>().AddCommandBuffer(CameraEvent.AfterForwardOpaque, outlineDepthBuffer);

    }

    void CreateOutlineColorBuffers()
    {
        CommandBuffer outlineColorBuffer = new CommandBuffer();
        RenderTexture rt = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
        RenderTexture innerRT = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
        RenderTexture tempRt = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
        rt.enableRandomWrite = true;
        tempRt.enableRandomWrite = true;
        outlineColorBuffer.SetGlobalTexture("_IsometricOutlineColor", rt);
        outlineColorBuffer.SetGlobalTexture("_IsometricInnerOutlineColor", innerRT);

        outlineColorBuffer.SetRenderTarget(rt);

        outlineColorBuffer.ClearRenderTarget(true, true, Vector4.zero);

        //Set outter colors
        DrawIsometricOutlineColor(outlineColorBuffer, 0);

        outlineColorBuffer.CopyTexture(rt, tempRt);
        computeOutlineColors.SetTexture(0, "_IsometricOutlineColor", rt);
        computeOutlineColors.SetTexture(0, "_TempTexture", tempRt);
        outlineColorBuffer.DispatchCompute(computeOutlineColors, 0, Mathf.CeilToInt(Screen.width / 8), Mathf.CeilToInt(Screen.height / 8), 1);
        outlineColorBuffer.CopyTexture(tempRt, rt);

        //Now set inner colors.
        outlineColorBuffer.SetRenderTarget(innerRT);
        outlineColorBuffer.ClearRenderTarget(true, true, Color.black);
        DrawIsometricOutlineColor(outlineColorBuffer, 1);
        GetComponent<Camera>().AddCommandBuffer(CameraEvent.AfterForwardOpaque, outlineColorBuffer);
    }

    void DrawIsometricDepthNormals(CommandBuffer outlineDepthBuffer, int pass)
    {
        foreach (UnigmaPostProcessingObjects r in _OutlineRenderObjects)
        {
            IsometricDepthNormalObject iso = r.gameObject.GetComponent<IsometricDepthNormalObject>();
            if (iso != null)
                if (r.materials.ContainsKey("IsometricDepthNormals") && r.renderer.enabled == true)
                    outlineDepthBuffer.DrawRenderer(r.renderer, r.materials["IsometricDepthNormals"], 0, pass);
        }

        foreach (Renderer r in _OutlineNullObjects)
        {
            if (r.enabled == true)
                outlineDepthBuffer.DrawRenderer(r, _nullMaterial, 0, -1);
        }
    }

    void DrawIsometricOutlineColor(CommandBuffer outlineColor, int pass)
    {
        foreach (UnigmaPostProcessingObjects r in _OutlineRenderObjects)
        {
            OutlineColor cr = r.gameObject.GetComponent<OutlineColor>();
            if (cr != null)
                if (cr.materials.ContainsKey("OutlineColors") && cr.renderer.enabled == true)
                    outlineColor.DrawRenderer(cr.renderer, cr.materials["OutlineColors"], 0, pass);
        }

        foreach (Renderer r in _OutlineNullObjects)
        {
            if (r.enabled == true)
                outlineColor.DrawRenderer(r, _nullMaterial, 0, -1);
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
                _OutlineRenderObjects.Add(sceneRenderers[i].gameObject.GetComponent(component) as UnigmaPostProcessingObjects);
            else
                _OutlineNullObjects.Add(sceneRenderers[i]);
    }
}
