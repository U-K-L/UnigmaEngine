using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


public class PaintingBlur : MonoBehaviour
{
    CommandBuffer paintingBuffer;
    private List<GameObject> objs;
    Matrix4x4 matrix = Matrix4x4.identity;
    int bufferAdd = 0;
    public Material material;
    public Material objectMat;
    // Start is called before the first frame update
    void Start()
    {
        //Get all gameobjects on the crystlizedrocks layer
        objs = new List<GameObject>(GameObject.FindGameObjectsWithTag("foilage"));

    }

    // Update is called once per frame
    void Update()
    {

        if (bufferAdd < 1)
        {

            //Start a new command buffer.
            paintingBuffer = new CommandBuffer();
            //Give that buffer a name.
            paintingBuffer.name = "Painting Map Buffer";
            //ID to reference the buffer within the shader.
            int tempID = Shader.PropertyToID("_Temp2");
            //Set the target properties.
            paintingBuffer.GetTemporaryRT(tempID, -1, -1, 24, FilterMode.Bilinear);
            paintingBuffer.SetRenderTarget(tempID);
            paintingBuffer.SetGlobalTexture("_PaintingMap", tempID);
            paintingBuffer.ClearRenderTarget(true, true, Color.black);
            DrawAllMeshes();
            GetComponent<Camera>().AddCommandBuffer(CameraEvent.AfterForwardOpaque, paintingBuffer);
            bufferAdd += 1;
        }
    }

    
    void DrawAllMeshes()
    {
        //strokeBuffer.ClearRenderTarget(true, true, Color.black);
        foreach (GameObject obj in objs)
        {
            Renderer r = obj.GetComponent<Renderer>();

            paintingBuffer.DrawRenderer(r, objectMat);
        }
    }



    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(source, destination, material);
    }

}
