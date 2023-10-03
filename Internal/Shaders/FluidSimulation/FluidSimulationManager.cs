using RayFire;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

public class FluidSimulationManager : MonoBehaviour
{
    ComputeShader _fluidSimulationCompute;
    ComputeBuffer _BVHNodesBuffer;
    int numParticles;
    int _UpdateParticlesKernel;
    int _ComputeForces;
    int _ComputeDensity;
    int _CreateGrid;
    int _UpdatePositionDeltas;
    int _UpdatePositions;
    int _HashParticles;
    int _SortParticles;
    int _CalculateCellOffsets;
    Camera _cam;
    RenderTexture _rtTarget;
    RenderTexture _densityMap;
    RenderTexture _tempTarget;
    RenderTexture _fluidNormalBuffer;
    RenderTexture _fluidDepthBuffer;

    Shader _fluidNormalShader;
    Shader _fluidDepthShader;
    Shader _fluidCompositeShader;
    Material _fluidSimMaterialDepthHori;
    Material _fluidSimMaterialDepthVert;
    Material _fluidSimMaterialNormal;
    public Material _fluidSimMaterialComposite;
    public Transform fluidSimTransform;
    public int textSizeDivision = 0; // (1 / t + 1) How much to divide the text size by. This lowers the resolution of the final image, but massively aids in performance.
    private int _width, _height = 0;

    public Color DeepWaterColor = Color.white;
    public Color ShallowWaterColor = Color.white;
    public float DepthMaxDistance = 100;
    public Vector2 BlurScale;
    public float BlurFallOff = 0.25f;
    public float BlurRadius = 5.0f;
    public Vector4 DepthScale = default;
    public float _Smoothness = 0.25f;
    public Transform _LightSouce;
    public Transform _LightScale;
    public int numOfParticles;
    public float _SizeOfParticle = 0.125f;
    public float _MassOfParticle = 1.0f;
    public float _GasConstant = 1.0f;
    public float _Viscosity = 1.0f;
    public float _TimeStep = 0.02f;
    public float _BoundsDamping = 9.8f;
    public float _Radius = 0.125f;
    public float _RestDensity = 1.0f;
    public Vector3 _BoxSize = Vector3.one;
    public int _BoxViewDebug = 0;
    public int _ChosenParticle = 0;
    private List<Vector3> SpawnParticles = default;

    ComputeBuffer _meshObjectBuffer;
    ComputeBuffer _verticesObjectBuffer;
    ComputeBuffer _indicesObjectBuffer;
    ComputeBuffer _particleBuffer;
    ComputeBuffer _pNodesBuffer;
    ComputeBuffer _particleIDsBuffer;
    ComputeBuffer _particleIndices;
    ComputeBuffer _particleCellIndices;
    ComputeBuffer _particleCellOffsets;

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
        public Vector3 force;
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
    
    int _ParticleStride = sizeof(int) + sizeof(float) + sizeof(float) + sizeof(float) + ((sizeof(float) * 3) * 6);
    int _BVHStride = sizeof(float) * 3 * 2 + sizeof(int) * 8;
    //Items to add to the raytracer.
    public LayerMask RayTracingLayers;

    private List<Renderer> _RayTracedObjects = new List<Renderer>();

    private List<MeshObject> meshObjects = new List<MeshObject>();
    private List<Vertex> Vertices = new List<Vertex>();
    private List<int> Indices = new List<int>();
    private Particles[] _Particles;
    private int[] _ParticleIDs;
    private int[] _ParticleIndices;
    private int[] _ParticleCellIndices;
    private int[] _ParticleCellOffsets;
    //private Stack<int> _ParticleIDs = new Stack<int>();

    private List<PNode> PNodes;
    private BVHNode[] _BVHNodes;

    public int _SolveIterations = 1;
    int nodesUsed = 1;
    private void Awake()
    {
        SpawnParticles = new List<Vector3>();
        _width = Mathf.Max(Mathf.Min(Mathf.CeilToInt(Screen.width * (1.0f / (1.0f + Mathf.Abs(textSizeDivision)))), Screen.width), 32);
        _height = Mathf.Max(Mathf.Min(Mathf.CeilToInt(Screen.height * (1.0f / (1.0f + Mathf.Abs(textSizeDivision)))), Screen.height), 32);
        _fluidSimulationCompute = Resources.Load<ComputeShader>("FluidSimCompute");
        
        _fluidNormalShader = Resources.Load<Shader>("FluidNormalBuffer");
        _fluidDepthShader = Resources.Load<Shader>("FluidBilateralFilter");
        
        //Create the material for the fluid simulation.
        _fluidSimMaterialDepthHori = new Material(_fluidDepthShader);
        _fluidSimMaterialDepthVert = new Material(_fluidDepthShader);
        _fluidSimMaterialNormal = new Material(_fluidNormalShader);


        _cam = Camera.main;
        //Create the texture for compute shader.
        _rtTarget = RenderTexture.GetTemporary(_width, _height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        _densityMap = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        _tempTarget = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        _fluidNormalBuffer = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        _fluidDepthBuffer = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);

        _Particles = new Particles[numOfParticles];
        _ParticleIDs = new int[numOfParticles];
        _ParticleIndices = new int[numOfParticles];
        _ParticleCellIndices = new int[numOfParticles];
        _ParticleCellOffsets = new int[numOfParticles];
        _BVHNodes = new BVHNode[numOfParticles];

        _UpdateParticlesKernel = _fluidSimulationCompute.FindKernel("UpdateParticles");
        _HashParticles = _fluidSimulationCompute.FindKernel("HashParticles");
        _SortParticles = _fluidSimulationCompute.FindKernel("BitonicSort");
        _CalculateCellOffsets = _fluidSimulationCompute.FindKernel("CalculateCellOffsets");
        _CreateGrid = _fluidSimulationCompute.FindKernel("CreateGrid");
        _ComputeForces = _fluidSimulationCompute.FindKernel("ComputeForces");
        _ComputeDensity = _fluidSimulationCompute.FindKernel("ComputeDensity");
        _UpdatePositionDeltas = _fluidSimulationCompute.FindKernel("UpdatePositionDeltas");
        _UpdatePositions = _fluidSimulationCompute.FindKernel("UpdatePositions");


        _rtTarget.enableRandomWrite = true;
        _rtTarget.Create();
        _densityMap.enableRandomWrite = true;
        _densityMap.Create();
        _fluidNormalBuffer.enableRandomWrite = true;
        _fluidNormalBuffer.Create();
        _fluidSimulationCompute.SetTexture(_CreateGrid, "Result", _rtTarget);
        _fluidSimulationCompute.SetTexture(_CreateGrid, "DensityMap", _densityMap);
        _fluidSimulationCompute.SetTexture(_CreateGrid, "NormalMap", _fluidNormalBuffer);
        
        
        AddObjectsToList();
        CreateNonAcceleratedStructure();
        CreateFluidCommandBuffers();
        //UpdateNonAcceleratedRayTracer();

    }

    void Update()
    {
        //Draw mesh instantiaonation.
        //Graphics.DrawMeshInstancedIndirect()
        

    }

    void SortParticles()
    {
        for (int biDim = 2; biDim <= numOfParticles; biDim <<= 1)
        {
            _fluidSimulationCompute.SetInt("biDim", biDim);
            for (int biBlock = biDim >> 1; biBlock > 0; biBlock >>= 1)
            {
                _fluidSimulationCompute.SetInt("biBlock", biBlock);
                _fluidSimulationCompute.Dispatch(_SortParticles, numOfParticles / 256, 1, 1);
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
                _RayTracedObjects.Add(obj);
            }
        }
    }

    void CreateNonAcceleratedStructure()
    {
        BuildTriangleList();
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
        _meshObjectBuffer = new ComputeBuffer(meshObjects.Count, 144);
        _verticesObjectBuffer = new ComputeBuffer(Vertices.Count, 32);
        _indicesObjectBuffer = new ComputeBuffer(Indices.Count, 4);
        _verticesObjectBuffer.SetData(Vertices);
        _fluidSimulationCompute.SetBuffer(_CreateGrid, "_Vertices", _verticesObjectBuffer);
        _fluidSimulationCompute.SetBuffer(_UpdateParticlesKernel, "_Vertices", _verticesObjectBuffer);
        _indicesObjectBuffer.SetData(Indices);
        _fluidSimulationCompute.SetBuffer(_CreateGrid, "_Indices", _indicesObjectBuffer);
        _fluidSimulationCompute.SetBuffer(_UpdateParticlesKernel, "_Indices", _indicesObjectBuffer);
    }

    void UpdateNonAcceleratedRayTracer()
    {
        //Build the BVH
        UpdateParticles();
        BuildBVH();
        //Only if spacebar is pressed
        if (Input.GetKeyDown(KeyCode.Space))
        {
            PrintParticleData();
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
            _BVHNodesBuffer = new ComputeBuffer(_BVHNodes.Length, _BVHStride);
            _particleIDsBuffer = new ComputeBuffer(_ParticleIDs.Length, 4);
        }

        for (int i = 0; i < _RayTracedObjects.Count; i++)
        {
            MeshObject meshobj = new MeshObject();
            RayTracingObject rto = _RayTracedObjects[i].GetComponent<RayTracingObject>();
            meshobj.localToWorld = _RayTracedObjects[i].GetComponent<Renderer>().localToWorldMatrix;
            meshobj.indicesOffset = meshObjects[i].indicesOffset;
            meshobj.indicesCount = meshObjects[i].indicesCount;
            meshobj.position = _RayTracedObjects[i].transform.position;
            meshobj.AABBMin = _RayTracedObjects[i].bounds.min;
            meshobj.AABBMax = _RayTracedObjects[i].bounds.max;
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
            meshObjects[i] = meshobj;
        }
        if (_meshObjectBuffer.count > 0)
        {
            _meshObjectBuffer.SetData(meshObjects);
            _fluidSimulationCompute.SetBuffer(_CreateGrid, "_MeshObjects", _meshObjectBuffer);
            _fluidSimulationCompute.SetBuffer(_UpdateParticlesKernel, "_MeshObjects", _meshObjectBuffer);
        }

        _meshObjectBuffer.SetData(meshObjects);
        _fluidSimulationCompute.SetBuffer(_CreateGrid, "_MeshObjects", _meshObjectBuffer);
        _fluidSimulationCompute.SetBuffer(_UpdateParticlesKernel, "_MeshObjects", _meshObjectBuffer);

        //Update Particle BVH.
        int rootNodeIndex = 0;
        nodesUsed = 1;
        //Initialize nodes, rebuild BVH each frame set to 0 index, and the number of particles to all.
        _BVHNodes[rootNodeIndex].index = rootNodeIndex;
        _BVHNodes[rootNodeIndex].leftChild = 0;
        _BVHNodes[rootNodeIndex].parent = -1;
        _BVHNodes[rootNodeIndex].primitiveOffset = 0;
        _BVHNodes[rootNodeIndex].primitiveCount = numOfParticles;

        UpdateNodeBounds(rootNodeIndex);
        SubdivideBVH(rootNodeIndex);
        CreateHitMissLinks();

        //Set the BVH to the compute shader.
        _BVHNodesBuffer.SetData(_BVHNodes);
        _particleIDsBuffer.SetData(_ParticleIDs);
        _fluidSimulationCompute.SetBuffer(_CreateGrid, "_BVHNodes", _BVHNodesBuffer);
        _fluidSimulationCompute.SetBuffer(_CreateGrid, "_ParticleIDs", _particleIDsBuffer);
        _fluidSimulationCompute.SetInt("_NumOfNodes", nodesUsed);
        _fluidSimulationCompute.SetInt("_NumOfParticles", numOfParticles);
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
            Vector3 particlePos = _Particles[particleIndex].position;
            Vector3 sizeOfParticle = 1 * new Vector3(_SizeOfParticle, _SizeOfParticle, _SizeOfParticle);
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
            if (_Particles[_ParticleIDs[i]].position[axis] < splitPos)
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
        for (int i = 0; i < numOfParticles; i++)
        {
            //Debug each particle struct.
            string log = "Particle ID: " + i + " Position: " + _Particles[i].position + "Predicted Position: " + _Particles[i].predictedPosition + " Velocity: " + _Particles[i].velocity + " Force: " + _Particles[i].force + " Mass: " + _Particles[i].mass + " Density: " + _Particles[i].density.ToString("F6") + " Pressure: " + _Particles[i].pressure.ToString("F6") + " Debug Vector: " + _Particles[i].debugVector.ToString("F6") + "Cell ID: " + _Particles[i].cellID;
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

    private void SpawnParticlesInBox()
    {
        Vector3 spawnTopLeft = _BoxSize / 2;
        int xIterations = Mathf.RoundToInt(_BoxSize.x / (_SizeOfParticle * 2));
        int yIterations = Mathf.RoundToInt(_BoxSize.y / (_SizeOfParticle * 2));
        int zIterations = Mathf.RoundToInt(_BoxSize.z / (_SizeOfParticle * 2));

        for (int x = 1; x < xIterations; x++)
        {
            for (int y = 1; y < yIterations; y++)
            {
                for (int z = 1; z < zIterations; z++)
                {

                    Vector3 spawnPosition = new Vector3(x * _SizeOfParticle * 2, y * _SizeOfParticle * 2, z * _SizeOfParticle * 2) + Random.onUnitSphere * _SizeOfParticle * 0.5f;

                    Vector3 p = new Vector3(spawnPosition.x, spawnPosition.y, spawnPosition.z);
                    SpawnParticles.Add(p);

                }
            }
        }

    }

    void UpdateParticles()
    {
        //Create particle buffer.
        if (_particleBuffer == null)
        {
            _particleBuffer = new ComputeBuffer(numOfParticles, _ParticleStride );
            _particleIndices = new ComputeBuffer(numOfParticles, sizeof(int));
            _particleCellIndices = new ComputeBuffer(numOfParticles, sizeof(int));
            _particleCellOffsets = new ComputeBuffer(numOfParticles, sizeof(int));

            //Cubed root num of particles:
            float numOfParticlesCubedRoot = Mathf.Pow(numOfParticles, 1.0f / 3.0f);
            float numOfParticlesSquaredRoot = Mathf.Sqrt(numOfParticles);
            //Create particles.
            SpawnParticlesInBox();
            for (int i = 0; i < numOfParticles; i++)
            {
                _ParticleIndices[i] = i;
                //_ParticleCellIndices[i] = i;
                //_ParticleCellOffsets[i] = i;
                _ParticleIDs[i] = i;
                //_Particles[i].position = new Vector3(0.25f, 0.25f, 0.25f) * i;
                _Particles[i].position = new Vector3( (i % numOfParticlesCubedRoot) / ((1/_BoxSize.x)* numOfParticlesCubedRoot) - (_BoxSize.x*0.5f), ((i / numOfParticlesCubedRoot) % numOfParticlesCubedRoot) / ( (1/_BoxSize.y)* numOfParticlesCubedRoot) - (_BoxSize.y * 0.5f), ((i / numOfParticlesSquaredRoot) % numOfParticlesCubedRoot) / ((1/_BoxSize.z) * numOfParticlesCubedRoot) - (_BoxSize.z * 0.5f));
                //_Particles[i].position = SpawnParticles[i];
                //_Particles[i].position = fluidSimTransform.localToWorldMatrix.MultiplyPoint(_Particles[i].position);
                //_Particles[i].position = new Vector3((i % numOfParticlesCubedRoot) / (_SizeOfParticle * numOfParticlesCubedRoot) - (_BoxSize.x * 0.5f), ((i / numOfParticlesCubedRoot) % numOfParticlesCubedRoot) / ((1 / _BoxSize.y) * numOfParticlesCubedRoot) - (_BoxSize.y * 0.5f), ((i / numOfParticlesSquaredRoot) % numOfParticlesCubedRoot) / ((1 / _BoxSize.z) * numOfParticlesCubedRoot) - (_BoxSize.z * 0.5f));
                _Particles[i].mass = _MassOfParticle;
                _Particles[i].velocity = Vector3.zero;
                _Particles[i].force = Vector3.zero;
                _Particles[i].density = 0.0f;
                _Particles[i].pressure = 0.0f;
                _Particles[i].predictedPosition = _Particles[i].position;
            }

            _particleIndices.SetData(_ParticleIndices);
            _particleBuffer.SetData(_Particles);
            _fluidSimulationCompute.SetBuffer(_UpdateParticlesKernel, "_Particles", _particleBuffer);
            //Set particle buffer to shader.
            _fluidSimulationCompute.SetBuffer(_CreateGrid, "_Particles", _particleBuffer);
            _fluidSimulationCompute.SetBuffer(_ComputeForces, "_Particles", _particleBuffer);
            _fluidSimulationCompute.SetBuffer(_ComputeDensity, "_Particles", _particleBuffer);
            _fluidSimulationCompute.SetBuffer(_UpdatePositionDeltas, "_Particles", _particleBuffer);
            _fluidSimulationCompute.SetBuffer(_UpdatePositions, "_Particles", _particleBuffer);
            _fluidSimulationCompute.SetBuffer(_HashParticles, "_Particles", _particleBuffer);
            _fluidSimulationCompute.SetBuffer(_SortParticles, "_Particles", _particleBuffer);
            

            _fluidSimulationCompute.SetBuffer(_UpdateParticlesKernel, "_ParticleIndices", _particleIndices);
            _fluidSimulationCompute.SetBuffer(_CreateGrid, "_ParticleIndices", _particleIndices);
            _fluidSimulationCompute.SetBuffer(_ComputeForces, "_ParticleIndices", _particleIndices);
            _fluidSimulationCompute.SetBuffer(_ComputeDensity, "_ParticleIndices", _particleIndices);
            _fluidSimulationCompute.SetBuffer(_UpdatePositionDeltas, "_ParticleIndices", _particleIndices);
            _fluidSimulationCompute.SetBuffer(_UpdatePositions, "_ParticleIndices", _particleIndices);
            _fluidSimulationCompute.SetBuffer(_HashParticles, "_ParticleIndices", _particleIndices);
            _fluidSimulationCompute.SetBuffer(_SortParticles, "_ParticleIndices", _particleIndices);
            _fluidSimulationCompute.SetBuffer(_CalculateCellOffsets, "_ParticleIndices", _particleIndices);

            _fluidSimulationCompute.SetBuffer(_UpdateParticlesKernel, "_ParticleCellIndices", _particleCellIndices);
            _fluidSimulationCompute.SetBuffer(_CreateGrid, "_ParticleCellIndices", _particleCellIndices);
            _fluidSimulationCompute.SetBuffer(_ComputeForces, "_ParticleCellIndices", _particleCellIndices);
            _fluidSimulationCompute.SetBuffer(_ComputeDensity, "_ParticleCellIndices", _particleCellIndices);
            _fluidSimulationCompute.SetBuffer(_UpdatePositionDeltas, "_ParticleCellIndices", _particleCellIndices);
            _fluidSimulationCompute.SetBuffer(_UpdatePositions, "_ParticleCellIndices", _particleCellIndices);
            _fluidSimulationCompute.SetBuffer(_HashParticles, "_ParticleCellIndices", _particleCellIndices);
            _fluidSimulationCompute.SetBuffer(_SortParticles, "_ParticleCellIndices", _particleCellIndices);
            _fluidSimulationCompute.SetBuffer(_CalculateCellOffsets, "_ParticleCellIndices", _particleCellIndices);

            _fluidSimulationCompute.SetBuffer(_UpdateParticlesKernel, "_ParticleCellOffsets", _particleCellOffsets);
            _fluidSimulationCompute.SetBuffer(_CreateGrid, "_ParticleCellOffsets", _particleCellOffsets);
            _fluidSimulationCompute.SetBuffer(_ComputeForces, "_ParticleCellOffsets", _particleCellOffsets);
            _fluidSimulationCompute.SetBuffer(_ComputeDensity, "_ParticleCellOffsets", _particleCellOffsets);
            _fluidSimulationCompute.SetBuffer(_UpdatePositionDeltas, "_ParticleCellOffsets", _particleCellOffsets);
            _fluidSimulationCompute.SetBuffer(_UpdatePositions, "_ParticleCellOffsets", _particleCellOffsets);
            _fluidSimulationCompute.SetBuffer(_HashParticles, "_ParticleCellOffsets", _particleCellOffsets);
            _fluidSimulationCompute.SetBuffer(_SortParticles, "_ParticleCellOffsets", _particleCellOffsets);
            _fluidSimulationCompute.SetBuffer(_CalculateCellOffsets, "_ParticleCellOffsets", _particleCellOffsets);

            //CreateQuadTree();

        }
        for (int i = 0; i < numOfParticles; i++)
        {
            _ParticleIDs[i] = i;
            _BVHNodes[i].primitiveOffset = 0;
            _BVHNodes[i].primitiveCount = 0;
            _BVHNodes[i].parent = -1;
            _BVHNodes[i].leftChild = -1;
            _BVHNodes[i].rightChild = -1;
            _Particles[i].mass = _MassOfParticle;
        }
        //Update particles.
        uint threadsX, threadsY, threadsZ;
        _fluidSimulationCompute.GetKernelThreadGroupSizes(_UpdateParticlesKernel, out threadsX, out threadsY, out threadsZ);

        //Make proper thread group sizes.
        _fluidSimulationCompute.Dispatch(_ComputeForces, numOfParticles / 64, (int)threadsY, (int)threadsZ);

        _fluidSimulationCompute.Dispatch(_HashParticles, numOfParticles / 256, (int)threadsY, (int)threadsZ);
        SortParticles();
        _fluidSimulationCompute.Dispatch(_CalculateCellOffsets, numOfParticles / 256, (int)threadsY, (int)threadsZ);

        for (int i = 0; i < _SolveIterations; i++)
        {
            _fluidSimulationCompute.Dispatch(_ComputeDensity, numOfParticles / 64, (int)threadsY, (int)threadsZ);
            _fluidSimulationCompute.Dispatch(_UpdatePositionDeltas, numOfParticles / 64, (int)threadsY, (int)threadsZ);
            _fluidSimulationCompute.Dispatch(_UpdateParticlesKernel, numOfParticles / 64, (int)threadsY, (int)threadsZ);
        }

        _fluidSimulationCompute.Dispatch(_UpdatePositions, numOfParticles / 64, (int)threadsY, (int)threadsZ);

        //Set Particle positions to script.
        _particleBuffer.GetData(_Particles);

    }

    //Temporarily attach this simulation to camera!!!
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

        if (_width != _rtTarget.width || _height != _rtTarget.height)
        {
            if (_rtTarget != null)
            {
                _rtTarget.Release();
            }
            _rtTarget = RenderTexture.GetTemporary(_width, _height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            _rtTarget.enableRandomWrite = true;
            _rtTarget.Create();
        }

        _fluidSimulationCompute.SetMatrix("_CameraToWorld", _cam.cameraToWorldMatrix);
        _fluidSimulationCompute.SetMatrix("_CameraWorldToLocal", _cam.transform.worldToLocalMatrix);
        _fluidSimulationCompute.SetMatrix("_CameraInverseProjection", _cam.projectionMatrix.inverse);
        _fluidSimulationCompute.SetMatrix("_ParentTransform", fluidSimTransform.localToWorldMatrix);
        _fluidSimulationCompute.SetMatrix("_ParentTransformToLocal", fluidSimTransform.worldToLocalMatrix);
        _fluidSimulationCompute.SetVector("_DepthScale", DepthScale);
        _fluidSimulationCompute.SetFloat("_Smoothness", _Smoothness);
        _fluidSimulationCompute.SetVector("_LightSource", _LightSouce.position);
        _fluidSimulationCompute.SetVector("_LightScale", _LightScale.position);
        _fluidSimulationCompute.SetFloat("_SizeOfParticle", _SizeOfParticle);
        _fluidSimulationCompute.SetFloat("_Radius", _Radius);
        _fluidSimulationCompute.SetFloat("_GasConstant", _GasConstant);
        _fluidSimulationCompute.SetFloat("_Viscosity", _Viscosity);
        _fluidSimulationCompute.SetFloat("_TimeStep", _TimeStep);
        _fluidSimulationCompute.SetFloat("_BoundsDamping", _BoundsDamping);
        _fluidSimulationCompute.SetFloat("_RestDensity", _RestDensity);
        _fluidSimulationCompute.SetVector("_BoxSize", _BoxSize);
        _fluidSimulationCompute.SetInt("_ChosenParticle", _ChosenParticle);
        _fluidSimulationCompute.SetBool("_IsOrthographic", _cam.orthographic);



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
        _fluidSimMaterialComposite.SetTexture("_DensityMap", _densityMap);


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

        uint threadsX, threadsY, threadsZ;
        _fluidSimulationCompute.GetKernelThreadGroupSizes(_CreateGrid, out threadsX, out threadsY, out threadsZ);

        fluidCommandBuffers.SetComputeTextureParam(_fluidSimulationCompute, _CreateGrid, "Result", _rtTarget);
        fluidCommandBuffers.DispatchCompute(_fluidSimulationCompute, _CreateGrid, Mathf.CeilToInt(_width / threadsX), Mathf.CeilToInt(_height / threadsY), (int)threadsZ);

        fluidCommandBuffers.SetGlobalTexture("_UnigmaFluidsDepth", _fluidDepthBuffer);

        fluidCommandBuffers.SetRenderTarget(_fluidDepthBuffer);

        //fluidCommandBuffers.ClearRenderTarget(true, true, new Vector4(0, 0, 0, 0));

        fluidCommandBuffers.Blit(_rtTarget, _fluidDepthBuffer, _fluidSimMaterialDepthHori);
        fluidCommandBuffers.Blit(_fluidDepthBuffer, _tempTarget);
        fluidCommandBuffers.Blit(_tempTarget, _fluidDepthBuffer, _fluidSimMaterialDepthVert);


        fluidCommandBuffers.SetGlobalTexture("_UnigmaFluidsNormals", _fluidNormalBuffer);

        fluidCommandBuffers.SetRenderTarget(_fluidNormalBuffer);

        //fluidCommandBuffers.ClearRenderTarget(true, true, new Vector4(0, 0, 0, 0));

        fluidCommandBuffers.Blit(_fluidDepthBuffer, _fluidNormalBuffer, _fluidSimMaterialNormal);

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
        return n % _Particles.Length;
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
        _densityMap.Release();
        _tempTarget.Release();
        _fluidNormalBuffer.Release();
        _fluidDepthBuffer.Release();

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
        if (_fluidSimulationCompute != null)
            _fluidSimulationCompute.SetInt("_BoxViewDebug", _BoxViewDebug);
        Gizmos.color = Color.yellow;
        //Gizmos.DrawSphere(fluidSimTransform.position, 10);
        Matrix4x4 rotationMatrix = Matrix4x4.TRS(fluidSimTransform.position, fluidSimTransform.rotation, fluidSimTransform.lossyScale);
        Gizmos.matrix = rotationMatrix;
        Gizmos.DrawWireCube(Vector3.zero, _BoxSize);
    }
}
