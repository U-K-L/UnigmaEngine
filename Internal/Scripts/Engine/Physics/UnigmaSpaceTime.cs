using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using static FluidSimulationManager;
using static UnigmaSpaceTime;
using static UnityStandardAssets.ImageEffects.BloomOptimized;

public class UnigmaSpaceTime : MonoBehaviour
{
    public struct SpaceTimePoint
    {
        public Vector3 index;
        public Vector3 position;
        public Vector3 force;
        public float kelvin;
    }

    ComputeShader _spaceTimeCompute;

    ComputeBuffer _spaceTimePointsBuffer;
    ComputeBuffer _vectorIDsBuffer;
    ComputeBuffer _vectorIndicesBuffer;
    ComputeBuffer _vectorCellIndicesBuffer;
    ComputeBuffer _vectorCellOffsets;

    private int[] _VectorIDs;
    private int[] _VectorIndices;
    private int[] _VectorCellIndices;
    private int[] _VectorCellOffsets;


    int _spaceTimePointStride = (sizeof(float) * 3) * 3 + sizeof(float);
    int _NumOfVectors;

    public Vector3 SpaceTimeSize;

    public int SpaceTimeResolution;

    public SpaceTimePoint[] VectorField;

    int _resetVectorFieldKernel;
    int _hashVectorsKernel;
    int _sortVectorsKernelId;
    int _CalculateCellOffsetsKernelId;
    int _attractionFieldKernelId;

    uint threadsX, threadsY, threadsZ;

    Vector3 _resetVectorFieldThreadIds;
    Vector3 _hashVectorsThreadIds;
    Vector3 _sortVectorsThreadSize;
    Vector3 _calculateCellOffsetsThreadSize;
    Vector3 _attractionFieldThreadSize;

    private void Awake()
    {
        int numOfVectors = Mathf.CeilToInt(SpaceTimeResolution) * Mathf.CeilToInt(SpaceTimeResolution) * Mathf.CeilToInt(SpaceTimeResolution);
        VectorField = new SpaceTimePoint[numOfVectors];

        for (int i = 0; i < VectorField.Length; i++)
        {
            VectorField[i].force = Vector3.zero;
            VectorField[i].position = Vector3.zero;
        }

        ShapeSpaceTime();
        CreateComputeBuffers();
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
                VectorField = request.GetData<SpaceTimePoint>().ToArray();
            }
            yield return new WaitForSeconds(0.05f);
        }
    }

    void CreateComputeBuffers()
    {
        _spaceTimeCompute = Resources.Load<ComputeShader>("SpaceTimeCompute");

        _resetVectorFieldKernel = _spaceTimeCompute.FindKernel("ResetVectorField");
        _hashVectorsKernel = _spaceTimeCompute.FindKernel("HashVectors");
        _sortVectorsKernelId = _spaceTimeCompute.FindKernel("BitonicSort");
        _CalculateCellOffsetsKernelId = _spaceTimeCompute.FindKernel("CalculateCellOffsets");

        _VectorIndices = new int[_NumOfVectors];
        _VectorCellIndices = new int[_NumOfVectors];
        _VectorCellOffsets = new int[_NumOfVectors];

        _vectorIndicesBuffer = new ComputeBuffer(_NumOfVectors, sizeof(int));
        _vectorCellIndicesBuffer = new ComputeBuffer(_NumOfVectors, sizeof(int));
        _vectorCellOffsets = new ComputeBuffer(_NumOfVectors, sizeof(int));

        _spaceTimePointsBuffer = new ComputeBuffer(VectorField.Length, _spaceTimePointStride);
        _spaceTimePointsBuffer.SetData(VectorField);


        _vectorIndicesBuffer.SetData(_VectorIndices);
        _vectorCellIndicesBuffer.SetData(_VectorCellIndices);
        _vectorCellOffsets.SetData(_VectorCellOffsets);

        _spaceTimeCompute.SetInt("_NumOfVectors", _NumOfVectors);
        _spaceTimeCompute.SetInt("_Resolution", SpaceTimeResolution);

        SetBuffers(_resetVectorFieldKernel);
        SetBuffers(_hashVectorsKernel);
        SetBuffers(_sortVectorsKernelId);
        SetBuffers(_CalculateCellOffsetsKernelId);
        SetBuffers(_attractionFieldKernelId);

        _spaceTimeCompute.GetKernelThreadGroupSizes(_resetVectorFieldKernel, out threadsX, out threadsY, out threadsZ);
        _resetVectorFieldThreadIds = new Vector3(threadsX, threadsY, threadsZ);

        _spaceTimeCompute.GetKernelThreadGroupSizes(_hashVectorsKernel, out threadsX, out threadsY, out threadsZ);
        _hashVectorsThreadIds = new Vector3(threadsX, threadsY, threadsZ);

        _spaceTimeCompute.GetKernelThreadGroupSizes(_sortVectorsKernelId, out threadsX, out threadsY, out threadsZ);
        _sortVectorsThreadSize = new Vector3(threadsX, threadsY, threadsZ);

        _spaceTimeCompute.GetKernelThreadGroupSizes(_CalculateCellOffsetsKernelId, out threadsX, out threadsY, out threadsZ);
        _calculateCellOffsetsThreadSize = new Vector3(threadsX, threadsY, threadsZ);

        _spaceTimeCompute.GetKernelThreadGroupSizes(_attractionFieldKernelId, out threadsX, out threadsY, out threadsZ);
        _attractionFieldThreadSize = new Vector3(threadsX, threadsY, threadsZ);
    }

    void SetBuffers(int kernelId)
    {
        _spaceTimeCompute.SetBuffer(kernelId, "_VectorField", _spaceTimePointsBuffer);
        _spaceTimeCompute.SetBuffer(kernelId, "_VectorIndices", _vectorIndicesBuffer);
        _spaceTimeCompute.SetBuffer(kernelId, "_VectorCellIndices", _vectorIndicesBuffer);
        _spaceTimeCompute.SetBuffer(kernelId, "_VectorCellOffsets", _vectorCellIndicesBuffer);
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
        _spaceTimeCompute.Dispatch(_hashVectorsKernel, Mathf.CeilToInt(VectorField.Length / _hashVectorsThreadIds.x), (int)_hashVectorsThreadIds.y, (int)_hashVectorsThreadIds.z);

        SortVectors();
        _spaceTimeCompute.Dispatch(_CalculateCellOffsetsKernelId, Mathf.CeilToInt(VectorField.Length / _calculateCellOffsetsThreadSize.x), 1, 1);

        _spaceTimeCompute.Dispatch(_resetVectorFieldKernel, Mathf.CeilToInt(VectorField.Length / _resetVectorFieldThreadIds.x), (int)_resetVectorFieldThreadIds.y, (int)_resetVectorFieldThreadIds.z);
        
        /*
        for (int i = 0; i < VectorField.Length; i++)
        {
            VectorField[i].previousDirection = VectorField[i].direction;
            VectorField[i].direction = Vector3.zero;
            
        }
        */
    }

    void ShapeSpaceTime()
    {
        int xSize = Mathf.CeilToInt(SpaceTimeResolution);
        int ySize = Mathf.CeilToInt(SpaceTimeResolution);
        int zSize = Mathf.CeilToInt(SpaceTimeResolution);

        Vector3 spacing = (SpaceTimeSize / (SpaceTimeResolution-1));
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

    private void OnDrawGizmos()
    {
        //Set int for simulation
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(Vector3.zero, SpaceTimeSize);

        
        if (VectorField != null)
        {
            float spacing = (SpaceTimeSize.x / (SpaceTimeResolution - 1)) *0.5f;
            foreach (SpaceTimePoint vp in VectorField)
            {
                Ray ray = new Ray(vp.position, vp.force * spacing);
                Vector3 normalizedDir = Vector3.Normalize(vp.force) *0.5f + Vector3.one*0.5f;
                Gizmos.color = new Vector4(normalizedDir.x * vp.force.magnitude*10.0f, normalizedDir.y, normalizedDir.z, 1.0f);
                Gizmos.DrawRay(ray);
                //Gizmos.DrawSphere(vp.position, 0.025f);
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
