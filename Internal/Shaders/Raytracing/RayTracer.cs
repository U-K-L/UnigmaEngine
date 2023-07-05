using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class RayTracer : MonoBehaviour
{
    public ComputeShader RayTracingShader;
    public RayTracingShader RayTracingShaderAccelerated;
    private RenderTexture _target;
    private Vector2 _previousDimen = new Vector2(0, 0);
    private Camera _cam;
    public Texture skyBox;
    public float _textSizeDivision = 0;

    public LayerMask RayTracingLayers;

    public List<Renderer> _RayTracedObjects = new List<Renderer>();
    RayTracingAccelerationStructure _AccelerationStructure;

    int width, height = 0;

    //Structs for ray tracing.
    struct MeshObjects
    {
        public Matrix4x4 localToWorld;
        public int indicesOffset;
        public int indicesCount;
    }
    
    private List<MeshObjects> meshObjects = new List<MeshObjects>();
    private List<Vector3> Vertices = new List<Vector3>();
    private List<int> Indices = new List<int>();
    
    void Awake()
    {
        _cam = GetComponent<Camera>();

        //Create GPU accelerated structure.
        var settings = new RayTracingAccelerationStructure.RASSettings();
        settings.layerMask = RayTracingLayers;
        //Change this to manual after some work.
        settings.managementMode = RayTracingAccelerationStructure.ManagementMode.Automatic;
        settings.rayTracingModeMask = RayTracingAccelerationStructure.RayTracingModeMask.Everything;

        _AccelerationStructure = new RayTracingAccelerationStructure(settings);
    }

    // Update is called once per frame
    void Update()
    {
        //Builds the BVH (Bounding Volum Hierachy aka objects for ray to hit)
        _AccelerationStructure.Build();
    }

    void BuildTriangleList()
    {
        Vertices.Clear();
        Indices.Clear();

        foreach (Renderer r in _RayTracedObjects)
        {
            MeshFilter mf = r.GetComponent<MeshFilter>();
            if (mf)
            {
                Mesh m = mf.sharedMesh;
                int startVert = Vertices.Count;
                int startIndex = Indices.Count;

                Vertices.AddRange(m.vertices);
                var indices = m.GetIndices(0);
                Indices.AddRange(indices.Select(index => index + startVert));
            }
        }
    }

    void DispatchGPURayTrace()
    {
        RayTracingShader.SetTexture(0, "_RayTracer", _target);
        RayTracingShader.SetMatrix("_CameraToWorld", _cam.cameraToWorldMatrix);
        RayTracingShader.SetMatrix("_CameraInverseProjection", _cam.projectionMatrix.inverse);
        RayTracingShader.SetTexture(0, "_SkyBoxTexture", skyBox);

        int threadGroupsX = Mathf.CeilToInt(width / 32.0f);
        int threadGroupsY = Mathf.CeilToInt(height / 32.0f);
        RayTracingShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);
    }

    void DispatchAcceleratedRayTrace()
    {
        RayTracingShaderAccelerated.SetTexture("_RayTracedImage", _target);
        RayTracingShaderAccelerated.SetMatrix("_CameraToWorld", _cam.cameraToWorldMatrix);
        RayTracingShaderAccelerated.SetMatrix("_CameraInverseProjection", _cam.projectionMatrix.inverse);
        RayTracingShaderAccelerated.SetShaderPass("MyRaytraceShaderPass");
        RayTracingShaderAccelerated.SetAccelerationStructure("_RaytracingAccelerationStructure", _AccelerationStructure);
        
        RayTracingShaderAccelerated.Dispatch("MyRaygenShader", width, height, 1);
    }
    
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        width = Mathf.Max(Mathf.Min(Mathf.CeilToInt(Screen.width * (1.0f / (1.0f + Mathf.Abs(_textSizeDivision)))), Screen.width), 32);
        height = Mathf.Max(Mathf.Min(Mathf.CeilToInt(Screen.height * (1.0f / (1.0f + Mathf.Abs(_textSizeDivision)))), Screen.height), 32);
        InitializeRenderTexture(width, height);
        DispatchGPURayTrace();
        //DispatchAcceleratedRayTrace();

        Graphics.Blit(_target, destination);
    }

    void InitializeRenderTexture(int width, int height)
    {

        if (_target == null || _target.width != _previousDimen.x || _target.height != _previousDimen.y)
        {
            if (_target != null)
                _target.Release();
            RenderTexture rt = RenderTexture.GetTemporary(width,height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            _target = rt;
            _target.enableRandomWrite = true;
            _target.Create();
        }
        _previousDimen.x = width;
        _previousDimen.y = height;
    }
}
