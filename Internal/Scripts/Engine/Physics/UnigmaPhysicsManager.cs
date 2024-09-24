using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

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

    };

    public class UnigmaPhysicsManager : MonoBehaviour
    {

        public static readonly int _physicsObjectsStride = sizeof(int) * 1 + sizeof(float) * 3 * 3 + sizeof(float) * 3;

        public FluidSimulationManager unigmaFluids;
        public UnigmaSpaceTime unigmaSpaceTime;

        public List<UnigmaPhysicsObject> _physicsObjects = new List<UnigmaPhysicsObject>();


        public NativeArray<PhysicsObject> PhysicsObjectsArray;

        [HideInInspector]
        public ComputeBuffer _physicsObjectsBuffer { get; private set; }


        public static UnigmaPhysicsManager Instance;

        //Native Functions
        unsafe delegate Vector3 GetPhysicsPosition(void* pObj, int size);
        GetPhysicsPosition GetPhysicsPos;
        IntPtr symbol;

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
            GetFunctionsAddresses();
        }

        unsafe void GetFunctionsAddresses()
        {
            GetPhysicsPos = UnigmaNativeManager.GetNativeFunction<GetPhysicsPosition>(ref symbol, "GetPhysicsPosition");
        }

        unsafe void DebugStructs()
        {

            void* ptr = NativeArrayUnsafeUtility.GetUnsafePtr(PhysicsObjectsArray);

            Debug.Log("PHYSICS NATIVE Vector is: " + GetPhysicsPos(ptr, PhysicsObjectsArray.Length) + " | Unity Objects: " + PhysicsObjectsArray[5].position);
        }

        private void Update()
        {
            DebugStructs();
            SetPhysicsObjects();
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
            /*
            if (_verticesObjectBuffer != null)
                _verticesObjectBuffer.Release();
            if (_indicesObjectBuffer != null)
                _indicesObjectBuffer.Release();
            if (_meshObjectBuffer != null)
                _meshObjectBuffer.Release();
            */
        }

    }
}