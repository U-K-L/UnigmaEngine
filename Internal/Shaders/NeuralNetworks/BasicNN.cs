using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class BasicNN : MonoBehaviour
{
    [SerializeField] string[] files;

    List<float> _PerformanceProfiling = new List<float>();
    float _profilingTimeTaken = 0;

    //The compute shader script.
    [SerializeField] private ComputeShader basicNNCompute = default;

    //Make the buffer to send and recieve data.
    private List<ComputeBuffer> _inputBufferArrayA = default;
    private List<ComputeBuffer> _inputBufferArrayB = default;
    private ComputeBuffer _inputDataA;
    private ComputeBuffer _inputDataB;
    private ComputeBuffer outputData;

    //The stride, how large each element in the buffer is.
    private const int DATA_STRIDE = sizeof(float);
   
    //Function ID from compute shader.
    private int idBasicNNKernelMul;
    private int idBasicNNKernelSum;
    private int idBasicNNKernelDot;
    private bool initialized = false;

    float[] outputMatrix;

    int Row = 0;
    int Col = 0;

    public enum Transpose
    {
        None, A, B, Both
    }
    [SerializeField] Transpose _transpose;
    void LoadDataFromCSV(string file, ref float[][] inputVectors)
    {
        string path = Application.streamingAssetsPath + "/NeuralNetworks/Data/" + file; 
        string[] data = System.IO.File.ReadAllLines(path);
        inputVectors = new float[data.Length][];
        for (int i = 0; i < data.Length; i++)
        {
            string[] line = data[i].Split(',');
            inputVectors[i] = new float[line.Length];
            for (int j = 0; j < line.Length; j++)
            {
                inputVectors[i][j] = float.Parse(line[j]);
            }
        }
    }

    void LoadMatrixDataToArray(float[][] inputmatrix, ref float[] matrixArray)
    {
        for (int i = 0; i < inputmatrix.Length; i++)
        {
            for (int j = 0; j < inputmatrix[0].Length; j++)
            {
                matrixArray[j + i * 2] = inputmatrix[i][j];
                Debug.Log(matrixArray[j + i * 2]);
            }
        }
    }
    private void OnEnable()
    {
        if (initialized)
        {
            OnDisable();
        }
        initialized = true;

        float[][] inputVectorsA = null;
        float[][] inputVectorsB = null;

        LoadDataFromCSV(files[0], ref inputVectorsA);
        LoadDataFromCSV(files[1], ref inputVectorsB);
        MatrixMul(inputVectorsA, inputVectorsB, ref outputMatrix);
        StartCoroutine(DispatchShader());
    }

    // Update is called once per frame
    void Update()
    {
    }

    private void LateUpdate()
    {
        //float[] resultValues = new float[outputData.count];
        //outputData.GetData(resultValues);
        //Debug.Log(resultValues[0]);
        //PrintMatrix(Row, Col);
    }

    IEnumerator DispatchShader()
    {
        _profilingTimeTaken = Time.time;
        int tx = Row;
        int ty = Col;
        int tz = 1;
        int index = 0;
        int batchSize = 2;
        int bufferSize = outputData.count;
        basicNNCompute.Dispatch(idBasicNNKernelDot, ty, tx, tz);
        if (Col > 65535)
        {
            ty = Mathf.CeilToInt(Mathf.Sqrt(Row));
            tz = ty;
        }
        while (true)
        {
            SumDotProduct(index, batchSize, bufferSize);
            float[] resultValues = new float[outputData.count];
            outputData.GetData(resultValues);
            index++;
            bufferSize = Mathf.CeilToInt(bufferSize / batchSize);

            _PerformanceProfiling.Add(Time.time - _profilingTimeTaken);
            PrintMatrix(Row, Col);
            yield return new WaitForSeconds(0.0f);
        }
    }
    
    void MatrixMul(float[][] inputVectorsA, float[][] inputVectorsB, ref float[] outputMatrix)
    {
        //Get the data in the struct.
        float[] inputDataMatrixA = new float[inputVectorsA.Length * inputVectorsA[0].Length];
        float[] inputDataMatrixB = new float[inputVectorsB.Length * inputVectorsB[0].Length];
        
        //Load matrix to a 1D array.
        LoadMatrixDataToArray(inputVectorsA, ref inputDataMatrixA);
        LoadMatrixDataToArray(inputVectorsB, ref inputDataMatrixB);

        Row = inputVectorsA.Length;
        Col = inputVectorsB[0].Length;

        outputMatrix = new float[inputVectorsA.Length * inputVectorsB[0].Length];
        //Initialize the buffers.
        _inputDataA = new ComputeBuffer(inputDataMatrixA.Length, DATA_STRIDE, ComputeBufferType.Structured, ComputeBufferMode.Immutable);
        _inputDataB = new ComputeBuffer(inputDataMatrixB.Length, DATA_STRIDE, ComputeBufferType.Structured, ComputeBufferMode.Immutable);
        outputData = new ComputeBuffer(outputMatrix.Length, DATA_STRIDE, ComputeBufferType.Structured, ComputeBufferMode.Immutable);

        _inputDataA.SetData(inputDataMatrixA);
        _inputDataB.SetData(inputDataMatrixB);
        outputData.SetData(outputMatrix);

        SetBuffers(0);
    }

    //Buffer size is the imaginary buffer remainding. For performance, the actual buffer length is never resized.
    void SumDotProduct(int Batch, int BatchSize, int BufferSize)
    {
        if (BufferSize < 1)
        {
            return;
        }


        float[] resultValues = new float[outputData.count];
        outputData.GetData(resultValues);
        _inputDataA.SetData(resultValues);
        basicNNCompute.SetInt("_BufferSize", BufferSize);
        basicNNCompute.SetInt("_BatchSize", BatchSize);
        basicNNCompute.SetInt("_Batch", Batch);

        int tx = BatchSize;
        int ty = 1;
        int tz = 1;
        
        basicNNCompute.Dispatch(idBasicNNKernelSum, tx, ty, tz);

    }

    void SetBuffers(int i)
    {
        //Get the function ID.
        idBasicNNKernelMul = basicNNCompute.FindKernel("Mul");
        idBasicNNKernelSum = basicNNCompute.FindKernel("Sum");
        idBasicNNKernelDot = basicNNCompute.FindKernel("Dot");

        //Now set the buffers to the buffers inside the compute shader
        basicNNCompute.SetBuffer(idBasicNNKernelMul, "_inputDataA", _inputDataA);
        basicNNCompute.SetBuffer(idBasicNNKernelMul, "_inputDataB", _inputDataB);
        basicNNCompute.SetBuffer(idBasicNNKernelMul, "_outputData", outputData);

        basicNNCompute.SetBuffer(idBasicNNKernelSum, "_inputDataA", _inputDataA);
        basicNNCompute.SetBuffer(idBasicNNKernelSum, "_inputDataB", _inputDataB);
        basicNNCompute.SetBuffer(idBasicNNKernelSum, "_outputData", outputData);

        basicNNCompute.SetBuffer(idBasicNNKernelDot, "_inputDataA", _inputDataA);
        basicNNCompute.SetBuffer(idBasicNNKernelDot, "_inputDataB", _inputDataB);
        basicNNCompute.SetBuffer(idBasicNNKernelDot, "_outputData", outputData);

        basicNNCompute.SetInt("_Cols", Col);
        basicNNCompute.SetInt("_Transpose", (int)_transpose);
        basicNNCompute.SetInt("_Batch", i);
    }

    void PrintMatrix(int row, int col)
    {
        float[] resultValues = new float[outputData.count];
        outputData.GetData(resultValues);
        PrintOutMatrix(resultValues, row, col);
    }

    void PrintOutMatrix(float[] result, int row, int col)
    {
        //Log the matrix in matrix notation and format.
        string matrix = "";
        for (int i = 0; i < row; i++)
        {
            matrix += "[";
            for (int j = 0; j < col; j++)
            {
                matrix += result[j + i * row];

                if (j != col - 1)
                {
                    matrix += ", ";
                }
            }
            matrix += "]\n";
        }
        Debug.Log(matrix);
    }
    
    private void OnDisable()
    {
        if (initialized)
        {
            _inputDataA.Release();
            _inputDataB.Release();
            outputData.Release();
        }
        initialized = false;
    }

    private void OnApplicationQuit()
    {
        Debug.Log("Application quitting");
        WritePerformanceListToFile();
    }

    void WritePerformanceListToFile()
    {
        string path = Application.streamingAssetsPath + "/NeuralNetworks/Data/" + "PerformanceList.txt";
        float avg = 0;
        for (int i = 0; i < _PerformanceProfiling.Count; i++)
        {
            avg += _PerformanceProfiling[i];
        }
        avg /= _PerformanceProfiling.Count;
        string someText = "Version.0.0.1: (DOT,SUM): " + avg;

        using (StreamWriter sw = File.AppendText(path))
        {
            sw.WriteLine(someText + "\n");
        }
    }
}
