using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class IsometricDepthNormals : MonoBehaviour
{
    public Material material;
    private CommandBuffer outlineBuffer = default;
    private int bufferAdd = 0;
    private List<GameObject> ObjectsWithOutlines;
    // Start is called before the first frame update
    void Start()
    {
        Camera cam = GetComponent<Camera>();
        ObjectsWithOutlines = new List<GameObject>(GameObject.FindGameObjectsWithTag("NormalObjects"));
        cam.depthTextureMode = cam.depthTextureMode | DepthTextureMode.DepthNormals;
    }

    // Update is called once per frame
    void Update()
    {
        
        if (bufferAdd < 1)
        {
            Debug.Log(ObjectsWithOutlines.Count);
            //Start a new command buffer.
            outlineBuffer = new CommandBuffer();
            //Give that buffer a name.
            outlineBuffer.name = "Outline Map Buffer";
            //ID to reference the buffer within the shader.
            int tempID = Shader.PropertyToID("_TempOutline");
            //Set the target properties.
            outlineBuffer.GetTemporaryRT(tempID, -1, -1, 24, FilterMode.Bilinear);
            outlineBuffer.SetRenderTarget(tempID);
            outlineBuffer.SetGlobalTexture("_OutlineMap", tempID);
            outlineBuffer.ClearRenderTarget(true, true, Color.black);
            DrawAllMeshes();
            GetComponent<Camera>().AddCommandBuffer(CameraEvent.AfterDepthNormalsTexture, outlineBuffer);
            bufferAdd += 1;
        }
    }

    void DrawAllMeshes()
    {
        GameObject obj = ObjectsWithOutlines[0];
        Renderer r = obj.GetComponent<Renderer>();
        outlineBuffer.DrawMesh(obj.GetComponent<MeshFilter>().mesh, obj.transform.localToWorldMatrix, r.material);
        /*
        foreach (GameObject obj in ObjectsWithOutlines)
        {
            if(!obj.activeSelf)
            {
                continue;
            }
            Renderer r = obj.GetComponent<Renderer>();

            outlineBuffer.DrawRenderer(r, r.material);
        }
        */
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(source, destination, material);
    }
}
