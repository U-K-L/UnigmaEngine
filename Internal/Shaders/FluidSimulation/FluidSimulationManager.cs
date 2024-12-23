using MudBun;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnigmaEngine;
public class FluidSimulationManager : MonoBehaviour
{

    public static FluidSimulationManager Instance { get; private set; }

    Material rasterMaterial;
    Material rayTracingMaterial;

    Mesh rasterMesh;

    ComputeShader _fluidSimulationComputeShader;
    GraphicsBuffer aabbList = null;
    AABB[] aabbs;

    ComputeBuffer _particleBuffer;
    ComputeBuffer _particlePositionsBuffer;
    ComputeBuffer _pNodesBuffer;
    ComputeBuffer _particleIDsBuffer;
    ComputeBuffer _particleIndicesBuffer;
    ComputeBuffer _particleCountBuffer;
    ComputeBuffer _particleCellIndicesBuffer;
    ComputeBuffer _particleCellOffsets;
    ComputeBuffer _BVHNodesBuffer;
    ComputeBuffer _MortonCodesBuffer;
    ComputeBuffer _MortonCodesTempBuffer;
    ComputeBuffer _MortonPrefixSumTotalZeroesBuffer;
    ComputeBuffer _MortonPrefixSumOffsetZeroesBuffer;
    ComputeBuffer _MortonPrefixSumOffsetOnesBuffer;
    ComputeBuffer _particleNeighbors;
    ComputeBuffer _controlNeighborsBuffer; //The control particles that are neighbors of the particles.
    ComputeBuffer _controlParticlesBuffer;
    ComputeBuffer _fluidObjectsBuffer;

    //Todo refactor some of these compute kernels out.
    int _UpdateParticlesKernelId;
    int _ComputeForcesKernelId;
    int _ComputeDensityKernelId;
    int _CreateGridKernelId;
    int _CreateDistancesKernelId;
    int _UpdatePositionDeltasKernelId;
    int _UpdatePositionsKernelId;
    int _HashParticlesKernelId;
    int _SortParticlesKernelId;
    int _RadixSortKernelId;
    int _CalculateCellOffsetsKernelId;
    int _CalculateCurlKernelId;
    int _CalculateVorticityKernelId;
    int _PrefixSumKernelId;
    int _AssignMortonCodesKernelId;
    int _AssignParentsKernelId;
    int _AssignIndexKernelId;
    int _CreateBVHTreeKernelId;
    int _CreateBoundingBoxKernelId;
    int _StoreParticleNeighborsKernelId;
    int _CalculateControlDensityKernelId;
    int _CalculateControlForcesKernelId;
    int _StoreControlParticleNeighborsKernelId;
    int _CalculateVelocityKernelId;
    int _CalculateSpatialDiffusionKernelId;

    Vector3 _updateParticlesThreadSize;
    Vector3 _computeForcesThreadSize;
    Vector3 _computeDensityThreadSize;
    Vector3 _createGridThreadSize;
    Vector3 _createDistancesThreadSize;
    Vector3 _updatePositionDeltasThreadSize;
    Vector3 _updatePositionsThreadSize;
    Vector3 _hashParticlesThreadSize;
    Vector3 _sortParticlesThreadSize;
    Vector3 _radixSortThreadSize;
    Vector3 _calculateCellOffsetsThreadSize;
    Vector3 _calculateCurlThreadSize;
    Vector3 _calculateVorticityThreadSize;
    Vector3 _prefixSumThreadSize;
    Vector3 _assignMortonCodesThreadSize;
    Vector3 _assignParentsThreadSize;
    Vector3 _assignIndexThreadSize;
    Vector3 _createBVHTreeThreadSize;
    Vector3 _createBoundingBoxThreadSize;
    Vector3 _storeParticleNeighborsThreadSize;
    Vector3 _calculateControlDensityThreadSize;
    Vector3 _calculateControlForcesThreadSize;
    Vector3 _storeControlParticleNeighborsThreadSize;
    Vector3 _calculateVelocityThreadSize;
    Vector3 _calculateSpatialDiffusionThreadSize;

    //TODO: Reduce texture overhead by combining some of these and lowering resolution for others.
    RenderTexture _rtTarget;
    RenderTexture _densityMapTexture;
    RenderTexture _normalMapTexture;
    RenderTexture _curlMapTexture;
    RenderTexture _velocityMapTexture;
    RenderTexture _surfaceMapTexture;
    RenderTexture _tempTarget;
    RenderTexture _fluidNormalBufferTexture;
    RenderTexture _fluidDepthBufferTexture;
    RenderTexture _velocitySurfaceDensityDepthTexture;
    RenderTexture _depthBufferTexture;
    RenderTexture _unigmaDepthTexture;
    RenderTexture _UnigmaFluidsFinal;
    List<RenderTexture> _previousPositionTextures;

    Shader _fluidNormalShader;
    Shader _fluidDepthShader;
    Shader _fluidCompositeShader;
    
    Material _fluidSimMaterialDepthHori;
    Material _fluidSimMaterialDepthVert;
    Material _fluidSimMaterialNormal;

    Camera _cam;

    //public Transform _LightSouce;
    //public Transform _LightScale;
    public FluidSettings fluidSettings;
    private Material _fluidSimCompositeLiquid;

    private Vector2 BlurScale;
    public Vector3 _BoxSize = Vector3.one;
    private Vector4 DepthScale = default;

    private int ResolutionDivider = 0; // (1 / t + 1) How much to divide the text size by. This lowers the resolution of the final image, but massively aids in performance.
    private int DistanceResolutionDivider = 0;
    public int NumOfParticles;
    public int NumOfControlParticles;
    private int MaxNumOfParticles = 1024;
    private int MaxNumOfControlParticles = 512;
    private float SizeOfParticle;

    private float BlurFallOff = 0.25f;
    private float BlurRadius = 5.0f;
    private float Viscosity = 1.0f;
    private float TimeStep = 0.02f;
    private float BoundsDamping = 9.8f;

    public float _ControlAlpha = 0.9355f;
    public float _CDHRadius = 0.525f;
    public float _CLHRadius = 2;

    public float _CPNorm = 300.0f;
    public float _CDNorm = 18.0f;

    public float DebugKelvin;
    private float _VoritictyEps = 25.0f;

    public List<FluidControl> fluidControlledObjects;
    private Particles[] _particles;
    private Vector3[] _particlesPositions;
    private uint[] _particleNeighborsArray;

    private int[] _ParticleIDs;
    private int[] _ParticleIndices;
    private int[] _ParticleCount;
    private int[] _ParticleCellIndices;
    private int[] _ParticleCellOffsets;
    
    private List<PNode> _pNodes;
    private BVHNode[] _BVHNodes;
    private int _renderTextureWidth, _renderTextureHeight, _distanceTextureWidth, _distanceTextureHeight = 0;
    private List<Vector3> _spawnParticles = default;
    private Vector4 _initialForce;
    private Vector3 _initialPosition;

    Vector3 controlParticlePosition;
    private Vector3[] controlParticlesPositions;

    int buffersAdded = 0;
    bool buffersInitialized = false;

    struct Particles
    {
        public Vector4 force;
        public Vector3 position;
        public Vector3 lastPosition;
        public Vector3 predictedPosition;
        public Vector3 positionDelta;
        public Vector3 velocity;
        public Vector3 normal;
        public Vector3 curl;
        public float density;
        public float lambda;
        public float spring;
        public Matrix4x4 anisotropicTRS;
        public Vector4 mean;
        public int phase;
        public int type;
        public float kelvin;
        public float tempKelvin;
    };

    public enum particlePhases
    {
        Gas,
        Liquid,
        Solid,
        Plasma
    };

    public enum particleTypes
    {
        Oxygen,
        H2O,
        CO2
    };
    
    struct PNode
    {
        public int index;
        public int[] children;
    };

    struct AABB
    {
        public Vector3 min;
        public Vector3 max;
    }

    struct BVHNode
    {
        public Vector3 aabbMin;
        public Vector3 aabbMax;
        public int leftChild;
        public int rightChild;
        public Vector3 topChild;
        public Vector3 bottomChild;
        public int parent;
        public int primitiveOffset;
        public int primitiveCount;
        public int index;
        public int hit;
        public int miss;
        public int isLeaf;
        public int leftChildLeaf;
        public int rightChildLeaf;
        public Vector3 topChildLeaf;
        public Vector3 bottomChildLeaf;
        public int indexedId;
        public Vector2 padding;
    };

    struct MortonCode
    {
        public uint mortonCode;
        public int particleIndex;
    };

    public struct ControlParticles
    {
        public Vector3 position;
        public float density;
        public float lambda;
        public Vector3 prevPosition;
        public int objectId;
    };

    public struct FluidObject
    {
        public float kelvin;
        public float controlRadius;
        public float smoothingRadius;
        public float controlStrength;
        public float controlNorm;

    };

    //Arrays to hold data on the CPU side and initialize data.
    private ControlParticles[] _controlParticlesArray;
    [HideInInspector]
    public FluidObject[] _fluidObjectsArray;

    //Maximum size of these arrays. Constants.
    public int _maxNumOfFluidObjects = 1000;


    private MortonCode[] _MortonCodes;
    private MortonCode[] _MortonCodesTemp;
    private uint _MortonPrefixSumTotalZeroes = 0, _MortonPrefixSumOffsetZeroes = 0, _MortonPrefixSumOffsetOnes = 0;

    int _particleStride = (sizeof(float) * 4*2) + ((sizeof(float) * 3) * 7 + (sizeof(float) * 5)) + (sizeof(float) * 4 * 4) + sizeof(int)*2;
    int _MortonCodeStride = sizeof(uint) + sizeof(int);
    int _BVHStride = sizeof(float) * 3 * 2 + sizeof(int) * 12 + sizeof(float)*14;
    int _controlParticleStride = sizeof(float) * 3 * 2 + sizeof(float) * 3;
    int _fluidObjectStride = sizeof(float) * 5;

    //Items to add to the raytracer.
    public LayerMask RayTracingLayers;
    public LayerMask FluidInteractionLayers;
    int _SolveIterations = 1;
    int nodesUsed = 1;
    
    Bounds bounds;

    int RayTracinghandle;
    
    public enum RenderMethod
    {
        Rasterization,
        RayTracing,
        RayTracingAccelerated

    }


    
    public RenderMethod _renderMethod = RenderMethod.Rasterization;
    private RayTracingShader _RayTracingShaderAccelerated;
    RayTracingAccelerationStructure _AccelerationStructure;
    RayTracingAccelerationStructure _MeshAccelerationStructure;
    private MaterialPropertyBlock properties = null;
    public Camera secondCam;
    private void Awake()
    {

        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    public void Initialize()
    {
        GetSettings();
        secondCam = Camera.main.GetComponent<PerspectiveCameraLerp>().perpCam;
        fluidControlledObjects = new List<FluidControl>();
        //Application.targetFrameRate = 30;
        Camera.main.depthTextureMode = DepthTextureMode.Depth;
        Debug.Log("Particle Stride size is: " + _particleStride);
        Debug.Log("BVH Stride size is: " + _BVHStride);
        Debug.Log("Morton Code Stride size is: " + _MortonCodeStride);

        aabbs = new AABB[MaxNumOfParticles];
        _spawnParticles = new List<Vector3>();
        _renderTextureWidth = Mathf.Max(Mathf.Min(Mathf.CeilToInt(Screen.width * (1.0f / (1.0f + Mathf.Abs(ResolutionDivider)))), Screen.width), 32);
        _renderTextureHeight = Mathf.Max(Mathf.Min(Mathf.CeilToInt(Screen.height * (1.0f / (1.0f + Mathf.Abs(ResolutionDivider)))), Screen.height), 32);

        _distanceTextureWidth = Mathf.Max(Mathf.Min(Mathf.CeilToInt(Screen.width * (1.0f / (1.0f + Mathf.Abs(DistanceResolutionDivider)))), Screen.width), 32);
        _distanceTextureHeight = Mathf.Max(Mathf.Min(Mathf.CeilToInt(Screen.height * (1.0f / (1.0f + Mathf.Abs(DistanceResolutionDivider)))), Screen.height), 32);
        _fluidSimulationComputeShader = Resources.Load<ComputeShader>("FluidSimCompute");

        _fluidNormalShader = Resources.Load<Shader>("FluidNormalBuffer");
        _fluidDepthShader = Resources.Load<Shader>("FluidBilateralFilter");

        //Create the material for the fluid simulation.
        _fluidSimMaterialDepthHori = new Material(_fluidDepthShader);
        _fluidSimMaterialDepthVert = new Material(_fluidDepthShader);
        _fluidSimMaterialNormal = new Material(_fluidNormalShader);

        _cam = Camera.main;
        //Create the texture for compute shader.
        _rtTarget = RenderTexture.GetTemporary(_renderTextureWidth, _renderTextureHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        _rtTarget.name = "FinalMainScreenTexture";
        _densityMapTexture = RenderTexture.GetTemporary(_renderTextureWidth, _renderTextureHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        _densityMapTexture.name = "DensityTexture";
        _unigmaDepthTexture = RenderTexture.GetTemporary(_distanceTextureWidth, _distanceTextureHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        _unigmaDepthTexture.name = "DistancesTexture";
        _normalMapTexture = RenderTexture.GetTemporary(_renderTextureWidth, _renderTextureHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        _normalMapTexture.name = "NormalTexture";
        _velocityMapTexture = RenderTexture.GetTemporary(_renderTextureWidth, _renderTextureHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        _velocityMapTexture.name = "VelocityTexture";
        _surfaceMapTexture = RenderTexture.GetTemporary(_renderTextureWidth, _renderTextureHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        _surfaceMapTexture.name = "SurfaceTexture";
        _curlMapTexture = RenderTexture.GetTemporary(_renderTextureWidth, _renderTextureHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        _curlMapTexture.name = "CurlTexture";
        _tempTarget = RenderTexture.GetTemporary(_renderTextureWidth, _renderTextureHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        _tempTarget.name = "TemporaryTextureForFinalFluidScreen";
        _fluidNormalBufferTexture = RenderTexture.GetTemporary(_renderTextureWidth, _renderTextureHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        _fluidNormalBufferTexture.name = "FluidNormalBufferTexture";
        _fluidDepthBufferTexture = RenderTexture.GetTemporary(_renderTextureWidth, _renderTextureHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        _fluidNormalBufferTexture.name = "FluidDepthBufferTexture";
        _velocitySurfaceDensityDepthTexture = RenderTexture.GetTemporary(_renderTextureWidth, _renderTextureHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        _velocitySurfaceDensityDepthTexture.name = "VelocitySurfaceDensityDepthTexture";
        _UnigmaFluidsFinal = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        _UnigmaFluidsFinal.name = "UnigmaFluidsFinal";

        //Initialize Arrays.
        _particles = new Particles[MaxNumOfParticles];
        _controlParticlesArray = new ControlParticles[MaxNumOfControlParticles];
        _fluidObjectsArray = new FluidObject[_maxNumOfFluidObjects];
        _particlesPositions = new Vector3[MaxNumOfParticles];
        _ParticleIDs = new int[MaxNumOfParticles];
        _particleIDsBuffer = new ComputeBuffer(_ParticleIDs.Length, 4);

        _ParticleIndices = new int[MaxNumOfParticles];
        _ParticleCellIndices = new int[MaxNumOfParticles];
        _ParticleCellOffsets = new int[MaxNumOfParticles];
        _ParticleCount = new int[MaxNumOfParticles];
        _BVHNodes = new BVHNode[MaxNumOfParticles];
        _MortonCodes = new MortonCode[MaxNumOfParticles];
        _MortonCodesTemp = new MortonCode[MaxNumOfParticles];

        _BVHNodesBuffer = new ComputeBuffer(_BVHNodes.Length, _BVHStride);
        _particleNeighbors = new ComputeBuffer((MaxNumOfParticles + MaxNumOfControlParticles) * 27, 4);
        //_controlNeighborsBuffer = new ComputeBuffer(MaxNumOfParticles * 27, 4);
        _particleNeighborsArray = new uint[(MaxNumOfParticles + MaxNumOfControlParticles) * 27];
        Debug.Log(_particleNeighborsArray.Length);
        _particleNeighbors.SetData(_particleNeighborsArray);

        controlParticlesPositions = new Vector3[MaxNumOfControlParticles];

        _controlParticlesBuffer = new ComputeBuffer(MaxNumOfControlParticles, _controlParticleStride);

        _fluidObjectsBuffer = new ComputeBuffer(_maxNumOfFluidObjects, _fluidObjectStride);

        _UpdateParticlesKernelId = _fluidSimulationComputeShader.FindKernel("UpdateParticles");
        _HashParticlesKernelId = _fluidSimulationComputeShader.FindKernel("HashParticles");
        _SortParticlesKernelId = _fluidSimulationComputeShader.FindKernel("BitonicSort");
        _RadixSortKernelId = _fluidSimulationComputeShader.FindKernel("RadixSort");
        _CalculateCellOffsetsKernelId = _fluidSimulationComputeShader.FindKernel("CalculateCellOffsets");
        _CalculateCurlKernelId = _fluidSimulationComputeShader.FindKernel("CalculateCurl");
        _CalculateVorticityKernelId = _fluidSimulationComputeShader.FindKernel("CalculateVorticity");
        _CreateGridKernelId = _fluidSimulationComputeShader.FindKernel("CreateGrid");
        _CreateDistancesKernelId = _fluidSimulationComputeShader.FindKernel("CreateDistances");
        _ComputeForcesKernelId = _fluidSimulationComputeShader.FindKernel("ComputeForces");
        _ComputeDensityKernelId = _fluidSimulationComputeShader.FindKernel("ComputeDensity");
        _UpdatePositionDeltasKernelId = _fluidSimulationComputeShader.FindKernel("UpdatePositionDeltas");
        _UpdatePositionsKernelId = _fluidSimulationComputeShader.FindKernel("UpdatePositions");
        _CalculateVelocityKernelId = _fluidSimulationComputeShader.FindKernel("ComputeVelocity");
        _PrefixSumKernelId = _fluidSimulationComputeShader.FindKernel("PrefixSum");
        _AssignMortonCodesKernelId = _fluidSimulationComputeShader.FindKernel("AssignMortonCodes");
        _AssignParentsKernelId = _fluidSimulationComputeShader.FindKernel("AssignParents");
        _AssignIndexKernelId = _fluidSimulationComputeShader.FindKernel("AssignIndex");
        _CreateBVHTreeKernelId = _fluidSimulationComputeShader.FindKernel("CreateBVHTree");
        _CreateBoundingBoxKernelId = _fluidSimulationComputeShader.FindKernel("CreateBoundingBox");
        _StoreParticleNeighborsKernelId = _fluidSimulationComputeShader.FindKernel("StoreParticleNeighbors");
        _StoreControlParticleNeighborsKernelId = _fluidSimulationComputeShader.FindKernel("StoreControlParticleNeighbors");
        _CalculateControlDensityKernelId = _fluidSimulationComputeShader.FindKernel("CalculateControlDensity");
        _CalculateControlForcesKernelId = _fluidSimulationComputeShader.FindKernel("CalculateControlForces");
        _CalculateSpatialDiffusionKernelId = _fluidSimulationComputeShader.FindKernel("CalculateSpatialDiffusion");

        _rtTarget.enableRandomWrite = true;
        _rtTarget.Create();
        _densityMapTexture.enableRandomWrite = true;
        _densityMapTexture.Create();
        _velocitySurfaceDensityDepthTexture.enableRandomWrite = true;
        _velocitySurfaceDensityDepthTexture.Create();
        _normalMapTexture.enableRandomWrite = true;
        _normalMapTexture.Create();
        _velocityMapTexture.enableRandomWrite = true;
        _velocityMapTexture.Create();
        _surfaceMapTexture.enableRandomWrite = true;
        _surfaceMapTexture.Create();
        _curlMapTexture.enableRandomWrite = true;
        _curlMapTexture.Create();
        _fluidNormalBufferTexture.enableRandomWrite = true;
        _fluidNormalBufferTexture.Create();
        _unigmaDepthTexture.enableRandomWrite = true;
        _unigmaDepthTexture.Create();
        _UnigmaFluidsFinal.enableRandomWrite = true;
        _UnigmaFluidsFinal.Create();
        _fluidSimulationComputeShader.SetTexture(_CreateGridKernelId, "Result", _rtTarget);
        _fluidSimulationComputeShader.SetTexture(_CreateGridKernelId, "DensityMap", _densityMapTexture);
        _fluidSimulationComputeShader.SetTexture(_CreateGridKernelId, "NormalMap", _fluidNormalBufferTexture);
        _fluidSimulationComputeShader.SetTexture(_CreateGridKernelId, "_ColorFieldNormalMap", _normalMapTexture);
        _fluidSimulationComputeShader.SetTexture(_CreateGridKernelId, "_VelocityMap", _velocityMapTexture);
        _fluidSimulationComputeShader.SetTexture(_CreateGridKernelId, "_SurfaceMap", _surfaceMapTexture);
        _fluidSimulationComputeShader.SetTexture(_CreateGridKernelId, "_CurlMap", _curlMapTexture);
        _fluidSimulationComputeShader.SetTexture(_CreateDistancesKernelId, "_UnigmaDepthMap", _unigmaDepthTexture);

        //material.SetBuffer("_Particles", _particleBuffer);
        bounds = new Bounds(Vector3.zero, _BoxSize);

        InitializeControlParticles();
        GetThreadSizes();
        if (UnigmaSettings.GetIsRTXEnabled() && _renderMethod == RenderMethod.RayTracingAccelerated)
        {
            CreateAcceleratedStructure();
            CreateNonAcceleratedStructure();
        }
        else
        {
            CreateNonAcceleratedStructure();
        }
        UpdateNonAcceleratedRayTracer();
        rasterMaterial.SetBuffer("_Particles", _particleBuffer);

        //StartCoroutine(ReactToForces());
    }

    void GetSettings()
    {
        //Set fluid settings.
        Viscosity = fluidSettings.Viscosity;
        BlurFallOff = fluidSettings.BlurFallOff;
        BlurRadius = fluidSettings.BlurRadius;
        BoundsDamping = fluidSettings.BoundsDamping;
        MaxNumOfParticles = fluidSettings.MaxNumOfParticles;
        MaxNumOfControlParticles = fluidSettings.MaxNumOfControlParticles;
        SizeOfParticle = fluidSettings.SizeOfParticle;
        BlurScale = fluidSettings.BlurScale;
        _BoxSize = fluidSettings._BoxSize;
        rasterMesh = fluidSettings.rasterMesh;
        _SolveIterations = fluidSettings.SolveIterations;
        TimeStep = fluidSettings.TimeStep;
        _VoritictyEps = fluidSettings.VoritictyEps;

        //Get the materials needed.
        rasterMaterial = Resources.Load<Material>("Unlit_WaterParticle");
        rayTracingMaterial = Resources.Load<Material>("FluidRaytraceMaterial");


        if (UnigmaSettings.QualityPresets == UnigmaSettings.QualityPreset.High)
            ResolutionDivider = 4;
        if (UnigmaSettings.QualityPresets == UnigmaSettings.QualityPreset.Mid)
            ResolutionDivider = 2;
        if (UnigmaSettings.QualityPresets == UnigmaSettings.QualityPreset.Low)
            ResolutionDivider = 1;

        if (UnigmaSettings.GetIsRTXEnabled())
            _renderMethod = RenderMethod.RayTracingAccelerated;
        else
            _renderMethod = RenderMethod.Rasterization;

        _fluidSimCompositeLiquid = Resources.Load<Material>("Hidden_FluidCompositionLiquids");

        if (_renderMethod == RenderMethod.Rasterization)
            _fluidSimCompositeLiquid.shader = Resources.Load<Shader>("FluidCompositionRaster");
        else
            _fluidSimCompositeLiquid.shader = Resources.Load<Shader>("FluidComposition");

    }

    private void Start()
    {

    }

    void CreateAcceleratedStructure()
    {
        if (_RayTracingShaderAccelerated == null)
            _RayTracingShaderAccelerated = Resources.Load<RayTracingShader>("FluidRaytracer");

        //Create GPU accelerated structure.
        var settings = new RayTracingAccelerationStructure.RASSettings();
        settings.layerMask = RayTracingLayers;
        //Change this to manual after some work.
        settings.managementMode = RayTracingAccelerationStructure.ManagementMode.Manual;
        settings.rayTracingModeMask = RayTracingAccelerationStructure.RayTracingModeMask.Everything;

        _AccelerationStructure = new RayTracingAccelerationStructure(settings);
        _MeshAccelerationStructure = new RayTracingAccelerationStructure(settings);
    }

    void DispatchAcceleratedRayTrace()
    {
        _RayTracingShaderAccelerated.SetTexture("_RayTracedImage", _rtTarget);
        _RayTracingShaderAccelerated.SetMatrix("_CameraToWorld", _cam.cameraToWorldMatrix);
        _RayTracingShaderAccelerated.SetMatrix("_CameraInverseProjection", _cam.projectionMatrix.inverse);
        _RayTracingShaderAccelerated.SetShaderPass("MyRaytraceShaderPass");
        _RayTracingShaderAccelerated.SetAccelerationStructure("_RaytracingAccelerationStructure", _AccelerationStructure);

        _RayTracingShaderAccelerated.Dispatch("MyRaygenShader", _renderTextureWidth, _renderTextureHeight, 1);
    }

    void GetThreadSizes()
    {
        uint threadsX, threadsY, threadsZ;
        _fluidSimulationComputeShader.GetKernelThreadGroupSizes(_UpdateParticlesKernelId, out threadsX, out threadsY, out threadsZ);
        _updateParticlesThreadSize = new Vector3(threadsX, threadsY, threadsZ);

        _fluidSimulationComputeShader.GetKernelThreadGroupSizes(_HashParticlesKernelId, out threadsX, out threadsY, out threadsZ);
        _hashParticlesThreadSize = new Vector3(threadsX, threadsY, threadsZ);

        _fluidSimulationComputeShader.GetKernelThreadGroupSizes(_SortParticlesKernelId, out threadsX, out threadsY, out threadsZ);
        _sortParticlesThreadSize = new Vector3(threadsX, threadsY, threadsZ);

        _fluidSimulationComputeShader.GetKernelThreadGroupSizes(_RadixSortKernelId, out threadsX, out threadsY, out threadsZ);
        _radixSortThreadSize = new Vector3(threadsX, threadsY, threadsZ);

        _fluidSimulationComputeShader.GetKernelThreadGroupSizes(_CalculateCellOffsetsKernelId, out threadsX, out threadsY, out threadsZ);
        _calculateCellOffsetsThreadSize = new Vector3(threadsX, threadsY, threadsZ);

        _fluidSimulationComputeShader.GetKernelThreadGroupSizes(_CalculateCurlKernelId, out threadsX, out threadsY, out threadsZ);
        _calculateCurlThreadSize = new Vector3(threadsX, threadsY, threadsZ);

        _fluidSimulationComputeShader.GetKernelThreadGroupSizes(_CalculateVorticityKernelId, out threadsX, out threadsY, out threadsZ);
        _calculateVorticityThreadSize = new Vector3(threadsX, threadsY, threadsZ);

        _fluidSimulationComputeShader.GetKernelThreadGroupSizes(_CreateGridKernelId, out threadsX, out threadsY, out threadsZ);
        _createGridThreadSize = new Vector3(threadsX, threadsY, threadsZ);

        _fluidSimulationComputeShader.GetKernelThreadGroupSizes(_CreateDistancesKernelId, out threadsX, out threadsY, out threadsZ);
        _createDistancesThreadSize = new Vector3(threadsX, threadsY, threadsZ);

        _fluidSimulationComputeShader.GetKernelThreadGroupSizes(_ComputeForcesKernelId, out threadsX, out threadsY, out threadsZ);
        _computeForcesThreadSize = new Vector3(threadsX, threadsY, threadsZ);

        _fluidSimulationComputeShader.GetKernelThreadGroupSizes(_ComputeDensityKernelId, out threadsX, out threadsY, out threadsZ);
        _computeDensityThreadSize = new Vector3(threadsX, threadsY, threadsZ);

        _fluidSimulationComputeShader.GetKernelThreadGroupSizes(_UpdatePositionDeltasKernelId, out threadsX, out threadsY, out threadsZ);
        _updatePositionDeltasThreadSize = new Vector3(threadsX, threadsY, threadsZ);

        _fluidSimulationComputeShader.GetKernelThreadGroupSizes(_UpdatePositionsKernelId, out threadsX, out threadsY, out threadsZ);
        _updatePositionsThreadSize = new Vector3(threadsX, threadsY, threadsZ);

        _fluidSimulationComputeShader.GetKernelThreadGroupSizes(_CalculateVelocityKernelId, out threadsX, out threadsY, out threadsZ);
        _calculateVelocityThreadSize = new Vector3(threadsX, threadsY, threadsZ);

        _fluidSimulationComputeShader.GetKernelThreadGroupSizes(_PrefixSumKernelId, out threadsX, out threadsY, out threadsZ);
        _prefixSumThreadSize = new Vector3(threadsX, threadsY, threadsZ);

        _fluidSimulationComputeShader.GetKernelThreadGroupSizes(_AssignMortonCodesKernelId, out threadsX, out threadsY, out threadsZ);
        _assignMortonCodesThreadSize = new Vector3(threadsX, threadsY, threadsZ);

        _fluidSimulationComputeShader.GetKernelThreadGroupSizes(_AssignParentsKernelId, out threadsX, out threadsY, out threadsZ);
        _assignParentsThreadSize = new Vector3(threadsX, threadsY, threadsZ);

        _fluidSimulationComputeShader.GetKernelThreadGroupSizes(_AssignIndexKernelId, out threadsX, out threadsY, out threadsZ);
        _assignIndexThreadSize = new Vector3(threadsX, threadsY, threadsZ);

        _fluidSimulationComputeShader.GetKernelThreadGroupSizes(_AssignIndexKernelId, out threadsX, out threadsY, out threadsZ);
        _assignIndexThreadSize = new Vector3(threadsX, threadsY, threadsZ);

        _fluidSimulationComputeShader.GetKernelThreadGroupSizes(_CreateBVHTreeKernelId, out threadsX, out threadsY, out threadsZ);
        _createBVHTreeThreadSize = new Vector3(threadsX, threadsY, threadsZ);

        _fluidSimulationComputeShader.GetKernelThreadGroupSizes(_CreateBoundingBoxKernelId, out threadsX, out threadsY, out threadsZ);
        _createBoundingBoxThreadSize = new Vector3(threadsX, threadsY, threadsZ);

        _fluidSimulationComputeShader.GetKernelThreadGroupSizes(_StoreParticleNeighborsKernelId, out threadsX, out threadsY, out threadsZ);
        _storeParticleNeighborsThreadSize = new Vector3(threadsX, threadsY, threadsZ);

        _fluidSimulationComputeShader.GetKernelThreadGroupSizes(_CalculateControlDensityKernelId, out threadsX, out threadsY, out threadsZ);
        _calculateControlDensityThreadSize = new Vector3(threadsX, threadsY, threadsZ);

        _fluidSimulationComputeShader.GetKernelThreadGroupSizes(_CalculateControlForcesKernelId, out threadsX, out threadsY, out threadsZ);
        _calculateControlForcesThreadSize = new Vector3(threadsX, threadsY, threadsZ);

        _fluidSimulationComputeShader.GetKernelThreadGroupSizes(_StoreControlParticleNeighborsKernelId, out threadsX, out threadsY, out threadsZ);
        _storeControlParticleNeighborsThreadSize = new Vector3(threadsX, threadsY, threadsZ);

        _fluidSimulationComputeShader.GetKernelThreadGroupSizes(_CalculateSpatialDiffusionKernelId, out threadsX, out threadsY, out threadsZ);
        _calculateSpatialDiffusionThreadSize = new Vector3(threadsX, threadsY, threadsZ);
    }

    void SortParticles()
    {
        for (int biDim = 2; biDim <= MaxNumOfParticles; biDim <<= 1)
        {
            _fluidSimulationComputeShader.SetInt("biDim", biDim);
            for (int biBlock = biDim >> 1; biBlock > 0; biBlock >>= 1)
            {
                _fluidSimulationComputeShader.SetInt("biBlock", biBlock);
                _fluidSimulationComputeShader.Dispatch(_SortParticlesKernelId, Mathf.CeilToInt(MaxNumOfParticles / _sortParticlesThreadSize.x), 1, 1);
            }
        }

        if (_renderMethod == RenderMethod.RayTracing)
        {
            int MaxNumSteps = Mathf.CeilToInt(Mathf.Log(MaxNumOfParticles, 2));
            for (int i = 0; i < 32; i++)
            {
                _fluidSimulationComputeShader.SetInt("biBlock", i);
                _fluidSimulationComputeShader.Dispatch(_AssignMortonCodesKernelId, Mathf.CeilToInt(NumOfParticles / _assignMortonCodesThreadSize.x), 1, 1);
                for (int j = 1; j <= MaxNumSteps; j++)
                {
                    _fluidSimulationComputeShader.SetInt("biDim", j);
                    _fluidSimulationComputeShader.Dispatch(_PrefixSumKernelId, Mathf.CeilToInt(NumOfParticles / _prefixSumThreadSize.x), 1, 1);
                }

                _fluidSimulationComputeShader.Dispatch(_RadixSortKernelId, Mathf.CeilToInt(NumOfParticles / _radixSortThreadSize.x), 1, 1);
            }
        }
    }

    private void FixedUpdate()
    {
        UpdateNonAcceleratedRayTracer();
        UpdateFluidConstants();
        UpdateControlParticles();
    }

    private void Update()
    {


        Matrix4x4 VP = GL.GetGPUProjectionMatrix(secondCam.projectionMatrix, true) * Camera.main.worldToCameraMatrix;
        Shader.SetGlobalMatrix("_Perspective_Matrix_VP", VP);


        //Need command buffers in specific ordering.
        if (buffersInitialized == false)
        {
            buffersInitialized = true;

            return;
        }
        if (buffersAdded < 1)
        {

            CreateFluidCommandBuffers();
            buffersAdded += 1;
        }

        if (buffersAdded < 2)
        {
            buffersAdded += 1;
        }
    }

    void CreateNonAcceleratedStructure()
    {
        BuildTriangleList();
    }

    void BuildTriangleList()
    {
        /*
        _vertices.Clear();
        _indices.Clear();

        int indexMeshObject = 0;
        _meshObjects = new MeshObject[_rayTracedObjects.Count];
        for(int rIndex = 0; rIndex < _rayTracedObjects.Count; rIndex++)
        {
            Renderer r = _rayTracedObjects[rIndex];
            MeshFilter mf = r.GetComponent<MeshFilter>();
            if (mf)
            {
                Mesh m = mf.sharedMesh;
                int startVert = _vertices.Count;
                int startIndex = _indices.Count;

                for (int i = 0; i < m.vertices.Length; i++)
                {
                    Vertex v = new Vertex();
                    v.position = m.vertices[i];
                    if(i < m.normals.Length)
                        v.normal = m.normals[i];
                    if(i < m.uv.Length)
                        v.uv = m.uv[i];
                    _vertices.Add(v);
                }
                var indices = m.GetIndices(0);
                _indices.AddRange(indices.Select(index => index + startVert));

                // Add the object itself
                _meshObjects[indexMeshObject] = new MeshObject()
                    {
                        localToWorld = r.transform.localToWorldMatrix,
                        indicesOffset = startIndex,
                        indicesCount = indices.Length,
                        id = (uint)rIndex

                    };
                indexMeshObject++;
            }
        }
        */
        if (UnigmaRendererManager.Instance.unigmaRendererObjects.Length > 0)
        {
            _fluidSimulationComputeShader.SetBuffer(_CreateGridKernelId, "_Vertices", UnigmaRendererManager.Instance._verticesObjectBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CreateDistancesKernelId, "_Vertices", UnigmaRendererManager.Instance._verticesObjectBuffer);
            _fluidSimulationComputeShader.SetBuffer(_UpdateParticlesKernelId, "_Vertices", UnigmaRendererManager.Instance._verticesObjectBuffer);
            _fluidSimulationComputeShader.SetBuffer(_UpdatePositionsKernelId, "_Vertices", UnigmaRendererManager.Instance._verticesObjectBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CalculateCurlKernelId, "_Vertices", UnigmaRendererManager.Instance._verticesObjectBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CalculateVelocityKernelId, "_Vertices", UnigmaRendererManager.Instance._verticesObjectBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CreateGridKernelId, "_Indices", UnigmaRendererManager.Instance._indicesObjectBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CreateDistancesKernelId, "_Indices", UnigmaRendererManager.Instance._indicesObjectBuffer);
            _fluidSimulationComputeShader.SetBuffer(_UpdateParticlesKernelId, "_Indices", UnigmaRendererManager.Instance._indicesObjectBuffer);
            _fluidSimulationComputeShader.SetBuffer(_UpdatePositionsKernelId, "_Indices", UnigmaRendererManager.Instance._indicesObjectBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CalculateCurlKernelId, "_Indices", UnigmaRendererManager.Instance._indicesObjectBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CalculateVelocityKernelId, "_Indices", UnigmaRendererManager.Instance._indicesObjectBuffer);
        }

    }

    void UpdateNonAcceleratedRayTracer()
    {
        //Build the BVH
        SetFluidBuffers();
        UpdateParticles();
        BuildBVH();
        //if(Time.realtimeSinceStartup < 10)

        //Only if spacebar is pressed



    }

    void SetFluidBuffers()
    {

        _fluidSimulationComputeShader.SetBuffer(_CreateGridKernelId, "_MeshObjects", UnigmaRendererManager.Instance._unigmaRendererObjectBuffer);
        _fluidSimulationComputeShader.SetBuffer(_CreateDistancesKernelId, "_MeshObjects", UnigmaRendererManager.Instance._unigmaRendererObjectBuffer);
        _fluidSimulationComputeShader.SetBuffer(_UpdateParticlesKernelId, "_MeshObjects", UnigmaRendererManager.Instance._unigmaRendererObjectBuffer);
        _fluidSimulationComputeShader.SetBuffer(_UpdatePositionsKernelId, "_MeshObjects", UnigmaRendererManager.Instance._unigmaRendererObjectBuffer);
        _fluidSimulationComputeShader.SetBuffer(_CalculateControlDensityKernelId, "_MeshObjects", UnigmaRendererManager.Instance._unigmaRendererObjectBuffer);
        _fluidSimulationComputeShader.SetBuffer(_CalculateControlForcesKernelId, "_MeshObjects", UnigmaRendererManager.Instance._unigmaRendererObjectBuffer);
        _fluidSimulationComputeShader.SetBuffer(_CalculateCurlKernelId, "_MeshObjects", UnigmaRendererManager.Instance._unigmaRendererObjectBuffer);
        _fluidSimulationComputeShader.SetBuffer(_CalculateVelocityKernelId, "_MeshObjects", UnigmaRendererManager.Instance._unigmaRendererObjectBuffer);

        _controlParticlesBuffer.SetData(_controlParticlesArray);
        _fluidSimulationComputeShader.SetBuffer(_CalculateControlDensityKernelId, "_ControlParticles", _controlParticlesBuffer);
        _fluidSimulationComputeShader.SetBuffer(_CalculateControlForcesKernelId, "_ControlParticles", _controlParticlesBuffer);
        _fluidSimulationComputeShader.SetBuffer(_StoreControlParticleNeighborsKernelId, "_ControlParticles", _controlParticlesBuffer);

        //Add fluid objects.
        _fluidObjectsBuffer.SetData(_fluidObjectsArray);
        _fluidSimulationComputeShader.SetBuffer(_CalculateControlDensityKernelId, "_FluidObjects", _fluidObjectsBuffer);
        _fluidSimulationComputeShader.SetBuffer(_CalculateControlForcesKernelId, "_FluidObjects", _fluidObjectsBuffer);
        _fluidSimulationComputeShader.SetBuffer(_StoreControlParticleNeighborsKernelId, "_FluidObjects", _fluidObjectsBuffer);

    }

    public void AddParticles(Vector3 initialSpawnPosition, int numberOfParticles, Vector3 containerSize, int containerType, int phase, int type, float kelvin = 273.0f)
    {
        if (NumOfParticles >= MaxNumOfParticles)
        {
            return;
        }
        int sizeOfNewParticlesAdded = numberOfParticles;
        //Cubed root num of particles:
        int numOfParticlesCubedRoot = Mathf.CeilToInt(Mathf.Pow(sizeOfNewParticlesAdded, 1.0f / 3.0f));

        switch (containerType)
        {
            case 0:
                //Cube
                SpawnInCube(numOfParticlesCubedRoot, containerSize, initialSpawnPosition, phase, type, kelvin);
                break;
        }

        _particleBuffer.SetData(_particles, NumOfParticles, NumOfParticles, sizeOfNewParticlesAdded);
        NumOfParticles += sizeOfNewParticlesAdded;

        if (UnigmaSettings.GetIsRTXEnabled() && _renderMethod == RenderMethod.RayTracingAccelerated)
        {

            _AccelerationStructure.RemoveInstance(RayTracinghandle);
            RayTracinghandle = _AccelerationStructure.AddInstance(aabbList, (uint)NumOfParticles, true, Matrix4x4.identity, rayTracingMaterial, true, properties);

        }
    }
    public void ShootParticles(Vector3 initialSpawnPosition, int numberOfParticles, Vector4 force, Vector3 radius = default, int phase = 0, int type = 0, float kelvin = 273.0f)
    {
        if (NumOfParticles >= MaxNumOfParticles)
        {
            return;
        }
        int sizeOfNewParticlesAdded = numberOfParticles;
        //Cubed root num of particles:
        float numOfParticlesCubedRoot = Mathf.Pow(sizeOfNewParticlesAdded, 1.0f / 3.0f);
        float numOfParticlesSquaredRoot = Mathf.Sqrt(sizeOfNewParticlesAdded);
        //Create particles.
        //SpawnParticlesInBox();
        for (int i = NumOfParticles; i < NumOfParticles + sizeOfNewParticlesAdded; i++)
        {
            _ParticleIndices[i] = i;
            _ParticleIDs[i] = i;

            Vector3 randPoint = Random.insideUnitSphere;
            randPoint = new Vector3(randPoint.x * radius.x, randPoint.y * radius.y, randPoint.z * radius.z);
            Vector3 randomPos = randPoint + initialSpawnPosition;
            _particles[i].position = randomPos;
            _particles[i].velocity = Vector3.zero;
            _particles[i].curl = Vector3.zero;
            _particles[i].force = force;
            _particles[i].density = 0.0f;
            _particles[i].lambda = 0.0f;
            _particles[i].spring = 0.0f;
            _particles[i].predictedPosition = _particles[i].position;
            _particles[i].anisotropicTRS = Matrix4x4.identity;
            _particles[i].phase = phase;
            _particles[i].type = type;
            _particles[i].kelvin = kelvin;
            _particles[i].tempKelvin = kelvin;
        }
        _particleBuffer.SetData(_particles, NumOfParticles, NumOfParticles, sizeOfNewParticlesAdded);
        NumOfParticles += sizeOfNewParticlesAdded;


        if (UnigmaSettings.GetIsRTXEnabled() && _renderMethod == RenderMethod.RayTracingAccelerated)
        {

            _AccelerationStructure.RemoveInstance(RayTracinghandle);
            RayTracinghandle = _AccelerationStructure.AddInstance(aabbList, (uint)NumOfParticles, true, Matrix4x4.identity, rayTracingMaterial, true, properties);

        }
    }

    void SpawnInCube(int numberOfParticlesCubed, Vector3 containerSize, Vector3 initialSpawnPosition, int phase, int type, float kelvin = 273.0f)
    {
        float particleSpacing = containerSize.x / numberOfParticlesCubed;
        float halfContainerSize = containerSize.x / 2.0f;
        int particleIndex = NumOfParticles;
        for (int i = 0; i < numberOfParticlesCubed; i++)
        {
            for (int j = 0; j < numberOfParticlesCubed; j++)
            {
                for (int k = 0; k < numberOfParticlesCubed; k++)
                {
                    if (particleIndex >= MaxNumOfParticles)
                    {
                        return;
                    }
                    _ParticleIndices[particleIndex] = particleIndex;
                    _ParticleIDs[particleIndex] = particleIndex;
                    Vector3 randomPos = new Vector3(i * particleSpacing - halfContainerSize, j * particleSpacing - halfContainerSize, k * particleSpacing - halfContainerSize);
                    _particles[particleIndex].position = initialSpawnPosition + randomPos;
                    _particles[particleIndex].velocity = Vector3.zero;
                    _particles[particleIndex].force = Vector3.zero;
                    _particles[particleIndex].density = 0.0f;
                    _particles[particleIndex].lambda = 0.0f;
                    _particles[particleIndex].predictedPosition = _particles[particleIndex].position;
                    _particles[particleIndex].anisotropicTRS = Matrix4x4.identity;
                    _particles[particleIndex].phase = phase;
                    _particles[particleIndex].type = type;
                    _particles[particleIndex].kelvin = kelvin;
                    _particles[particleIndex].tempKelvin = kelvin;
                    particleIndex++;
                }
            }
        }
    }

    //To build the BVH we need to take all of the game objects in our raytracing list.
    //Afterwards place them in a tree with the root node containing all of the objects.
    //The bounding box is calculated by finding the min and max vertices for each axis.
    //If the ray intersects the box we search the triangles of that node, if not we traverse another node and ignore the children.
    unsafe void BuildBVH()
    {
        //First traverse through all of the objects and create their bounding boxes.

        //Get positions stored them to mesh objects.

        //Update position of mesh objects.
        if (_BVHNodesBuffer == null)
        {
            //_BVHNodesBuffer = new ComputeBuffer(_BVHNodes.Length, _BVHStride);
            //_particleIDsBuffer = new ComputeBuffer(_ParticleIDs.Length, 4);
        }

        /*
        //Update Particle BVH.
        int rootNodeIndex = 0;
        nodesUsed = 1;
        //Initialize nodes, rebuild BVH each frame set to 0 index, and the number of particles to all.
        _BVHNodes[rootNodeIndex].index = rootNodeIndex;
        _BVHNodes[rootNodeIndex].leftChild = 0;
        _BVHNodes[rootNodeIndex].parent = -1;
        _BVHNodes[rootNodeIndex].primitiveOffset = 0;
        _BVHNodes[rootNodeIndex].primitiveCount = NumOfParticles;

        UpdateNodeBounds(rootNodeIndex);
        SubdivideBVH(rootNodeIndex);
        CreateHitMissLinks();
        */
        //Set the BVH to the compute shader.
        //_BVHNodesBuffer.SetData(_BVHNodes);
        //_particleIDsBuffer.SetData(_ParticleIDs);
        _fluidSimulationComputeShader.SetBuffer(_CreateGridKernelId, "_BVHNodes", _BVHNodesBuffer);
        _fluidSimulationComputeShader.SetBuffer(_CreateGridKernelId, "_ParticleIDs", _particleIDsBuffer);
        _fluidSimulationComputeShader.SetBuffer(_UpdateParticlesKernelId, "_BVHNodes", _BVHNodesBuffer);
        _fluidSimulationComputeShader.SetBuffer(_UpdateParticlesKernelId, "_ParticleIDs", _particleIDsBuffer);
        _fluidSimulationComputeShader.SetBuffer(_UpdatePositionsKernelId, "_BVHNodes", _BVHNodesBuffer);
        _fluidSimulationComputeShader.SetBuffer(_UpdatePositionsKernelId, "_ParticleIDs", _particleIDsBuffer);
        _fluidSimulationComputeShader.SetBuffer(_CalculateCurlKernelId, "_BVHNodes", _BVHNodesBuffer);
        _fluidSimulationComputeShader.SetBuffer(_CalculateCurlKernelId, "_ParticleIDs", _particleIDsBuffer);
        _fluidSimulationComputeShader.SetBuffer(_CalculateVelocityKernelId, "_BVHNodes", _BVHNodesBuffer);
        _fluidSimulationComputeShader.SetBuffer(_CalculateVelocityKernelId, "_ParticleIDs", _particleIDsBuffer);
        _fluidSimulationComputeShader.SetInt("_NumOfNodes", NumOfParticles -1);
        _fluidSimulationComputeShader.SetInt("_NumOfParticles", NumOfParticles);
        _fluidSimulationComputeShader.SetInt("_NumOfControlParticles", NumOfControlParticles);
        _fluidSimulationComputeShader.SetInt("_MaxNumOfParticles", MaxNumOfParticles);
        _fluidSimulationComputeShader.SetInt("_MaxNumOfControlParticles", MaxNumOfControlParticles);
        //PrintBVH();

    }

    void UpdateNodeBounds(int nodeIndex)
    {
        //Make lowest possible so that we can make it the size of the furthest and closet particle.
        _BVHNodes[nodeIndex].aabbMin = new Vector3(1e30f, 1e30f, 1e30f);
        _BVHNodes[nodeIndex].aabbMax = new Vector3(-1e30f, -1e30f, -1e30f);

        for (int i = _BVHNodes[nodeIndex].primitiveOffset; i < _BVHNodes[nodeIndex].primitiveCount + _BVHNodes[nodeIndex].primitiveOffset; i++)
        {
            int particleIndex = _ParticleIDs[i];
            Vector3 particlePos = _particles[particleIndex].position;
            float sizeOfParticleSquared = SizeOfParticle / SizeOfParticle;
            Vector3 sizeOfParticle = 1 * new Vector3(sizeOfParticleSquared, sizeOfParticleSquared, sizeOfParticleSquared);
            _BVHNodes[nodeIndex].aabbMin = Vector3.Min(_BVHNodes[nodeIndex].aabbMin, particlePos - sizeOfParticle);
            _BVHNodes[nodeIndex].aabbMax = Vector3.Max(_BVHNodes[nodeIndex].aabbMax, particlePos + sizeOfParticle);
        }
    }

    void SubdivideBVH(int nodeIndex)
    {
        if (_BVHNodes[nodeIndex].primitiveCount <= 128)
        {
            return;
        }
        Vector3 extent = _BVHNodes[nodeIndex].aabbMax - _BVHNodes[nodeIndex].aabbMin;
        int axis = 0;
        if (extent.y > extent.x) axis = 1;
        if (extent.z > extent[axis]) axis = 2;

        float splitPos = _BVHNodes[nodeIndex].aabbMin[axis] + extent[axis] * 0.5f;

        int i = _BVHNodes[nodeIndex].primitiveOffset;
        int n = i + _BVHNodes[nodeIndex].primitiveCount - 1;

        while (i <= n)
        {
            if (_particles[_ParticleIDs[i]].position[axis] < splitPos)
            {
                i++;
            }
            else
            {
                SwapParticles(i, n);
                n--;
            }
        }

        int leftCount = i - _BVHNodes[nodeIndex].primitiveOffset;
        if (leftCount == 0 || leftCount == _BVHNodes[nodeIndex].primitiveCount)
        {
            return;
        }

        int leftChildIndex = nodesUsed++;
        

        _BVHNodes[leftChildIndex].index = leftChildIndex;
        _BVHNodes[leftChildIndex].parent = nodeIndex;
        _BVHNodes[leftChildIndex].primitiveOffset = _BVHNodes[nodeIndex].primitiveOffset;
        _BVHNodes[leftChildIndex].primitiveCount = leftCount;
        _BVHNodes[nodeIndex].leftChild = leftChildIndex;
        
        UpdateNodeBounds(leftChildIndex);
        SubdivideBVH(leftChildIndex);
        int rightChildIndex = nodesUsed++;

        _BVHNodes[rightChildIndex].index = rightChildIndex;
        _BVHNodes[rightChildIndex].parent = nodeIndex;
        _BVHNodes[rightChildIndex].primitiveOffset = i;
        _BVHNodes[rightChildIndex].primitiveCount = _BVHNodes[nodeIndex].primitiveCount - leftCount;
        _BVHNodes[nodeIndex].primitiveCount = 0;
        _BVHNodes[nodeIndex].rightChild = rightChildIndex;

        UpdateNodeBounds(rightChildIndex);
        SubdivideBVH(rightChildIndex);


    }

    void CreateHitMissLinks()
    {
        for (int i = 0; i < nodesUsed; i++)
        {
            BVHNode node = _BVHNodes[i];
            _BVHNodes[i].hit = -1;
            _BVHNodes[i].miss = -1;

            
            if (_BVHNodes[i].index + 1 >= nodesUsed)
            {
                _BVHNodes[i].hit = -1;
                _BVHNodes[i].miss = -1;
                continue;
            }

            _BVHNodes[i].hit = _BVHNodes[i].index + 1;


            if (node.primitiveCount > 0)
            {
                _BVHNodes[i].miss = _BVHNodes[i].index + 1;
                continue;
            }

            while (node.parent > -1)
            {
                
                if (node.index == _BVHNodes[node.parent].leftChild && _BVHNodes[node.parent].rightChild != -1)
                {
                    _BVHNodes[i].miss = _BVHNodes[node.parent].rightChild;
                    break;
                }
                node = _BVHNodes[node.parent];
            }




        }
    }

    void SwapParticles(int i, int j)
    {
        int temp = _ParticleIDs[i];
        _ParticleIDs[i] = _ParticleIDs[j];
        _ParticleIDs[j] = temp;
    }

    void PrintParticleData()
    {
        for (int i = 0; i < NumOfParticles; i++)
        {
            //Debug each particle struct.
            string log = "Particle ID: " + i + " Position: " + _particles[i].position + "Predicted Position: " + _particles[i].predictedPosition + " Velocity: " + _particles[i].velocity + " Force: " + _particles[i].force  + " Density: " + _particles[i].density.ToString("F6") + " Lambda: " + _particles[i].lambda.ToString("F6");
            Debug.Log(log);
        }
    }

    void PrintNeighborData()
    {
        uint[] neighbors = _particleNeighborsArray;
        _particleNeighbors.GetData(neighbors);
        for (int i = 0; i < _particleNeighbors.count; i++)
        {
            string log = "Particle ID: " + i + " ParticleNeighbors: ";

            for (int j = 0; j < 27; j++)
            {
                log += "{ ";
                log += neighbors[i * 27 + j];
                log += " }";
            }

            Debug.Log(log);
        }
    }

    void PrintBVH()
    {
        for (int i = 0; i < nodesUsed; i++)
        {
            string log = "ID: " + i + " Node: " + _BVHNodes[i].index + " AABB Min: " + _BVHNodes[i].aabbMin + " AABB Max: " + _BVHNodes[i].aabbMax + " Left Child: " + _BVHNodes[i].leftChild + " Right Child: " + _BVHNodes[i].rightChild + " Primitive Count: " + _BVHNodes[i].primitiveCount + " Primitive Offset: " + _BVHNodes[i].primitiveOffset + " Parent: " + _BVHNodes[i].parent + " Hit: " + _BVHNodes[i].hit + " Miss: " + _BVHNodes[i].miss;
            //Debug.Log(log);
            for (int j = _BVHNodes[i].primitiveOffset; j < _BVHNodes[i].primitiveCount + _BVHNodes[i].primitiveOffset; j++)
            {
                log += " Particle " + _ParticleIDs[j];
            }
            Debug.Log(log);
        }
    }
    
    void UpdateParticles()
    {
        if (_particleBuffer == null)
        {
            _particleBuffer = new ComputeBuffer(MaxNumOfParticles, _particleStride );
            _particleBuffer.name = "ParticlesBuffer";
            _particlePositionsBuffer = new ComputeBuffer(MaxNumOfParticles, sizeof(float) * 3);
            _particleIndicesBuffer = new ComputeBuffer(MaxNumOfParticles, sizeof(int));
            _particleCellIndicesBuffer = new ComputeBuffer(MaxNumOfParticles, sizeof(int));
            _particleCellOffsets = new ComputeBuffer(MaxNumOfParticles, sizeof(int));
            _particleCountBuffer = new ComputeBuffer(MaxNumOfParticles, sizeof(int));
            _MortonCodesBuffer = new ComputeBuffer(_MortonCodes.Length, _MortonCodeStride);
            _MortonCodesTempBuffer = new ComputeBuffer(_MortonCodes.Length, _MortonCodeStride);
            _MortonPrefixSumOffsetOnesBuffer = new ComputeBuffer(_MortonCodes.Length, sizeof(uint));
            _MortonPrefixSumOffsetZeroesBuffer = new ComputeBuffer(_MortonCodes.Length, sizeof(uint));
            _MortonPrefixSumTotalZeroesBuffer = new ComputeBuffer(_MortonCodes.Length, sizeof(uint));
            aabbList = new GraphicsBuffer(GraphicsBuffer.Target.Structured, MaxNumOfParticles, 6 * sizeof(float));

            //Cubed root num of particles:
            float numOfParticlesCubedRoot = Mathf.Pow(NumOfParticles, 1.0f / 3.0f);
            float numOfParticlesSquaredRoot = Mathf.Sqrt(NumOfParticles);
            Vector3 boxSize = Vector3.Min(_BoxSize, Vector3.one * 50);
            //Create particles.
            for (int i = 0; i < MaxNumOfParticles; i++)
            {
                _ParticleIndices[i] = i;
                _ParticleIDs[i] = i;
                _particles[i].position = new Vector3(99999, 99999, 99999);//new Vector3( (i % numOfParticlesCubedRoot) / ((1/ boxSize.x)* numOfParticlesCubedRoot) - (boxSize.x*0.5f), ((i / numOfParticlesCubedRoot) % numOfParticlesCubedRoot) / ( (1/ boxSize.y)* numOfParticlesCubedRoot) - (boxSize.y * 0.5f), ((i / numOfParticlesSquaredRoot) % numOfParticlesCubedRoot) / ((1/ boxSize.z) * numOfParticlesCubedRoot) - (boxSize.z * 0.5f));
                _particlesPositions[i] = _particles[i].position;
                _particles[i].velocity = Vector3.zero;
                _particles[i].force = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
                _particles[i].density = 0.0f;
                _particles[i].lambda = 0.0f;
                _particles[i].spring = 0.0f;
                _particles[i].normal = Vector3.zero;
                _particles[i].predictedPosition = _particles[i].position;
                _particles[i].anisotropicTRS = Matrix4x4.identity;
                _particles[i].phase = 0;
                _particles[i].type = 0;
                _particles[i].kelvin = 273.0f;
                _particles[i].tempKelvin = 273.0f;
                _ParticleIndices[i] = MaxNumOfParticles-1;
                _ParticleCount[i] = 0;
                _ParticleCellIndices[i] = MaxNumOfParticles-1;
                _ParticleCellOffsets[i] = MaxNumOfParticles-1;
                AABB aabb = new AABB();
                //aabb.min = -new Vector3(SizeOfParticle, SizeOfParticle, SizeOfParticle);
                //aabb.max = new Vector3(SizeOfParticle, SizeOfParticle, SizeOfParticle);

                Vector3 center = _particles[i].position*0;
                Vector3 size = new Vector3(SizeOfParticle, SizeOfParticle, SizeOfParticle)*10;

                aabb.min = center - size;
                aabb.max = center + size;

                aabbs[i] = aabb;
            }

            if (properties == null)
            {
                properties = new MaterialPropertyBlock();
            }
            
            aabbList.SetData(aabbs);
            _MortonCodesBuffer.SetData(_MortonCodes);
            _MortonCodesTempBuffer.SetData(_MortonCodesTemp);
            _particleIndicesBuffer.SetData(_ParticleIndices);
            _particleBuffer.SetData(_particles);
            _particlePositionsBuffer.SetData(_particlesPositions);
            _particleCellIndicesBuffer.SetData(_ParticleCellIndices);
            _particleCellOffsets.SetData(_ParticleCellOffsets);
            _particleCountBuffer.SetData(_ParticleCount);
            properties.SetBuffer("_Particles", _particleBuffer);

            Debug.Log("Space Time Compute Buffer: " + UnigmaSpaceTime.Instance._spaceTimePointsBuffer.count);
            _fluidSimulationComputeShader.SetBuffer(_ComputeForcesKernelId, "_VectorField", UnigmaSpaceTime.Instance._spaceTimePointsBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CalculateSpatialDiffusionKernelId, "_VectorField", UnigmaSpaceTime.Instance._spaceTimePointsBuffer);

            _fluidSimulationComputeShader.SetBuffer(_UpdateParticlesKernelId, "_Particles", _particleBuffer);
            _fluidSimulationComputeShader.SetBuffer(_UpdatePositionsKernelId, "g_AABBs", aabbList);
            _fluidSimulationComputeShader.SetBuffer(_StoreParticleNeighborsKernelId, "_particleNeighbors", _particleNeighbors);
            _fluidSimulationComputeShader.SetBuffer(_StoreControlParticleNeighborsKernelId, "_particleNeighbors", _particleNeighbors);
            _fluidSimulationComputeShader.SetBuffer(_ComputeDensityKernelId, "_particleNeighbors", _particleNeighbors);
            _fluidSimulationComputeShader.SetBuffer(_UpdatePositionDeltasKernelId, "_particleNeighbors", _particleNeighbors);
            _fluidSimulationComputeShader.SetBuffer(_CalculateCurlKernelId, "_particleNeighbors", _particleNeighbors);
            _fluidSimulationComputeShader.SetBuffer(_CalculateVorticityKernelId, "_particleNeighbors", _particleNeighbors);
            _fluidSimulationComputeShader.SetBuffer(_CalculateControlDensityKernelId, "_particleNeighbors", _particleNeighbors);
            _fluidSimulationComputeShader.SetBuffer(_CalculateControlForcesKernelId, "_particleNeighbors", _particleNeighbors);
            _fluidSimulationComputeShader.SetBuffer(_CalculateSpatialDiffusionKernelId, "_particleNeighbors", _particleNeighbors);
            //_fluidSimulationComputeShader.SetBuffer(_ComputeDensityKernelId, "_particleNeighbors", _particleNeighbors);

            //Set particle buffer to shader.
            _fluidSimulationComputeShader.SetBuffer(_CreateGridKernelId, "_Particles", _particleBuffer);
            _fluidSimulationComputeShader.SetBuffer(_ComputeForcesKernelId, "_Particles", _particleBuffer);
            _fluidSimulationComputeShader.SetBuffer(_ComputeDensityKernelId, "_Particles", _particleBuffer);
            _fluidSimulationComputeShader.SetBuffer(_UpdatePositionDeltasKernelId, "_Particles", _particleBuffer);
            _fluidSimulationComputeShader.SetBuffer(_UpdatePositionsKernelId, "_Particles", _particleBuffer);
            _fluidSimulationComputeShader.SetBuffer(_UpdatePositionsKernelId, "_ParticlesPositions", _particlePositionsBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CalculateVelocityKernelId, "_Particles", _particleBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CalculateVelocityKernelId, "_ParticlesPositions", _particlePositionsBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CalculateCurlKernelId, "_ParticlesPositions", _particlePositionsBuffer);
            _fluidSimulationComputeShader.SetBuffer(_HashParticlesKernelId, "_Particles", _particleBuffer);
            _fluidSimulationComputeShader.SetBuffer(_SortParticlesKernelId, "_Particles", _particleBuffer);
            _fluidSimulationComputeShader.SetBuffer(_RadixSortKernelId, "_Particles", _particleBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CalculateCurlKernelId, "_Particles", _particleBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CalculateVorticityKernelId, "_Particles", _particleBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CreateBVHTreeKernelId, "_Particles", _particleBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CreateBoundingBoxKernelId, "_Particles", _particleBuffer);
            _fluidSimulationComputeShader.SetBuffer(_StoreParticleNeighborsKernelId, "_Particles", _particleBuffer);
            _fluidSimulationComputeShader.SetBuffer(_StoreControlParticleNeighborsKernelId, "_Particles", _particleBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CalculateControlDensityKernelId, "_Particles", _particleBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CalculateControlForcesKernelId, "_Particles", _particleBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CalculateSpatialDiffusionKernelId, "_Particles", _particleBuffer);

            _fluidSimulationComputeShader.SetBuffer(_CreateBVHTreeKernelId, "_BVHNodes", _BVHNodesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CalculateCellOffsetsKernelId, "_BVHNodes", _BVHNodesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CreateBoundingBoxKernelId, "_BVHNodes", _BVHNodesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_AssignIndexKernelId, "_BVHNodes", _BVHNodesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_AssignParentsKernelId, "_BVHNodes", _BVHNodesBuffer);

            _fluidSimulationComputeShader.SetBuffer(_UpdateParticlesKernelId, "_ParticleIndices", _particleIndicesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CreateGridKernelId, "_ParticleIndices", _particleIndicesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_ComputeForcesKernelId, "_ParticleIndices", _particleIndicesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_ComputeDensityKernelId, "_ParticleIndices", _particleIndicesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_UpdatePositionDeltasKernelId, "_ParticleIndices", _particleIndicesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_UpdatePositionsKernelId, "_ParticleIndices", _particleIndicesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CalculateVelocityKernelId, "_ParticleIndices", _particleIndicesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_HashParticlesKernelId, "_ParticleIndices", _particleIndicesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_SortParticlesKernelId, "_ParticleIndices", _particleIndicesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_RadixSortKernelId, "_ParticleIndices", _particleIndicesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CalculateCellOffsetsKernelId, "_ParticleIndices", _particleIndicesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CalculateCurlKernelId, "_ParticleIndices", _particleIndicesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CalculateVorticityKernelId, "_ParticleIndices", _particleIndicesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_StoreParticleNeighborsKernelId, "_ParticleIndices", _particleIndicesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_StoreControlParticleNeighborsKernelId, "_ParticleIndices", _particleIndicesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CalculateControlDensityKernelId, "_ParticleIndices", _particleIndicesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CalculateControlForcesKernelId, "_ParticleIndices", _particleIndicesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CalculateSpatialDiffusionKernelId, "_ParticleIndices", _particleIndicesBuffer);


            _fluidSimulationComputeShader.SetBuffer(_StoreParticleNeighborsKernelId, "_ParticleCellIndices", _particleCellIndicesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_StoreControlParticleNeighborsKernelId, "_ParticleCellIndices", _particleCellIndicesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_UpdateParticlesKernelId, "_ParticleCellIndices", _particleCellIndicesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CreateGridKernelId, "_ParticleCellIndices", _particleCellIndicesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_ComputeForcesKernelId, "_ParticleCellIndices", _particleCellIndicesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_ComputeDensityKernelId, "_ParticleCellIndices", _particleCellIndicesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_UpdatePositionDeltasKernelId, "_ParticleCellIndices", _particleCellIndicesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_UpdatePositionsKernelId, "_ParticleCellIndices", _particleCellIndicesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CalculateVelocityKernelId, "_ParticleCellIndices", _particleCellIndicesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_HashParticlesKernelId, "_ParticleCellIndices", _particleCellIndicesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_SortParticlesKernelId, "_ParticleCellIndices", _particleCellIndicesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_RadixSortKernelId, "_ParticleCellIndices", _particleCellIndicesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CalculateCellOffsetsKernelId, "_ParticleCellIndices", _particleCellIndicesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CalculateCurlKernelId, "_ParticleCellIndices", _particleCellIndicesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CalculateVorticityKernelId, "_ParticleCellIndices", _particleCellIndicesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CalculateControlDensityKernelId, "_ParticleCellIndices", _particleCellIndicesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CalculateControlForcesKernelId, "_ParticleCellIndices", _particleCellIndicesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CalculateSpatialDiffusionKernelId, "_ParticleCellIndices", _particleCellIndicesBuffer);


            _fluidSimulationComputeShader.SetBuffer(_CalculateCellOffsetsKernelId, "_ParticleIDs", _particleIDsBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CreateBoundingBoxKernelId, "_ParticleIDs", _particleIDsBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CreateBVHTreeKernelId, "_ParticleIDs", _particleIDsBuffer);
            _fluidSimulationComputeShader.SetBuffer(_StoreParticleNeighborsKernelId, "_ParticleIDs", _particleIDsBuffer);
            _fluidSimulationComputeShader.SetBuffer(_StoreControlParticleNeighborsKernelId, "_ParticleIDs", _particleIDsBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CalculateControlDensityKernelId, "_ParticleIDs", _particleIDsBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CalculateControlForcesKernelId, "_ParticleIDs", _particleIDsBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CalculateSpatialDiffusionKernelId, "_ParticleIDs", _particleIDsBuffer);


            _fluidSimulationComputeShader.SetBuffer(_UpdateParticlesKernelId, "_ParticleCellOffsets", _particleCellOffsets);
            _fluidSimulationComputeShader.SetBuffer(_CreateGridKernelId, "_ParticleCellOffsets", _particleCellOffsets);
            _fluidSimulationComputeShader.SetBuffer(_ComputeForcesKernelId, "_ParticleCellOffsets", _particleCellOffsets);
            _fluidSimulationComputeShader.SetBuffer(_ComputeDensityKernelId, "_ParticleCellOffsets", _particleCellOffsets);
            _fluidSimulationComputeShader.SetBuffer(_UpdatePositionDeltasKernelId, "_ParticleCellOffsets", _particleCellOffsets);
            _fluidSimulationComputeShader.SetBuffer(_UpdatePositionsKernelId, "_ParticleCellOffsets", _particleCellOffsets);
            _fluidSimulationComputeShader.SetBuffer(_CalculateVelocityKernelId, "_ParticleCellOffsets", _particleCellOffsets);
            _fluidSimulationComputeShader.SetBuffer(_HashParticlesKernelId, "_ParticleCellOffsets", _particleCellOffsets);
            _fluidSimulationComputeShader.SetBuffer(_SortParticlesKernelId, "_ParticleCellOffsets", _particleCellOffsets);
            _fluidSimulationComputeShader.SetBuffer(_RadixSortKernelId, "_ParticleCellOffsets", _particleCellOffsets);
            _fluidSimulationComputeShader.SetBuffer(_CalculateCellOffsetsKernelId, "_ParticleCellOffsets", _particleCellOffsets);
            _fluidSimulationComputeShader.SetBuffer(_CalculateCurlKernelId, "_ParticleCellOffsets", _particleCellOffsets);
            _fluidSimulationComputeShader.SetBuffer(_CalculateVorticityKernelId, "_ParticleCellOffsets", _particleCellOffsets);
            _fluidSimulationComputeShader.SetBuffer(_StoreParticleNeighborsKernelId, "_ParticleCellOffsets", _particleCellOffsets);
            _fluidSimulationComputeShader.SetBuffer(_StoreControlParticleNeighborsKernelId, "_ParticleCellOffsets", _particleCellOffsets);
            _fluidSimulationComputeShader.SetBuffer(_CalculateControlDensityKernelId, "_ParticleCellOffsets", _particleCellOffsets);
            _fluidSimulationComputeShader.SetBuffer(_CalculateControlForcesKernelId, "_ParticleCellOffsets", _particleCellOffsets);
            _fluidSimulationComputeShader.SetBuffer(_CalculateSpatialDiffusionKernelId, "_ParticleCellOffsets", _particleCellOffsets);


            _fluidSimulationComputeShader.SetBuffer(_SortParticlesKernelId, "_ParticleCount", _particleCountBuffer);
            _fluidSimulationComputeShader.SetBuffer(_RadixSortKernelId, "_ParticleCount", _particleCountBuffer);
            _fluidSimulationComputeShader.SetBuffer(_HashParticlesKernelId, "_ParticleCount", _particleCountBuffer);
            _fluidSimulationComputeShader.SetBuffer(_PrefixSumKernelId, "_ParticleCount", _particleCountBuffer);

            _fluidSimulationComputeShader.SetBuffer(_RadixSortKernelId, "_MortonCodes", _MortonCodesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CalculateCellOffsetsKernelId, "_MortonCodes", _MortonCodesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_RadixSortKernelId, "_MortonCodesTemp", _MortonCodesTempBuffer);
            _fluidSimulationComputeShader.SetBuffer(_HashParticlesKernelId, "_MortonCodes", _MortonCodesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_HashParticlesKernelId, "_MortonCodesTemp", _MortonCodesTempBuffer);
            _fluidSimulationComputeShader.SetBuffer(_PrefixSumKernelId, "_MortonCodes", _MortonCodesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_AssignMortonCodesKernelId, "_MortonCodes", _MortonCodesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CreateBVHTreeKernelId, "_MortonCodes", _MortonCodesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CreateBoundingBoxKernelId, "_MortonCodes", _MortonCodesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_PrefixSumKernelId, "_MortonCodesTemp", _MortonCodesTempBuffer);
            _fluidSimulationComputeShader.SetBuffer(_AssignMortonCodesKernelId, "_MortonCodesTemp", _MortonCodesTempBuffer);
            _fluidSimulationComputeShader.SetBuffer(_AssignParentsKernelId, "_MortonCodes", _MortonCodesBuffer);


            _fluidSimulationComputeShader.SetBuffer(_HashParticlesKernelId, "_MortonPrefixSumTotalZeroes", _MortonPrefixSumTotalZeroesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_HashParticlesKernelId, "_MortonPrefixSumOffsetOnes", _MortonPrefixSumOffsetOnesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_HashParticlesKernelId, "_MortonPrefixSumOffsetZeroes", _MortonPrefixSumOffsetZeroesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_RadixSortKernelId, "_MortonPrefixSumTotalZeroes", _MortonPrefixSumTotalZeroesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_RadixSortKernelId, "_MortonPrefixSumOffsetOnes", _MortonPrefixSumOffsetOnesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_RadixSortKernelId, "_MortonPrefixSumOffsetZeroes", _MortonPrefixSumOffsetZeroesBuffer);

            _fluidSimulationComputeShader.SetBuffer(_PrefixSumKernelId, "_MortonPrefixSumTotalZeroes", _MortonPrefixSumTotalZeroesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_PrefixSumKernelId, "_MortonPrefixSumOffsetOnes", _MortonPrefixSumOffsetOnesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_AssignMortonCodesKernelId, "_MortonPrefixSumOffsetOnes", _MortonPrefixSumOffsetOnesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_PrefixSumKernelId, "_MortonPrefixSumOffsetZeroes", _MortonPrefixSumOffsetZeroesBuffer);

        }
        for (int i = 0; i < NumOfParticles; i++)
        {
            _ParticleIDs[i] = i;
            _BVHNodes[i].primitiveOffset = 0;
            _BVHNodes[i].primitiveCount = 0;
            _BVHNodes[i].parent = -1;
            _BVHNodes[i].leftChild = -1;
            _BVHNodes[i].rightChild = -1;
        }

        if (NumOfParticles > 0)
        {
            ComputeForces();

            HashParticles();
            //quick morton code debug.log.

            SortParticles();
            CalculateCellOffsets();
            StoreParticleNeighbors();
            if (_renderMethod == RenderMethod.RayTracing)
                CreateBVHTree();

            //DebugParticlesBVH();
            if (NumOfControlParticles > 0)
            {
                ControlDensity();
                ControlForces();
            }
            for (int i = 0; i < _SolveIterations; i++)
            {
                ComputeDensity();
                ComputePositionDelta();
                UpdatePredictedPositions();
            }

            ComputeVelocity();
            ComputeCurl();
            ComputeVorticity();
            ComputePositions();
            ComputeSpatialDiffusion();
            //DebugParticlesBVH();
            //Set Particle positions to script.
            //NOT NEEDED. MOVE ENTIRE BVH TO GPU!!!!
            //_particleBuffer.GetData(_particles);

            //Get data asynchronously from the GPU. Gets MeshObjectData.
            //ReactToForces();

        }

    }

    private void InitializeControlParticles()
    {
        for (int i = 0; i < MaxNumOfControlParticles; i++)
            controlParticlesPositions[i] = new Vector3(999999999, 999999999, 999999999);

    }

    private void UpdateControlParticles()
    {
        int currentIndex = 0;
        for (int i = 0; i < fluidControlledObjects.Count; i++)
        {
            FluidControl fluidControl = fluidControlledObjects[i];

            AddControlParticles(fluidControl, currentIndex, i);

            currentIndex += fluidControl.points.Length;
        }


        NumOfControlParticles = Mathf.Min(currentIndex, MaxNumOfControlParticles);
    }

    void AddControlParticles(FluidControl fluidControl, int startIndex, int objectId)
    {
        for (int i = 0; i < fluidControl.points.Length; i++)
        {
            int index = i + startIndex;
            _controlParticlesArray[index].prevPosition = _controlParticlesArray[index].position;
            _controlParticlesArray[index].position = fluidControl.points[i];

            //Set various other properties.
            _controlParticlesArray[index].objectId = objectId;
        }
    }

    void ComputeForces()
    {
        _fluidSimulationComputeShader.Dispatch(_ComputeForcesKernelId, Mathf.CeilToInt(NumOfParticles / _computeForcesThreadSize.x), 1, 1);

    }

    void HashParticles()
    {
        _fluidSimulationComputeShader.Dispatch(_HashParticlesKernelId, Mathf.CeilToInt(MaxNumOfParticles / _hashParticlesThreadSize.x), 1, 1);
    }

    void CalculateCellOffsets()
    {
        _fluidSimulationComputeShader.Dispatch(_CalculateCellOffsetsKernelId, Mathf.CeilToInt(NumOfParticles / _calculateCellOffsetsThreadSize.x), 1, 1);
    }

    void StoreParticleNeighbors()
    {
        _fluidSimulationComputeShader.Dispatch(_StoreParticleNeighborsKernelId, Mathf.CeilToInt((NumOfParticles*27) / _storeParticleNeighborsThreadSize.x), 1, 1);
        if(NumOfControlParticles > 0)
            _fluidSimulationComputeShader.Dispatch(_StoreControlParticleNeighborsKernelId, Mathf.CeilToInt((NumOfControlParticles * 27) / _storeControlParticleNeighborsThreadSize.x), 1, 1);
    }

    void ComputeDensity()
    {
        _fluidSimulationComputeShader.Dispatch(_ComputeDensityKernelId, Mathf.CeilToInt(NumOfParticles / _computeDensityThreadSize.x), 1, 1);
    }

    void ComputePositionDelta()
    {
        _fluidSimulationComputeShader.Dispatch(_UpdatePositionDeltasKernelId, Mathf.CeilToInt(NumOfParticles / _updatePositionDeltasThreadSize.x), 1, 1);
    }

    void UpdatePredictedPositions()
    {
        _fluidSimulationComputeShader.Dispatch(_UpdateParticlesKernelId, Mathf.CeilToInt(NumOfParticles / _updateParticlesThreadSize.x), 1, 1);
    }

    void ComputeCurl()
    {
        _fluidSimulationComputeShader.Dispatch(_CalculateCurlKernelId, Mathf.CeilToInt(NumOfParticles / _calculateCurlThreadSize.x), 1, 1);
    }

    void ComputePositions()
    {
        _fluidSimulationComputeShader.Dispatch(_UpdatePositionsKernelId, Mathf.CeilToInt(NumOfParticles / _updatePositionsThreadSize.x), 1, 1);
    }

    void ComputeVorticity()
    {
        _fluidSimulationComputeShader.Dispatch(_CalculateVorticityKernelId, Mathf.CeilToInt(NumOfParticles / _calculateVorticityThreadSize.x), 1, 1);
    }

    void CreateBVHTree()
    {
        _fluidSimulationComputeShader.Dispatch(_CreateBVHTreeKernelId, Mathf.CeilToInt(NumOfParticles / _createBVHTreeThreadSize.x), 1, 1);
        _fluidSimulationComputeShader.Dispatch(_AssignParentsKernelId, Mathf.CeilToInt((NumOfParticles - 1) / _assignParentsThreadSize.x), 1, 1);
        _fluidSimulationComputeShader.Dispatch(_CreateBoundingBoxKernelId, Mathf.CeilToInt((NumOfParticles - 1) / _createBoundingBoxThreadSize.x), 1, 1);
    }

    void ControlDensity()
    {
        _fluidSimulationComputeShader.Dispatch(_CalculateControlDensityKernelId, Mathf.CeilToInt((NumOfControlParticles) / _calculateControlDensityThreadSize.x), 1, 1);
    }

    void ControlForces()
    {
        _fluidSimulationComputeShader.Dispatch(_CalculateControlForcesKernelId, Mathf.CeilToInt((NumOfParticles) / _calculateControlForcesThreadSize.x), 1, 1);
    }

    void ComputeVelocity()
    {
        _fluidSimulationComputeShader.Dispatch(_CalculateVelocityKernelId, Mathf.CeilToInt(NumOfParticles / _calculateVelocityThreadSize.x), 1, 1);
    }

    void ComputeSpatialDiffusion()
    {
        _fluidSimulationComputeShader.Dispatch(_CalculateSpatialDiffusionKernelId, Mathf.CeilToInt(UnigmaSpaceTime.Instance._NumOfVectors / _calculateSpatialDiffusionThreadSize.x), 1, 1);
    }

    void DebugParticlesBVH()
    {
        if (Input.GetKey(KeyCode.Space))
        {
            _MortonCodesBuffer.GetData(_MortonCodes);
            _particleIDsBuffer.GetData(_ParticleIDs);
            _BVHNodesBuffer.GetData(_BVHNodes);
            _particleBuffer.GetData(_particles);

            for (int i = 0; i < NumOfParticles; i++)
            {
                Debug.Log(" Particle Density: " + _particles[i].density + " Particle Kelvin: " + _particles[i].kelvin + " Particle position Delta: " + _particles[i].positionDelta + " Particle lambda: " + _particles[i].lambda + " Particle velocity: " + _particles[i].velocity);
            }
            /*
            for (int i = 0; i < NumOfParticles; i++)
            {
                //Debug.Log(" Particle IDs: " + _MortonCodes[i].mortonCode);
                Debug.Log(" Particle IDs: " + _ParticleIDs[i] + " Current Node: " + i + " Parent Node: " + _BVHNodes[i].parent + " Left Child: " + _BVHNodes[i].leftChild + " Right Child: " + _BVHNodes[i].rightChild + " Nodes Contained: " + _BVHNodes[i].primitiveOffset + "-" + (_BVHNodes[i].primitiveCount + _BVHNodes[i].primitiveOffset) + " Hit: " + _BVHNodes[i].hit + " Miss: " + _BVHNodes[i].miss + " AABB Max: " + _BVHNodes[i].aabbMax + " AABB Min: " + _BVHNodes[i].aabbMin + " IsLeaf: " + _BVHNodes[i].isLeaf + " Left Child: " + _BVHNodes[i].leftChildLeaf + " Right Child: " + _BVHNodes[i].rightChildLeaf + " Morton Code: " + _MortonCodes[i].mortonCode.ToString("F7")  + " Position: " + _particles[_ParticleIDs[i]].position + " Index Node " + _BVHNodes[i].indexedId);
            }
            */
            //PrintNeighborData();

            /*
            using (StreamWriter sw = new StreamWriter("Assets/Debug/FluidSim/ParticlePositions" + Time.timeAsDouble + ".txt"))
            {
                for (int i = 0; i < NumOfParticles; i++)
                {
                    sw.WriteLine("Particle: " + _MortonCodes[i].particleIndex + " Particle IDs: " + _ParticleIDs[i] + " Current Node: " + i + " Parent Node: " + _BVHNodes[i].parent + " Left Child: " + _BVHNodes[i].leftChild + " Right Child: " + _BVHNodes[i].rightChild + " Nodes Contained: " + _BVHNodes[i].primitiveOffset + "-" + (_BVHNodes[i].primitiveCount + _BVHNodes[i].primitiveOffset) + " Hit: " + _BVHNodes[i].hit + " Miss: " + _BVHNodes[i].miss + " AABB Max: " + _BVHNodes[i].aabbMax + " AABB Min: " + _BVHNodes[i].aabbMin + " Morton Code: " + _MortonCodes[i].mortonCode.ToString("F7"));

                    //sw.WriteLine(_particles[i].position);
                }

                sw.Close();
            }
            */
            }
        }
    //Temporarily attach this simulation to camera!!!
    private void UpdateFluidConstants()
    {
        /*
        //Guard clause, ensure there are objects to ray trace.
        if (UnigmaPhysicsManager.Instance._physicsObjects.Count == 0)
        {
            return;
        }
        */

        _renderTextureWidth = Mathf.Max(Mathf.Min(Mathf.CeilToInt(Screen.width * (1.0f / (1.0f + Mathf.Abs(ResolutionDivider)))), Screen.width), 32);
        _renderTextureHeight = Mathf.Max(Mathf.Min(Mathf.CeilToInt(Screen.height * (1.0f / (1.0f + Mathf.Abs(ResolutionDivider)))), Screen.height), 32);

        if (_renderTextureWidth != _rtTarget.width || _renderTextureHeight != _rtTarget.height)
        {
            if (_rtTarget != null)
            {
                _rtTarget.Release();
            }
            _rtTarget = RenderTexture.GetTemporary(_renderTextureWidth, _renderTextureHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            _rtTarget.enableRandomWrite = true;
            _rtTarget.Create();
        }

        _fluidSimulationComputeShader.SetMatrix("_CameraToWorld", _cam.cameraToWorldMatrix);
        _fluidSimulationComputeShader.SetMatrix("_CameraWorldToLocal", _cam.transform.worldToLocalMatrix);
        _fluidSimulationComputeShader.SetMatrix("_CameraInverseProjection", _cam.projectionMatrix.inverse);
        _fluidSimulationComputeShader.SetVector("_DepthScale", DepthScale);
        _fluidSimulationComputeShader.SetFloat("_Viscosity", Viscosity);
        _fluidSimulationComputeShader.SetFloat("_VoritictyEps", _VoritictyEps);
        _fluidSimulationComputeShader.SetFloat("_TimeStep", TimeStep);
        _fluidSimulationComputeShader.SetFloat("_Alpha", _ControlAlpha);
        _fluidSimulationComputeShader.SetFloat("_CDHRadius", _CDHRadius);
        _fluidSimulationComputeShader.SetFloat("_CLHRadius",  _CLHRadius);
        _fluidSimulationComputeShader.SetFloat("_CPNorm", _CPNorm);
        _fluidSimulationComputeShader.SetFloat("_CDNorm", _CDNorm);
        _fluidSimulationComputeShader.SetFloat("_BoundsDamping", BoundsDamping);
        _fluidSimulationComputeShader.SetVector("_BoxSize", _BoxSize);
        _fluidSimulationComputeShader.SetBool("_IsOrthographic", _cam.orthographic);
        _fluidSimulationComputeShader.SetVector("_initialPosition", _initialPosition);
        _fluidSimulationComputeShader.SetVector("_initialForce", _initialForce);

        _fluidSimulationComputeShader.SetVector("_BoxSizeSpaceTime", UnigmaSpaceTime.Instance.SpaceTimeSize);
        _fluidSimulationComputeShader.SetInt("_ResolutionSpaceTime", UnigmaSpaceTime.Instance.SpaceTimeResolution);
        _fluidSimulationComputeShader.SetInt("_NumOfSpatialUnits", UnigmaSpaceTime.Instance._NumOfVectors);

        properties.SetBuffer("g_AABBs", aabbList);
        Shader.SetGlobalFloat("_SizeOfParticle", SizeOfParticle);




        Matrix4x4 m = GL.GetGPUProjectionMatrix(_cam.projectionMatrix, false);
        m[2, 3] = m[3, 2] = 0.0f; m[3, 3] = 1.0f;
        Matrix4x4 ProjectionToWorld = Matrix4x4.Inverse(m * _cam.worldToCameraMatrix) * Matrix4x4.TRS(new Vector3(0, 0, -m[2, 2]), Quaternion.identity, Vector3.one);
        Shader.SetGlobalMatrix("_ProjectionToWorld", ProjectionToWorld);
        Shader.SetGlobalMatrix("_CameraInverseProjection", _cam.projectionMatrix.inverse);

        //Change to horizonal / vertical scale.
        _fluidSimMaterialDepthHori.SetFloat("_ScaleX", BlurScale.x);
        _fluidSimMaterialDepthHori.SetFloat("_ScaleY", 0.0f);
        _fluidSimMaterialDepthHori.SetFloat("_BlurFallOff", BlurFallOff);
        _fluidSimMaterialDepthHori.SetFloat("_BlurRadius", BlurRadius);

        _fluidSimMaterialDepthVert.SetFloat("_ScaleX", 0.0f);
        _fluidSimMaterialDepthVert.SetFloat("_ScaleY", BlurScale.y);
        _fluidSimMaterialDepthVert.SetFloat("_BlurFallOff", BlurFallOff);
        _fluidSimMaterialDepthVert.SetFloat("_BlurRadius", BlurRadius);

        _fluidSimCompositeLiquid.SetTexture("_ColorFieldNormalMap", _normalMapTexture);
        _fluidSimCompositeLiquid.SetTexture("_CurlMap", _curlMapTexture);
    }

    void CreateFluidCommandBuffers()
    {
        CommandBuffer fluidCommandBuffers = new CommandBuffer();
        RenderTexture[] rtGBuffers = new RenderTexture[4];

        RenderTargetIdentifier[] rtGBuffersID = new RenderTargetIdentifier[rtGBuffers.Length];
        Material writeOutDepth = Resources.Load<Material>("WriteOutDepth");

        rtGBuffers[0] = _velocitySurfaceDensityDepthTexture;
        rtGBuffersID[0] = rtGBuffers[0];

        rtGBuffers[1] = _densityMapTexture;
        rtGBuffersID[1] = rtGBuffers[1];

        rtGBuffers[2] = _rtTarget;
        rtGBuffersID[2] = rtGBuffers[2];

        rtGBuffers[3] = _normalMapTexture;
        rtGBuffersID[3] = rtGBuffers[3];

        _depthBufferTexture = RenderTexture.GetTemporary(Screen.width, Screen.height, 0);


        fluidCommandBuffers.name = "Fluid Command Buffer";
        fluidCommandBuffers.SetGlobalTexture("_UnigmaFluids", _rtTarget);

        //fluidCommandBuffers.SetRenderTarget(_rtTarget);

        //fluidCommandBuffers.ClearRenderTarget(true, true, new Vector4(0,0,0,0));

        fluidCommandBuffers.SetComputeTextureParam(_fluidSimulationComputeShader, _CreateGridKernelId, "Result", _rtTarget);
        fluidCommandBuffers.SetComputeTextureParam(_fluidSimulationComputeShader, _CreateGridKernelId, "_VelocitySurfaceDensityDepthTexture", _velocitySurfaceDensityDepthTexture);

        if (_renderMethod == RenderMethod.RayTracing)
            fluidCommandBuffers.DispatchCompute(_fluidSimulationComputeShader, _CreateGridKernelId, Mathf.CeilToInt(_renderTextureWidth / _createGridThreadSize.x), Mathf.CeilToInt(_renderTextureHeight / _createGridThreadSize.y), (int)_createGridThreadSize.z);
        else
        {
            //fluidCommandBuffers.SetRenderTarget(_unigmaDepthTexture);
            //fluidCommandBuffers.ClearRenderTarget(true, true, new Vector4(0, 0, 0, 0));
            //foreach (Renderer r in UnigmaPhysicsManager.Instance._physicsObjects)
            //{
                /*
                int stencil = 0;//r.material.GetInt("_StencilRef");
                float ssj = 0.01f;
                writeOutDepth.SetFloat("_StencilSSJ", ssj);
                writeOutDepth.SetInt("_StencilRef", stencil);
                Debug.Log(r.name + " " + stencil);
                r.GetComponent<IsometricDepthNormalObject>().materials.Add("FluidPositions", writeOutDepth);
                Material m = r.GetComponent<IsometricDepthNormalObject>().materials["FluidPositions"];
                */
                //fluidCommandBuffers.DrawRenderer(r, m);
            //}
            //fluidCommandBuffers.DispatchCompute(_fluidSimulationComputeShader, _CreateDistancesKernelId, Mathf.CeilToInt(_distanceTextureWidth / _createDistancesThreadSize.x), Mathf.CeilToInt(_distanceTextureHeight / _createDistancesThreadSize.y), (int)_createDistancesThreadSize.z);
            //fluidCommandBuffers.SetRenderTarget(_rtTarget);
        }
        fluidCommandBuffers.SetGlobalTexture("_UnigmaFluidsDepth", _velocitySurfaceDensityDepthTexture);
        fluidCommandBuffers.SetGlobalTexture("_DensityMap", _densityMapTexture);
        fluidCommandBuffers.SetGlobalTexture("_VelocityMap", _velocityMapTexture);
        fluidCommandBuffers.SetGlobalTexture("_SurfaceMap", _surfaceMapTexture);
        fluidCommandBuffers.SetGlobalTexture("_CurlMap", _curlMapTexture);
        fluidCommandBuffers.SetGlobalTexture("_ColorFieldNormalMap", _normalMapTexture);
        //fluidCommandBuffers.SetGlobalTexture("_UnigmaDepthMap", _unigmaDepthTexture);



        //fluidCommandBuffers.SetRenderTarget(_velocitySurfaceDensityDepthTexture);

        if (_renderMethod == RenderMethod.Rasterization)
        {
            fluidCommandBuffers.SetRenderTarget(rtGBuffersID, _rtTarget.depthBuffer);
            fluidCommandBuffers.ClearRenderTarget(true, true, new Vector4(0, 0, 0, 0));
            fluidCommandBuffers.DrawMeshInstancedProcedural(rasterMesh, 0, rasterMaterial, 0, MaxNumOfParticles);
            fluidCommandBuffers.DrawMeshInstancedProcedural(rasterMesh, 0, rasterMaterial, 1, MaxNumOfParticles);
            //fluidCommandBuffers.DrawMeshInstancedProcedural(rasterMesh, 0, rasterMaterial, 2, MaxNumOfParticles);
        }

        if (_renderMethod == RenderMethod.RayTracingAccelerated)
        {
            foreach (UnigmaRendererObject r in UnigmaRendererManager.Instance._renderObjects)
            {
                _MeshAccelerationStructure.AddInstance(r._renderer);
            }
            //_AccelerationStructure.UpdateInstanceTransform(handle, 
            //fluidCommandBuffers.ClearRenderTarget(true, true, new Vector4(0, 0, 0, 0));
            fluidCommandBuffers.SetRayTracingTextureParam(_RayTracingShaderAccelerated, "_RayTracedImage", _rtTarget);
            fluidCommandBuffers.SetRayTracingTextureParam(_RayTracingShaderAccelerated, "_VelocitySurfaceDensityDepthTexture", _velocitySurfaceDensityDepthTexture);
            fluidCommandBuffers.SetRayTracingTextureParam(_RayTracingShaderAccelerated, "_UnigmaDepthMapRayTrace", _unigmaDepthTexture);
            fluidCommandBuffers.SetRayTracingBufferParam(_RayTracingShaderAccelerated, "_Particles", _particleBuffer);
            fluidCommandBuffers.BuildRayTracingAccelerationStructure(_AccelerationStructure);
            fluidCommandBuffers.BuildRayTracingAccelerationStructure(_MeshAccelerationStructure);
            fluidCommandBuffers.SetRayTracingShaderPass(_RayTracingShaderAccelerated, "MyRaytraceShaderPass");
            fluidCommandBuffers.SetRayTracingAccelerationStructure(_RayTracingShaderAccelerated, "_RaytracingAccelerationStructure", _MeshAccelerationStructure);
            fluidCommandBuffers.SetRayTracingAccelerationStructure(_RayTracingShaderAccelerated, Shader.PropertyToID("g_SceneAccelStruct"), _AccelerationStructure);
            fluidCommandBuffers.DispatchRays(_RayTracingShaderAccelerated, "MyRaygenShader", (uint)_renderTextureWidth, (uint)_renderTextureHeight, 1);


        }

        //Blur.
        fluidCommandBuffers.Blit(_velocitySurfaceDensityDepthTexture, _tempTarget, _fluidSimMaterialDepthHori);
        fluidCommandBuffers.Blit(_tempTarget, _velocitySurfaceDensityDepthTexture, _fluidSimMaterialDepthVert);


        fluidCommandBuffers.SetGlobalTexture("_UnigmaFluidsNormals", _fluidNormalBufferTexture);
        fluidCommandBuffers.SetGlobalTexture("_UnigmaFluidsFinal", _UnigmaFluidsFinal);

        //fluidCommandBuffers.SetRenderTarget(_fluidNormalBufferTexture);

        //fluidCommandBuffers.ClearRenderTarget(true, true, new Vector4(0, 0, 0, 0));

        fluidCommandBuffers.Blit(_fluidDepthBufferTexture, _fluidNormalBufferTexture, _fluidSimMaterialNormal);

        //fluidCommandBuffers.Blit(BuiltinRenderTextureType.CameraTarget, _UnigmaFluidsFinal, _fluidSimMaterialComposite);
        //fluidCommandBuffers.SetRenderTarget(_fluidNormalBufferTexture);

        //fluidCommandBuffers.ClearRenderTarget(true, true, new Vector4(0, 0, 0, 0));
        fluidCommandBuffers.Blit(BuiltinRenderTextureType.CameraTarget, _UnigmaFluidsFinal, _fluidSimCompositeLiquid, 0);
        fluidCommandBuffers.Blit(_UnigmaFluidsFinal, BuiltinRenderTextureType.CameraTarget, _fluidSimCompositeLiquid, 1);
        //backgroundColorBuffer.Blit(_UnigmaBackgroundColor, BuiltinRenderTextureType.CameraTarget, _unigmaBackgroundMaterial, 1);
        _cam.AddCommandBuffer(CameraEvent.AfterForwardAlpha, fluidCommandBuffers);
        
        //UnityEditor.SceneView.GetAllSceneCameras()[0].AddCommandBuffer(CameraEvent.AfterForwardOpaque, fluidCommandBuffers);

    }
    int HashVec(Vector3 p)
    {
        int p1 = 18397;
        int p2 = 20483;
        int p3 = 29303;

        //float e = p1 * Mathf.Pow(p.x, p2) * Mathf.Pow(p.y, p3) * p.z;
        //int n = Mathf.FloorToInt(e);
        //return (n % _Particles.Length + _Particles.Length) % _Particles.Length;
        int n = (Mathf.FloorToInt(p.x) * p1 + Mathf.FloorToInt(p.y) * p2 + Mathf.FloorToInt(p.z) * p3);
        return n % _particles.Length;
    }
    
    void CreateQuadTree()
    {
        /*
        float startRadius = 0.03125f;
        for (int i = 0; i < _Particles.Length; i++)
        {
            Vector3 pos = _Particles[i].position / startRadius;
            Vector3 squashedPos = new Vector3(Mathf.Floor(pos.x), Mathf.Floor(pos.y), Mathf.Floor(pos.z));
            int key = HashVec(squashedPos);
            _Particles[i].cellID = key;

            Debug.Log("Cell ID: " + _Particles[i].cellID); //" Floored Position: " + squashedPos.ToString("F8") + " Position: " + _Particles[i].position.ToString("F8"));
        }
        */
        /*
        for (int i = 0; i < _Particles.Length; i++)
        {
            _ParticleIDs.Push(i);
        }

        int id = _ParticleIDs.Pop();
        Particles currentParticle = _Particles[id];
        Stack<int> _pNodes = new Stack<int>();
        while (_ParticleIDs.Count > 0)
        {
            for (int i = 0; i < _Particles.Length; i++)
            {
                if (Vector3.Distance(_Particles[id].position, _Particles[i].position) < startRadius)
                    _pNodes.Push(i);
            }
        }
        _pNodesBuffer = new ComputeBuffer(PNodes.Count, 2 * sizeof(int));
        */

    }

    //Release all buffers and memory
    void ReleaseBuffers()
    {
        if (_particleBuffer != null)
            _particleBuffer.Release();
        if (_particlePositionsBuffer != null)
            _particlePositionsBuffer.Release();
        if (_pNodesBuffer != null)
            _pNodesBuffer.Release();
        if (_particleIDsBuffer != null)
            _particleIDsBuffer.Release();
        if (_particleIndicesBuffer != null)
            _particleIndicesBuffer.Release();
        if (_BVHNodesBuffer != null)
            _BVHNodesBuffer.Release();
        if (_particleCountBuffer != null)
            _particleCountBuffer.Release();
        if (_particleCellIndicesBuffer != null)
            _particleCellIndicesBuffer.Release();
        if (_particleCellOffsets != null)
            _particleCellOffsets.Release();
        if (_MortonCodesBuffer != null)
            _MortonCodesBuffer.Release();
        if (_MortonCodesTempBuffer != null)
            _MortonCodesTempBuffer.Release();
        if (_MortonPrefixSumTotalZeroesBuffer != null)
            _MortonPrefixSumTotalZeroesBuffer.Release();
        if (_MortonPrefixSumOffsetZeroesBuffer != null)
            _MortonPrefixSumOffsetZeroesBuffer.Release();
        if (_MortonPrefixSumOffsetOnesBuffer != null)
            _MortonPrefixSumOffsetOnesBuffer.Release();
        if (_AccelerationStructure != null)
            _AccelerationStructure.Release();
        if (_MeshAccelerationStructure != null)
            _MeshAccelerationStructure.Release();
        if (_unigmaDepthTexture != null)
            _unigmaDepthTexture.Release();
        if (_UnigmaFluidsFinal != null)
            _UnigmaFluidsFinal.Release();
        if (aabbList != null)
            aabbList.Release();
        if (_particleNeighbors != null)
            _particleNeighbors.Release();
        if (_controlNeighborsBuffer != null)
            _controlNeighborsBuffer.Release();
        if (_controlParticlesBuffer != null)
            _controlParticlesBuffer.Release();
        if (_fluidObjectsBuffer != null)
            _fluidObjectsBuffer.Release();


        if(_rtTarget)
            _rtTarget.Release();
        if(_densityMapTexture)
            _densityMapTexture.Release();
        if(_velocityMapTexture)
            _velocityMapTexture.Release();
        if(_normalMapTexture)
            _normalMapTexture.Release();
        if(_curlMapTexture)
            _curlMapTexture.Release();
        if(_tempTarget)
            _tempTarget.Release();
        if(_fluidNormalBufferTexture)
            _fluidNormalBufferTexture.Release();
        if(_fluidDepthBufferTexture)
            _fluidDepthBufferTexture.Release();
        if(_velocitySurfaceDensityDepthTexture)
            _velocitySurfaceDensityDepthTexture.Release();
        if(_surfaceMapTexture)
            _surfaceMapTexture.Release();

        Debug.Log("Buffers Released");

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

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(Vector3.zero, _BoxSize);
        /*
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(controlParticlePosition, 0.1f);
        //Set int for simulation
        if (_fluidSimulationComputeShader != null)
            _fluidSimulationComputeShader.SetInt("_BoxViewDebug", BoxViewDebug);


        //Draw the bounding box
        Gizmos.color = Color.red;
        //Quick debug of meshObjects
        for (int i = 0; i < _meshObjects.Length; i++)
        {
            BoxCollider boxCollide = _rayTracedObjects[(int)_meshObjects[i].id].GetComponent<BoxCollider>();
            Rigidbody rb = _rayTracedObjects[(int)_meshObjects[i].id].GetComponent<Rigidbody>();
            if (boxCollide != null && rb != null)
            {
                //Get the 4 corners and center
                Vector3 point0 = _rayTracedObjects[(int)_meshObjects[i].id].transform.TransformPoint(boxCollide.center + new Vector3(boxCollide.size.x, -boxCollide.size.y, -boxCollide.size.z) * 0.5f);
                Vector3 point1 = _rayTracedObjects[(int)_meshObjects[i].id].transform.TransformPoint(boxCollide.center + new Vector3(-boxCollide.size.x, boxCollide.size.y, -boxCollide.size.z) * 0.5f);
                Vector3 point2 = _rayTracedObjects[(int)_meshObjects[i].id].transform.TransformPoint(boxCollide.center + new Vector3(-boxCollide.size.x, -boxCollide.size.y, boxCollide.size.z) * 0.5f);
                Vector3 point3 = _rayTracedObjects[(int)_meshObjects[i].id].transform.TransformPoint(boxCollide.center + new Vector3(boxCollide.size.x, boxCollide.size.y, -boxCollide.size.z) * 0.5f);
                Vector3 point4 = _rayTracedObjects[(int)_meshObjects[i].id].transform.TransformPoint(boxCollide.center + new Vector3(boxCollide.size.x, -boxCollide.size.y, boxCollide.size.z) * 0.5f);
                Vector3 point5 = _rayTracedObjects[(int)_meshObjects[i].id].transform.TransformPoint(boxCollide.center + new Vector3(-boxCollide.size.x, boxCollide.size.y, boxCollide.size.z) * 0.5f);

                Vector3 minPoint = _rayTracedObjects[(int)_meshObjects[i].id].transform.TransformPoint(boxCollide.center + new Vector3(-boxCollide.size.x, -boxCollide.size.y, -boxCollide.size.z) * 0.5f);
                Vector3 maxPoint = _rayTracedObjects[(int)_meshObjects[i].id].transform.TransformPoint(boxCollide.center + new Vector3(boxCollide.size.x, boxCollide.size.y, boxCollide.size.z) * 0.5f);

                //Draw tiny sphere at all the corners.
                Gizmos.DrawSphere(point0, 0.21f);
                Gizmos.DrawSphere(point1, 0.21f);
                Gizmos.DrawSphere(point2, 0.21f);
                Gizmos.DrawSphere(point3, 0.21f);
                Gizmos.DrawSphere(point4, 0.21f);
                Gizmos.DrawSphere(point5, 0.21f);
                Gizmos.DrawSphere(minPoint, 0.21f);
                Gizmos.DrawSphere(maxPoint, 0.21f);

            }
        }
        */
    }


    Vector3 GetControlParticlePosition()
    {
        //Get the 3D position from the mouse when clicked.
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            controlParticlePosition = hit.point;
        }
        return controlParticlePosition;
    }
}
