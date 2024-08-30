using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using static UnigmaPhysicsManager;

public class UnigmaPhysicsManager : MonoBehaviour
{
    public struct MeshObject
    {
        public Matrix4x4 localToWorld;
        public int indicesOffset;
        public int indicesCount;
        public Vector3 position;
        public Vector3 AABBMin;
        public Vector3 AABBMax;
        public Vector3 color;
        public Matrix4x4 collisionForcesHigh;
        public Matrix4x4 collisionForcesLow;
        public uint id;
        public Matrix4x4 absorbtionMatrix;
        public int emitterType;
    };
    int _meshObjectStride = (sizeof(float) * 4 * 4 * 4) + sizeof(int) * 4 + sizeof(float) * 3 * 4;
    
    public struct PhysicsObject
    {
        public int objectId;
        public float3 position;
        public float strength;
        public float kelvin;
        public float radius;

    };
    int _physicsObjectsStride = sizeof(int) * 1 + sizeof(float) * 3 * 1 + sizeof(float) * 3;
    


    public struct Vertex
    {
        public Vector3 position;
        public Vector3 normal;
        public Vector2 uv;
    };

    [HideInInspector]
    public MeshObject[] MeshObjects;


    public FluidSimulationManager unigmaFluids;
    public UnigmaSpaceTime unigmaSpaceTime;

    //Lists
    public List<Renderer> _physicsRenderers = new List<Renderer>();
    public List<UnigmaPhysicsObject> _physicsObjects = new List<UnigmaPhysicsObject>();
    public List<Vertex> _vertices = new List<Vertex>();
    public List<int> _indices = new List<int>();

    public List<PhysicsObject> PhysicsObjectsArray { get; private set; } = default;

    //Compute Buffers.
    [HideInInspector]
    public ComputeBuffer _meshObjectBuffer { get; private set; }
    [HideInInspector]
    public ComputeBuffer _verticesObjectBuffer { get; private set; }
    [HideInInspector]
    public ComputeBuffer _indicesObjectBuffer { get; private set; }
    [HideInInspector]
    public ComputeBuffer _physicsObjectsBuffer { get; private set; }


    public static UnigmaPhysicsManager Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    public void Initialize(UnigmaScene uScene)
    {
        PhysicsObjectsArray = new List<PhysicsObject>();

        AddObjectsToList();
        BuildTriangleList();
        CreateMeshes();

        //Add physics objects.
        UnigmaPhysicsObject[] physicsObjects = GetComponentsInChildren<UnigmaPhysicsObject>();
        for (int i = 0; i < physicsObjects.Length; i++)
        {
            AddPhysicsObject(physicsObjects[i].physicsObject);
            physicsObjects[i].Initialize();
        }


        _physicsObjectsBuffer = new ComputeBuffer(Mathf.Max(PhysicsObjectsArray.Count, 1), _physicsObjectsStride);

        unigmaSpaceTime = gameObject.AddComponent<UnigmaSpaceTime>() as UnigmaSpaceTime;
        //unigmaFluids = gameObject.AddComponent<FluidSimulationManager>() as FluidSimulationManager;
        unigmaSpaceTime.Initialize(uScene.SpaceTimeBoxSize, uScene.SpaceTimeResolution, uScene.Temperature);
    }

    private void Start()
    {

    }

    private void Update()
    {
        SetPhysicsObjects();
        //BuildTriangleList();
        CreateMeshes();
    }
    void AddObjectsToList()
    {
        foreach (var obj in FindObjectsOfType<Renderer>())
        {
            //Check if object in the RaytracingLayers.
            if (((1 << obj.gameObject.layer) & UnigmaEngineManager.Instance.RayTracingLayers) != 0)
            {
                if (((1 << obj.gameObject.layer) & UnigmaEngineManager.Instance.FluidInteractionLayers) != 0)
                {
                    Debug.Log(obj.name);
                    _physicsRenderers.Add(obj);
                }
            }
        }
    }
    void BuildTriangleList()
    {
        _vertices.Clear();
        _indices.Clear();

        int indexMeshObject = 0;
        MeshObjects = new MeshObject[_physicsRenderers.Count];
        for (int rIndex = 0; rIndex < _physicsRenderers.Count; rIndex++)
        {
            Renderer r = _physicsRenderers[rIndex];
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
                    if (i < m.normals.Length)
                        v.normal = m.normals[i];
                    if (i < m.uv.Length)
                        v.uv = m.uv[i];
                    _vertices.Add(v);
                }
                var indices = m.GetIndices(0);
                _indices.AddRange(indices.Select(index => index + startVert));

                // Add the object itself
                MeshObjects[indexMeshObject] = new MeshObject()
                {
                    localToWorld = r.transform.localToWorldMatrix,
                    indicesOffset = startIndex,
                    indicesCount = indices.Length,
                    id = (uint)rIndex

                };
                indexMeshObject++;
            }
        }
        if (MeshObjects.Length > 0)
        {
            _meshObjectBuffer = new ComputeBuffer(MeshObjects.Length, _meshObjectStride);
            _verticesObjectBuffer = new ComputeBuffer(_vertices.Count, 32);
            _indicesObjectBuffer = new ComputeBuffer(_indices.Count, 4);
            _verticesObjectBuffer.SetData(_vertices);
            _indicesObjectBuffer.SetData(_indices);
        }

    }

    void CreateMeshes()
    {
        for (int i = 0; i < _physicsRenderers.Count; i++)
        {
            MeshObject meshobj = new MeshObject();
            UnigmaPhysicsObject uPObj = _physicsRenderers[i].GetComponent<UnigmaPhysicsObject>();
            RayTracingObject rto = _physicsRenderers[i].GetComponent<RayTracingObject>();
            meshobj.localToWorld = _physicsRenderers[i].GetComponent<Renderer>().localToWorldMatrix;
            meshobj.indicesOffset = MeshObjects[i].indicesOffset;
            meshobj.indicesCount = MeshObjects[i].indicesCount;
            meshobj.position = _physicsRenderers[i].transform.position;
            meshobj.AABBMin = _physicsRenderers[i].bounds.min;
            meshobj.AABBMax = _physicsRenderers[i].bounds.max;
            meshobj.id = (uint)i;
            meshobj.emitterType = -1;
            //Set matrix4x4 to all zeroes.
            meshobj.collisionForcesHigh = Matrix4x4.zero;
            meshobj.collisionForcesLow = Matrix4x4.zero;
            if (rto)
            {
                meshobj.color = new Vector3(rto.color.r, rto.color.g, rto.color.b);
            }

            if (uPObj)
            {
                meshobj.emitterType = uPObj.emitterType;
            }

            BoxCollider boxCollide = _physicsRenderers[(int)MeshObjects[i].id].GetComponent<BoxCollider>();
            Rigidbody rb = _physicsRenderers[(int)MeshObjects[i].id].GetComponent<Rigidbody>();
            if (boxCollide != null && rb != null)
            {
                Vector3 minPoint = _physicsRenderers[(int)MeshObjects[i].id].transform.TransformPoint(boxCollide.center + new Vector3(-boxCollide.size.x, -boxCollide.size.y, -boxCollide.size.z) * 0.5f);
                Vector3 maxPoint = _physicsRenderers[(int)MeshObjects[i].id].transform.TransformPoint(boxCollide.center + new Vector3(boxCollide.size.x, boxCollide.size.y, boxCollide.size.z) * 0.5f);

                meshobj.AABBMin = minPoint;
                meshobj.AABBMax = maxPoint;
            }
            MeshObjects[i] = meshobj;
        }

        if (_meshObjectBuffer.count > 0)
        {
            _meshObjectBuffer.SetData(MeshObjects);
        }

        _meshObjectBuffer.SetData(MeshObjects);
    }


    private IEnumerator ReactToForces()
    {

        //_meshObjectBuffer.GetData(_meshObjects);

        while (true)
        {
            AsyncGPUReadbackRequest request = AsyncGPUReadback.Request(_meshObjectBuffer);
            while (!request.done)
            {
                yield return null;
            }

            if (request.done)
            {
                MeshObjects = request.GetData<MeshObject>().ToArray();

                for (int i = 0; i < MeshObjects.Length; i++)
                {
                    MeshObject mObj = MeshObjects[i];
                    PushObjectsWithForce(mObj);
                    GetAbsorbtion(mObj);
                }

            }
            yield return new WaitForSeconds(0.05f);
        }

        //Set data back on GPU.
        //_meshObjectBuffer.SetData(_meshObjects);
    }

    void GetAbsorbtion(MeshObject meshObject)
    {
        UnigmaHelpers.PrintOutMatrix(meshObject.absorbtionMatrix);
    }

    void PushObjectsWithForce(MeshObject meshObject)
    {
        BoxCollider boxCollide = _physicsObjects[(int)meshObject.id].GetComponent<BoxCollider>();
        Rigidbody rb = _physicsObjects[(int)meshObject.id].GetComponent<Rigidbody>();
        UnigmaPhysicsObject uobj = _physicsObjects[(int)meshObject.id].GetComponent<UnigmaPhysicsObject>();
        if (boxCollide != null && rb != null)
        {
            Debug.Log("Gizmos is being drawn");
            Vector3 sizeVecs = boxCollide.size;

            Debug.Log("Size of vecs " + sizeVecs);
            //Get the 4 corners and center

            Vector3 point0 = _physicsObjects[(int)meshObject.id].transform.TransformPoint(boxCollide.center + new Vector3(boxCollide.size.x, -boxCollide.size.y, -boxCollide.size.z) * 0.5f);
            Vector3 point1 = _physicsObjects[(int)meshObject.id].transform.TransformPoint(boxCollide.center + new Vector3(-boxCollide.size.x, boxCollide.size.y, -boxCollide.size.z) * 0.5f);
            Vector3 point2 = _physicsObjects[(int)meshObject.id].transform.TransformPoint(boxCollide.center + new Vector3(-boxCollide.size.x, -boxCollide.size.y, boxCollide.size.z) * 0.5f);
            Vector3 point3 = _physicsObjects[(int)meshObject.id].transform.TransformPoint(boxCollide.center + new Vector3(boxCollide.size.x, boxCollide.size.y, -boxCollide.size.z) * 0.5f);
            Vector3 point4 = _physicsObjects[(int)meshObject.id].transform.TransformPoint(boxCollide.center + new Vector3(boxCollide.size.x, -boxCollide.size.y, boxCollide.size.z) * 0.5f);
            Vector3 point5 = _physicsObjects[(int)meshObject.id].transform.TransformPoint(boxCollide.center + new Vector3(-boxCollide.size.x, boxCollide.size.y, boxCollide.size.z) * 0.5f);

            Vector3 minPoint = _physicsObjects[(int)meshObject.id].transform.TransformPoint(boxCollide.center + new Vector3(-boxCollide.size.x, -boxCollide.size.y, -boxCollide.size.z) * 0.5f);
            Vector3 maxPoint = _physicsObjects[(int)meshObject.id].transform.TransformPoint(boxCollide.center + new Vector3(boxCollide.size.x, boxCollide.size.y, boxCollide.size.z) * 0.5f);

            float forceConstant = 24.5f;

            rb.AddForceAtPosition(forceConstant * meshObject.collisionForcesHigh.GetRow(0) * Time.deltaTime, point0);
            rb.AddForceAtPosition(forceConstant * meshObject.collisionForcesHigh.GetRow(1) * Time.deltaTime, point1);
            rb.AddForceAtPosition(forceConstant * meshObject.collisionForcesHigh.GetRow(2) * Time.deltaTime, point2);
            rb.AddForceAtPosition(forceConstant * meshObject.collisionForcesHigh.GetRow(3) * Time.deltaTime, point3);

            rb.AddForceAtPosition(forceConstant * meshObject.collisionForcesLow.GetRow(0) * Time.deltaTime, point4);
            rb.AddForceAtPosition(forceConstant * meshObject.collisionForcesLow.GetRow(1) * Time.deltaTime, point5);
            rb.AddForceAtPosition(forceConstant * meshObject.collisionForcesLow.GetRow(2) * Time.deltaTime, minPoint);
            rb.AddForceAtPosition(forceConstant * meshObject.collisionForcesLow.GetRow(3) * Time.deltaTime, maxPoint);

            //Debug.Log("Right top corner: " + rightTopCorner);

            if (uobj != null)
            {
                uobj.netForce += (meshObject.collisionForcesHigh.GetRow(0) +
                                 meshObject.collisionForcesHigh.GetRow(1) +
                                 meshObject.collisionForcesHigh.GetRow(2) +
                                 meshObject.collisionForcesHigh.GetRow(3) +
                                 meshObject.collisionForcesLow.GetRow(0) +
                                 meshObject.collisionForcesLow.GetRow(1) +
                                 meshObject.collisionForcesLow.GetRow(2) +
                                 meshObject.collisionForcesLow.GetRow(3)) * Time.deltaTime * forceConstant;
            }

        }
    }

    void SetPhysicsObjects()
    {
        if(PhysicsObjectsArray.Count > 0)
            _physicsObjectsBuffer.SetData(PhysicsObjectsArray);
    }


    public void AddPhysicsObject(PhysicsObject pobj)
    {
        PhysicsObjectsArray.Add(pobj);
    }

    public void UodatePhysicsArray(int objectId, PhysicsObject pobj)
    {
        if (PhysicsObjectsArray.Count > objectId && objectId >= 0)
        {
            PhysicsObjectsArray[objectId] = pobj;
        }
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

    void ReleaseBuffers()
    {
        if (_verticesObjectBuffer != null)
            _verticesObjectBuffer.Release();
        if (_indicesObjectBuffer != null)
            _indicesObjectBuffer.Release();
        if (_meshObjectBuffer != null)
            _meshObjectBuffer.Release();
    }

}