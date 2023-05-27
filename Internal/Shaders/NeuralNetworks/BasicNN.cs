using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicNN : MonoBehaviour
{
    //The compute shader script.
    [SerializeField] private ComputeShader basicNNCompute = default;

    //Make the buffer to send and recieve data.
    private ComputeBuffer _inputDataA;
    private ComputeBuffer _inputDataB;
    private ComputeBuffer outputData;

    //The stride, how large each element in the buffer is.
    private const int DATA_STRIDE = sizeof(float);
   
    //Function ID from compute shader.
    private int idBasicNNKernel;
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

        LoadDataFromCSV("BasicNNA.csv", ref inputVectorsA);
        LoadDataFromCSV("BasicNNB.csv", ref inputVectorsB);
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
        int tx = Row;
        int ty = Col;
        int tz = 1;
        if (Col > 65535)
        {
            ty = Mathf.CeilToInt(Mathf.Sqrt(Row));
            tz = ty;
        }
        while (true)
        {
            basicNNCompute.Dispatch(idBasicNNKernel, tx, ty, tz);
            float[] resultValues = new float[outputData.count];
            outputData.GetData(resultValues);
            Debug.Log(resultValues[0]);
            yield return new WaitForSeconds(5);
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

        outputMatrix = new float[inputVectorsA.Length * inputVectorsB[0].Length];
        //Initialize the buffers.
        _inputDataA = new ComputeBuffer(inputDataMatrixA.Length, DATA_STRIDE, ComputeBufferType.Structured, ComputeBufferMode.Immutable);
        _inputDataB = new ComputeBuffer(inputDataMatrixB.Length, DATA_STRIDE, ComputeBufferType.Structured, ComputeBufferMode.Immutable);
        outputData = new ComputeBuffer(outputMatrix.Length, DATA_STRIDE, ComputeBufferType.Structured, ComputeBufferMode.Immutable);

        _inputDataA.SetData(inputDataMatrixA);
        _inputDataB.SetData(inputDataMatrixB);
        outputData.SetData(outputMatrix);

        //Get the function ID.
        idBasicNNKernel = basicNNCompute.FindKernel("Main");

        //Now set the buffers to the buffers inside the compute shader
        basicNNCompute.SetBuffer(idBasicNNKernel, "_inputDataA", _inputDataA);
        basicNNCompute.SetBuffer(idBasicNNKernel, "_inputDataB", _inputDataB);
        basicNNCompute.SetBuffer(idBasicNNKernel, "_outputData", outputData);
        basicNNCompute.SetInt("_Cols", inputVectorsA[0].Length);
        basicNNCompute.SetInt("_Transpose", (int)_transpose);

        Row = inputVectorsA.Length;
        Col = inputVectorsB[0].Length;
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
}
