using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using static UnigmaSpaceTime;

public class UnigmaSpaceTime : MonoBehaviour
{
    public struct SpaceTimePoint
    {
        public Vector3 position;
        public Vector3 direction;
        public Vector3 previousDirection;
        public Vector3 force;
        public float kelvin;
    }

    ComputeShader _spaceTimeCompute;
    ComputeBuffer _spaceTimePointsBuffer;
    int _spaceTimePointStride = (sizeof(float) * 3) * 4 + sizeof(float);

    public Vector3 SpaceTimeSize;

    public int SpaceTimeResolution;

    public SpaceTimePoint[] VectorField;

    uint threadsX, threadsY, threadsZ;

    Vector3 _resetVectorFieldThreadIds;
    private void Awake()
    {
        int numOfVectors = Mathf.CeilToInt(SpaceTimeResolution) * Mathf.CeilToInt(SpaceTimeResolution) * Mathf.CeilToInt(SpaceTimeResolution);
        VectorField = new SpaceTimePoint[numOfVectors];

        for (int i = 0; i < VectorField.Length; i++)
        {
            VectorField[i].direction = Vector3.zero;
            VectorField[i].position = Vector3.zero;
            VectorField[i].previousDirection = Vector3.zero;
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
        _spaceTimePointsBuffer = new ComputeBuffer(VectorField.Length, _spaceTimePointStride);
        _spaceTimePointsBuffer.SetData(VectorField);

        _spaceTimeCompute.SetBuffer(0, "_VectorField", _spaceTimePointsBuffer);
        _spaceTimeCompute.GetKernelThreadGroupSizes(0, out threadsX, out threadsY, out threadsZ);
        _resetVectorFieldThreadIds = new Vector3(threadsX, threadsY, threadsZ);
    }

    private void FixedUpdate()
    {
        _spaceTimeCompute.Dispatch(0, Mathf.CeilToInt(VectorField.Length / _resetVectorFieldThreadIds.x), (int)_resetVectorFieldThreadIds.y, (int)_resetVectorFieldThreadIds.z);
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

        float spacing = (SpaceTimeSize.x / (SpaceTimeResolution-1));
        float halfContainerSize = SpaceTimeSize.x / 2.0f;
        for (int i = 0; i < xSize; i++)
        {
            for (int j = 0; j < ySize; j++)
            {
                for (int k = 0; k < zSize; k++)
                {
                    int index = i * ySize * zSize + j * zSize + k;

                    VectorField[index].position = new Vector3(i * spacing - halfContainerSize, j * spacing - halfContainerSize, k * spacing - halfContainerSize);
                    VectorField[index].direction = Vector3.zero;
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        /*
        //Set int for simulation
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(Vector3.zero, SpaceTimeSize);

        
        if (VectorField != null)
        {
            float spacing = (SpaceTimeSize.x / (SpaceTimeResolution - 1)) *0.5f;
            foreach (SpaceTimePoint vp in VectorField)
            {
                Ray ray = new Ray(vp.position, vp.previousDirection * spacing);
                Vector3 normalizedDir = Vector3.Normalize(vp.previousDirection)*0.5f + Vector3.one*0.5f;
                Gizmos.color = new Vector4(normalizedDir.x * vp.previousDirection.magnitude*10.0f, normalizedDir.y, normalizedDir.z, 1.0f);
                Gizmos.DrawRay(ray);
                //Gizmos.DrawSphere(vp.position, 0.025f);
            }
        }
        */
    }
}
