using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

public class FluidSimulationManager : MonoBehaviour
{
    
    ComputeShader _fluidSimulationComputeShader;

    ComputeBuffer _meshObjectBuffer;
    ComputeBuffer _verticesObjectBuffer;
    ComputeBuffer _indicesObjectBuffer;
    ComputeBuffer _particleBuffer;
    ComputeBuffer _pNodesBuffer;
    ComputeBuffer _particleIDsBuffer;
    ComputeBuffer _particleIndices;
    ComputeBuffer _particleCellIndices;
    ComputeBuffer _particleCellOffsets;
    ComputeBuffer _BVHNodesBuffer;

    //Todo refactor some of these compute kernels out.
    int _UpdateParticlesKernelId;
    int _ComputeForcesKernelId;
    int _ComputeDensityKernelId;
    int _CreateGridKernelId;
    int _UpdatePositionDeltasKernelId;
    int _UpdatePositionsKernelId;
    int _HashParticlesKernelId;
    int _SortParticlesKernelId;
    int _CalculateCellOffsetsKernelId;

    Vector3 _updateParticlesThreadSize;
    Vector3 _computeForcesThreadSize;
    Vector3 _computeDensityThreadSize;
    Vector3 _createGridThreadSize;
    Vector3 _updatePositionDeltasThreadSize;
    Vector3 _updatePositionsThreadSize;
    Vector3 _hashParticlesThreadSize;
    Vector3 _sortParticlesThreadSize;
    Vector3 _calculateCellOffsetsThreadSize;

    RenderTexture _rtTarget;
    RenderTexture _densityMapTexture;
    RenderTexture _tempTarget;
    RenderTexture _fluidNormalBufferTexture;
    RenderTexture _fluidDepthBufferTexture;

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

    private List<Renderer> _rayTracedObjects = new List<Renderer>();
    private List<MeshObject> _meshObjects = new List<MeshObject>();
    private List<Vertex> _vertices = new List<Vertex>();
    private List<int> _indices = new List<int>();
    private Particles[] _particles;
    
    private int[] _ParticleIDs;
    private int[] _ParticleIndices;
    private int[] _ParticleCellIndices;
    private int[] _ParticleCellOffsets;
    
    private List<PNode> _pNodes;
    private BVHNode[] _BVHNodes;
    private int _renderTextureWidth, _renderTextureHeight = 0;
    private List<Vector3> _spawnParticles = default;
    
    int _particleStride = sizeof(int) + sizeof(float) + sizeof(float) + sizeof(float) + ((sizeof(float) * 3) * 5 + (sizeof(float) * 4));
    int _BVHStride = sizeof(float) * 3 * 2 + sizeof(int) * 8;


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
        public Vector3 position;
        public Vector4 force;
        public Vector3 velocity;
        public float density;
        public float mass;
        public float pressure;
        public int cellID;
        public Vector3 predictedPosition;
        public Vector3 debugVector;
        public Vector3 lastPosition;
    };

    struct PNode
    {
        public int index;
        public int[] children;
    }

    struct BVHNode
    {
        public Vector3 aabbMin;
        public Vector3 aabbMax;
        public int leftChild;
        public int rightChild;
        public int parent;
        public int primitiveOffset;
        public int primitiveCount;
        public int index;
        public int hit;
        public int miss;
    }
    
    //Items to add to the raytracer.
    public LayerMask RayTracingLayers;
    public int _SolveIterations = 1;
    int nodesUsed = 1;
    private void Awake()
    {
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
        _densityMapTexture = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        _tempTarget = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        _fluidNormalBufferTexture = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        _fluidDepthBufferTexture = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);

        _particles = new Particles[MaxNumOfParticles];
        _ParticleIDs = new int[MaxNumOfParticles];
        _ParticleIndices = new int[MaxNumOfParticles];
        _ParticleCellIndices = new int[MaxNumOfParticles];
        _ParticleCellOffsets = new int[MaxNumOfParticles];
        _BVHNodes = new BVHNode[MaxNumOfParticles];

        _UpdateParticlesKernelId = _fluidSimulationComputeShader.FindKernel("UpdateParticles");
        _HashParticlesKernelId = _fluidSimulationComputeShader.FindKernel("HashParticles");
        _SortParticlesKernelId = _fluidSimulationComputeShader.FindKernel("BitonicSort");
        _CalculateCellOffsetsKernelId = _fluidSimulationComputeShader.FindKernel("CalculateCellOffsets");
        _CreateGridKernelId = _fluidSimulationComputeShader.FindKernel("CreateGrid");
        _ComputeForcesKernelId = _fluidSimulationComputeShader.FindKernel("ComputeForces");
        _ComputeDensityKernelId = _fluidSimulationComputeShader.FindKernel("ComputeDensity");
        _UpdatePositionDeltasKernelId = _fluidSimulationComputeShader.FindKernel("UpdatePositionDeltas");
        _UpdatePositionsKernelId = _fluidSimulationComputeShader.FindKernel("UpdatePositions");
        
        _rtTarget.enableRandomWrite = true;
        _rtTarget.Create();
        _densityMapTexture.enableRandomWrite = true;
        _densityMapTexture.Create();
        _fluidNormalBufferTexture.enableRandomWrite = true;
        _fluidNormalBufferTexture.Create();
        _fluidSimulationComputeShader.SetTexture(_CreateGridKernelId, "Result", _rtTarget);
        _fluidSimulationComputeShader.SetTexture(_CreateGridKernelId, "DensityMap", _densityMapTexture);
        _fluidSimulationComputeShader.SetTexture(_CreateGridKernelId, "NormalMap", _fluidNormalBufferTexture);

        GetThreadSizes();
        AddObjectsToList();
        CreateNonAcceleratedStructure();
        CreateFluidCommandBuffers();

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

        _fluidSimulationComputeShader.GetKernelThreadGroupSizes(_CalculateCellOffsetsKernelId, out threadsX, out threadsY, out threadsZ);
        _calculateCellOffsetsThreadSize = new Vector3(threadsX, threadsY, threadsZ);

        _fluidSimulationComputeShader.GetKernelThreadGroupSizes(_CreateGridKernelId, out threadsX, out threadsY, out threadsZ);
        _createGridThreadSize = new Vector3(threadsX, threadsY, threadsZ);

        _fluidSimulationComputeShader.GetKernelThreadGroupSizes(_ComputeForcesKernelId, out threadsX, out threadsY, out threadsZ);
        _computeForcesThreadSize = new Vector3(threadsX, threadsY, threadsZ);

        _fluidSimulationComputeShader.GetKernelThreadGroupSizes(_ComputeDensityKernelId, out threadsX, out threadsY, out threadsZ);
        _computeDensityThreadSize = new Vector3(threadsX, threadsY, threadsZ);

        _fluidSimulationComputeShader.GetKernelThreadGroupSizes(_UpdatePositionDeltasKernelId, out threadsX, out threadsY, out threadsZ);
        _updatePositionDeltasThreadSize = new Vector3(threadsX, threadsY, threadsZ);

        _fluidSimulationComputeShader.GetKernelThreadGroupSizes(_UpdatePositionsKernelId, out threadsX, out threadsY, out threadsZ);
        _updatePositionsThreadSize = new Vector3(threadsX, threadsY, threadsZ);

    }

    void SortParticles()
    {
        for (int biDim = 2; biDim <= NumOfParticles; biDim <<= 1)
        {
            _fluidSimulationComputeShader.SetInt("biDim", biDim);
            for (int biBlock = biDim >> 1; biBlock > 0; biBlock >>= 1)
            {
                _fluidSimulationComputeShader.SetInt("biBlock", biBlock);
                _fluidSimulationComputeShader.Dispatch(_SortParticlesKernelId, Mathf.CeilToInt(NumOfParticles / _sortParticlesThreadSize.x), 1, 1);
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
            _meshObjectBuffer = new ComputeBuffer(_meshObjects.Count, 144);
            _verticesObjectBuffer = new ComputeBuffer(_vertices.Count, 32);
            _indicesObjectBuffer = new ComputeBuffer(_indices.Count, 4);
            _verticesObjectBuffer.SetData(_vertices);
            _fluidSimulationComputeShader.SetBuffer(_CreateGridKernelId, "_Vertices", _verticesObjectBuffer);
            _fluidSimulationComputeShader.SetBuffer(_UpdateParticlesKernelId, "_Vertices", _verticesObjectBuffer);
            _fluidSimulationComputeShader.SetBuffer(_UpdatePositionsKernelId, "_Vertices", _verticesObjectBuffer);
            _indicesObjectBuffer.SetData(_indices);
            _fluidSimulationComputeShader.SetBuffer(_CreateGridKernelId, "_Indices", _indicesObjectBuffer);
            _fluidSimulationComputeShader.SetBuffer(_UpdateParticlesKernelId, "_Indices", _indicesObjectBuffer);
            _fluidSimulationComputeShader.SetBuffer(_UpdatePositionsKernelId, "_Indices", _indicesObjectBuffer);
        }

    }

    void UpdateNonAcceleratedRayTracer()
    {
        if (Input.GetKey(KeyCode.Space))
        {
            //PrintParticleData();
            ShootParticles();
        }
        //Build the BVH
        CreateMeshes();

        UpdateParticles();
        BuildBVH();
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
            _fluidSimulationComputeShader.SetBuffer(_UpdateParticlesKernelId, "_MeshObjects", _meshObjectBuffer);
            _fluidSimulationComputeShader.SetBuffer(_UpdatePositionsKernelId, "_MeshObjects", _meshObjectBuffer);
        }

        _meshObjectBuffer.SetData(_meshObjects);
        _fluidSimulationComputeShader.SetBuffer(_CreateGridKernelId, "_MeshObjects", _meshObjectBuffer);
        _fluidSimulationComputeShader.SetBuffer(_UpdateParticlesKernelId, "_MeshObjects", _meshObjectBuffer);
        _fluidSimulationComputeShader.SetBuffer(_UpdatePositionsKernelId, "_MeshObjects", _meshObjectBuffer);

    }

    void ShootParticles()
    {
        if (NumOfParticles >= MaxNumOfParticles)
        {
            return;
        }
        int sizeOfNewParticlesAdded = 16;
        //Cubed root num of particles:
        float numOfParticlesCubedRoot = Mathf.Pow(sizeOfNewParticlesAdded, 1.0f / 3.0f);
        float numOfParticlesSquaredRoot = Mathf.Sqrt(sizeOfNewParticlesAdded);
        //Create particles.
        //SpawnParticlesInBox();
        for (int i = NumOfParticles; i < NumOfParticles + sizeOfNewParticlesAdded; i++)
        {
            _ParticleIndices[i] = i;
            _ParticleIDs[i] = i;

            Vector3 randomPos = Random.insideUnitSphere;
            _particles[i].position = randomPos;
            _particles[i].mass = MassOfParticle;
            _particles[i].velocity = Vector3.zero;
            _particles[i].force = new Vector4(555.0f, 0.0f, 0.0f, 0.35f);
            _particles[i].density = 0.0f;
            _particles[i].pressure = 0.0f;
            _particles[i].predictedPosition = _particles[i].position;
        }
        NumOfParticles += sizeOfNewParticlesAdded;
        _particleBuffer.SetData(_particles);
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
            _BVHNodesBuffer = new ComputeBuffer(_BVHNodes.Length, _BVHStride);
            _particleIDsBuffer = new ComputeBuffer(_ParticleIDs.Length, 4);
        }


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

        //Set the BVH to the compute shader.
        _BVHNodesBuffer.SetData(_BVHNodes);
        _particleIDsBuffer.SetData(_ParticleIDs);
        _fluidSimulationComputeShader.SetBuffer(_CreateGridKernelId, "_BVHNodes", _BVHNodesBuffer);
        _fluidSimulationComputeShader.SetBuffer(_CreateGridKernelId, "_ParticleIDs", _particleIDsBuffer);
        _fluidSimulationComputeShader.SetBuffer(_UpdateParticlesKernelId, "_BVHNodes", _BVHNodesBuffer);
        _fluidSimulationComputeShader.SetBuffer(_UpdateParticlesKernelId, "_ParticleIDs", _particleIDsBuffer);
        _fluidSimulationComputeShader.SetBuffer(_UpdatePositionsKernelId, "_BVHNodes", _BVHNodesBuffer);
        _fluidSimulationComputeShader.SetBuffer(_UpdatePositionsKernelId, "_ParticleIDs", _particleIDsBuffer);
        _fluidSimulationComputeShader.SetInt("_NumOfNodes", nodesUsed);
        _fluidSimulationComputeShader.SetInt("_NumOfParticles", NumOfParticles);
        _fluidSimulationComputeShader.SetInt("_MaxNumOfParticles", MaxNumOfParticles);
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
            Vector3 sizeOfParticle = 1 * new Vector3(SizeOfParticle, SizeOfParticle, SizeOfParticle);
            _BVHNodes[nodeIndex].aabbMin = Vector3.Min(_BVHNodes[nodeIndex].aabbMin, particlePos - sizeOfParticle);
            _BVHNodes[nodeIndex].aabbMax = Vector3.Max(_BVHNodes[nodeIndex].aabbMax, particlePos + sizeOfParticle);
        }
    }

    void SubdivideBVH(int nodeIndex)
    {
        if (_BVHNodes[nodeIndex].primitiveCount <= 512)
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
            string log = "Particle ID: " + i + " Position: " + _particles[i].position + "Predicted Position: " + _particles[i].predictedPosition + " Velocity: " + _particles[i].velocity + " Force: " + _particles[i].force + " Mass: " + _particles[i].mass + " Density: " + _particles[i].density.ToString("F6") + " Pressure: " + _particles[i].pressure.ToString("F6") + " Debug Vector: " + _particles[i].debugVector.ToString("F6") + "Cell ID: " + _particles[i].cellID;
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
        //Create particle buffer.
        if (_particleBuffer == null)
        {
            _particleBuffer = new ComputeBuffer(MaxNumOfParticles, _particleStride );
            _particleIndices = new ComputeBuffer(MaxNumOfParticles, sizeof(int));
            _particleCellIndices = new ComputeBuffer(MaxNumOfParticles, sizeof(int));
            _particleCellOffsets = new ComputeBuffer(MaxNumOfParticles, sizeof(int));

            //Cubed root num of particles:
            float numOfParticlesCubedRoot = Mathf.Pow(NumOfParticles, 1.0f / 3.0f);
            float numOfParticlesSquaredRoot = Mathf.Sqrt(NumOfParticles);
            Vector3 boxSize = Vector3.Min(_BoxSize, Vector3.one * 50);
            //Create particles.
            for (int i = 0; i < NumOfParticles; i++)
            {
                _ParticleIndices[i] = i;
                _ParticleIDs[i] = i;
                _particles[i].position = new Vector3( (i % numOfParticlesCubedRoot) / ((1/ boxSize.x)* numOfParticlesCubedRoot) - (boxSize.x*0.5f), ((i / numOfParticlesCubedRoot) % numOfParticlesCubedRoot) / ( (1/ boxSize.y)* numOfParticlesCubedRoot) - (boxSize.y * 0.5f), ((i / numOfParticlesSquaredRoot) % numOfParticlesCubedRoot) / ((1/ boxSize.z) * numOfParticlesCubedRoot) - (boxSize.z * 0.5f));
                _particles[i].mass = MassOfParticle;
                _particles[i].velocity = Vector3.zero;
                _particles[i].force = new Vector4(0.0f, -9.8f, 0.0f);
                _particles[i].density = 0.0f;
                _particles[i].pressure = 0.0f;
                _particles[i].predictedPosition = _particles[i].position;
            }

            _particleIndices.SetData(_ParticleIndices);
            _particleBuffer.SetData(_particles);
            _fluidSimulationComputeShader.SetBuffer(_UpdateParticlesKernelId, "_Particles", _particleBuffer);
            //Set particle buffer to shader.
            _fluidSimulationComputeShader.SetBuffer(_CreateGridKernelId, "_Particles", _particleBuffer);
            _fluidSimulationComputeShader.SetBuffer(_ComputeForcesKernelId, "_Particles", _particleBuffer);
            _fluidSimulationComputeShader.SetBuffer(_ComputeDensityKernelId, "_Particles", _particleBuffer);
            _fluidSimulationComputeShader.SetBuffer(_UpdatePositionDeltasKernelId, "_Particles", _particleBuffer);
            _fluidSimulationComputeShader.SetBuffer(_UpdatePositionsKernelId, "_Particles", _particleBuffer);
            _fluidSimulationComputeShader.SetBuffer(_HashParticlesKernelId, "_Particles", _particleBuffer);
            _fluidSimulationComputeShader.SetBuffer(_SortParticlesKernelId, "_Particles", _particleBuffer);
            
            _fluidSimulationComputeShader.SetBuffer(_UpdateParticlesKernelId, "_ParticleIndices", _particleIndices);
            _fluidSimulationComputeShader.SetBuffer(_CreateGridKernelId, "_ParticleIndices", _particleIndices);
            _fluidSimulationComputeShader.SetBuffer(_ComputeForcesKernelId, "_ParticleIndices", _particleIndices);
            _fluidSimulationComputeShader.SetBuffer(_ComputeDensityKernelId, "_ParticleIndices", _particleIndices);
            _fluidSimulationComputeShader.SetBuffer(_UpdatePositionDeltasKernelId, "_ParticleIndices", _particleIndices);
            _fluidSimulationComputeShader.SetBuffer(_UpdatePositionsKernelId, "_ParticleIndices", _particleIndices);
            _fluidSimulationComputeShader.SetBuffer(_HashParticlesKernelId, "_ParticleIndices", _particleIndices);
            _fluidSimulationComputeShader.SetBuffer(_SortParticlesKernelId, "_ParticleIndices", _particleIndices);
            _fluidSimulationComputeShader.SetBuffer(_CalculateCellOffsetsKernelId, "_ParticleIndices", _particleIndices);

            _fluidSimulationComputeShader.SetBuffer(_UpdateParticlesKernelId, "_ParticleCellIndices", _particleCellIndices);
            _fluidSimulationComputeShader.SetBuffer(_CreateGridKernelId, "_ParticleCellIndices", _particleCellIndices);
            _fluidSimulationComputeShader.SetBuffer(_ComputeForcesKernelId, "_ParticleCellIndices", _particleCellIndices);
            _fluidSimulationComputeShader.SetBuffer(_ComputeDensityKernelId, "_ParticleCellIndices", _particleCellIndices);
            _fluidSimulationComputeShader.SetBuffer(_UpdatePositionDeltasKernelId, "_ParticleCellIndices", _particleCellIndices);
            _fluidSimulationComputeShader.SetBuffer(_UpdatePositionsKernelId, "_ParticleCellIndices", _particleCellIndices);
            _fluidSimulationComputeShader.SetBuffer(_HashParticlesKernelId, "_ParticleCellIndices", _particleCellIndices);
            _fluidSimulationComputeShader.SetBuffer(_SortParticlesKernelId, "_ParticleCellIndices", _particleCellIndices);
            _fluidSimulationComputeShader.SetBuffer(_CalculateCellOffsetsKernelId, "_ParticleCellIndices", _particleCellIndices);

            _fluidSimulationComputeShader.SetBuffer(_UpdateParticlesKernelId, "_ParticleCellOffsets", _particleCellOffsets);
            _fluidSimulationComputeShader.SetBuffer(_CreateGridKernelId, "_ParticleCellOffsets", _particleCellOffsets);
            _fluidSimulationComputeShader.SetBuffer(_ComputeForcesKernelId, "_ParticleCellOffsets", _particleCellOffsets);
            _fluidSimulationComputeShader.SetBuffer(_ComputeDensityKernelId, "_ParticleCellOffsets", _particleCellOffsets);
            _fluidSimulationComputeShader.SetBuffer(_UpdatePositionDeltasKernelId, "_ParticleCellOffsets", _particleCellOffsets);
            _fluidSimulationComputeShader.SetBuffer(_UpdatePositionsKernelId, "_ParticleCellOffsets", _particleCellOffsets);
            _fluidSimulationComputeShader.SetBuffer(_HashParticlesKernelId, "_ParticleCellOffsets", _particleCellOffsets);
            _fluidSimulationComputeShader.SetBuffer(_SortParticlesKernelId, "_ParticleCellOffsets", _particleCellOffsets);
            _fluidSimulationComputeShader.SetBuffer(_CalculateCellOffsetsKernelId, "_ParticleCellOffsets", _particleCellOffsets);

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

        _fluidSimulationComputeShader.Dispatch(_ComputeForcesKernelId, Mathf.CeilToInt(NumOfParticles / _computeForcesThreadSize.x), 1, 1);
        _fluidSimulationComputeShader.Dispatch(_HashParticlesKernelId, Mathf.CeilToInt(NumOfParticles / _hashParticlesThreadSize.x), 1, 1);
        SortParticles();
        _fluidSimulationComputeShader.Dispatch(_CalculateCellOffsetsKernelId, Mathf.CeilToInt(NumOfParticles / _calculateCellOffsetsThreadSize.x), 1, 1);

        for (int i = 0; i < _SolveIterations; i++)
        {
            _fluidSimulationComputeShader.Dispatch(_ComputeDensityKernelId, Mathf.CeilToInt(NumOfParticles / _computeDensityThreadSize.x), 1, 1);
            _fluidSimulationComputeShader.Dispatch(_UpdatePositionDeltasKernelId, Mathf.CeilToInt(NumOfParticles / _updatePositionDeltasThreadSize.x), 1, 1);
            _fluidSimulationComputeShader.Dispatch(_UpdateParticlesKernelId, Mathf.CeilToInt(NumOfParticles / _updateParticlesThreadSize.x), 1, 1);
        }

        _fluidSimulationComputeShader.Dispatch(_UpdatePositionsKernelId, Mathf.CeilToInt(NumOfParticles / _updatePositionsThreadSize.x), 1, 1);
        //Set Particle positions to script.
        _particleBuffer.GetData(_particles);

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
        _fluidSimulationComputeShader.SetFloat("_SizeOfParticle", SizeOfParticle);
        _fluidSimulationComputeShader.SetFloat("_Radius", Radius);
        _fluidSimulationComputeShader.SetFloat("_GasConstant", GasConstant);
        _fluidSimulationComputeShader.SetFloat("_Viscosity", Viscosity);
        _fluidSimulationComputeShader.SetFloat("_TimeStep", TimeStep);
        _fluidSimulationComputeShader.SetFloat("_BoundsDamping", BoundsDamping);
        _fluidSimulationComputeShader.SetFloat("_RestDensity", RestDensity);
        _fluidSimulationComputeShader.SetVector("_BoxSize", _BoxSize);
        _fluidSimulationComputeShader.SetInt("_ChosenParticle", ChosenParticle);
        _fluidSimulationComputeShader.SetBool("_IsOrthographic", _cam.orthographic);



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
        _fluidSimMaterialComposite.SetTexture("_DensityMap", _densityMapTexture);


        //uint threadsX, threadsY, threadsZ;
        //_fluidSimulationCompute.GetKernelThreadGroupSizes(0, out threadsX, out threadsY, out threadsZ);
        //_fluidSimulationCompute.Dispatch(0, Mathf.CeilToInt(Screen.width / threadsX), Mathf.CeilToInt(Screen.width / threadsY), (int)threadsZ);

        //Execute shaders on render target.
        //Graphics.Blit(_rtTarget, _fluidDepthBuffer, _fluidSimMaterialDepth);
        //Graphics.Blit(source, _rtTarget, _fluidSimMaterialDepthHori);
        Graphics.Blit(source, destination, _fluidSimMaterialComposite);
    }
    
    void CreateFluidCommandBuffers()
    {
        CommandBuffer fluidCommandBuffers = new CommandBuffer();
        fluidCommandBuffers.SetGlobalTexture("_UnigmaFluids", _rtTarget);

        fluidCommandBuffers.SetRenderTarget(_rtTarget);

        fluidCommandBuffers.ClearRenderTarget(true, true, new Vector4(0,0,0,0));

        fluidCommandBuffers.SetComputeTextureParam(_fluidSimulationComputeShader, _CreateGridKernelId, "Result", _rtTarget);
        fluidCommandBuffers.DispatchCompute(_fluidSimulationComputeShader, _CreateGridKernelId, Mathf.CeilToInt(_renderTextureWidth / _createGridThreadSize.x), Mathf.CeilToInt(_renderTextureHeight / _createGridThreadSize.y), (int)_createGridThreadSize.z);

        fluidCommandBuffers.SetGlobalTexture("_UnigmaFluidsDepth", _fluidDepthBufferTexture);

        fluidCommandBuffers.SetRenderTarget(_fluidDepthBufferTexture);

        //fluidCommandBuffers.ClearRenderTarget(true, true, new Vector4(0, 0, 0, 0));
        
        fluidCommandBuffers.Blit(_rtTarget, _fluidDepthBufferTexture, _fluidSimMaterialDepthHori);
        fluidCommandBuffers.Blit(_fluidDepthBufferTexture, _tempTarget);
        fluidCommandBuffers.Blit(_tempTarget, _fluidDepthBufferTexture, _fluidSimMaterialDepthVert);


        fluidCommandBuffers.SetGlobalTexture("_UnigmaFluidsNormals", _fluidNormalBufferTexture);

        fluidCommandBuffers.SetRenderTarget(_fluidNormalBufferTexture);

        //fluidCommandBuffers.ClearRenderTarget(true, true, new Vector4(0, 0, 0, 0));

        fluidCommandBuffers.Blit(_fluidDepthBufferTexture, _fluidNormalBufferTexture, _fluidSimMaterialNormal);

        GetComponent<Camera>().AddCommandBuffer(CameraEvent.AfterForwardOpaque, fluidCommandBuffers);

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
        if (_pNodesBuffer != null)
            _pNodesBuffer.Release();
        if (_particleIDsBuffer != null)
            _particleIDsBuffer.Release();
        if(_BVHNodesBuffer != null)
            _BVHNodesBuffer.Release();

        _rtTarget.Release();
        _densityMapTexture.Release();
        _tempTarget.Release();
        _fluidNormalBufferTexture.Release();
        _fluidDepthBufferTexture.Release();

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
