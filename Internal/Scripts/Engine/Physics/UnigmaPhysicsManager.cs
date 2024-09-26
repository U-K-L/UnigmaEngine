using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using static UnityEditor.Searcher.SearcherWindow.Alignment;

namespace UnigmaEngine
{
    public struct PhysicsObject
    {
        public uint objectId;
        public float3 position;
        public float3 velocity;
        public float3 acceleration;
        public float strength;
        public float kelvin;
        public float radius;
        public int collisionPrimitivesStart;
        public int collisionPrimitivesCount;
        public Matrix4x4 localToWorld;

    };

    public struct CollisionPrimitive
    {
        public Vector3 position;
        public Vector3 normal;
        public Vector3 force;
    };

    public class UnigmaPhysicsManager : MonoBehaviour
    {

        public static readonly int _physicsObjectsStride = sizeof(int) * 3 + sizeof(float) * 3 * 3 + sizeof(float) * 3 + sizeof(float) * 4*4;

        public FluidSimulationManager unigmaFluids;
        public UnigmaSpaceTime unigmaSpaceTime;

        public List<UnigmaPhysicsObject> _physicsObjects = new List<UnigmaPhysicsObject>();


        public NativeArray<PhysicsObject> PhysicsObjectsArray;
        public NativeArray<CollisionPrimitive> CollisionPrimitives;
        public NativeArray<int> CollisionIndices;

        [HideInInspector]
        public ComputeBuffer _physicsObjectsBuffer { get; private set; }


        public static UnigmaPhysicsManager Instance;

        //Native Functions
        unsafe delegate void SetUpPhysicsBuffers(void* physicsObjects, int physicsObjectsSize,
                                                 void* collisionPrims, int collisionPrimsSize,
                                                 void* collisionIndices, int collisionIndicesSize);
        SetUpPhysicsBuffers setUpPhysicsBuffers;
        IntPtr setupPhysicsSymbol;
        unsafe delegate Vector3 CheckObjectCollisionsTest(int objectAId);
        CheckObjectCollisionsTest checkObjectCollisionsTest;
        IntPtr collisionsTestSymbol;

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

            CreateObjectBuffers();
            CreatePhysicsComponents(uScene);

        }

        void CreateObjectBuffers()
        {
            //Add physics objects.
            UnigmaPhysicsObject[] physicsObjects = GetComponentsInChildren<UnigmaPhysicsObject>();
            for (uint i = 0; i < physicsObjects.Length; i++)
            {
                //if (!physicsObjects[i].influenceSpaceTime)
                //    continue;
                UnigmaGameObject uObj = physicsObjects[i].GetComponent<UnigmaGameObject>();

                if (uObj != null)
                {
                    physicsObjects[i].Initialize();
                    AddPhysicsObject(physicsObjects[i]);
                }
            }

            PhysicsObjectsArray = new NativeArray<PhysicsObject>(_physicsObjects.Count, Allocator.Persistent);
            Debug.Log("Physics Objects Array Size: " + PhysicsObjectsArray.Length);

            _physicsObjectsBuffer = new ComputeBuffer(Mathf.Max(PhysicsObjectsArray.Length, 1), _physicsObjectsStride);

        }

        void CreatePhysicsComponents(UnigmaScene uScene)
        {

            //Add components. Awake is called.
            unigmaSpaceTime = gameObject.AddComponent<UnigmaSpaceTime>() as UnigmaSpaceTime;
            unigmaFluids = gameObject.AddComponent<FluidSimulationManager>() as FluidSimulationManager;

            //Setup spaceTime.
            unigmaSpaceTime.Initialize(uScene.SpaceTimeBoxSize, uScene.SpaceTimeResolution, uScene.Temperature);

            //Setup fluids.
            unigmaFluids.fluidSettings = Resources.Load<FluidSettings>("DefaultFluidSettings");
            unigmaFluids.Initialize();
            unigmaFluids.enabled = true;
        }


        private void Start()
        {
            BuildCollisionTriangles();
            GetFunctionsAddresses();
            SetUpPhysicsNative();
        }

        unsafe void GetFunctionsAddresses()
        {
            setUpPhysicsBuffers = UnigmaNativeManager.GetNativeFunction<SetUpPhysicsBuffers>(ref setupPhysicsSymbol, "SetUpPhysicsBuffers");
            checkObjectCollisionsTest = UnigmaNativeManager.GetNativeFunction<CheckObjectCollisionsTest>(ref collisionsTestSymbol, "CheckObjectCollisionsTest");
        }

        unsafe void SetUpPhysicsNative()
        {
            void* physicsObjectsPrt = NativeArrayUnsafeUtility.GetUnsafePtr(PhysicsObjectsArray);
            void* collisionPrimPtr = NativeArrayUnsafeUtility.GetUnsafePtr(CollisionPrimitives);
            void* collisionIndPtr = NativeArrayUnsafeUtility.GetUnsafePtr(CollisionIndices);
            setUpPhysicsBuffers(physicsObjectsPrt, PhysicsObjectsArray.Length,
                              collisionPrimPtr, CollisionPrimitives.Length,
                              collisionIndPtr, CollisionIndices.Length);
        }

        private void Update()
        {
            SetPhysicsObjects();

            //Debug.Log("Did object 31 collide? " + checkObjectCollisionsTest(31).ToString("F7"));
        }
        /*
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
        */
        void SetPhysicsObjects()
        {
            if (PhysicsObjectsArray.Length > 0)
                _physicsObjectsBuffer.SetData(PhysicsObjectsArray);
        }


        public void AddPhysicsObject(UnigmaPhysicsObject pobj)
        {
            _physicsObjects.Add(pobj);
        }

        public void UodatePhysicsArray(uint objectId, PhysicsObject pobj)
        {
            PhysicsObjectsArray[(int)objectId] = pobj;
        }

        void BuildCollisionTriangles()
        {
            List<CollisionPrimitive> Vertices = new List<CollisionPrimitive>();
            List<int> Indices = new List<int>();
            for(int j = 0; j < _physicsObjects.Count; j++)
            {
                UnigmaPhysicsObject physUnigmaObj = _physicsObjects[j];
                MeshFilter mf = physUnigmaObj.GetComponent<MeshFilter>();
                if (mf)
                {
                    Mesh m = mf.sharedMesh;
                    int startVert = Vertices.Count;
                    int startIndex = Indices.Count;

                    for (int i = 0; i < m.vertices.Length; i++)
                    {
                        CollisionPrimitive v = new CollisionPrimitive();
                        v.position = m.vertices[i];
                        v.normal = m.normals[i];
                        v.force = Vector3.zero;
                        Vertices.Add(v);
                    }
                    var indices = m.GetIndices(0);
                    Indices.AddRange(indices.Select(index => index + startVert));

                    _physicsObjects[j].physicsObject.collisionPrimitivesStart = startIndex;
                    _physicsObjects[j].physicsObject.collisionPrimitivesCount = indices.Length;
                    _physicsObjects[j].physicsObject.localToWorld = _physicsObjects[j].transform.localToWorldMatrix;


                }
            }

            CollisionPrimitives = new NativeArray<CollisionPrimitive>(Vertices.Count, Allocator.Persistent);
            CollisionIndices = new NativeArray<int>(Indices.Count, Allocator.Persistent);

            CollisionPrimitives.CopyFrom(Vertices.ToArray());
            CollisionIndices.CopyFrom(Indices.ToArray());
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
            PhysicsObjectsArray.Dispose();
            _physicsObjectsBuffer.Release();
            CollisionPrimitives.Dispose();
            CollisionIndices.Dispose();
            /*
            if (_verticesObjectBuffer != null)
                _verticesObjectBuffer.Release();
            if (_indicesObjectBuffer != null)
                _indicesObjectBuffer.Release();
            if (_meshObjectBuffer != null)
                _meshObjectBuffer.Release();
            */
        }

        void OnDrawGizmos()
        {
            if (!Application.isPlaying || PhysicsObjectsArray.Length == 0 || CollisionPrimitives.Length == 0)
                return;

            Gizmos.color = Color.red; // Set the color for drawing

            // Loop through each physics object
            for (int i = 0; i < PhysicsObjectsArray.Length; i++)
            {
                PhysicsObject physicsObject = PhysicsObjectsArray[i];

                // Get the range of primitives for this object
                int start = physicsObject.collisionPrimitivesStart;
                int count = physicsObject.collisionPrimitivesCount;

                // Loop through the indices of the object's collision primitives
                for (int j = 0; j < count; j += 3)
                {
                    // Get the triangle indices
                    int index0 = CollisionIndices[start + j];
                    int index1 = CollisionIndices[start + j + 1];
                    int index2 = CollisionIndices[start + j + 2];

                    // Get the actual vertices from the CollisionPrimitives array
                    Vector3 vertex0 = CollisionPrimitives[index0].position;
                    Vector3 vertex1 = CollisionPrimitives[index1].position;
                    Vector3 vertex2 = CollisionPrimitives[index2].position;

                    // Transform vertices by the object's localToWorld matrix
                    vertex0 = physicsObject.localToWorld.MultiplyPoint3x4(vertex0);
                    vertex1 = physicsObject.localToWorld.MultiplyPoint3x4(vertex1);
                    vertex2 = physicsObject.localToWorld.MultiplyPoint3x4(vertex2);

                    // Draw the triangle using Gizmos
                    Gizmos.DrawLine(vertex0, vertex1);
                    Gizmos.DrawLine(vertex1, vertex2);
                    Gizmos.DrawLine(vertex2, vertex0);
                }
            }
        }


    }
}