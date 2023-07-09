/* Handles gathering of data to dispatch to the GPU for ray tracing. To do so it must gather all applicable objects in the scene, store their vertex data 
 * and send this data to the GPU. A script must be supplied holding information about what the execute. In addition, once finished it must then
 * create a final texture to add to the screen. The textures created are stored and applied to the Unigma's G buffer.
 * 
 */
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class RayTracer : MonoBehaviour
{
    //Provides the script to be executed for computing the ray tracer. One is RTX the other is a fallback compute shader.
    private ComputeShader _RayTracingShader;
    private RayTracingShader _RayTracingShaderAccelerated;
    
    //The render texture has two stages one is an in progress image which may take multiple cycles to complete.
    //The other is a finalized target which is displayed. A single finalized target may be displayed for multiple frames before updating.
    private RenderTexture _finalizedTarget;
    private RenderTexture _inProgressTarget;

    //How long until the ray tracer is forced to update its image. By default this is set to roughly 15FPS.
    //15 FPS is the bare minimum for the human eye to believe in movement. We round it to 0.07 to ensure it reaches above said threshold.
    public float timeUntilNextUpdateDelta = 0.07f;
    public int taskDivisiblity = 1; //How many tasks to divide the ray tracer into per update (frame). When it finished an image it is witheld until _timeUntilNextUpdateDelta is finished.
    private bool _isInProgress = false; //Is the ray tracer currently in progress of rendering an image.
    public int textSizeDivision = 0; // (1 / t + 1) How much to divide the text size by. This lowers the resolution of the final image, but massively aids in performance.
    public int maxBounces = 1; //How many times a ray can bounce before it is terminated.

    //Dimensions of the texture. After being computed with division.
    private Vector2 _previousDimen = new Vector2(0, 0);
    private int _width, _height = 0;
    
    private Camera _cam;
    public Texture skyBox;
    
    //Items to add to the raytracer.
    public LayerMask RayTracingLayers;

    private List<Renderer> _RayTracedObjects = new List<Renderer>();
    RayTracingAccelerationStructure _AccelerationStructure;

    
    ComputeBuffer _meshObjectBuffer;
    ComputeBuffer _verticesObjectBuffer;
    ComputeBuffer _indicesObjectBuffer;

    //Structs for ray tracing.
    //Unsafe is required for the fixed array.
    unsafe struct MeshObject
    {
        public Matrix4x4 localToWorld;
        public int indicesOffset;
        public int indicesCount;
        public Vector3 position;
        public fixed float AABB[6]; //Xmax,xmin,ymax,ymin,zmax,zmin.
    }

    struct Vertex
    {
        public Vector3 position;
        public Vector3 normal;
        public Vector2 uv;
    };

    private List<MeshObject> meshObjects = new List<MeshObject>();
    private List<Vertex> Vertices = new List<Vertex>();
    private List<int> Indices = new List<int>();

    
    void Awake()
    {
        _cam = GetComponent<Camera>();
        AddObjectsToList();

        //Guard clause, ensure there are objects to ray trace.
        if (_RayTracedObjects.Count == 0)
        {
            Debug.LogWarning("No objects to ray trace. Please add objects to the RayTracingLayers.");
            return;
        }

        if (UnigmaSettings.GetIsRTXEnabled())
            CreateAcceleratedStructure();
        else
            CreateNonAcceleratedStructure();

    }

    void AddObjectsToList()
    {
        foreach (var obj in FindObjectsOfType<Renderer>())
        {
            //Check if object in the RaytracingLayers.
            if (((1 << obj.gameObject.layer) & RayTracingLayers) != 0)
            {
                _RayTracedObjects.Add(obj);
            }
        }
    }

    void CreateAcceleratedStructure()
    {
        if (_RayTracingShaderAccelerated == null)
            _RayTracingShaderAccelerated = Resources.Load<RayTracingShader>("AcceleratedRayTracer");
        
        //Create GPU accelerated structure.
        var settings = new RayTracingAccelerationStructure.RASSettings();
        settings.layerMask = RayTracingLayers;
        //Change this to manual after some work.
        settings.managementMode = RayTracingAccelerationStructure.ManagementMode.Automatic;
        settings.rayTracingModeMask = RayTracingAccelerationStructure.RayTracingModeMask.Everything;

        _AccelerationStructure = new RayTracingAccelerationStructure(settings);
    }

    void CreateNonAcceleratedStructure()
    {
        if (_RayTracingShader == null)
            _RayTracingShader = Resources.Load<ComputeShader>("RayTracer");
        BuildTriangleList();
    }

    void Update()
    {
        //Guard clause, ensure there are objects to ray trace.
        if (_RayTracedObjects.Count == 0)
        {
            Debug.LogWarning("No objects to ray trace. Please add objects to the RayTracingLayers.");
            return;
        }
        
        if (UnigmaSettings.GetIsRTXEnabled())
            UpdateAcceleratedRayTracer();
        else
            UpdateNonAcceleratedRayTracer();

    }

    void UpdateAcceleratedRayTracer()
    {
        //Builds the BVH (Bounding Volum Hierachy aka objects for ray to hit)
        _AccelerationStructure.Build();
    }

    void UpdateNonAcceleratedRayTracer()
    {
        //Build the BVH
        BuildBVH();
    }

    //To build the BVH we need to take all of the game objects in our raytracing list.
    //Afterwards place them in a tree with the root node containing all of the objects.
    //The bounding box is calculated by finding the min and max vertices for each axis.
    //If the ray intersects the box we search the triangles of that node, if not we traverse another node and ignore the children.
    void BuildBVH()
    {
        //First traverse through all of the objects and create their bounding boxes.

        //Get positions stored them to mesh objects.

        //Update position of mesh objects.
        for (int i = 0; i < _RayTracedObjects.Count; i++)
        {
            MeshObject meshobj = new MeshObject();
            meshobj.localToWorld = _RayTracedObjects[i].transform.localToWorldMatrix;
            meshobj.indicesOffset = meshObjects[i].indicesOffset;
            meshobj.indicesCount = meshObjects[i].indicesCount;
            meshobj.position = _RayTracedObjects[i].transform.position;
            meshObjects[i] = meshobj;
        }
        if (_meshObjectBuffer.count > 0)
        {
            _meshObjectBuffer.SetData(meshObjects);
            _RayTracingShader.SetBuffer(0, "_MeshObjects", _meshObjectBuffer);
        }

        _meshObjectBuffer.SetData(meshObjects);
        _RayTracingShader.SetBuffer(0, "_MeshObjects", _meshObjectBuffer);

    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        //Guard clause, ensure there are objects to ray trace.
        if (_RayTracedObjects.Count == 0)
        {
            Debug.LogWarning("No objects to ray trace. Please add objects to the RayTracingLayers.");
            return;
        }
        
        _width = Mathf.Max(Mathf.Min(Mathf.CeilToInt(Screen.width * (1.0f / (1.0f + Mathf.Abs(textSizeDivision)))), Screen.width), 32);
        _height = Mathf.Max(Mathf.Min(Mathf.CeilToInt(Screen.height * (1.0f / (1.0f + Mathf.Abs(textSizeDivision)))), Screen.height), 32);
        InitializeRenderTexture(_width, _height);

        if (UnigmaSettings.GetIsRTXEnabled())
            DispatchAcceleratedRayTrace();
        else
            DispatchGPURayTrace();

        Graphics.Blit(_inProgressTarget, destination);
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

                for (int i = 0; i < m.vertices.Length; i++)
                {
                    Vertex v = new Vertex();
                    v.position = m.vertices[i];
                    v.normal = m.normals[i];
                    v.uv = m.uv[i];
                    Vertices.Add(v);
                }
                var indices = m.GetIndices(0);
                Indices.AddRange(indices.Select(index => index + startVert));

                // Add the object itself
                meshObjects.Add(new MeshObject()
                {
                    localToWorld = r.transform.localToWorldMatrix,
                    indicesOffset = startIndex,
                    indicesCount = indices.Length
                });
            }
        }
        _meshObjectBuffer = new ComputeBuffer(meshObjects.Count, 108);
        _verticesObjectBuffer = new ComputeBuffer(Vertices.Count, 32);
        _indicesObjectBuffer = new ComputeBuffer(Indices.Count, 4);
        _verticesObjectBuffer.SetData(Vertices);
        _RayTracingShader.SetBuffer(0, "_Vertices", _verticesObjectBuffer);
        _indicesObjectBuffer.SetData(Indices);
        _RayTracingShader.SetBuffer(0, "_Indices", _indicesObjectBuffer);
    }
    void InitializeRenderTexture(int width, int height)
    {

        if (_inProgressTarget == null || _inProgressTarget.width != _previousDimen.x || _inProgressTarget.height != _previousDimen.y)
        {
            if (_inProgressTarget != null)
                _inProgressTarget.Release();
            RenderTexture rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            _inProgressTarget = rt;
            _inProgressTarget.enableRandomWrite = true;
            _inProgressTarget.Create();
        }
        _previousDimen.x = width;
        _previousDimen.y = height;
    }

    void DispatchGPURayTrace()
    {
        _RayTracingShader.SetTexture(0, "_RayTracer", _inProgressTarget);
        _RayTracingShader.SetMatrix("_CameraToWorld", _cam.cameraToWorldMatrix);
        _RayTracingShader.SetMatrix("_CameraInverseProjection", _cam.projectionMatrix.inverse);
        _RayTracingShader.SetTexture(0, "_SkyBoxTexture", skyBox);
        int threadGroupsX = Mathf.CeilToInt(_width / 32.0f);
        int threadGroupsY = Mathf.CeilToInt(_height / 32.0f);
        _RayTracingShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);
    }

    void DispatchAcceleratedRayTrace()
    {
        _RayTracingShaderAccelerated.SetTexture("_RayTracedImage", _inProgressTarget);
        _RayTracingShaderAccelerated.SetMatrix("_CameraToWorld", _cam.cameraToWorldMatrix);
        _RayTracingShaderAccelerated.SetMatrix("_CameraInverseProjection", _cam.projectionMatrix.inverse);
        _RayTracingShaderAccelerated.SetShaderPass("MyRaytraceShaderPass");
        _RayTracingShaderAccelerated.SetAccelerationStructure("_RaytracingAccelerationStructure", _AccelerationStructure);
        
        _RayTracingShaderAccelerated.Dispatch("MyRaygenShader", _width, _height, 1);
    }
}
