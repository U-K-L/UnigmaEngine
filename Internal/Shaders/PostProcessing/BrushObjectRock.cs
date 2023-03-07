using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


public class BrushObjectRock : MonoBehaviour
{
    CommandBuffer strokeBuffer;
    private List<GameObject> rocks;
    Matrix4x4 matrix = Matrix4x4.identity;
    int bufferAdd = 0;
    public Material material;
    public Material finalMat;
    Camera cam;
    // Start is called before the first frame update
    void Start()
    {
        cam = GetComponent<Camera>();
        //Get all gameobjects on the crystlizedrocks layer
        rocks = new List<GameObject>(GameObject.FindGameObjectsWithTag("CrystalizedRocks"));
        cam.depthTextureMode |= DepthTextureMode.DepthNormals;
        cam.depthTextureMode |= DepthTextureMode.MotionVectors;
        SetValues();
    }


    void SetValues()
    {
        float fovY = cam.fieldOfView;
        float far = cam.farClipPlane;
        float y = cam.orthographic ? 2 * cam.orthographicSize : 2 * Mathf.Tan(fovY * Mathf.Deg2Rad * 0.5f) * far;
        float x = y * cam.aspect;
        Shader.SetGlobalVector("_FarCorner", new Vector3(x, y, far));
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
        material.SetMatrix("_CamToWorld", _CamToWorld);


        Graphics.Blit(source, destination, material);
    }
    
    // Update is called once per frame
    void Update()
    {
        //Each update drawMesh all the rocks.
        if (strokeBuffer != null)
        {
            //DrawAllMeshes();

        }

        if (bufferAdd < 1)
        {

            //Start a new command buffer.
            strokeBuffer = new CommandBuffer();
            //Give that buffer a name.
            strokeBuffer.name = "Stroke Map Buffer";
            //ID to reference the buffer within the shader.
            int tempID = Shader.PropertyToID("_Temp1");
            Debug.Log(tempID);
            //Set the target properties.
            strokeBuffer.GetTemporaryRT(tempID, -1, -1, 24, FilterMode.Bilinear);
            strokeBuffer.SetRenderTarget(tempID);
            strokeBuffer.SetGlobalTexture("_StrokesMap", tempID);
            strokeBuffer.ClearRenderTarget(true, true, Color.black);
            DrawAllMeshes();
            GetComponent<Camera>().AddCommandBuffer(CameraEvent.AfterForwardOpaque, strokeBuffer);
            bufferAdd += 1;
        }
    }

    
    void DrawAllMeshes()
    {
        //strokeBuffer.ClearRenderTarget(true, true, Color.black);
        foreach (GameObject rock in rocks)
        {
            Renderer r = rock.GetComponent<Renderer>();

            strokeBuffer.DrawRenderer(r, finalMat);
            //strokeBuffer.DrawMesh(rock.GetComponent<MeshFilter>().mesh, rock.transform.localToWorldMatrix, rock.GetComponent<Renderer>().material);
        }
    }
}
