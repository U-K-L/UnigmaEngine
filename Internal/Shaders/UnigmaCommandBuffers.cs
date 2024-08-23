using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

public class UnigmaCommandBuffers : MonoBehaviour
{

    public bool RayTracingOn = true;
    private int buffersAdded = 0;
    private List<UnigmaPostProcessingObjects> _OutlineRenderObjects; //Objects part of this render.
    private List<Renderer> _OutlineNullObjects = default; //Objects not part of this render.
    private List<UnigmaWater> _WaterObjects = default; //Objects that consist of water.
    int _temporalReservoirsCount = 2;
    private Camera mainCam;
    public int ResolutionDivider = 0;
    public int RayTracingResolutionDivider = 0;
    private int _renderTextureWidth;
    private int _renderTextureHeight;
    private int _rayTracingRenderTextureWidth;
    private int _rayTracingRenderTextureHeight;
    private int _renderTextureWidthPrev;
    private int _renderTextureHeightPrev;

    public Texture2D SphericalMap;

    struct Sample
    {
        public Vector3 x0;
        public Vector3 x1;
        public Vector3 x2;
        public float weight;
    };

    struct Reservoir
    {
        public uint Y; //Index most important light.
        public float W; //light weight
        public float wSum; // weight summed.
        public float M; //Number of total lights for this reservoir.
        public float pHat;
        public Vector3 x1; //position of the hit point.
        public int age; //how many times used.
    };

    struct UnigmaLight
    {
        public Vector3 position;
        public float emission;
        public Vector3 area;
        public Vector3 color;
    };

    struct ReservoirPath
    {
        public float wSum; // weight summed.
        public float M; //Number of total lights for this reservoir.
        public Vector3 radiance;
        public Vector3 position;
        public Vector3 normal;

    };

    struct UnigmaDispatchInfo
    {
        public int FrameCount;
    };

    struct SVGF
    {
        public float history;
        public Vector2 moments;
    };

    //Structs for ray tracing.
    struct MeshObject
    {
        public Matrix4x4 localToWorld;
        public int indicesOffset;
        public int indicesCount;
        public Vector3 position;
        public Vector3 AABBMin;
        public Vector3 AABBMax;
        public Vector3 color;
        public float emission;
        public float smoothness;
        public float transparency;
        public float absorbtion;
        public float celShaded;
        public uint id;
    }

    struct Ray
    {
        public Vector3 o;
        public Vector3 d;
        public Vector3 color;
        public Vector3 energy;
        int bounces;
    };

    struct Vertex
    {
        public Vector3 position;
        public Vector3 normal;
        public Vector2 uv;
    };

    private List<MeshObject> meshObjects = new List<MeshObject>();
    private List<Vertex> Vertices = new List<Vertex>();
    private List<int> Indices = new List<int>();
    private List<Ray> _rays = new List<Ray>();

    int _reservoirStride = sizeof(float) * 6 + sizeof(float)*3;
    int _reservoirPathStride = sizeof(float) * 2 + sizeof(float) * 3*3;
    int _lightStride = sizeof(float) * 3*3 + sizeof(float);
    int _sampleStride = (sizeof(float) * 3) * 3 + sizeof(float);
    int _svgfStride = sizeof(float) + sizeof(float) * 2;

    ComputeBuffer samplesBuffer;
    ComputeBuffer lightsBuffer;
    ComputeBuffer reservoirsBuffer;
    ComputeBuffer reservoirPathsBuffer;
    ComputeBuffer unigmaDispatchInfoBuffer;
    ComputeBuffer svgfBuffer;
    //Provides the script to be executed for computing the ray tracer. One is RTX the other is a fallback compute shader.
    private ComputeShader _RayTracingShader;
    private ComputeShader _DepthShadowsNoRaytracer;
    ComputeBuffer _meshObjectBuffer;
    ComputeBuffer _verticesObjectBuffer;
    ComputeBuffer _indicesObjectBuffer;
    ComputeBuffer _rayBuffer;

    private List<Reservoir> reservoirs;
    private List<ReservoirPath> reservoirPaths;
    private List<Sample> samplesList;
    private List<UnigmaLight> lightList;
    private List<SVGF> svgfList;

    private ComputeShader computeOutlineColors;
    private ComputeShader svgfComputeShader;
    private CommandBuffer outlineDepthBuffer;
    private CommandBuffer backgroundColorBuffer;
    private CommandBuffer compositeBuffer;
    private Material _nullMaterial = default;

    private bool BuffersReady = false;

    public static int _UnigmaFrameCount;

    [SerializeField]
    Material rayTracingDepthShadowMaterial;

    private RayTracingShader _DepthShadowsRayTracingShaderAccelerated;
    private RayTracingShader _RestirGlobalIllumRayTracingShaderAccelerated;
    private RayTracingShader _RestirSpatialShaderAccelerated;
    RayTracingAccelerationStructure _AccelerationStructure;

    public LayerMask RayTracingLayers;
    public LayerMask OutlineLayers;
    RenderTexture _DepthShadowsTexture;
    RenderTexture _UnigmaScreenSpaceShadows;
    RenderTexture _ReflectionsTexture;
    RenderTexture _UnigmaGlobalIllumination;

    //Unigma G buffers albedo, normal and motion ID, global illumination
    RenderTexture _UnigmaAlbedo;
    RenderTexture _UnigmaNormal;
    RenderTexture _UnigmaMotionID;
    RenderTexture _UnigmaGlobalIlluminationTemporal;
    RenderTexture _UnigmaAlbedoTemporal;
    RenderTexture _UnigmaNormalTemporal;
    RenderTexture _UnigmaMotionIDTemporal;
    RenderTexture _UnigmaDenoisedGlobalIllumination;
    RenderTexture _UnigmaDenoisedGlobalIlluminationTemporal;
    RenderTexture _UnigmaDenoisedGlobalIlluminationTemp;
    RenderTexture _UnigmaSpecularLights;
    RenderTexture _UnigmaIdsTexture;
    RenderTexture _UnigmaWaterNormals;
    RenderTexture _UnigmaWaterPosition;
    RenderTexture _UnigmaWaterReflections;
    RenderTexture _UnigmaShadowColors;
    RenderTexture _UnigmaBackgroundColor;
    RenderTexture _UnigmaComposite;
    public Texture2D _UnigmaBlueNoise;

    private List<Renderer> _rayTracedObjects = default;
    public Camera secondCam;
    Material _unigmaBackgroundMaterial;

    public bool debugMode = false;

    void Awake()
    {
        if (RayTracingOn == true)
            UnigmaSettings.SetRaytracing();
        UnigmaSettings.Initialize();
    }
    // Start is called before the first frame update
    void Start()
    {

        _rayTracedObjects = new List<Renderer>();
        _unigmaBackgroundMaterial = Resources.Load<Material>("UnigmaBackgroundMaterial");
        //SetUpWallsAndObjects();
        UpdateRenderTextures();
        SetUpOutline();
    }


    void SetUpOutline()
    {
        _nullMaterial = new Material(Shader.Find("Unigma/IsometricNull"));
        computeOutlineColors = Resources.Load("OutlineColorsBoxBlur") as ComputeShader;

        Camera cam = GetComponent<Camera>();
        Camera.main.depthTextureMode = DepthTextureMode.MotionVectors;
        mainCam = Camera.main;
        cam.depthTextureMode = cam.depthTextureMode | DepthTextureMode.Depth;
        cam.depthTextureMode = cam.depthTextureMode | DepthTextureMode.DepthNormals;

        if (UnigmaSettings.GetIsRTXEnabled())
            SetUpGlobalIllumination();
    }

    void SetUpGlobalIllumination()
    {
        if (!UnigmaSettings.GetIsRTXEnabled())
            return;
        SetupReSTIR();
        CreateRayTracingAcceleratedStructure();
        Debug.Log("setup raytracing");
    }

    void SetupReSTIR()
    {
        if (!UnigmaSettings.GetIsRTXEnabled())
            return;
        samplesList = new List<Sample>();
        lightList = new List<UnigmaLight>();
        reservoirs = new List<Reservoir>();
        reservoirPaths = new List<ReservoirPath>();

        AddLightsToList();
        svgfList = new List<SVGF>();
        svgfComputeShader = Resources.Load("SVGF") as ComputeShader;
    }

    void CreateRayTracingAcceleratedStructure()
    {
        if (!UnigmaSettings.GetIsRTXEnabled())
            return;
        int amountOfSamples = _renderTextureWidth * _renderTextureHeight;

        for(int i = 0; i < amountOfSamples; i++)
        {
            Sample s = new Sample();
            s.x0 = Vector3.zero;
            s.x1 = Vector3.zero;
            s.x2 = Vector3.zero;
            s.weight = 0;

            samplesList.Add(s);

            SVGF svgf = new SVGF();
            svgf.history = 0;
            svgf.moments = Vector2.zero;

            svgfList.Add(svgf);

        }

        for (int j = 0; j < amountOfSamples * _temporalReservoirsCount; j++)
        {
            Reservoir r = new Reservoir();
            r.W = 0;
            r.wSum = 0;
            r.Y = 0;
            r.M = 0;
            r.x1 = Vector3.zero;
            r.age = 0;

            reservoirs.Add(r);

            ReservoirPath rp = new ReservoirPath();
            rp.wSum = 0;
            rp.M = 0;
            rp.radiance = Vector3.zero;
            rp.normal = Vector3.zero;

            reservoirPaths.Add(rp);
        }
        

        samplesBuffer = new ComputeBuffer(amountOfSamples, _sampleStride);
        samplesBuffer.SetData(samplesList);

        reservoirsBuffer = new ComputeBuffer(amountOfSamples * _temporalReservoirsCount, _reservoirStride);
        reservoirsBuffer.SetData(reservoirs);

        reservoirPathsBuffer = new ComputeBuffer(amountOfSamples * _temporalReservoirsCount, _reservoirPathStride);
        reservoirPathsBuffer.SetData(reservoirPaths);

        svgfBuffer = new ComputeBuffer(amountOfSamples, _svgfStride);
        svgfBuffer.SetData(svgfList);

        if (_DepthShadowsRayTracingShaderAccelerated == null)
            _DepthShadowsRayTracingShaderAccelerated = Resources.Load<RayTracingShader>("DepthShadowsRaytracer");

        if (_RestirGlobalIllumRayTracingShaderAccelerated == null)
            _RestirGlobalIllumRayTracingShaderAccelerated = Resources.Load<RayTracingShader>("ReStirGlobalIllumination");
        if (_RestirSpatialShaderAccelerated == null)
            _RestirSpatialShaderAccelerated = Resources.Load<RayTracingShader>("ReSTIRSpatialReuse");

        //Create GPU accelerated structure.
        var settings = new RayTracingAccelerationStructure.RASSettings();
        //settings.layerMask = RayTracingLayers;
        //Change this to manual after some work.
        settings.managementMode = RayTracingAccelerationStructure.ManagementMode.Manual;
        settings.rayTracingModeMask = RayTracingAccelerationStructure.RayTracingModeMask.Everything;

        _AccelerationStructure = new RayTracingAccelerationStructure(settings);

    }

    void CreateNonAcceleratedStructure()
    {
        if (_RayTracingShader == null)
            _RayTracingShader = Resources.Load<ComputeShader>("RayTracer");
        BuildTriangleList();
    }

    //To build the BVH we need to take all of the game objects in our raytracing list.
    //Afterwards place them in a tree with the root node containing all of the objects.
    //The bounding box is calculated by finding the min and max vertices for each axis.
    //If the ray intersects the box we search the triangles of that node, if not we traverse another node and ignore the children.
    unsafe void BuildBVH()
    {
        //First traverse through all of the objects and create their bounding boxes.

        //Get positions stored them to mesh objects.
        int kernelId = _RayTracingShader.FindKernel("DepthShadowsReflection");

        //Update position of mesh objects.
        for (int i = 0; i < _rayTracedObjects.Count; i++)
        {
            
            MeshObject meshobj = new MeshObject();
            Renderer rto = _rayTracedObjects[i];
            if (!rto.GetComponent<MeshRenderer>().enabled)
                continue;
            meshobj.localToWorld = _rayTracedObjects[i].GetComponent<Renderer>().localToWorldMatrix;
            meshobj.indicesOffset = meshObjects[i].indicesOffset;
            meshobj.indicesCount = meshObjects[i].indicesCount;
            meshobj.position = _rayTracedObjects[i].transform.position;
            meshobj.AABBMin = _rayTracedObjects[i].bounds.min;
            meshobj.AABBMax = _rayTracedObjects[i].bounds.max;
            meshobj.id = (uint)i;
            if (rto)
            {
                Color color = rto.material.GetColor("_MainColor");
                meshobj.color = new Vector3(color.r, color.g, color.b);//new Vector3(rto.color.r, rto.color.g, rto.color.b);
                meshobj.emission = 1;//rto.material.GetFloat("_Emmittance");
                meshobj.smoothness = 1;//rto.material.GetFloat("_Smoothness");
                meshobj.transparency = color.a;
                meshobj.absorbtion = 0;//rto.material.GetFloat("_LightAbsorbtion");
                meshobj.celShaded = 0;
            }
            meshObjects[i] = meshobj;
        }
        if (_meshObjectBuffer.count > 0)
        {
            _meshObjectBuffer.SetData(meshObjects);
            _RayTracingShader.SetBuffer(kernelId, "_MeshObjects", _meshObjectBuffer);
        }

        _meshObjectBuffer.SetData(meshObjects);
        _RayTracingShader.SetBuffer(kernelId, "_MeshObjects", _meshObjectBuffer);

    }

    void BuildTriangleList()
    {
        Vertices.Clear();
        Indices.Clear();
        int kernelId = _RayTracingShader.FindKernel("DepthShadowsReflection");
        foreach (Renderer r in _rayTracedObjects)
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
        _meshObjectBuffer = new ComputeBuffer(meshObjects.Count, 144);
        _verticesObjectBuffer = new ComputeBuffer(Vertices.Count, 32);
        _indicesObjectBuffer = new ComputeBuffer(Indices.Count, 4);
        _verticesObjectBuffer.SetData(Vertices);
        _RayTracingShader.SetBuffer(kernelId, "_Vertices", _verticesObjectBuffer);
        _indicesObjectBuffer.SetData(Indices);
        _RayTracingShader.SetBuffer(kernelId, "_Indices", _indicesObjectBuffer);
    }
    
    void UpdateScreenResolution()
    {
        _renderTextureWidth = Mathf.Max(Mathf.Min(Mathf.CeilToInt(Screen.width * (1.0f / (1.0f + Mathf.Abs(ResolutionDivider)))), Screen.width), 32);
        _renderTextureHeight = Mathf.Max(Mathf.Min(Mathf.CeilToInt(Screen.height * (1.0f / (1.0f + Mathf.Abs(ResolutionDivider)))), Screen.height), 32);
        _rayTracingRenderTextureWidth = Mathf.Max(Mathf.Min(Mathf.CeilToInt(Screen.width * (1.0f / (1.0f + Mathf.Abs(RayTracingResolutionDivider)))), Screen.width), 32);
        _rayTracingRenderTextureHeight = Mathf.Max(Mathf.Min(Mathf.CeilToInt(Screen.height * (1.0f / (1.0f + Mathf.Abs(RayTracingResolutionDivider)))), Screen.height), 32);
    }

    // Update is called once per frame
    void LateUpdate()
    {
        UpdateScreenResolution();
        UpdateCameraVP();
        UpdateRayTracedObjects();
        AddLightsToList();
        AddCommandBuffers();
        UpdateRenderTextures();
        SaveRenderTexture();
    }

    void UpdateRayTracedObjects()
    {
        if (!UnigmaSettings.GetIsRTXEnabled())
            return;
        foreach (Renderer r in _rayTracedObjects)
        {
            uint stencilValue = 0;
            if (r.material.HasInt("_StencilRef"))
            {
                stencilValue = (uint)r.material.GetInt("_StencilRef");
                if (stencilValue == 0)
                    stencilValue = 255;
            }
            _AccelerationStructure.UpdateInstanceTransform(r);
            _AccelerationStructure.UpdateInstanceMask(r, stencilValue);
        }
    }

    void AddCommandBuffers()
    {
        //Need command buffers in specific ordering.
        if (BuffersReady == false)
        {
            BuffersReady = true;
            CreateBackground();
            return;
        }
        if (buffersAdded < 1)
        {
            
            //Create isometric depth normals.
            _OutlineRenderObjects = new List<UnigmaPostProcessingObjects>();
            _OutlineNullObjects = new List<Renderer>();
            //Create Water Objects.
            _WaterObjects = new List<UnigmaWater>();
            FindObjects("IsometricDepthNormalObject");
            FindWaterObjects();
            AddObjectsToList();

            AddObjectsToAccerleration();

            CreateDepthNormalBuffers();

            if (UnigmaSettings.GetIsRTXEnabled())
                CreateDepthShadowBuffers();
            else if (UnigmaSettings.GetIsRayTracingEnabled())
                CreateDepthShadowBuffersNonAccelerated();
            CreateDenoisedGlobalIllumination();


            buffersAdded += 1;
        }

        if (buffersAdded < 2)
        {
            CreateOutlineColorBuffers();
            CreateCompositeBuffer();

            buffersAdded += 1;
        }
    }

    void UpdateCameraVP()
    {
        Matrix4x4 VP = GL.GetGPUProjectionMatrix(secondCam.projectionMatrix, true) * Camera.main.worldToCameraMatrix;
        Shader.SetGlobalMatrix("_Perspective_Matrix_VP", VP);
    }

    void UpdateRenderTextures()
    {

        //Check if screen size changed, if so update the texture.
        _renderTextureWidth = Mathf.Max(Mathf.Min(Mathf.CeilToInt(Screen.width * (1.0f / (1.0f + Mathf.Abs(ResolutionDivider)))), Screen.width), 32);
        _renderTextureHeight = Mathf.Max(Mathf.Min(Mathf.CeilToInt(Screen.height * (1.0f / (1.0f + Mathf.Abs(ResolutionDivider)))), Screen.height), 32);
        _rayTracingRenderTextureWidth = Mathf.Max(Mathf.Min(Mathf.CeilToInt(Screen.width * (1.0f / (1.0f + Mathf.Abs(RayTracingResolutionDivider)))), Screen.width), 32);
        _rayTracingRenderTextureHeight = Mathf.Max(Mathf.Min(Mathf.CeilToInt(Screen.height * (1.0f / (1.0f + Mathf.Abs(RayTracingResolutionDivider)))), Screen.height), 32);

        if (_renderTextureWidth != _renderTextureWidthPrev || _renderTextureHeight != _renderTextureHeightPrev)
        {
            CreateRenderTextures();
        }
    }

    void CreateRenderTextures()
    {
        //check if already exists, if so release it.
        ReleaseRenderTextures();
        CreateGlobalIlluminationTextures();


        _UnigmaBackgroundColor = new RenderTexture(_renderTextureWidth, _renderTextureHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        _UnigmaBackgroundColor.enableRandomWrite = true;
        _UnigmaBackgroundColor.Create();

        _UnigmaComposite = new RenderTexture(_renderTextureWidth, _renderTextureHeight, 0, RenderTextureFormat.DefaultHDR, RenderTextureReadWrite.Linear);
        _UnigmaComposite.enableRandomWrite = true;
        _UnigmaComposite.Create();

        _DepthShadowsTexture = new RenderTexture(_rayTracingRenderTextureWidth, _rayTracingRenderTextureHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        _DepthShadowsTexture.enableRandomWrite = true;
        _DepthShadowsTexture.Create();

        _UnigmaShadowColors = new RenderTexture(_rayTracingRenderTextureWidth, _rayTracingRenderTextureHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        _UnigmaShadowColors.enableRandomWrite = true;
        _UnigmaShadowColors.Create();

        _UnigmaMotionID = new RenderTexture(_renderTextureWidth, _renderTextureHeight, 32, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        _UnigmaMotionID.enableRandomWrite = true;
        _UnigmaMotionID.Create();

        if (UnigmaSettings.GetIsRTXEnabled() || UnigmaSettings.GetIsRayTracingEnabled())
        {

            _ReflectionsTexture = new RenderTexture(_rayTracingRenderTextureWidth, _rayTracingRenderTextureHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            _ReflectionsTexture.enableRandomWrite = true;
            _ReflectionsTexture.Create();
        }


        _UnigmaNormal = new RenderTexture(_renderTextureWidth, _renderTextureHeight, 32, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        _UnigmaNormal.enableRandomWrite = true;
        _UnigmaNormal.Create();

        _UnigmaSpecularLights = new RenderTexture(_renderTextureWidth, _renderTextureHeight, 32, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        _UnigmaSpecularLights.enableRandomWrite = true;
        _UnigmaSpecularLights.Create();

        _UnigmaIdsTexture = new RenderTexture(_renderTextureWidth, _renderTextureHeight, 32, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        _UnigmaIdsTexture.enableRandomWrite = true;
        _UnigmaIdsTexture.Create();

        _renderTextureWidthPrev = _renderTextureWidth;
        _renderTextureHeightPrev = _renderTextureHeight;
    }

    void CreateGlobalIlluminationTextures()
    {
        if (!UnigmaSettings.GetIsRTXEnabled())
            return;

        _UnigmaAlbedoTemporal = new RenderTexture(_renderTextureWidth, _renderTextureHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        _UnigmaAlbedoTemporal.enableRandomWrite = true;
        _UnigmaAlbedoTemporal.Create();

        _UnigmaNormalTemporal = new RenderTexture(_renderTextureWidth, _renderTextureHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        _UnigmaNormalTemporal.enableRandomWrite = true;
        _UnigmaNormalTemporal.Create();

        _UnigmaMotionIDTemporal = new RenderTexture(_renderTextureWidth, _renderTextureHeight, 16, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        _UnigmaMotionIDTemporal.enableRandomWrite = true;
        _UnigmaMotionIDTemporal.Create();
        
        _UnigmaAlbedo = new RenderTexture(_renderTextureWidth, _renderTextureHeight, 16, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        _UnigmaAlbedo.enableRandomWrite = true;
        _UnigmaAlbedo.Create();

        _UnigmaGlobalIllumination = new RenderTexture(_renderTextureWidth, _renderTextureHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        _UnigmaGlobalIllumination.enableRandomWrite = true;
        _UnigmaGlobalIllumination.Create();

        _UnigmaGlobalIlluminationTemporal = new RenderTexture(_renderTextureWidth, _renderTextureHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        _UnigmaGlobalIlluminationTemporal.enableRandomWrite = true;
        _UnigmaGlobalIlluminationTemporal.Create();

        _UnigmaDenoisedGlobalIllumination = new RenderTexture(_renderTextureWidth, _renderTextureHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        _UnigmaDenoisedGlobalIllumination.enableRandomWrite = true;
        _UnigmaDenoisedGlobalIllumination.Create();

        _UnigmaDenoisedGlobalIlluminationTemporal = new RenderTexture(_renderTextureWidth, _renderTextureHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        _UnigmaDenoisedGlobalIlluminationTemporal.enableRandomWrite = true;
        _UnigmaDenoisedGlobalIlluminationTemporal.Create();

        _UnigmaDenoisedGlobalIlluminationTemp = new RenderTexture(_renderTextureWidth, _renderTextureHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        _UnigmaDenoisedGlobalIlluminationTemp.enableRandomWrite = true;
        _UnigmaDenoisedGlobalIlluminationTemp.Create();

        /*
        _UnigmaWaterNormals = new RenderTexture(_renderTextureWidth, _renderTextureHeight, 16, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        _UnigmaWaterNormals.enableRandomWrite = true;
        _UnigmaWaterNormals.Create();

        _UnigmaWaterPosition = new RenderTexture(_renderTextureWidth, _renderTextureHeight, 16, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        _UnigmaWaterPosition.enableRandomWrite = true;
        _UnigmaWaterPosition.Create();
        */

    }

    void ReleaseRenderTextures()
    {
        //Check if already exists if so release textures.
        if (_DepthShadowsTexture != null)
        {
            _DepthShadowsTexture.Release();
            _DepthShadowsTexture = null;
        }
        if (_ReflectionsTexture != null)
        {
            _ReflectionsTexture.Release();
            _ReflectionsTexture = null;
        }
        if (_UnigmaGlobalIllumination != null)
        {
            _UnigmaGlobalIllumination.Release();
            _UnigmaGlobalIllumination = null;
        }
        if (_UnigmaAlbedo != null)
        {
            _UnigmaAlbedo.Release();
            _UnigmaAlbedo = null;
        }
        if (_UnigmaNormal != null)
        {
            _UnigmaNormal.Release();
            _UnigmaNormal = null;
        }
        if (_UnigmaGlobalIlluminationTemporal != null)
        {
            _UnigmaGlobalIlluminationTemporal.Release();
            _UnigmaGlobalIlluminationTemporal = null;
        }
        if (_UnigmaDenoisedGlobalIllumination != null)
        {
            _UnigmaDenoisedGlobalIllumination.Release();
            _UnigmaDenoisedGlobalIllumination = null;
        }
        if (_UnigmaDenoisedGlobalIlluminationTemporal != null)
        {
            _UnigmaDenoisedGlobalIlluminationTemporal.Release();
            _UnigmaDenoisedGlobalIlluminationTemporal = null;
        }
        if (_UnigmaAlbedoTemporal != null)
        {
            _UnigmaAlbedoTemporal.Release();
            _UnigmaAlbedoTemporal = null;
        }
        if (_UnigmaNormalTemporal != null)
        {
            _UnigmaNormalTemporal.Release();
            _UnigmaNormalTemporal = null;
        }
        if (_UnigmaMotionID != null)
        {
            _UnigmaMotionID.Release();
            _UnigmaMotionID = null;
        }
        if (_UnigmaMotionIDTemporal != null)
        {
            _UnigmaMotionIDTemporal.Release();
            _UnigmaMotionIDTemporal = null;
        }
        if (_UnigmaDenoisedGlobalIlluminationTemp != null)
        {
            _UnigmaDenoisedGlobalIlluminationTemp.Release();
            _UnigmaDenoisedGlobalIlluminationTemp = null;
        }
        if (_UnigmaSpecularLights != null)
        {
            _UnigmaSpecularLights.Release();
            _UnigmaSpecularLights = null;
        }
        if (_UnigmaIdsTexture != null)
        {
            _UnigmaIdsTexture.Release();
            _UnigmaIdsTexture = null;
        }
        if (_UnigmaWaterNormals != null)
        {
            _UnigmaWaterNormals.Release();
            _UnigmaWaterNormals = null;
        }
        if (_UnigmaWaterPosition != null)
        {
            _UnigmaWaterPosition.Release();
            _UnigmaWaterPosition = null;
        }
        if (_UnigmaShadowColors != null)
        {
            _UnigmaShadowColors.Release();
            _UnigmaShadowColors = null;
        }
        if (_UnigmaBackgroundColor != null)
        {
            _UnigmaBackgroundColor.Release();
            _UnigmaBackgroundColor = null;
        }
        if (_UnigmaComposite != null)
        {
            _UnigmaComposite.Release();
            _UnigmaComposite = null;
        }

    }

    void AddObjectsToList()
    {
        foreach (var obj in FindObjectsOfType<Renderer>())
        {
            //Check if object in the RaytracingLayers.
            if (((1 << obj.gameObject.layer) & RayTracingLayers) != 0)
            {
                _rayTracedObjects.Add(obj);
                //Debug.Log(obj.name);
            }
        }
    }

    void AddLightsToList()
    {
        if (!UnigmaSettings.GetIsRTXEnabled())
            return;
        if (lightList != null)
            lightList.Clear();
        else
            return;
        int index = 0;
        foreach (Renderer obj in FindObjectsOfType<Renderer>())
        {
            
            //Check if object in the RaytracingLayers.
            if (((1 << obj.gameObject.layer) & RayTracingLayers) != 0)
            {
                if (!obj.material.HasFloat("_Emmittance"))
                    continue;
                if (obj.material.GetFloat("_Emmittance") > 0.01)
                {
                    UnigmaLight ulight = new UnigmaLight();
                    ulight.position = obj.GetComponent<BoxCollider>().bounds.min;
                    Vector3 minBox = obj.GetComponent<BoxCollider>().bounds.min;
                    Vector3 maxBox = obj.GetComponent<BoxCollider>().bounds.max;
                    //Area
                    Vector3 areaBox = new Vector3(Mathf.Abs(maxBox.x - minBox.x), Mathf.Abs(maxBox.y - minBox.y), Mathf.Abs(maxBox.z - minBox.z));
                    ulight.emission = obj.material.GetFloat("_Emmittance");
                    ulight.area = areaBox;//obj.transform.localScale;
                    Vector4 color = obj.material.GetColor("_MainColor"); 
                    ulight.color = new Vector3(color.x, color.y, color.z);
                    //Debug.Log("Light: " + index + " : " + obj.name);
                    index += 1;
                    lightList.Add(ulight);
                }
            }
        }

        /*
        //Add spherical map.
        int width = SphericalMap.width;
        int height = SphericalMap.height;
        for (int i = 0; i < width; i++)
            for (int j = 0; j < height; j++)
            {
                float u = (float)i / (float)width;
                float v = (float)j / (float)height;
                UnigmaLight ulight = new UnigmaLight();
                Vector4 color = SphericalMap.GetPixel(i, j);
                ulight.color = new Vector3(color.x, color.y, color.z);
                ulight.position = SphericalMapping(new Vector2(u, v), 10);
                ulight.emission = Vector3.Magnitude(color);
                ulight.area = new Vector3(1, 1, 1);
                lightList.Add(ulight);
            }

        //Debug Light List
        for (int i = 0; i < lightList.Count; i++)
        {
            Debug.Log("Light: " + i + " : " + lightList[i].position);
        }
        */
        if (lightsBuffer == null)
            lightsBuffer = new ComputeBuffer(Mathf.Max(lightList.Count, 1), _lightStride);
        lightsBuffer.SetData(lightList);
    }

    void AddObjectsToAccerleration()
    {
        if (!UnigmaSettings.GetIsRTXEnabled())
            return;
        uint index = 0;
        foreach (Renderer r in _rayTracedObjects)
        {
            uint stencilValue = 0;
            if (r.material.HasInt("_StencilRef"))
            {
                stencilValue = (uint)r.material.GetInt("_StencilRef");
                if (stencilValue == 0)
                {
                    stencilValue = 255;
                }
            }
            _AccelerationStructure.AddInstance(r, id:index, mask:stencilValue);
            _AccelerationStructure.UpdateInstanceTransform(r); 
            index++;
        }
    }

    void CreateDepthShadowBuffers()
    {
        if (!UnigmaSettings.GetIsRTXEnabled())
            return;
        CommandBuffer depthShadowsCommandBuffer = new CommandBuffer();
        depthShadowsCommandBuffer.name = "DepthShadowsBuffer";

        depthShadowsCommandBuffer.BuildRayTracingAccelerationStructure(_AccelerationStructure);

        depthShadowsCommandBuffer.SetGlobalTexture("_UnigmaDepthShadowsMap", _DepthShadowsTexture);
        depthShadowsCommandBuffer.SetGlobalTexture("_UnigmaShadowColors", _UnigmaShadowColors);
        depthShadowsCommandBuffer.SetGlobalTexture("_UnigmaDepthReflectionsMap", _ReflectionsTexture);
        depthShadowsCommandBuffer.SetRenderTarget(_DepthShadowsTexture);
        depthShadowsCommandBuffer.ClearRenderTarget(true, true, new Vector4(0,0,0,0));
        depthShadowsCommandBuffer.SetRayTracingTextureParam(_DepthShadowsRayTracingShaderAccelerated, "_UnigmaDepthShadowsMap", _DepthShadowsTexture);
        depthShadowsCommandBuffer.SetRayTracingTextureParam(_DepthShadowsRayTracingShaderAccelerated, "_UnigmaShadowColors", _UnigmaShadowColors);
        //depthShadowsCommandBuffer.SetBufferData(lightsBuffer, lightList);

        depthShadowsCommandBuffer.SetRayTracingShaderPass(_DepthShadowsRayTracingShaderAccelerated, "DepthShadowsRaytracingShaderPass");
        depthShadowsCommandBuffer.SetRayTracingBufferParam(_DepthShadowsRayTracingShaderAccelerated, "_Samples", samplesBuffer);
        depthShadowsCommandBuffer.SetRayTracingAccelerationStructure(_DepthShadowsRayTracingShaderAccelerated, "_RaytracingAccelerationStructure", _AccelerationStructure);
        depthShadowsCommandBuffer.DispatchRays(_DepthShadowsRayTracingShaderAccelerated, "DepthShadowsRaygenShader", (uint)_renderTextureWidth, (uint)_renderTextureHeight, 1);

        depthShadowsCommandBuffer.SetGlobalTexture("_UnigmaGlobalIllumination", _UnigmaGlobalIllumination);
        depthShadowsCommandBuffer.SetRenderTarget(_UnigmaGlobalIllumination);
        //depthShadowsCommandBuffer.ClearRenderTarget(true, true, new Vector4(0, 0, 0, 0));

        //First ReSTIR pass.
        depthShadowsCommandBuffer.SetRayTracingBufferParam(_RestirGlobalIllumRayTracingShaderAccelerated, "_samples", samplesBuffer);
        depthShadowsCommandBuffer.SetRayTracingBufferParam(_RestirGlobalIllumRayTracingShaderAccelerated, "_unigmaLights", lightsBuffer);
        depthShadowsCommandBuffer.SetRayTracingBufferParam(_RestirGlobalIllumRayTracingShaderAccelerated, "_reservoirs", reservoirsBuffer);
        depthShadowsCommandBuffer.SetRayTracingBufferParam(_RestirGlobalIllumRayTracingShaderAccelerated, "_reservoirPaths", reservoirPathsBuffer);

        depthShadowsCommandBuffer.SetRayTracingIntParam(_RestirGlobalIllumRayTracingShaderAccelerated, "_NumberOfLights", lightList.Count);
        depthShadowsCommandBuffer.SetRayTracingIntParam(_RestirGlobalIllumRayTracingShaderAccelerated, "_TemporalReservoirsCount", _temporalReservoirsCount);
        depthShadowsCommandBuffer.SetRayTracingTextureParam(_RestirGlobalIllumRayTracingShaderAccelerated, "_GlobalIllumination", _DepthShadowsTexture);
        depthShadowsCommandBuffer.SetRayTracingTextureParam(_RestirGlobalIllumRayTracingShaderAccelerated, "_CameraMotionVectorsTextureReSTIR", Shader.GetGlobalTexture("_CameraMotionVectorsTexture"));
        depthShadowsCommandBuffer.SetRayTracingTextureParam(_RestirGlobalIllumRayTracingShaderAccelerated, "_UnigmaDepthShadowsMap", _DepthShadowsTexture);

        //depthShadowsCommandBuffer.SetRayTracingIntParam(_RestirGlobalIllumRayTracingShaderAccelerated, "_UnigmaFrameCount", _UnigmaFrameCount);
        depthShadowsCommandBuffer.SetRayTracingAccelerationStructure(_RestirGlobalIllumRayTracingShaderAccelerated, "_RaytracingAccelerationStructure", _AccelerationStructure);
        depthShadowsCommandBuffer.SetRayTracingShaderPass(_RestirGlobalIllumRayTracingShaderAccelerated, "GlobalIlluminationRaytracingShaderPass");
        

        //Reusepass
        depthShadowsCommandBuffer.SetRayTracingBufferParam(_RestirSpatialShaderAccelerated, "_samples", samplesBuffer);
        depthShadowsCommandBuffer.SetRayTracingBufferParam(_RestirSpatialShaderAccelerated, "_unigmaLights", lightsBuffer);
        depthShadowsCommandBuffer.SetRayTracingBufferParam(_RestirSpatialShaderAccelerated, "_reservoirPaths", reservoirPathsBuffer);
        depthShadowsCommandBuffer.SetRayTracingBufferParam(_RestirSpatialShaderAccelerated, "_reservoirs", reservoirsBuffer);

        depthShadowsCommandBuffer.SetRayTracingIntParam(_RestirSpatialShaderAccelerated, "_NumberOfLights", lightList.Count);
        depthShadowsCommandBuffer.SetRayTracingIntParam(_RestirSpatialShaderAccelerated, "_TemporalReservoirsCount", _temporalReservoirsCount);
        depthShadowsCommandBuffer.SetRayTracingTextureParam(_RestirSpatialShaderAccelerated, "_GlobalIllumination", _DepthShadowsTexture);
        depthShadowsCommandBuffer.SetRayTracingTextureParam(_RestirSpatialShaderAccelerated, "_CameraMotionVectorsTextureReSTIR", Shader.GetGlobalTexture("_CameraMotionVectorsTexture"));
        depthShadowsCommandBuffer.SetRayTracingTextureParam(_RestirSpatialShaderAccelerated, "_UnigmaDepthShadowsMap", _DepthShadowsTexture);


        //depthShadowsCommandBuffer.SetRayTracingIntParam(_RestirGlobalIllumRayTracingShaderAccelerated, "_UnigmaFrameCount", _UnigmaFrameCount);
        depthShadowsCommandBuffer.SetRayTracingAccelerationStructure(_RestirSpatialShaderAccelerated, "_RaytracingAccelerationStructure", _AccelerationStructure);
        depthShadowsCommandBuffer.SetRayTracingShaderPass(_RestirSpatialShaderAccelerated, "GlobalIlluminationRaytracingShaderPass");

        
        //Dispatch
        int passCount = 1;
        for (int i = 0; i < passCount; i++)
        {
            depthShadowsCommandBuffer.SetRayTracingIntParam(_RestirGlobalIllumRayTracingShaderAccelerated, "_PassCount", i);
            depthShadowsCommandBuffer.SetRayTracingIntParam(_RestirSpatialShaderAccelerated, "_PassCount", i);
            depthShadowsCommandBuffer.DispatchRays(_RestirGlobalIllumRayTracingShaderAccelerated, "RestirGlobalIllumantionRayGen", (uint)_renderTextureWidth, (uint)_renderTextureHeight, 1);
            depthShadowsCommandBuffer.DispatchRays(_RestirSpatialShaderAccelerated, "RestirGlobalIllumantionRayGen", (uint)_renderTextureWidth, (uint)_renderTextureHeight, 1);
        }
        //depthShadowsCommandBuffer.DispatchCompute(unigmaDispatchInfoComputeShader, 0, 1, 1, 1);
        GetComponent<Camera>().AddCommandBuffer(CameraEvent.AfterForwardOpaque, depthShadowsCommandBuffer);
    }

    void CreateDepthShadowBuffersNonAccelerated()
    {
        if (!UnigmaSettings.GetIsRayTracingEnabled())
            return;
        if (UnigmaSettings.GetIsRTXEnabled())
            return;
        CommandBuffer depthShadowsCommandBuffer = new CommandBuffer();
        depthShadowsCommandBuffer.name = "DepthShadowsBuffer";

        //Update BVH here.
        CreateNonAcceleratedStructure();
        BuildBVH();



        //Get Kernel.
        int kernelHandleRayTrace = _RayTracingShader.FindKernel("DepthShadowsReflection");

        //Set all the parameters.
        depthShadowsCommandBuffer.SetGlobalTexture("_UnigmaDepthShadowsMap", _DepthShadowsTexture);
        depthShadowsCommandBuffer.SetGlobalTexture("_UnigmaDepthReflectionsMap", _ReflectionsTexture);
        depthShadowsCommandBuffer.SetComputeTextureParam(_RayTracingShader, kernelHandleRayTrace, "_UnigmaDepthShadowsMap", _DepthShadowsTexture);

        //Get thread sizes.
        uint tx, ty, tz;
        _RayTracingShader.GetKernelThreadGroupSizes(0, out tx, out ty, out tz);

        //Set shader variables.
        depthShadowsCommandBuffer.SetComputeMatrixParam(_RayTracingShader, "_CameraInverseProjection", mainCam.projectionMatrix.inverse);
        
        int threadGroupsX = Mathf.CeilToInt(_rayTracingRenderTextureWidth / (float)tx);
        int threadGroupsY = Mathf.CeilToInt(_rayTracingRenderTextureHeight / (float)ty);
        int threadGroupsZ = Mathf.CeilToInt((float)tz);

        //Dispatch the shader.
        depthShadowsCommandBuffer.DispatchCompute(_RayTracingShader, kernelHandleRayTrace, threadGroupsX, threadGroupsY, threadGroupsZ);

        GetComponent<Camera>().AddCommandBuffer(CameraEvent.AfterForwardOpaque, depthShadowsCommandBuffer);
    }

    void UpdateRayTracer()
    {
        if (!UnigmaSettings.GetIsRTXEnabled())
            return;
        _AccelerationStructure.Build();
        Shader.SetGlobalTexture("_UnigmaDepthShadowsMap", _DepthShadowsTexture);
        RenderTexture.active = _DepthShadowsTexture;
        GL.Clear(true, true, new Vector4(0, 0, 0, 0));
        _DepthShadowsRayTracingShaderAccelerated.SetTexture("_UnigmaDepthShadowsMap", _DepthShadowsTexture);
        _DepthShadowsRayTracingShaderAccelerated.SetShaderPass("DepthShadowsRaytracingShaderPass");
        _DepthShadowsRayTracingShaderAccelerated.SetAccelerationStructure("_RaytracingAccelerationStructure", _AccelerationStructure);
        _DepthShadowsRayTracingShaderAccelerated.Dispatch("DepthShadowsRaygenShader", (int)Screen.width, (int)Screen.height, 1);

        Shader.SetGlobalTexture("_UnigmaGlobalIllumination", _UnigmaGlobalIllumination);
        RenderTexture.active = _UnigmaGlobalIllumination;



        _RestirGlobalIllumRayTracingShaderAccelerated.SetInt("_NumberOfLights", lightList.Count);
        _RestirGlobalIllumRayTracingShaderAccelerated.SetInt("_TemporalReservoirsCount", _temporalReservoirsCount);

        _RestirGlobalIllumRayTracingShaderAccelerated.SetTexture("_GlobalIllumination", _DepthShadowsTexture);

        _RestirGlobalIllumRayTracingShaderAccelerated.SetAccelerationStructure("_RaytracingAccelerationStructure", _AccelerationStructure);
        _RestirGlobalIllumRayTracingShaderAccelerated.SetShaderPass("GlobalIlluminationRaytracingShaderPass");
        _RestirGlobalIllumRayTracingShaderAccelerated.Dispatch("RestirGlobalIllumantionRayGen", (int)Screen.width, (int)Screen.height, 1);

        _UnigmaFrameCount += 1;
        _RestirGlobalIllumRayTracingShaderAccelerated.SetInt("_UnigmaFrameCount", _UnigmaFrameCount);
    }

    void CreateDenoisedGlobalIllumination()
    {
        if (!UnigmaSettings.GetIsRTXEnabled())
            return;
        CommandBuffer svgfUnigma = new CommandBuffer();
        svgfUnigma.name = "SVGFBuffer";
        svgfUnigma.SetGlobalTexture("_UnigmaDenoisedGlobalIllumination", _UnigmaDenoisedGlobalIllumination);
        svgfUnigma.SetGlobalTexture("_UnigmaDenoisedGlobalIlluminationTemporal", _UnigmaDenoisedGlobalIlluminationTemporal);
        
        uint threadsX, threadsY, threadsZ;
        svgfComputeShader.GetKernelThreadGroupSizes(0, out threadsX, out threadsY, out threadsZ);
        Vector3 threads = new Vector3(threadsX, threadsY, threadsZ);
        svgfUnigma.SetComputeTextureParam(svgfComputeShader, 0, "_UnigmaGlobalIllumination", _UnigmaGlobalIllumination);
        svgfUnigma.SetComputeTextureParam(svgfComputeShader, 0, "_CameraMotionVectorsTexture", Shader.GetGlobalTexture("_CameraMotionVectorsTexture"));
        svgfUnigma.SetComputeTextureParam(svgfComputeShader, 2, "_UnigmaCameraDepthTexture", Shader.GetGlobalTexture("_CameraDepthTexture"));
        svgfUnigma.SetComputeBufferParam(svgfComputeShader, 0, "_SVGFBuffer", svgfBuffer);
        svgfUnigma.SetComputeBufferParam(svgfComputeShader, 2, "_SVGFBuffer", svgfBuffer);
        svgfUnigma.SetComputeTextureParam(svgfComputeShader, 2, "_UnigmaGlobalIllumination", _UnigmaGlobalIllumination);
        svgfUnigma.SetComputeTextureParam(svgfComputeShader, 2, "_CameraMotionVectorsTexture", Shader.GetGlobalTexture("_CameraMotionVectorsTexture"));
        svgfUnigma.SetComputeTextureParam(svgfComputeShader, 2, "_UnigmaDenoisedGlobalIlluminationTemp", _UnigmaDenoisedGlobalIlluminationTemp);
        svgfUnigma.SetComputeTextureParam(svgfComputeShader, 1, "_UnigmaDenoisedGlobalIlluminationTemp", _UnigmaDenoisedGlobalIlluminationTemp);
        svgfUnigma.DispatchCompute(svgfComputeShader, 0, Mathf.CeilToInt((float)_renderTextureWidth / (float)threads.x), Mathf.CeilToInt((float)_renderTextureHeight / (float)threads.y), 1);

        //Atorus
        for (int i = 0; i <2; i++)
        {
            int stepSize = 1 << i;
            svgfUnigma.SetComputeIntParam(svgfComputeShader, "_StepSize", stepSize);
            svgfUnigma.DispatchCompute(svgfComputeShader, 2, Mathf.CeilToInt((float)_renderTextureWidth / (float)threads.x), Mathf.CeilToInt((float)_renderTextureHeight / (float)threads.y), 1);
        }

        //Store the current frame's global illumination for temporal reprojection
        svgfUnigma.DispatchCompute(svgfComputeShader, 1, Mathf.CeilToInt((float)_renderTextureWidth / (float)threads.x), Mathf.CeilToInt((float)_renderTextureHeight / (float)threads.y), 1);

        GetComponent<Camera>().AddCommandBuffer(CameraEvent.AfterForwardOpaque, svgfUnigma);

    }

    void CreateBackground()
    {

        CommandBuffer blitMainTextBuffer = new CommandBuffer();
        blitMainTextBuffer.name = "BlitFinalPass";
        blitMainTextBuffer.SetGlobalTexture("_UnigmaBackgroundColor", _UnigmaBackgroundColor);

        blitMainTextBuffer.Blit(BuiltinRenderTextureType.CameraTarget, _UnigmaBackgroundColor, _unigmaBackgroundMaterial, 1);
        GetComponent<Camera>().AddCommandBuffer(CameraEvent.AfterImageEffects, blitMainTextBuffer);


        backgroundColorBuffer = new CommandBuffer();
        backgroundColorBuffer.name = "UnigmaBackgroundColor";


        //blit with UnigmaBackgroundMaterial
        backgroundColorBuffer.Blit(BuiltinRenderTextureType.CameraTarget, BuiltinRenderTextureType.CameraTarget, _unigmaBackgroundMaterial, 0);
        //backgroundColorBuffer.Blit(_UnigmaBackgroundColor, BuiltinRenderTextureType.CameraTarget, _unigmaBackgroundMaterial, 1);



        GetComponent<Camera>().AddCommandBuffer(CameraEvent.AfterEverything, backgroundColorBuffer);

    }


    void CreateDepthNormalBuffers()
    {
        outlineDepthBuffer = new CommandBuffer();
        outlineDepthBuffer.name = "OutlineDepthBuffer";
        outlineDepthBuffer.SetGlobalTexture("_UnigmaNormal", _UnigmaNormal);
        outlineDepthBuffer.SetGlobalTexture("_UnigmaAlbedo", _UnigmaAlbedo);
        outlineDepthBuffer.SetGlobalTexture("_UnigmaBlueNoise", _UnigmaBlueNoise);
        outlineDepthBuffer.SetGlobalTexture("_UnigmaSpecularLights", _UnigmaSpecularLights);
        outlineDepthBuffer.SetGlobalTexture("_UnigmaIds", _UnigmaIdsTexture);
        outlineDepthBuffer.SetGlobalTexture("_UnigmaMotionID", _UnigmaMotionID);
        
        SetGlobalIlluminationTextures();

        if (UnigmaSettings.GetIsRTXEnabled())
        {
            outlineDepthBuffer.SetRenderTarget(_UnigmaWaterNormals);

            outlineDepthBuffer.ClearRenderTarget(true, true, Vector4.zero);
            DrawWaterNormals(outlineDepthBuffer);

            outlineDepthBuffer.SetRenderTarget(_UnigmaWaterPosition);

            outlineDepthBuffer.ClearRenderTarget(true, true, Vector4.zero);
            DrawWaterPosition(outlineDepthBuffer);
        }

        outlineDepthBuffer.SetRenderTarget(_UnigmaIdsTexture);

        outlineDepthBuffer.ClearRenderTarget(true, true, Vector4.zero);
        DrawIds(outlineDepthBuffer, 2);

        outlineDepthBuffer.SetRenderTarget(_UnigmaNormal);

        outlineDepthBuffer.ClearRenderTarget(true, true, Vector4.zero);
        DrawIsometricDepthNormals(outlineDepthBuffer, 0);

        //Second pass
        outlineDepthBuffer.SetRenderTarget(_UnigmaMotionID);
        outlineDepthBuffer.ClearRenderTarget(true, true, Vector4.zero);
        DrawIsometricDepthNormals(outlineDepthBuffer, 1);

        if (!UnigmaSettings.GetIsRTXEnabled() || UnigmaSettings.GetIsRayTracingEnabled())
        {
            CreateShadowBuffer();
        }

        //Third pass specular highlights
        outlineDepthBuffer.SetRenderTarget(_UnigmaSpecularLights);
        outlineDepthBuffer.ClearRenderTarget(true, true, Vector4.zero);
        DrawSpecularLight(outlineDepthBuffer);
       

        //Final pass albedo
        outlineDepthBuffer.SetRenderTarget(_UnigmaAlbedo);
        outlineDepthBuffer.ClearRenderTarget(true, true, Vector4.zero);
        DrawIsometricAlbedo(outlineDepthBuffer);

        GetComponent<Camera>().AddCommandBuffer(CameraEvent.AfterForwardOpaque, outlineDepthBuffer);

    }

    void CreateShadowBuffer()
    {
        //Create render texture.
        _UnigmaScreenSpaceShadows = new RenderTexture(_renderTextureWidth, _renderTextureHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        _UnigmaScreenSpaceShadows.enableRandomWrite = true;
        _UnigmaScreenSpaceShadows.Create();

        CommandBuffer unigmaShadowBuffer = new CommandBuffer();
        unigmaShadowBuffer.name = "UnigmaStoreShadowBuffer";
        unigmaShadowBuffer.SetGlobalTexture("_UnigmaScreenSpaceShadows", BuiltinRenderTextureType.CurrentActive);
        unigmaShadowBuffer.Blit(BuiltinRenderTextureType.CurrentActive, _UnigmaScreenSpaceShadows);


        Light mainDirectionalLight = GameObject.Find("Directional Light").GetComponent<Light>();
        mainDirectionalLight.AddCommandBuffer(LightEvent.AfterScreenspaceMask, unigmaShadowBuffer);

        //Calculate the shadows and depth.

        if (_DepthShadowsNoRaytracer == null)
            _DepthShadowsNoRaytracer = Resources.Load<ComputeShader>("DepthShadowsNoRaytrace");
        
        CommandBuffer unigmaCalculateShadowDepthBuffer = new CommandBuffer();
        unigmaCalculateShadowDepthBuffer.name = "unigmaCalculateShadowDepthBuffer";

        int kernelId = _DepthShadowsNoRaytracer.FindKernel("CalculateShadowDepth");
        //Get threads.
        uint threadsX, threadsY, threadsZ;
        _DepthShadowsNoRaytracer.GetKernelThreadGroupSizes(kernelId, out threadsX, out threadsY, out threadsZ);


        unigmaCalculateShadowDepthBuffer.SetComputeTextureParam(_DepthShadowsNoRaytracer, kernelId, "_UnigmaDepthShadowsMap2", _DepthShadowsTexture);
        unigmaCalculateShadowDepthBuffer.SetComputeTextureParam(_DepthShadowsNoRaytracer, kernelId, "_UnigmaScreenSpaceShadowsMap2", _UnigmaScreenSpaceShadows);
        unigmaCalculateShadowDepthBuffer.SetComputeTextureParam(_DepthShadowsNoRaytracer, kernelId, "_UnigmaMotionID2", _UnigmaMotionID);
        unigmaCalculateShadowDepthBuffer.DispatchCompute(_DepthShadowsNoRaytracer, kernelId, Mathf.CeilToInt((float)_renderTextureWidth / (float)threadsX), Mathf.CeilToInt((float)_renderTextureHeight / (float)threadsY), 1);
        unigmaCalculateShadowDepthBuffer.SetGlobalTexture("_UnigmaDepthShadowsMap", _DepthShadowsTexture);

        GetComponent<Camera>().AddCommandBuffer(CameraEvent.AfterForwardOpaque, unigmaCalculateShadowDepthBuffer);
        //mainDirectionalLight.AddCommandBuffer(LightEvent.AfterScreenspaceMask, unigmaCalculateShadowDepthBuffer);
    }

    void CreateCompositeBuffer()
    {
        compositeBuffer = new CommandBuffer();
        compositeBuffer.name = "CompositeBuffer";

        //Get Composite Material.
        Material compositeMaterial = null;
        if (UnigmaSettings.GetIsRTXEnabled())
        {
            compositeMaterial = Resources.Load<Material>("UnigmaCompositeRayTrace" + UnigmaSettings.CurrentPreset());
        }
        else
        {
            compositeMaterial = Resources.Load<Material>("UnigmaComposite" + UnigmaSettings.CurrentPreset());
        }

        compositeBuffer.SetGlobalTexture("_UnigmaComposite", _UnigmaComposite);

        //Blit material to current camera target.

        compositeBuffer.Blit(BuiltinRenderTextureType.CameraTarget, BuiltinRenderTextureType.CameraTarget, compositeMaterial);
        compositeBuffer.Blit(BuiltinRenderTextureType.CameraTarget, _UnigmaComposite, compositeMaterial, 0); //Refactor this such that second pass only calculates outlines.

        //This happens right before post processing.
        GetComponent<Camera>().AddCommandBuffer(CameraEvent.AfterForwardOpaque, compositeBuffer);
    }

    void SetGlobalIlluminationTextures()
    {
        if (!UnigmaSettings.GetIsRTXEnabled())
            return;
        outlineDepthBuffer.SetGlobalTexture("_UnigmaNormalTemporal", _UnigmaNormalTemporal);
        outlineDepthBuffer.SetGlobalTexture("_UnigmaMotionIDTemporal", _UnigmaMotionIDTemporal);
        outlineDepthBuffer.SetGlobalTexture("_UnigmaAlbedoTemporal", _UnigmaAlbedoTemporal);

        outlineDepthBuffer.SetGlobalTexture("_UnigmaWaterNormals", _UnigmaWaterNormals);
        outlineDepthBuffer.SetGlobalTexture("_UnigmaWaterPosition", _UnigmaWaterPosition);
        outlineDepthBuffer.SetGlobalTexture("_UnigmaWaterReflections", _UnigmaWaterReflections);
    }

    void CreateOutlineColorBuffers()
    {
        CommandBuffer outlineColorBuffer = new CommandBuffer();
        outlineColorBuffer.name = "OutlineColorBuffer";
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
        outlineColorBuffer.ClearRenderTarget(true, true, Vector4.zero);
        DrawIsometricOutlineColor(outlineColorBuffer, 1);
        GetComponent<Camera>().AddCommandBuffer(CameraEvent.AfterForwardOpaque, outlineColorBuffer);
    }

    void DrawIsometricAlbedo(CommandBuffer outlineDepthBuffer)
    {
        foreach (UnigmaPostProcessingObjects r in _OutlineRenderObjects)
        {
            IsometricDepthNormalObject iso = r.gameObject.GetComponent<IsometricDepthNormalObject>();
            if (iso != null)
                if (r.materials.ContainsKey("IsometricDepthNormals") && r.renderer.enabled == true && iso._writeToTexture)
                {
                    outlineDepthBuffer.DrawRenderer(r.renderer, r.renderer.material, 0, 1);
                }
        }
    }

    void DrawSpecularLight(CommandBuffer outlineDepthBuffer)
    {
        foreach (UnigmaPostProcessingObjects r in _OutlineRenderObjects)
        {
            IsometricDepthNormalObject iso = r.gameObject.GetComponent<IsometricDepthNormalObject>();
            if (iso != null)
                if (r.materials.ContainsKey("IsometricDepthNormals") && r.renderer.enabled == true && iso._writeToTexture)
                {
                    outlineDepthBuffer.DrawRenderer(r.renderer, r.renderer.material, 0, 0);
                }
        }
    }

    void DrawWaterNormals(CommandBuffer outlineDepthBuffer)
    {
        foreach (UnigmaWater r in _WaterObjects)
        {
            Renderer render = r.gameObject.GetComponent<Renderer>();
            outlineDepthBuffer.DrawRenderer(render, render.material, 0, 0);
        }
    }

    void DrawWaterPosition(CommandBuffer outlineDepthBuffer)
    {
        foreach (UnigmaWater r in _WaterObjects)
        {
            Renderer render = r.gameObject.GetComponent<Renderer>();
            outlineDepthBuffer.DrawRenderer(render, render.material, 0, 1);
        }
    }

    void DrawIds(CommandBuffer outlineDepthBuffer, int pass)
    {
        int i = 0;
        foreach (UnigmaPostProcessingObjects r in _OutlineRenderObjects)
        {
            IsometricDepthNormalObject iso = r.gameObject.GetComponent<IsometricDepthNormalObject>();
            if (iso != null)
                if (r.materials.ContainsKey("IsometricDepthNormals") && r.renderer.enabled == true && iso._writeToTexture)
                {
                    Debug.Log("_UNIGMA Reading materials " + r.materials.ContainsKey("IsometricDepthNormals") + " UnigmaCommandBuffers");
                    r.materials["IsometricDepthNormals"].SetInt("_ObjectID", i);
                    outlineDepthBuffer.DrawRenderer(r.renderer, r.materials["IsometricDepthNormals"], 0, pass);
                }
            i++;
        }

        foreach (Renderer r in _OutlineNullObjects)
        {
            if (r.enabled == true)
                outlineDepthBuffer.DrawRenderer(r, _nullMaterial, 0, -1);
        }
    }

    void DrawIsometricDepthNormals(CommandBuffer outlineDepthBuffer, int pass)
    {
        foreach (UnigmaPostProcessingObjects r in _OutlineRenderObjects)
        {
            IsometricDepthNormalObject iso = r.gameObject.GetComponent<IsometricDepthNormalObject>();
            if (iso != null)
                if (r.materials.ContainsKey("IsometricDepthNormals") && r.renderer.enabled == true && iso._writeToTexture)
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
        {
            bool addOutline = false;
            if (((1 << sceneRenderers[i].gameObject.layer) & OutlineLayers) != 0)
            {
                addOutline = true;
            }

            if (sceneRenderers[i].GetComponent(component) && addOutline)
                _OutlineRenderObjects.Add(sceneRenderers[i].gameObject.GetComponent(component) as UnigmaPostProcessingObjects);
            else
                _OutlineNullObjects.Add(sceneRenderers[i]);
        }
    }

    void FindWaterObjects()
    {
        string component = "UnigmaWater";
        // Retrieve all renderers in scene
        Renderer[] sceneRenderers = FindObjectsOfType<Renderer>();

        // Store only visible renderers
        _WaterObjects.Clear();
        for (int i = 0; i < sceneRenderers.Length; i++)
            if (sceneRenderers[i].GetComponent(component))
                _WaterObjects.Add(sceneRenderers[i].gameObject.GetComponent(component) as UnigmaWater);
    }

    //Release all buffers and memory
    void ReleaseBuffers()
    {
        Debug.Log("Buffers Released");
        if (samplesBuffer != null)
            samplesBuffer.Release();
        if (lightsBuffer != null)
            lightsBuffer.Release();
        if (reservoirsBuffer != null)
            reservoirsBuffer.Release();
        if (reservoirPathsBuffer != null)
            reservoirPathsBuffer.Release();
        if (unigmaDispatchInfoBuffer != null)
            unigmaDispatchInfoBuffer.Release();
        if (svgfBuffer != null)
            svgfBuffer.Release();
        if (outlineDepthBuffer != null)
            outlineDepthBuffer.Release();
        CommandBuffer[] buffers = mainCam.GetCommandBuffers(CameraEvent.AfterForwardOpaque);
        foreach (CommandBuffer buffer in buffers)
        {
            buffer.Release();
        }

        _UnigmaGlobalIllumination.Release();
        _DepthShadowsTexture.Release();
        _ReflectionsTexture.Release();
        _UnigmaAlbedo.Release();
        _UnigmaAlbedoTemporal.Release();
        _UnigmaNormal.Release();
        _UnigmaNormalTemporal.Release();
        _UnigmaDenoisedGlobalIlluminationTemp.Release();
        _UnigmaScreenSpaceShadows.Release();

        if (_verticesObjectBuffer != null)
            _verticesObjectBuffer.Release();
        if (_indicesObjectBuffer != null)
            _indicesObjectBuffer.Release();
        if (_meshObjectBuffer != null)
            _meshObjectBuffer.Release();



    }

    Vector3 SphericalMapping(Vector2 uv, float radius)
    {
        float theta = 2 * Mathf.PI * uv.x;
        float phi = Mathf.PI * uv.y;

        float x = Mathf.Cos(theta) * Mathf.Sin(phi) * radius;
        float y = Mathf.Sin(theta) * Mathf.Sin(phi) * radius;
        float z = -Mathf.Cos(phi) * radius;

        return new Vector3(x,y,z);
    }

    public void SaveRenderTexture()
    {
        if (debugMode)
        {
            //Get space bar input.
            if (Input.GetKeyDown(KeyCode.Space))
            {
                //Save all render textures.
                SaveTexture.SaveRenderTexture2D(_UnigmaAlbedo, "ScreenShots/Albedo.png");
                SaveTexture.SaveRenderTexture2D(_UnigmaNormal, "ScreenShots/Normal.png");
                SaveTexture.SaveRenderTexture2D(_UnigmaSpecularLights, "ScreenShots/SpecularLights.png");
                SaveTexture.SaveRenderTexture2D(_DepthShadowsTexture, "ScreenShots/Depth.png");
                SaveTexture.SaveRenderTexture2D(_ReflectionsTexture, "ScreenShots/Reflections.png");

                //Unigma Ids
                SaveTexture.SaveRenderTexture2D(_UnigmaIdsTexture, "ScreenShots/Ids.png");

                SaveTexture.SaveRenderTexture2D(_UnigmaGlobalIllumination, "ScreenShots/GlobalIllumination.png");
                SaveTexture.SaveRenderTexture2D(_UnigmaDenoisedGlobalIllumination, "ScreenShots/DenoisedGlobalIllumination.png");
                //Render what camera sees.
                Camera cam = GetComponent<Camera>();
                SaveTexture.TakeScreenshot("ScreenShots/CameraTexture.png");
            }
        }
    }

    private void OnDrawGizmos()
    {


        /*
        int width = SphericalMap.width;
        int height = SphericalMap.height;
        for (int i = 0; i < width; i++)
            for (int j = 0; j < height; j++)
            {
                float u = (float)i / (float)width;
                float v = (float)j / (float)height;
                Gizmos.color = SphericalMap.GetPixel(i, j);
                Gizmos.DrawSphere(SphericalMapping(new Vector2(u, v), 10), 1f);
            }
        */
    }

    void OnDisable()
    {
        ReleaseBuffers();
    }

    //On application quit
    void OnApplicationQuit()
    {
        ReleaseBuffers();
    }

    //On playtest end
    void OnDestroy()
    {
        ReleaseBuffers();
    }
}
