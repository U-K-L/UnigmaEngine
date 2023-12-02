using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

public class FluidSimulationManager : MonoBehaviour
{

    [SerializeField]
    Material rasterMaterial;
    [SerializeField]
    Material rayTracingMaterial;

    [SerializeField]
    Mesh rasterMesh;
    Mesh rayTracingMesh;

    ComputeShader _fluidSimulationComputeShader;
    GraphicsBuffer aabbList = null;
    AABB[] aabbs;

    ComputeBuffer _meshObjectBuffer;
    ComputeBuffer _verticesObjectBuffer;
    ComputeBuffer _indicesObjectBuffer;
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
    RenderTexture _distancesMapTexture;
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
    public Material _fluidSimMaterialComposite;
    public Material _fluidObjectsMaterial;

    public Vector2 BlurScale;
    public Vector3 _BoxSize = Vector3.one;
    public Vector4 DepthScale = default;
    public Color DeepWaterColor = Color.white;
    public Color ShallowWaterColor = Color.white;

    public int ResolutionDivider = 0; // (1 / t + 1) How much to divide the text size by. This lowers the resolution of the final image, but massively aids in performance.
    public int NumOfParticles;
    public int MaxNumOfParticles;

    //Todo remove from final product.
    public int BoxViewDebug = 0;
    public int ChosenParticle = 0;

    public float DepthMaxDistance = 100;
    public float BlurFallOff = 0.25f;
    public float BlurRadius = 5.0f;
    public float Smoothness = 0.25f;
    public float SizeOfParticle = 0.125f;
    public float MassOfParticle = 1.0f;
    public float GasConstant = 1.0f;
    public float Viscosity = 1.0f;
    public float TimeStep = 0.02f;
    public float BoundsDamping = 9.8f;
    public float Radius = 0.125f;
    public float RestDensity = 1.0f;
    public int MaxNeighbors = 50;

    private List<Renderer> _rayTracedObjects = new List<Renderer>();
    private List<MeshObject> _meshObjects = new List<MeshObject>();
    private List<Vertex> _vertices = new List<Vertex>();
    private List<int> _indices = new List<int>();
    private Particles[] _particles;
    private Vector3[] _particlesPositions;

    private int[] _ParticleIDs;
    private int[] _ParticleIndices;
    private int[] _ParticleCount;
    private int[] _ParticleCellIndices;
    private int[] _ParticleCellOffsets;
    
    private List<PNode> _pNodes;
    private BVHNode[] _BVHNodes;
    private int _renderTextureWidth, _renderTextureHeight = 0;
    private List<Vector3> _spawnParticles = default;
    private Vector4 _initialForce;
    private Vector3 _initialPosition;

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
    
    struct Vertex
    {
        public Vector3 position;
        public Vector3 normal;
        public Vector2 uv;
    };

    struct Particles
    {
        public Vector4 force;
        public Vector3 position;
        public Vector3 lastPosition;
        public Vector3 predictedPosition;
        public Vector3 positionDelta;
        public Vector3 debugVector;
        public Vector3 velocity;
        public Vector3 normal;
        public Vector3 curl;
        public float density;
        public float lambda;
        public float mass;
        public int parent;

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

    private MortonCode[] _MortonCodes;
    private MortonCode[] _MortonCodesTemp;
    private uint _MortonPrefixSumTotalZeroes = 0, _MortonPrefixSumOffsetZeroes = 0, _MortonPrefixSumOffsetOnes = 0;

    int _meshObjectStride = (sizeof(float) * 4 * 4) + sizeof(float) * 5 + sizeof(int) * 3 + sizeof(float) * 3 * 4;
    int _particleStride = sizeof(int) + sizeof(float) + sizeof(float) + sizeof(float) + ((sizeof(float) * 3) * 8 + (sizeof(float) * 4));
    int _MortonCodeStride = sizeof(uint) + sizeof(int);
    int _BVHStride = sizeof(float) * 3 * 2 + sizeof(int) * 12 + sizeof(float)*14;

    //Items to add to the raytracer.
    public LayerMask RayTracingLayers;
    public int _SolveIterations = 1;
    int nodesUsed = 1;
    
    Bounds bounds;

    public int RayTracinghandle;
    
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
    private void Awake()
    {
        Debug.Log("Particle Stride size is: " + _particleStride);
        Debug.Log("BVH Stride size is: " + _BVHStride);
        Debug.Log("Mesh Object Stride size is: " + _meshObjectStride);
        Debug.Log("Morton Code Stride size is: " + _MortonCodeStride);
        aabbs = new AABB[MaxNumOfParticles];
        _spawnParticles = new List<Vector3>();
        _renderTextureWidth = Mathf.Max(Mathf.Min(Mathf.CeilToInt(Screen.width * (1.0f / (1.0f + Mathf.Abs(ResolutionDivider)))), Screen.width), 32);
        _renderTextureHeight = Mathf.Max(Mathf.Min(Mathf.CeilToInt(Screen.height * (1.0f / (1.0f + Mathf.Abs(ResolutionDivider)))), Screen.height), 32);
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
        _distancesMapTexture = RenderTexture.GetTemporary(_renderTextureWidth, _renderTextureHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        _distancesMapTexture.name = "DistancesTexture";
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
        _particles = new Particles[MaxNumOfParticles];
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
        _PrefixSumKernelId = _fluidSimulationComputeShader.FindKernel("PrefixSum");
        _AssignMortonCodesKernelId = _fluidSimulationComputeShader.FindKernel("AssignMortonCodes");
        _AssignParentsKernelId = _fluidSimulationComputeShader.FindKernel("AssignParents");
        _AssignIndexKernelId = _fluidSimulationComputeShader.FindKernel("AssignIndex");
        _CreateBVHTreeKernelId = _fluidSimulationComputeShader.FindKernel("CreateBVHTree");
        _CreateBoundingBoxKernelId = _fluidSimulationComputeShader.FindKernel("CreateBoundingBox");


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
        _distancesMapTexture.enableRandomWrite = true;
        _distancesMapTexture.Create();
        _fluidSimulationComputeShader.SetTexture(_CreateGridKernelId, "Result", _rtTarget);
        _fluidSimulationComputeShader.SetTexture(_CreateGridKernelId, "DensityMap", _densityMapTexture);
        _fluidSimulationComputeShader.SetTexture(_CreateGridKernelId, "NormalMap", _fluidNormalBufferTexture);
        _fluidSimulationComputeShader.SetTexture(_CreateGridKernelId, "_ColorFieldNormalMap", _normalMapTexture);
        _fluidSimulationComputeShader.SetTexture(_CreateGridKernelId, "_VelocityMap", _velocityMapTexture);
        _fluidSimulationComputeShader.SetTexture(_CreateGridKernelId, "_SurfaceMap", _surfaceMapTexture);
        _fluidSimulationComputeShader.SetTexture(_CreateGridKernelId, "_CurlMap", _curlMapTexture);
        _fluidSimulationComputeShader.SetTexture(_CreateDistancesKernelId, "_DistancesMap", _distancesMapTexture);

        //material.SetBuffer("_Particles", _particleBuffer);
        bounds = new Bounds(Vector3.zero, _BoxSize);
        
        GetThreadSizes();
        AddObjectsToList();
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
        CreateFluidCommandBuffers();


    }

    void CreateAcceleratedStructure()
    {
        if (_RayTracingShaderAccelerated == null)
            _RayTracingShaderAccelerated = Resources.Load<RayTracingShader>("AcceleratedRayTracer");

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

    }

    void AddObjectsToList()
    {
        foreach (var obj in FindObjectsOfType<Renderer>())
        {
            //Check if object in the RaytracingLayers.
            if (((1 << obj.gameObject.layer) & RayTracingLayers) != 0)
            {
                _rayTracedObjects.Add(obj);
            }
        }
    }

    void CreateNonAcceleratedStructure()
    {
        BuildTriangleList();
    }

    void BuildTriangleList()
    {
        _vertices.Clear();
        _indices.Clear();

        foreach (Renderer r in _rayTracedObjects)
        {
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
                    v.normal = m.normals[i];
                    v.uv = m.uv[i];
                    _vertices.Add(v);
                }
                var indices = m.GetIndices(0);
                _indices.AddRange(indices.Select(index => index + startVert));

                // Add the object itself
                _meshObjects.Add(new MeshObject()
                {
                    localToWorld = r.transform.localToWorldMatrix,
                    indicesOffset = startIndex,
                    indicesCount = indices.Length
                });
            }
        }
        if (_meshObjects.Count > 0)
        {
            _meshObjectBuffer = new ComputeBuffer(_meshObjects.Count, _meshObjectStride);
            _verticesObjectBuffer = new ComputeBuffer(_vertices.Count, 32);
            _indicesObjectBuffer = new ComputeBuffer(_indices.Count, 4);
            _verticesObjectBuffer.SetData(_vertices);
            _fluidSimulationComputeShader.SetBuffer(_CreateGridKernelId, "_Vertices", _verticesObjectBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CreateDistancesKernelId, "_Vertices", _verticesObjectBuffer);
            _fluidSimulationComputeShader.SetBuffer(_UpdateParticlesKernelId, "_Vertices", _verticesObjectBuffer);
            _fluidSimulationComputeShader.SetBuffer(_UpdatePositionsKernelId, "_Vertices", _verticesObjectBuffer);
            _indicesObjectBuffer.SetData(_indices);
            _fluidSimulationComputeShader.SetBuffer(_CreateGridKernelId, "_Indices", _indicesObjectBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CreateDistancesKernelId, "_Indices", _indicesObjectBuffer);
            _fluidSimulationComputeShader.SetBuffer(_UpdateParticlesKernelId, "_Indices", _indicesObjectBuffer);
            _fluidSimulationComputeShader.SetBuffer(_UpdatePositionsKernelId, "_Indices", _indicesObjectBuffer);
        }

    }

    void UpdateNonAcceleratedRayTracer()
    {
        //Build the BVH
        CreateMeshes();
        UpdateParticles();
        BuildBVH();
        //if(Time.realtimeSinceStartup < 10)

        //Only if spacebar is pressed



    }

    void CreateMeshes()
    {
        for (int i = 0; i < _rayTracedObjects.Count; i++)
        {
            MeshObject meshobj = new MeshObject();
            RayTracingObject rto = _rayTracedObjects[i].GetComponent<RayTracingObject>();
            meshobj.localToWorld = _rayTracedObjects[i].GetComponent<Renderer>().localToWorldMatrix;
            meshobj.indicesOffset = _meshObjects[i].indicesOffset;
            meshobj.indicesCount = _meshObjects[i].indicesCount;
            meshobj.position = _rayTracedObjects[i].transform.position;
            meshobj.AABBMin = _rayTracedObjects[i].bounds.min;
            meshobj.AABBMax = _rayTracedObjects[i].bounds.max;
            meshobj.id = (uint)i;
            if (rto)
            {
                meshobj.color = new Vector3(rto.color.r, rto.color.g, rto.color.b);
                meshobj.emission = rto.emission;
                meshobj.smoothness = rto.smoothness;
                meshobj.transparency = rto.transparency;
                meshobj.absorbtion = rto.absorbtion;
                meshobj.celShaded = rto.celShaded;
            }
            _meshObjects[i] = meshobj;
        }
        
        if (_meshObjectBuffer.count > 0)
        {
            _meshObjectBuffer.SetData(_meshObjects);
            _fluidSimulationComputeShader.SetBuffer(_CreateGridKernelId, "_MeshObjects", _meshObjectBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CreateDistancesKernelId, "_MeshObjects", _meshObjectBuffer);
            _fluidSimulationComputeShader.SetBuffer(_UpdateParticlesKernelId, "_MeshObjects", _meshObjectBuffer);
            _fluidSimulationComputeShader.SetBuffer(_UpdatePositionsKernelId, "_MeshObjects", _meshObjectBuffer);
        }

        _meshObjectBuffer.SetData(_meshObjects);
        _fluidSimulationComputeShader.SetBuffer(_CreateGridKernelId, "_MeshObjects", _meshObjectBuffer);
        _fluidSimulationComputeShader.SetBuffer(_CreateDistancesKernelId, "_MeshObjects", _meshObjectBuffer);
        _fluidSimulationComputeShader.SetBuffer(_UpdateParticlesKernelId, "_MeshObjects", _meshObjectBuffer);
        _fluidSimulationComputeShader.SetBuffer(_UpdatePositionsKernelId, "_MeshObjects", _meshObjectBuffer);

    }

    public void AddParticles(Vector3 initialSpawnPosition, int numberOfParticles, Vector3 containerSize, int containerType)
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
                SpawnInCube(numOfParticlesCubedRoot, containerSize, initialSpawnPosition);
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
    public void ShootParticles(Vector3 initialSpawnPosition, int numberOfParticles, Vector4 force)
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

            Vector3 randomPos = Random.insideUnitSphere + initialSpawnPosition;
            _particles[i].position = randomPos;
            _particles[i].mass = MassOfParticle;
            _particles[i].velocity = Vector3.zero;
            _particles[i].force = force;
            _particles[i].density = 0.0f;
            _particles[i].lambda = 0.0f;
            _particles[i].predictedPosition = _particles[i].position;
        }
        _particleBuffer.SetData(_particles, NumOfParticles, NumOfParticles, sizeOfNewParticlesAdded);
        NumOfParticles += sizeOfNewParticlesAdded;


        if (UnigmaSettings.GetIsRTXEnabled() && _renderMethod == RenderMethod.RayTracingAccelerated)
        {

            _AccelerationStructure.RemoveInstance(RayTracinghandle);
            RayTracinghandle = _AccelerationStructure.AddInstance(aabbList, (uint)NumOfParticles, true, Matrix4x4.identity, rayTracingMaterial, true, properties);

        }
    }

    void SpawnInCube(int numberOfParticlesCubed, Vector3 containerSize, Vector3 initialSpawnPosition)
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
                    _particles[particleIndex].mass = MassOfParticle;
                    _particles[particleIndex].velocity = Vector3.zero;
                    _particles[particleIndex].force = Vector3.zero;
                    _particles[particleIndex].density = 0.0f;
                    _particles[particleIndex].lambda = 0.0f;
                    _particles[particleIndex].predictedPosition = _particles[particleIndex].position;
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
        _fluidSimulationComputeShader.SetInt("_NumOfNodes", NumOfParticles -1);
        _fluidSimulationComputeShader.SetInt("_NumOfParticles", NumOfParticles);
        _fluidSimulationComputeShader.SetInt("_MaxNumOfParticles", MaxNumOfParticles);
        _fluidSimulationComputeShader.SetInt("MaxNeighbors", MaxNeighbors);
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
            string log = "Particle ID: " + i + " Position: " + _particles[i].position + "Predicted Position: " + _particles[i].predictedPosition + " Velocity: " + _particles[i].velocity + " Force: " + _particles[i].force + " Mass: " + _particles[i].mass + " Density: " + _particles[i].density.ToString("F6") + " Lambda: " + _particles[i].lambda.ToString("F6") + " Debug Vector: " + _particles[i].debugVector.ToString("F6") + "Cell ID: " + _particles[i].parent;
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
                _particles[i].mass = MassOfParticle;
                _particles[i].velocity = Vector3.zero;
                _particles[i].force = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
                _particles[i].density = 0.0f;
                _particles[i].lambda = 0.0f;
                _particles[i].debugVector = new Vector3(-999, -999, -999);
                _particles[i].parent = -1;
                _particles[i].normal = Vector3.zero;
                _particles[i].predictedPosition = _particles[i].position;
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
            
            _fluidSimulationComputeShader.SetBuffer(_UpdateParticlesKernelId, "_Particles", _particleBuffer);
            _fluidSimulationComputeShader.SetBuffer(_UpdatePositionsKernelId, "g_AABBs", aabbList);
            //Set particle buffer to shader.
            _fluidSimulationComputeShader.SetBuffer(_CreateGridKernelId, "_Particles", _particleBuffer);
            _fluidSimulationComputeShader.SetBuffer(_ComputeForcesKernelId, "_Particles", _particleBuffer);
            _fluidSimulationComputeShader.SetBuffer(_ComputeDensityKernelId, "_Particles", _particleBuffer);
            _fluidSimulationComputeShader.SetBuffer(_UpdatePositionDeltasKernelId, "_Particles", _particleBuffer);
            _fluidSimulationComputeShader.SetBuffer(_UpdatePositionsKernelId, "_Particles", _particleBuffer);
            _fluidSimulationComputeShader.SetBuffer(_UpdatePositionsKernelId, "_ParticlesPositions", _particlePositionsBuffer);
            _fluidSimulationComputeShader.SetBuffer(_HashParticlesKernelId, "_Particles", _particleBuffer);
            _fluidSimulationComputeShader.SetBuffer(_SortParticlesKernelId, "_Particles", _particleBuffer);
            _fluidSimulationComputeShader.SetBuffer(_RadixSortKernelId, "_Particles", _particleBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CalculateCurlKernelId, "_Particles", _particleBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CalculateVorticityKernelId, "_Particles", _particleBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CreateBVHTreeKernelId, "_Particles", _particleBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CreateBoundingBoxKernelId, "_Particles", _particleBuffer);

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
            _fluidSimulationComputeShader.SetBuffer(_HashParticlesKernelId, "_ParticleIndices", _particleIndicesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_SortParticlesKernelId, "_ParticleIndices", _particleIndicesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_RadixSortKernelId, "_ParticleIndices", _particleIndicesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CalculateCellOffsetsKernelId, "_ParticleIndices", _particleIndicesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CalculateCurlKernelId, "_ParticleIndices", _particleIndicesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CalculateVorticityKernelId, "_ParticleIndices", _particleIndicesBuffer);

            _fluidSimulationComputeShader.SetBuffer(_UpdateParticlesKernelId, "_ParticleCellIndices", _particleCellIndicesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CreateGridKernelId, "_ParticleCellIndices", _particleCellIndicesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_ComputeForcesKernelId, "_ParticleCellIndices", _particleCellIndicesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_ComputeDensityKernelId, "_ParticleCellIndices", _particleCellIndicesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_UpdatePositionDeltasKernelId, "_ParticleCellIndices", _particleCellIndicesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_UpdatePositionsKernelId, "_ParticleCellIndices", _particleCellIndicesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_HashParticlesKernelId, "_ParticleCellIndices", _particleCellIndicesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_SortParticlesKernelId, "_ParticleCellIndices", _particleCellIndicesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_RadixSortKernelId, "_ParticleCellIndices", _particleCellIndicesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CalculateCellOffsetsKernelId, "_ParticleCellIndices", _particleCellIndicesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CalculateCurlKernelId, "_ParticleCellIndices", _particleCellIndicesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CalculateVorticityKernelId, "_ParticleCellIndices", _particleCellIndicesBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CalculateCellOffsetsKernelId, "_ParticleIDs", _particleIDsBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CreateBoundingBoxKernelId, "_ParticleIDs", _particleIDsBuffer);
            _fluidSimulationComputeShader.SetBuffer(_CreateBVHTreeKernelId, "_ParticleIDs", _particleIDsBuffer);

            _fluidSimulationComputeShader.SetBuffer(_UpdateParticlesKernelId, "_ParticleCellOffsets", _particleCellOffsets);
            _fluidSimulationComputeShader.SetBuffer(_CreateGridKernelId, "_ParticleCellOffsets", _particleCellOffsets);
            _fluidSimulationComputeShader.SetBuffer(_ComputeForcesKernelId, "_ParticleCellOffsets", _particleCellOffsets);
            _fluidSimulationComputeShader.SetBuffer(_ComputeDensityKernelId, "_ParticleCellOffsets", _particleCellOffsets);
            _fluidSimulationComputeShader.SetBuffer(_UpdatePositionDeltasKernelId, "_ParticleCellOffsets", _particleCellOffsets);
            _fluidSimulationComputeShader.SetBuffer(_UpdatePositionsKernelId, "_ParticleCellOffsets", _particleCellOffsets);
            _fluidSimulationComputeShader.SetBuffer(_HashParticlesKernelId, "_ParticleCellOffsets", _particleCellOffsets);
            _fluidSimulationComputeShader.SetBuffer(_SortParticlesKernelId, "_ParticleCellOffsets", _particleCellOffsets);
            _fluidSimulationComputeShader.SetBuffer(_RadixSortKernelId, "_ParticleCellOffsets", _particleCellOffsets);
            _fluidSimulationComputeShader.SetBuffer(_CalculateCellOffsetsKernelId, "_ParticleCellOffsets", _particleCellOffsets);
            _fluidSimulationComputeShader.SetBuffer(_CalculateCurlKernelId, "_ParticleCellOffsets", _particleCellOffsets);
            _fluidSimulationComputeShader.SetBuffer(_CalculateVorticityKernelId, "_ParticleCellOffsets", _particleCellOffsets);

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
            _particles[i].mass = MassOfParticle;
        }

        if (NumOfParticles > 0)
        {
            ComputeForces();
            HashParticles();
            //quick morton code debug.log.

            SortParticles();
            CalculateCellOffsets();
            if(_renderMethod == RenderMethod.RayTracing)
                CreateBVHTree();
            //DebugParticlesBVH();

            for (int i = 0; i < _SolveIterations; i++)
            {
                ComputeDensity();
                ComputePositionDelta();
                UpdatePredictedPositions();
            }

            ComputeCurl();
            ComputePositions();
            //ComputeVorticity();
            //Set Particle positions to script.
            //NOT NEEDED. MOVE ENTIRE BVH TO GPU!!!!
            //_particleBuffer.GetData(_particles);
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
                //Debug.Log(" Particle IDs: " + _MortonCodes[i].mortonCode);
                Debug.Log(" Particle IDs: " + _ParticleIDs[i] + " Current Node: " + i + " Parent Node: " + _BVHNodes[i].parent + " Left Child: " + _BVHNodes[i].leftChild + " Right Child: " + _BVHNodes[i].rightChild + " Nodes Contained: " + _BVHNodes[i].primitiveOffset + "-" + (_BVHNodes[i].primitiveCount + _BVHNodes[i].primitiveOffset) + " Hit: " + _BVHNodes[i].hit + " Miss: " + _BVHNodes[i].miss + " AABB Max: " + _BVHNodes[i].aabbMax + " AABB Min: " + _BVHNodes[i].aabbMin + " Parent Of Particle " + _particles[_ParticleIDs[i]].parent + " IsLeaf: " + _BVHNodes[i].isLeaf + " Left Child: " + _BVHNodes[i].leftChildLeaf + " Right Child: " + _BVHNodes[i].rightChildLeaf + " Morton Code: " + _MortonCodes[i].mortonCode.ToString("F7")  + " Position: " + _particles[_ParticleIDs[i]].position + " Index Node " + _BVHNodes[i].indexedId);
            }

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
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        //Guard clause, ensure there are objects to ray trace.
        if (_rayTracedObjects.Count == 0)
        {
            Debug.LogWarning("No objects to ray trace. Please add objects to the RayTracingLayers.");
            return;
        }

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
        _fluidSimulationComputeShader.SetFloat("_Smoothness", Smoothness);
        //_fluidSimulationComputeShader.SetVector("_LightSource", _LightSouce.position);
        //_fluidSimulationComputeShader.SetVector("_LightScale", _LightScale.position);
        //_fluidSimulationComputeShader.SetFloat("_SizeOfParticle", SizeOfParticle);
        _fluidSimulationComputeShader.SetFloat("_Radius", Radius);
        _fluidSimulationComputeShader.SetFloat("_GasConstant", GasConstant);
        _fluidSimulationComputeShader.SetFloat("_Viscosity", Viscosity);
        _fluidSimulationComputeShader.SetFloat("_TimeStep", TimeStep);
        _fluidSimulationComputeShader.SetFloat("_BoundsDamping", BoundsDamping);
        _fluidSimulationComputeShader.SetFloat("_RestDensity", RestDensity);
        _fluidSimulationComputeShader.SetVector("_BoxSize", _BoxSize);
        _fluidSimulationComputeShader.SetInt("_ChosenParticle", ChosenParticle);
        _fluidSimulationComputeShader.SetBool("_IsOrthographic", _cam.orthographic);
        _fluidSimulationComputeShader.SetVector("_initialPosition", _initialPosition);
        _fluidSimulationComputeShader.SetVector("_initialForce", _initialForce);
        
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

        _fluidSimMaterialComposite.SetColor("_DeepWaterColor", DeepWaterColor);
        _fluidSimMaterialComposite.SetColor("_ShallowWaterColor", ShallowWaterColor);
        _fluidSimMaterialComposite.SetFloat("_DepthMaxDistance", DepthMaxDistance);
        //_fluidSimMaterialComposite.SetTexture("_DensityMap", _densityMapTexture);
        _fluidSimMaterialComposite.SetTexture("_ColorFieldNormalMap", _normalMapTexture);
        //_fluidSimMaterialComposite.SetTexture("_VelocityMap", _velocityMapTexture);
        _fluidSimMaterialComposite.SetTexture("_CurlMap", _curlMapTexture);

        //aabbList.GetData(aabbs);
        //aabbList.SetData(aabbs);
        //uint threadsX, threadsY, threadsZ;
        //_fluidSimulationCompute.GetKernelThreadGroupSizes(0, out threadsX, out threadsY, out threadsZ);
        //_fluidSimulationCompute.Dispatch(0, Mathf.CeilToInt(Screen.width / threadsX), Mathf.CeilToInt(Screen.width / threadsY), (int)threadsZ);

        //Execute shaders on render target.
        //Graphics.Blit(_rtTarget, _fluidDepthBuffer, _fluidSimMaterialDepth);
        //Graphics.Blit(source, _rtTarget, _fluidSimMaterialDepthHori);
        //if (UnigmaSettings.GetIsRTXEnabled() && _renderMethod == RenderMethod.RayTracingAccelerated)
        //    DispatchAcceleratedRayTrace();
        Graphics.Blit(source, destination, _fluidSimMaterialComposite);


    }

    void CreateFluidCommandBuffers()
    {
        CommandBuffer fluidCommandBuffers = new CommandBuffer();
        RenderTexture[] rtGBuffers = new RenderTexture[4];
        RenderTargetIdentifier[] rtGBuffersID = new RenderTargetIdentifier[rtGBuffers.Length];

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

        fluidCommandBuffers.SetRenderTarget(_rtTarget);

        fluidCommandBuffers.ClearRenderTarget(true, true, new Vector4(0,0,0,0));

        fluidCommandBuffers.SetComputeTextureParam(_fluidSimulationComputeShader, _CreateGridKernelId, "Result", _rtTarget);
        fluidCommandBuffers.SetComputeTextureParam(_fluidSimulationComputeShader, _CreateGridKernelId, "_VelocitySurfaceDensityDepthTexture", _velocitySurfaceDensityDepthTexture);
        
        if(_renderMethod == RenderMethod.RayTracing)
            fluidCommandBuffers.DispatchCompute(_fluidSimulationComputeShader, _CreateGridKernelId, Mathf.CeilToInt(_renderTextureWidth / _createGridThreadSize.x), Mathf.CeilToInt(_renderTextureHeight / _createGridThreadSize.y), (int)_createGridThreadSize.z);
        else
            fluidCommandBuffers.DispatchCompute(_fluidSimulationComputeShader, _CreateDistancesKernelId, Mathf.CeilToInt(_renderTextureWidth / _createDistancesThreadSize.x), Mathf.CeilToInt(_renderTextureHeight / _createDistancesThreadSize.y), (int)_createDistancesThreadSize.z);
        
        fluidCommandBuffers.SetGlobalTexture("_UnigmaFluidsDepth", _velocitySurfaceDensityDepthTexture);
        fluidCommandBuffers.SetGlobalTexture("_DensityMap", _densityMapTexture);
        fluidCommandBuffers.SetGlobalTexture("_VelocityMap", _velocityMapTexture);
        fluidCommandBuffers.SetGlobalTexture("_SurfaceMap", _surfaceMapTexture);
        fluidCommandBuffers.SetGlobalTexture("_CurlMap", _curlMapTexture);
        fluidCommandBuffers.SetGlobalTexture("_ColorFieldNormalMap", _normalMapTexture);
        fluidCommandBuffers.SetGlobalTexture("_DistancesMap", _distancesMapTexture);



        fluidCommandBuffers.SetRenderTarget(_velocitySurfaceDensityDepthTexture);

        if (_renderMethod == RenderMethod.Rasterization)
        {
            fluidCommandBuffers.SetRenderTarget(rtGBuffersID, _rtTarget.depthBuffer);
            fluidCommandBuffers.ClearRenderTarget(true, true, new Vector4(0, 0, 0, 0));
            fluidCommandBuffers.DrawMeshInstancedProcedural(rasterMesh, 0, rasterMaterial, 0, MaxNumOfParticles);
            fluidCommandBuffers.DrawMeshInstancedProcedural(rasterMesh, 0, rasterMaterial, 1, MaxNumOfParticles);

        }

        if (_renderMethod == RenderMethod.RayTracingAccelerated)
        {
            foreach (Renderer r in _rayTracedObjects)
            {
                _MeshAccelerationStructure.AddInstance(r);
            }
            //_AccelerationStructure.UpdateInstanceTransform(handle, 
            fluidCommandBuffers.ClearRenderTarget(true, true, new Vector4(0, 0, 0, 0));
            fluidCommandBuffers.SetRayTracingTextureParam(_RayTracingShaderAccelerated, "_RayTracedImage", _rtTarget);
            fluidCommandBuffers.SetRayTracingTextureParam(_RayTracingShaderAccelerated, "_VelocitySurfaceDensityDepthTexture", _velocitySurfaceDensityDepthTexture);
            fluidCommandBuffers.SetRayTracingBufferParam(_RayTracingShaderAccelerated, "_Particles", _particleBuffer);
            fluidCommandBuffers.BuildRayTracingAccelerationStructure(_AccelerationStructure);
            fluidCommandBuffers.BuildRayTracingAccelerationStructure(_MeshAccelerationStructure);
            fluidCommandBuffers.SetRayTracingShaderPass(_RayTracingShaderAccelerated, "MyRaytraceShaderPass");
            fluidCommandBuffers.SetRayTracingAccelerationStructure(_RayTracingShaderAccelerated, "_RaytracingAccelerationStructure", _MeshAccelerationStructure);
            fluidCommandBuffers.SetRayTracingAccelerationStructure(_RayTracingShaderAccelerated, Shader.PropertyToID("g_SceneAccelStruct"), _AccelerationStructure);
            fluidCommandBuffers.DispatchRays(_RayTracingShaderAccelerated, "MyRaygenShader", (uint)_renderTextureWidth, (uint)_renderTextureHeight, 1);


        }

        fluidCommandBuffers.Blit(_velocitySurfaceDensityDepthTexture, _tempTarget, _fluidSimMaterialDepthHori);
        fluidCommandBuffers.Blit(_tempTarget, _velocitySurfaceDensityDepthTexture, _fluidSimMaterialDepthVert);


        fluidCommandBuffers.SetGlobalTexture("_UnigmaFluidsNormals", _fluidNormalBufferTexture);

        fluidCommandBuffers.SetRenderTarget(_fluidNormalBufferTexture);

        //fluidCommandBuffers.ClearRenderTarget(true, true, new Vector4(0, 0, 0, 0));

        fluidCommandBuffers.Blit(_fluidDepthBufferTexture, _fluidNormalBufferTexture, _fluidSimMaterialNormal);

        GetComponent<Camera>().AddCommandBuffer(CameraEvent.AfterForwardOpaque, fluidCommandBuffers);
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
        if (_verticesObjectBuffer != null)
            _verticesObjectBuffer.Release();
        if (_indicesObjectBuffer != null)
            _indicesObjectBuffer.Release();
        if (_meshObjectBuffer != null)
            _meshObjectBuffer.Release();
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
        if (_distancesMapTexture != null)
            _distancesMapTexture.Release();
        if (aabbList != null)
            aabbList.Release();


        _rtTarget.Release();
        _densityMapTexture.Release();
        _velocityMapTexture.Release();
        _normalMapTexture.Release();
        _curlMapTexture.Release();
        _tempTarget.Release();
        _fluidNormalBufferTexture.Release();
        _fluidDepthBufferTexture.Release();
        _velocitySurfaceDensityDepthTexture.Release();
        _surfaceMapTexture.Release();
        _tempTarget.Release();

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
        //Set int for simulation
        if (_fluidSimulationComputeShader != null)
            _fluidSimulationComputeShader.SetInt("_BoxViewDebug", BoxViewDebug);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(Vector3.zero, _BoxSize);
    }
}
