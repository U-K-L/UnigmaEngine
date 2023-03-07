using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

//To be attached to a camera.
public class ImageEffectPaintingStrokes : MonoBehaviour
{
    public Material finalMat;
    Camera cam;
    CommandBuffer strokeBuffer;
    public GameObject rock;
    Matrix4x4 matrix = Matrix4x4.identity;
    // Start is called before the first frame update
    void Start()
    {
        cam = GetComponent<Camera>();
        //Sets the bit values to allow depth normals and motion vectors.
        cam.depthTextureMode |= DepthTextureMode.DepthNormals;
        cam.depthTextureMode |= DepthTextureMode.MotionVectors;
        SetValues();
        //CreateBuffers(); //Create camera buffers.
    }

    void SetValues()
    {
        float fovY = cam.fieldOfView;
        float far = cam.farClipPlane;
        float y = cam.orthographic ? 2 * cam.orthographicSize : 2 * Mathf.Tan(fovY * Mathf.Deg2Rad * 0.5f) * far;
        float x = y * cam.aspect;
        Shader.SetGlobalVector("_FarCorner", new Vector3(x, y, far));
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {

        //int tempID = Shader.PropertyToID("_DepthMap");
        //int tempID2 = Shader.PropertyToID("_NormalMap");
        //depthBuffer.Blit(source, tempID, depthMat);
        /*
       depthBuffer.Blit(source, tempID2, materialNormal);
              */
        Matrix4x4 _CamToWorld = cam.cameraToWorldMatrix;
        finalMat.SetMatrix("_CamToWorld", _CamToWorld);


        Graphics.Blit(source, destination, finalMat);
    }


    void CreateBuffers()
    {
        //Start a new command buffer.
        strokeBuffer = new CommandBuffer();
        //Give that buffer a name.
        strokeBuffer.name = "_StrokesMap";
        //ID to reference the buffer within the shader.
        int tempID = Shader.PropertyToID("_StrokesMap");
        //Set the target properties.
        strokeBuffer.GetTemporaryRT(tempID, -1, -1, 24, FilterMode.Bilinear);
        strokeBuffer.SetRenderTarget(tempID);
        strokeBuffer.SetGlobalTexture("_StrokesMap", tempID);




        //Add it to the pipeline.

        cam.AddCommandBuffer(CameraEvent.AfterForwardOpaque, strokeBuffer);


        //Add object mesh data.
        strokeBuffer.ClearRenderTarget(true, true, Color.black);
        strokeBuffer.DrawMesh(rock.GetComponent<Mesh>(), matrix, rock.GetComponent<Material>(), 0, -1);



        //Depth buffer
        int depthID = Shader.PropertyToID("_DepthCopyTexture");
        strokeBuffer.GetTemporaryRT(depthID, cam.pixelWidth, cam.pixelHeight, 0, FilterMode.Point, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
        strokeBuffer.Blit(BuiltinRenderTextureType.Depth, depthID);
        strokeBuffer.SetGlobalTexture("_StrokeDepthBuffer", depthID);

        cam.AddCommandBuffer(CameraEvent.AfterSkybox, strokeBuffer);

    }
}
