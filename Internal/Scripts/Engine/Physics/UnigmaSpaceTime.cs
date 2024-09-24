using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using static FluidSimulationManager;
using static UnityStandardAssets.ImageEffects.BloomOptimized;

namespace UnigmaEngine
{

    public class UnigmaSpaceTime : MonoBehaviour
    {
        //Needs to be set.
        public Vector3 SpaceTimeSize;
        public int SpaceTimeResolution;

        public struct SpaceTimePoint
        {
            public Vector3 index;
            public Vector3 position;
            public Vector3 force;
            public float kelvin;
            public float tempVal;
            public float conductivity;
            public uint particlesCount;
        }

        /*
        public struct UnigmaPhysicsPoints
        {
            public Vector3 position;
            public float strength;
            public float kelvin;
            public float radius;
        };
        */

        ComputeShader _spaceTimeCompute;

        public ComputeBuffer _spaceTimePointsBuffer;
        ComputeBuffer _vectorIDsBuffer;
        ComputeBuffer _vectorIndicesBuffer;
        ComputeBuffer _vectorCellIndicesBuffer;
        ComputeBuffer _vectorCellOffsets;
        public ComputeBuffer _unigmaPhysicsPointsBuffer;

        private int[] _VectorIDs;
        private int[] _VectorIndices;
        private int[] _VectorCellIndices;
        private int[] _VectorCellOffsets;
        //private UnigmaPhysicsPoints[] _UnigmaPhysicsPoints;


        int _spaceTimePointStride = (sizeof(float) * 3) * 3 + sizeof(float) * 3 + sizeof(int);
        int _unigmaPhysicsPointsStride = (sizeof(float) * 3) + sizeof(float) * 3;
        public int _NumOfVectors;

        public SpaceTimePoint[] VectorField;
        public NativeArray<SpaceTimePoint> VectorFieldNative;

        int _resetVectorFieldKernel;
        int _gatherSpaceTimeKernel;
        int _hashVectorsKernel;
        int _sortVectorsKernelId;
        int _CalculateCellOffsetsKernelId;
        int _attractionFieldKernelId;

        uint threadsX, threadsY, threadsZ;

        Vector3 _resetVectorFieldThreadSize;
        Vector3 _gatherSpaceTimeThreadSize;
        Vector3 _hashVectorsThreadIds;
        Vector3 _sortVectorsThreadSize;
        Vector3 _calculateCellOffsetsThreadSize;
        Vector3 _attractionFieldThreadSize;

        private int _MaxNumOfPoints = 1024;
        public int _NumOfPoints;
        public float initialFahrenheit = 78.0f;
        public float GlobalTemperature = 78.0f;

        public static UnigmaSpaceTime Instance { get; private set; }

        public float temperatureSample;

        public float FahrenheitToKelvin(float faren)
        {
            float K = (faren - 32.0f) * (5.0f / 9.0f) + 273.15f;

            return K;
        }

        public float KelvinToFahrenheit(float kelvin)
        {
            float F = (kelvin - 273.15f) * (9.0f / 5.0f) + 32.0f;

            return F;
        }

        private void Awake()
        {

            if (Instance != null && Instance != this)
            {
                Destroy(this);
            }
            else
            {
                Instance = this;
            }

        }

        public void Initialize(Vector3 boxSize, int resolution, float temperature)
        {
            SpaceTimeSize = boxSize;
            SpaceTimeResolution = resolution;
            GlobalTemperature = temperature;
            int numOfVectors = Mathf.CeilToInt(SpaceTimeResolution) * Mathf.CeilToInt(SpaceTimeResolution) * Mathf.CeilToInt(SpaceTimeResolution);
            VectorField = new SpaceTimePoint[numOfVectors];
            VectorFieldNative = new NativeArray<SpaceTimePoint>(numOfVectors, Allocator.Persistent);


            for (int i = 0; i < VectorField.Length; i++)
            {
                VectorField[i].force = Vector3.zero;
                VectorField[i].position = Vector3.zero;
                VectorField[i].kelvin = FahrenheitToKelvin(initialFahrenheit);
                VectorField[i].conductivity = 0.995f;

            }

            VectorFieldNative.CopyFrom(VectorField);
            ShapeSpaceTime();
            CreateComputeBuffers();
        }

        private void Start()
        {
            StartSpaceTime();
        }

        public void StartSpaceTime()
        {
            CreatePhysicsBuffers();
            StartCoroutine(GetVectorFields());
        }

        private IEnumerator GetVectorFields()
        {

            while (true)
            {
                AsyncGPUReadbackRequest request = AsyncGPUReadback.Request(_spaceTimePointsBuffer);
                while (!request.done)
                {
                    yield return null;
                }

                if (request.done)
                {
                    VectorFieldNative.CopyFrom(request.GetData<SpaceTimePoint>());
                }
                yield return new WaitForSeconds(0.05f);
            }
        }

        public float GetTemperatureSample()
        {
            float temperatureAvg = 0;
            float ne = 0;
            for (int i = 0; i < _NumOfVectors; i += SpaceTimeResolution)
            {
                temperatureAvg += VectorFieldNative[i].kelvin;

                ne += 1.0f;
            }

            Debug.Log("Physics objects are: " + UnigmaPhysicsManager.Instance.PhysicsObjectsArray.Length);
            for (int i = 0; i < UnigmaPhysicsManager.Instance.PhysicsObjectsArray.Length; i++)
            {
                Debug.Log("Object physics is: " + UnigmaPhysicsManager.Instance.PhysicsObjectsArray[i].radius);
            }

            return KelvinToFahrenheit(temperatureAvg / ne);
        }

        void CreateComputeBuffers()
        {
            _spaceTimeCompute = Resources.Load<ComputeShader>("SpaceTimeCompute");

            _resetVectorFieldKernel = _spaceTimeCompute.FindKernel("ResetVectorField");
            _gatherSpaceTimeKernel = _spaceTimeCompute.FindKernel("GatherSpaceTime");
            _hashVectorsKernel = _spaceTimeCompute.FindKernel("HashVectors");
            _sortVectorsKernelId = _spaceTimeCompute.FindKernel("BitonicSort");
            _CalculateCellOffsetsKernelId = _spaceTimeCompute.FindKernel("CalculateCellOffsets");
            _attractionFieldKernelId = _spaceTimeCompute.FindKernel("AttractionField");

            _VectorIndices = new int[_NumOfVectors];
            _VectorCellIndices = new int[_NumOfVectors];
            _VectorCellOffsets = new int[_NumOfVectors];
            //_UnigmaPhysicsPoints = new UnigmaPhysicsPoints[_MaxNumOfPoints];

            _vectorIndicesBuffer = new ComputeBuffer(_NumOfVectors, sizeof(int));
            _vectorCellIndicesBuffer = new ComputeBuffer(_NumOfVectors, sizeof(int));
            _vectorCellOffsets = new ComputeBuffer(_NumOfVectors, sizeof(int));
            _unigmaPhysicsPointsBuffer = new ComputeBuffer(_MaxNumOfPoints, _unigmaPhysicsPointsStride);

            _spaceTimePointsBuffer = new ComputeBuffer(VectorField.Length, _spaceTimePointStride);
            _spaceTimePointsBuffer.SetData(VectorField);


            _vectorIndicesBuffer.SetData(_VectorIndices);
            _vectorCellIndicesBuffer.SetData(_VectorCellIndices);
            _vectorCellOffsets.SetData(_VectorCellOffsets);
            SetPhysicsPoints();
            _spaceTimeCompute.SetInt("_NumOfVectors", _NumOfVectors);
            _spaceTimeCompute.SetInt("_Resolution", SpaceTimeResolution);
            _spaceTimeCompute.SetVector("_BoxSize", SpaceTimeSize);

            SetBuffers(_resetVectorFieldKernel);
            SetBuffers(_gatherSpaceTimeKernel);
            SetBuffers(_hashVectorsKernel);
            SetBuffers(_sortVectorsKernelId);
            SetBuffers(_CalculateCellOffsetsKernelId);
            SetBuffers(_attractionFieldKernelId);

            _spaceTimeCompute.GetKernelThreadGroupSizes(_resetVectorFieldKernel, out threadsX, out threadsY, out threadsZ);
            _resetVectorFieldThreadSize = new Vector3(threadsX, threadsY, threadsZ);

            _spaceTimeCompute.GetKernelThreadGroupSizes(_gatherSpaceTimeKernel, out threadsX, out threadsY, out threadsZ);
            _gatherSpaceTimeThreadSize = new Vector3(threadsX, threadsY, threadsZ);

            _spaceTimeCompute.GetKernelThreadGroupSizes(_hashVectorsKernel, out threadsX, out threadsY, out threadsZ);
            _hashVectorsThreadIds = new Vector3(threadsX, threadsY, threadsZ);

            _spaceTimeCompute.GetKernelThreadGroupSizes(_sortVectorsKernelId, out threadsX, out threadsY, out threadsZ);
            _sortVectorsThreadSize = new Vector3(threadsX, threadsY, threadsZ);

            _spaceTimeCompute.GetKernelThreadGroupSizes(_CalculateCellOffsetsKernelId, out threadsX, out threadsY, out threadsZ);
            _calculateCellOffsetsThreadSize = new Vector3(threadsX, threadsY, threadsZ);

            _spaceTimeCompute.GetKernelThreadGroupSizes(_attractionFieldKernelId, out threadsX, out threadsY, out threadsZ);
            _attractionFieldThreadSize = new Vector3(threadsX, threadsY, threadsZ);
        }

        void CreatePhysicsBuffers()
        {
            SetPhysicsBuffers(_resetVectorFieldKernel);
            SetPhysicsBuffers(_gatherSpaceTimeKernel);
            SetPhysicsBuffers(_hashVectorsKernel);
            SetPhysicsBuffers(_sortVectorsKernelId);
            SetPhysicsBuffers(_CalculateCellOffsetsKernelId);
            SetPhysicsBuffers(_attractionFieldKernelId);
        }

        void SetPhysicsBuffers(int kernelId)
        {
            _spaceTimeCompute.SetBuffer(kernelId, "_PhysicsObjects", UnigmaPhysicsManager.Instance._physicsObjectsBuffer);
        }

        void SetBuffers(int kernelId)
        {
            _spaceTimeCompute.SetBuffer(kernelId, "_VectorField", _spaceTimePointsBuffer);
            _spaceTimeCompute.SetBuffer(kernelId, "_VectorIndices", _vectorIndicesBuffer);
            _spaceTimeCompute.SetBuffer(kernelId, "_VectorCellIndices", _vectorIndicesBuffer);
            _spaceTimeCompute.SetBuffer(kernelId, "_VectorCellOffsets", _vectorCellIndicesBuffer);

        }

        void SetPhysicsPoints()
        {
            _spaceTimeCompute.SetInt("_NumOfPhysicsObjects", UnigmaPhysicsManager.Instance.PhysicsObjectsArray.Length);
            /*
            _NumOfPoints = 4;
            _spaceTimeCompute.SetInt("_NumOfPhysicsPoints", _NumOfPoints);
            _UnigmaPhysicsPoints[1].strength = debugObject.gravityStrength;
            _UnigmaPhysicsPoints[1].radius = debugObject.gravityRadius;
            _UnigmaPhysicsPoints[1].position = debugObject.transform.position;
            _UnigmaPhysicsPoints[1].kelvin = debugObject.kelvin;
            _unigmaPhysicsPointsBuffer.SetData(_UnigmaPhysicsPoints);
            */
        }

        void SortVectors()
        {
            for (int biDim = 2; biDim <= _NumOfVectors; biDim <<= 1)
            {
                _spaceTimeCompute.SetInt("biDim", biDim);
                for (int biBlock = biDim >> 1; biBlock > 0; biBlock >>= 1)
                {
                    _spaceTimeCompute.SetInt("biBlock", biBlock);
                    _spaceTimeCompute.Dispatch(_sortVectorsKernelId, Mathf.CeilToInt(_NumOfVectors / _sortVectorsThreadSize.x), 1, 1);
                }
            }
        }

        private void FixedUpdate()
        {
            SetPhysicsPoints();
            _spaceTimeCompute.SetFloat("_GlobalTemperature", FahrenheitToKelvin(GlobalTemperature));
            _spaceTimeCompute.Dispatch(_hashVectorsKernel, Mathf.CeilToInt(VectorField.Length / _hashVectorsThreadIds.x), (int)_hashVectorsThreadIds.y, (int)_hashVectorsThreadIds.z);

            SortVectors();
            _spaceTimeCompute.Dispatch(_CalculateCellOffsetsKernelId, Mathf.CeilToInt(VectorField.Length / _calculateCellOffsetsThreadSize.x), 1, 1);

            _spaceTimeCompute.Dispatch(_resetVectorFieldKernel, Mathf.CeilToInt(VectorField.Length / _resetVectorFieldThreadSize.x), (int)_resetVectorFieldThreadSize.y, (int)_resetVectorFieldThreadSize.z);

            _spaceTimeCompute.Dispatch(_attractionFieldKernelId, Mathf.CeilToInt(VectorField.Length / _attractionFieldThreadSize.x), (int)_attractionFieldThreadSize.y, (int)_attractionFieldThreadSize.z);

            _spaceTimeCompute.Dispatch(_gatherSpaceTimeKernel, Mathf.CeilToInt(VectorField.Length / _gatherSpaceTimeThreadSize.x), (int)_gatherSpaceTimeThreadSize.y, (int)_gatherSpaceTimeThreadSize.z);
            /*
            for (int i = 0; i < VectorField.Length; i++)
            {
                VectorField[i].previousDirection = VectorField[i].direction;
                VectorField[i].direction = Vector3.zero;

            }
            */
            temperatureSample = GetTemperatureSample();
        }

        void ShapeSpaceTime()
        {
            int xSize = Mathf.CeilToInt(SpaceTimeResolution);
            int ySize = Mathf.CeilToInt(SpaceTimeResolution);
            int zSize = Mathf.CeilToInt(SpaceTimeResolution);

            Vector3 spacing = (SpaceTimeSize / (SpaceTimeResolution - 1));
            Vector3 halfContainerSize = SpaceTimeSize / 2.0f;
            for (int i = 0; i < xSize; i++)
            {
                for (int j = 0; j < ySize; j++)
                {
                    for (int k = 0; k < zSize; k++)
                    {
                        int index = i * ySize * zSize + j * zSize + k;

                        VectorField[index].position = new Vector3(i * spacing.x - halfContainerSize.x, j * spacing.y - halfContainerSize.y, k * spacing.z - halfContainerSize.z);
                        VectorField[index].force = Vector3.zero;
                        VectorField[index].index = new Vector3(i, j, k);
                    }
                }

                _NumOfVectors = VectorField.Length;
            }
        }

        Vector4 KelvinToRGB(float kelvin)
        {
            float temp = kelvin / 1000;
            return new Vector4(temp * 0.1f, Mathf.Min(temp * 1.8f, 0.7f), Mathf.Min(temp * 5.0f, 0.7f), 1.0f);
        }

        private void OnDrawGizmos()
        {

            //Set int for simulation
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(Vector3.zero, SpaceTimeSize);


            if (VectorFieldNative != null)
            {
                float spacing = (SpaceTimeSize.x / (SpaceTimeResolution - 1)) * 0.5f;
                for (int i = 0; i < VectorFieldNative.Length; i++)
                {
                    SpaceTimePoint vp = VectorFieldNative[i];
                    Ray ray = new Ray(vp.position, vp.force * spacing);
                    Vector3 normalizedDir = Vector3.Normalize(vp.force) * 0.5f + Vector3.one * 0.5f;
                    Gizmos.color = new Vector4(normalizedDir.x * vp.force.magnitude * 10.0f, normalizedDir.y, normalizedDir.z, 1.0f);

                    Gizmos.DrawRay(ray);

                    Gizmos.color = KelvinToRGB(vp.kelvin);

                    //Debug.Log("Cell " + vp.index + " position: " + vp.position + " Kelvin: " + vp.kelvin);

                    //Gizmos.DrawCube(vp.position, SpaceTimeSize / (SpaceTimeResolution));
                    Gizmos.DrawSphere(vp.position, 0.25f);
                    Handles.Label( vp.position + Vector3.up*0.5f, "Particle: " + i + " | " + "Kelvin: " + vp.kelvin + " | Particles Count: " + vp.particlesCount);
                }
            }

        }

        void ReleaseBuffers()
        {
            if (_vectorIDsBuffer != null)
                _vectorIDsBuffer.Release();
            if (_vectorIndicesBuffer != null)
                _vectorIndicesBuffer.Release();
            if (_vectorCellIndicesBuffer != null)
                _vectorCellIndicesBuffer.Release();
            if (_vectorCellOffsets != null)
                _vectorCellOffsets.Release();
            if (_spaceTimePointsBuffer != null)
                _spaceTimePointsBuffer.Release();
            if (_unigmaPhysicsPointsBuffer != null)
                _unigmaPhysicsPointsBuffer.Release();

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
    }
}