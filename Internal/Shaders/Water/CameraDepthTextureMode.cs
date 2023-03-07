using UnityEngine;

public class CameraDepthTextureMode : MonoBehaviour
{
    [SerializeField]
    DepthTextureMode depthTextureMode;

    Matrix4x4 ortho;
    Matrix4x4 perp;
    public Camera cam;
    public void Start()
    {
        perp = Camera.main.projectionMatrix;
        ortho = cam.projectionMatrix;
        //Camera.main.projectionMatrix = cam.projectionMatrix;
        //Camera.main.ResetProjectionMatrix();
    }

    public void Update()
    {
        for(int i = 0; i < 4; i++)
        {

        }
        for (int i = 0; i < 4; i++)
        {
        }
        //Debug.Log(Camera.main.projectionMatrix);
        Matrix4x4 p = new Matrix4x4();
        p.SetRow(0,new Vector4(0.0735294f, 0.0000000f, 0.0000000f, 0.0000000f));
        p.SetRow(1, new Vector4(0.0000000f, 0.1307189f, 0.0000000f, 0.0000000f));
        p.SetRow(2, new Vector4(0.0000000f, 0.0000000f, -1.0000200f, -0.0200002f));
        p.SetRow(3, new Vector4(0.0000000f, 0.0000000f, -1.0000000f, 0.0000000f));
        Camera.main.projectionMatrix = p;

    }

    private void OnValidate()
    {
        SetCameraDepthTextureMode();
    }

    private void Awake()
    {
        SetCameraDepthTextureMode();
    }

    private void SetCameraDepthTextureMode()
    {
        GetComponent<Camera>().depthTextureMode = depthTextureMode;
    }
}