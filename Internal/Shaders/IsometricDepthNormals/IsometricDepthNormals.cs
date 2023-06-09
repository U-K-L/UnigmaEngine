using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class IsometricDepthNormals : MonoBehaviour
{
    public Material material;
    private CommandBuffer outlineDepthBuffer = default;
    private CommandBuffer outlineNormalBuffer = default;
    private int bufferAdd = 0;
    private List<GameObject> ObjectsWithOutlines;
    private List<Renderer> NullOutlines = default;
    public Material depthMaterial;
    public Material normalMaterial;
    public Material nullMaterial;
    // Start is called before the first frame update
    void Start()
    {
        NullOutlines = new List<Renderer>();
        Camera cam = GetComponent<Camera>();
        ObjectsWithOutlines = new List<GameObject>(GameObject.FindGameObjectsWithTag("NormalObjects"));
        cam.depthTextureMode = cam.depthTextureMode | DepthTextureMode.Depth;
    }

    // Update is called once per frame
    void Update()
    {
        
        if (bufferAdd < 1)
        {
            /*
            outlineNormalBuffer = new CommandBuffer();
            RenderTexture rt = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);

            outlineNormalBuffer.SetGlobalTexture("_OutlineNormalMap", rt);
            outlineNormalBuffer.SetRenderTarget(rt);
            outlineNormalBuffer.get
            outlineNormalBuffer.ClearRenderTarget(true, true, Color.clear, 0);
            //outlineNormalBuffer.CopyTexture(rt, renderTargetBuffer);



            DrawAllNormalMeshes();
            outlineNormalBuffer.Blit(rt, BuiltinRenderTextureType.CameraTarget);
            GetComponent<Camera>().AddCommandBuffer(CameraEvent.AfterForwardOpaque, outlineNormalBuffer);
            */
            //Start a new command buffer.
            outlineNormalBuffer = new CommandBuffer();
            //Give that buffer a name.
            outlineNormalBuffer.name = "Outline Normal Buffer";
            int tempID = Shader.PropertyToID("_TempOutlineNormal");
            Debug.Log(tempID);
            //Set the target properties.
            outlineNormalBuffer.GetTemporaryRT(tempID, -1, -1, 24, FilterMode.Bilinear);
            outlineNormalBuffer.SetGlobalTexture("_OutlineNormalMap", tempID);
            outlineNormalBuffer.SetRenderTarget(tempID);
            

            //ID to reference the buffer within the shader.

            //outlineBuffer.CopyTexture(BuiltinRenderTextureType.Depth, tempID);
            outlineNormalBuffer.ClearRenderTarget(true, true, Color.black);

            DrawAllNormalMeshes();
            //outlineNormalBuffer.Blit(tempID, Shader.PropertyToID("_OutlineNormalMap"));
            outlineNormalBuffer.ReleaseTemporaryRT(tempID);
            GetComponent<Camera>().AddCommandBuffer(CameraEvent.AfterForwardOpaque, outlineNormalBuffer);
            bufferAdd += 1;
        }
        if (bufferAdd < 2)
        {

            Debug.Log(ObjectsWithOutlines.Count);
            outlineDepthBuffer = new CommandBuffer();
            int tempID2 = Shader.PropertyToID("_TempOutline");

            Debug.Log(tempID2);
            //Give that buffer a name.
            outlineDepthBuffer.name = "Outline Depth Buffer";
            //ID to reference the buffer within the shader.

            //Set the target properties.
            outlineDepthBuffer.GetTemporaryRT(tempID2, -1, -1, 24, FilterMode.Bilinear);

            //outlineBuffer.CopyTexture(BuiltinRenderTextureType.Depth, tempID);
            outlineDepthBuffer.SetGlobalTexture("_OutlineMap", tempID2);

            //outlineDepthBuffer.SetRenderTarget(tempID2);
            //Start a new command buffer.

            outlineDepthBuffer.ClearRenderTarget(true, true, Color.black);
            DrawAllDepthMeshes();
            GetComponent<Camera>().AddCommandBuffer(CameraEvent.AfterForwardOpaque, outlineDepthBuffer);
            
            bufferAdd += 1;
        }
    }

    void DrawAllNormalMeshes()
    {
        FindObjects();
        foreach (GameObject obj in ObjectsWithOutlines)
        {
            if (!obj.activeSelf)
            {
                continue;
            }
            Renderer r = obj.GetComponent<Renderer>();

            if (r.enabled == true)
                outlineNormalBuffer.DrawRenderer(r, normalMaterial, 0, -1);
        }

        foreach (Renderer r in NullOutlines)
        {
            if (r.enabled == true)
                outlineNormalBuffer.DrawRenderer(r, nullMaterial, 0, -1);
        }
    }

    void DrawAllDepthMeshes()
    {
        FindObjects();
        foreach (GameObject obj in ObjectsWithOutlines)
        {
            if(!obj.activeSelf)
            {
                continue;
            }
            Renderer r = obj.GetComponent<Renderer>();

            if(r.enabled == true)
                outlineDepthBuffer.DrawRenderer(r, depthMaterial, 0, -1);
        }

        foreach (Renderer r in NullOutlines)
        {
            if (r.enabled == true)
                outlineDepthBuffer.DrawRenderer(r, nullMaterial, 0, -1);
        }
    }

    // Find and store visible renderers to a list
    void FindObjects()
    {
        // Retrieve all renderers in scene
        Renderer[] sceneRenderers = FindObjectsOfType<Renderer>();

        // Store only visible renderers
        NullOutlines.Clear();
        for (int i = 0; i < sceneRenderers.Length; i++)
            if (sceneRenderers[i].gameObject.tag != "NormalObjects")
                NullOutlines.Add(sceneRenderers[i]);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(source, destination, material);
    }
}
