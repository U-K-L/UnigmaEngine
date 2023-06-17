using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnigmaFastMath : MonoBehaviour
{
    public ComputeShader UnigmaCompute;
    public static UnigmaFastMath _instance;

    float _dotProductResult = -1.1234567f;
    ComputeBuffer ResultBuffer;
    ComputeBuffer ABuffer;
    ComputeBuffer BBuffer;

    int MaxSize = 100;
    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            Destroy(this);
        }
    }
    public void InitializeCompute()
    {
        UnigmaCompute = Resources.Load<ComputeShader>("UnigmaMathCompute");
        ABuffer = new ComputeBuffer(MaxSize, sizeof(float), ComputeBufferType.Structured);
        BBuffer = new ComputeBuffer(MaxSize, sizeof(float), ComputeBufferType.Structured);
        ResultBuffer = new ComputeBuffer(MaxSize, sizeof(float), ComputeBufferType.Structured, ComputeBufferMode.Immutable);
    }

    void SetBuffers(float[] A, float[] B, int kernel)
    {
        ABuffer.SetData(A);
        BBuffer.SetData(B);
        UnigmaCompute.SetBuffer(kernel, "_inputDataA", ABuffer);
        UnigmaCompute.SetBuffer(kernel, "_inputDataB", BBuffer);
        UnigmaCompute.SetBuffer(kernel, "_outputData", ResultBuffer);
    }

    public float[] Add(float[] A, float[] B)
    {
        if (UnigmaCompute == null)
            InitializeCompute();
        int kernel = UnigmaCompute.FindKernel("Add");
        SetBuffers(A, B, kernel);
        int tx = A.Length;
        int ty = B.Length;
        int tz = 1;
        int Col = A.Length;
        UnigmaCompute.SetInt("_Cols", Col);
        UnigmaCompute.Dispatch(kernel, tx, ty, tz);
        float[] resultValues = new float[A.Length];
        ResultBuffer.GetData(resultValues);
        return resultValues;
    }

    public float[] Mul(float[] A, float[] B, int transpose, int col, int row)
    {
        if (UnigmaCompute == null)
            InitializeCompute();
        int kernel = UnigmaCompute.FindKernel("Mul");
        SetBuffers(A, B, kernel);
        int tx = col;
        int ty = row;
        int tz = 1;
        UnigmaCompute.SetInt("_Cols", col);
        UnigmaCompute.SetInt("_Transpose", transpose);
        UnigmaCompute.Dispatch(kernel, tx, ty, tz);
        float[] resultValues = new float[col*row];
        ResultBuffer.GetData(resultValues);
        return resultValues;
    }

    public float Dot(float[] A, float[] B)
    {
        if (UnigmaCompute == null)
            InitializeCompute();
        float result = -1;
        int kernel = UnigmaCompute.FindKernel("Dot");
        SetBuffers(A, B, kernel);
        SetBuffers(A, B, UnigmaCompute.FindKernel("Sum"));
        StartCoroutine(DispatchShader(A, B, kernel));
        return result;
    }

    IEnumerator DispatchShader(float[] A, float[] B, int kernel)
    {

        int Col = A.Length;
        int Row = B.Length;
        int tx = A.Length;
        int ty = B.Length;
        int tz = 1;
        int steps = 0;
        int batchSize = 1000;
        int bufferSize = A.Length;
        float[] resultValues = new float[A.Length];
        if (Col > 65535)
        {
            tx = ty = tz = Mathf.CeilToInt(Mathf.Pow(Col, 1f / 3f));
            UnigmaCompute.SetInt("_Cols", ty);
        }
        //For testing purposes only.
        UnigmaCompute.Dispatch(kernel, tx, ty, tz);
        while (bufferSize > 1)
        {
            SumDotProduct(steps, batchSize, bufferSize, ref resultValues, kernel);
            steps++;
            Debug.Log(bufferSize);
            bufferSize = Mathf.CeilToInt(bufferSize / batchSize);
            yield return new WaitForSeconds(0.16f);
        }
        ResultBuffer.GetData(resultValues);
        _dotProductResult = resultValues[0];
    }


    //Buffer size is the imaginary buffer remainding. For performance, the actual buffer length is never resized.
    void SumDotProduct(int Batch, int BatchSize, int BufferSize, ref float[] resultValues, int kernel)
    {
        if (BufferSize < 1)
        {
            return;
        }
        UnigmaCompute.SetInt("_BufferSize", BufferSize);
        UnigmaCompute.SetInt("_BatchSize", BatchSize);
        UnigmaCompute.SetInt("_Batch", Batch);

        int tx = Mathf.CeilToInt(Mathf.Pow(BatchSize, 1f / 3f));
        int ty = Mathf.CeilToInt(Mathf.Pow(BatchSize, 1f / 3f));
        int tz = Mathf.CeilToInt(Mathf.Pow(BatchSize, 1f / 3f));

        UnigmaCompute.Dispatch(UnigmaCompute.FindKernel("Sum"), tx, ty, tz);

    }
    
    public float GetDotProductResult()
    {
        return _dotProductResult;
    }

    public void ReleaseBuffers()
    {
        ResultBuffer.Release();
        ABuffer.Release();
        BBuffer.Release();
    }

    public void OnDestroy()
    {
        ReleaseBuffers();
    }

    public void OnDisable()
    {
        ReleaseBuffers();
    }

    public void OnApplicationQuit()
    {
        ReleaseBuffers();
    }
}
