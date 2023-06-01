using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PixelGrass : MonoBehaviour
{
    
    [SerializeField] private Mesh sourceMesh = default;
    [SerializeField] private Mesh sourceInstantiateMesh = default;
    [SerializeField] private Material material = default;
    [SerializeField] private ComputeShader pixelGrassComputeShader = default;
    [SerializeField] public float height = 1;
    [SerializeField] public float width = 1;
    [SerializeField] public int _NumOfMeshesPerTriangle = 1;
    private int _previousNumOfMehsesPerTriangle = 1;
    
    //Ensure the data is laid out sequentially
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct SourceVertex
    {
        public Vector3 position;
    }

    private struct OutputVertex
    {
        public Vector3 position;
        public Vector3 normal;
        public Vector2 uv;
    };

    private bool initialized;
    private ComputeBuffer sourceVertexBuffer;

    private ComputeBuffer _sourceInstantiateVertices;
    private ComputeBuffer _sourceInstantiateTriangles;

    private ComputeBuffer sourceTriBuffer;
    private ComputeBuffer outputTriangles;
    private ComputeBuffer outputVertices;
    private ComputeBuffer argsBuffer;

    private int idPyramidKernel;
    private int idTriToVertKernal;
    private Vector3Int dispatchSize;
    private Bounds localBounds;

    private const int SOURCE_VERT_STRIDE = sizeof(float) * (3);
    private const int OUTPUT_VERT_STRIDE = sizeof(float) * (3 + 3 + 2);
    private const int SOURCE_TRI_STRIDE = sizeof(int);
    private const int OUTPUT_TRI_STRIDE = (3*(3 + 2) * sizeof(float)) + (3 + 2) * sizeof(float);
    private const int ARGS_STRIDE = sizeof(int) * 4;

    private int[] argsBufferInitialized = new int[] {0, 1, 0, 0 };
    
public void OnEnable()
    {
        if (initialized)
        {
            OnDisable();
        }
        initialized = true;
        InitializeBuffers();
        localBounds = sourceMesh.bounds;
        localBounds.Expand(1);
    }

    void InitializeBuffers()
    {
        //Create the source vertex buffer
        Vector3[] positions = sourceMesh.vertices;
        Vector2[] uvs = sourceMesh.uv;
        int[] tris = sourceMesh.triangles;

        //Adding all the vertices from the mesh to buffer
        SourceVertex[] sourceVertices = new SourceVertex[positions.Length];
        for (int i = 0; i < positions.Length; i++)
        {
            sourceVertices[i] = new SourceVertex()
            {
                position = positions[i]
            };
        }
        int numTriangles = Mathf.CeilToInt(tris.Length / 3);

        //Create the source vertex buffer
        Vector3[] Ipositions = sourceInstantiateMesh.vertices;
        Vector2[] Iuvs = sourceInstantiateMesh.uv;
        int[] Itris = sourceInstantiateMesh.triangles;

        //Adding all the vertices from the mesh to buffer
        SourceVertex[] IsourceVertices = new SourceVertex[Ipositions.Length];
        for (int i = 0; i < Ipositions.Length; i++)
        {
            IsourceVertices[i] = new SourceVertex()
            {
                position = Ipositions[i]
            };
        }

        //Create compute buffer.
        sourceVertexBuffer = new ComputeBuffer(sourceVertices.Length, SOURCE_VERT_STRIDE, ComputeBufferType.Structured, ComputeBufferMode.Immutable);
        sourceTriBuffer = new ComputeBuffer(tris.Length, SOURCE_TRI_STRIDE, ComputeBufferType.Structured, ComputeBufferMode.Immutable);

        _sourceInstantiateVertices = new ComputeBuffer(IsourceVertices.Length, SOURCE_VERT_STRIDE, ComputeBufferType.Structured, ComputeBufferMode.Immutable);
        _sourceInstantiateTriangles = new ComputeBuffer(Itris.Length, SOURCE_TRI_STRIDE, ComputeBufferType.Structured, ComputeBufferMode.Immutable);

        int numOfOutputTriangles = numTriangles * _NumOfMeshesPerTriangle * _sourceInstantiateTriangles.count;

        outputVertices = new ComputeBuffer(Itris.Length * numOfOutputTriangles, OUTPUT_VERT_STRIDE, ComputeBufferType.Append);
        outputTriangles = new ComputeBuffer(numOfOutputTriangles, OUTPUT_TRI_STRIDE, ComputeBufferType.Structured, ComputeBufferMode.Immutable);
        argsBuffer = new ComputeBuffer(1, ARGS_STRIDE, ComputeBufferType.IndirectArguments);

        //Set data on the compute buffer
        sourceVertexBuffer.SetData(sourceVertices);
        _sourceInstantiateVertices.SetData(IsourceVertices);
        _sourceInstantiateTriangles.SetData(Itris);
        sourceTriBuffer.SetData(tris);
        argsBuffer.SetData(new int[] { 0, 1, 0, 0 });
        outputTriangles.SetCounterValue(0);
        outputVertices.SetCounterValue(0);


        idPyramidKernel = pixelGrassComputeShader.FindKernel("Main");

        pixelGrassComputeShader.SetBuffer(idPyramidKernel, "_sourceVertices", sourceVertexBuffer);
        pixelGrassComputeShader.SetBuffer(idPyramidKernel, "_sourceTriangles", sourceTriBuffer);
        pixelGrassComputeShader.SetBuffer(idPyramidKernel, "_outputTriangles", outputTriangles);
        pixelGrassComputeShader.SetBuffer(idPyramidKernel, "_outputVertices", outputVertices);
        pixelGrassComputeShader.SetBuffer(idPyramidKernel, "_sourceInstantiateVertices", _sourceInstantiateVertices);
        pixelGrassComputeShader.SetBuffer(idPyramidKernel, "_sourceInstantiateTriangles", _sourceInstantiateTriangles);
        
        pixelGrassComputeShader.SetInt("_NumOfTriangles", numTriangles);
        pixelGrassComputeShader.SetInt("_NumOfMeshesPerTriangle", _NumOfMeshesPerTriangle);
        pixelGrassComputeShader.SetBuffer(idTriToVertKernal, "_IndirectArgsBuffer", argsBuffer);

        //place on graphics shader.
        material.SetBuffer("_outputTriangles", outputTriangles);
        material.SetBuffer("_outputVertices", outputVertices);
        material.SetInt("_NumVerts", _sourceInstantiateTriangles.count);



        argsBufferInitialized[0] = _sourceInstantiateTriangles.count * numTriangles;
        //Calculate dipatch size.
        pixelGrassComputeShader.GetKernelThreadGroupSizes(idPyramidKernel, out uint threadGroupSizex, out uint threadGroupSizey, out uint threadGroupSizez);

        dispatchSize.x = Mathf.CeilToInt(numTriangles / (float)threadGroupSizex);
        dispatchSize.y = Mathf.CeilToInt(_NumOfMeshesPerTriangle / (float)threadGroupSizey);
        dispatchSize.z = Mathf.CeilToInt(1 / (float)threadGroupSizez);

        Debug.Log("Size of the buffers " + _sourceInstantiateVertices.count + " " + _sourceInstantiateTriangles.count + " " + Itris.Length);
    }

    private void OnDisable()
    {
        if (initialized)
        {
            sourceVertexBuffer.Release();
            sourceTriBuffer.Release();
            outputTriangles.Release();
            argsBuffer.Release();
        }
        initialized = false;
    }

    private void LateUpdate()
    {
        //Clear the draw buffer.
        outputTriangles.SetCounterValue(0);
        outputVertices.SetCounterValue(0);
        argsBuffer.SetData(argsBufferInitialized);

        Bounds bounds = TransformBounds(localBounds);

        DrawBounds(bounds, 1);
        //update for this frame, position and height.
        pixelGrassComputeShader.SetMatrix("_LocalToWorldMatrix", transform.localToWorldMatrix);

        Debug.Log(transform.localToWorldMatrix);

        Debug.Log(transform.up);
        Debug.Log(transform.localToWorldMatrix.m01 + " " + transform.localToWorldMatrix.m11 + " " + transform.localToWorldMatrix.m21);
        pixelGrassComputeShader.SetFloat("_Height", height);
        pixelGrassComputeShader.SetFloat("_Width", width);
        pixelGrassComputeShader.SetVector("_CameraPosition", Camera.main.transform.up);

        //Finally, dispatch the shader.
        Debug.Log(dispatchSize);
        pixelGrassComputeShader.Dispatch(idPyramidKernel, dispatchSize.x, dispatchSize.y, dispatchSize.z);

        //Render the generated mesh.
        Graphics.DrawProceduralIndirect(material, bounds, MeshTopology.Triangles, argsBuffer, 0, null, null, UnityEngine.Rendering.ShadowCastingMode.Off, true, gameObject.layer);


        //Print out vertices from output.
        if (_previousNumOfMehsesPerTriangle != _NumOfMeshesPerTriangle)
        {
            RefreshInstantiatedMeshes();
            _previousNumOfMehsesPerTriangle = _NumOfMeshesPerTriangle;
        }
    }

    void RefreshInstantiatedMeshes()
    {
        int numOfOutputTriangles = (sourceMesh.triangles.Length/3) * _NumOfMeshesPerTriangle * (_sourceInstantiateTriangles.count/3);
        Debug.Log((sourceMesh.triangles.Length / 3) + " " + _NumOfMeshesPerTriangle + " " + _sourceInstantiateTriangles.count);
        //The number of vertices, which is X3.
        argsBufferInitialized[0] = numOfOutputTriangles*3;
        
        outputTriangles = new ComputeBuffer(numOfOutputTriangles, OUTPUT_TRI_STRIDE, ComputeBufferType.Structured, ComputeBufferMode.Immutable);
        pixelGrassComputeShader.SetInt("_NumOfMeshesPerTriangle", _NumOfMeshesPerTriangle);
        pixelGrassComputeShader.SetBuffer(idPyramidKernel, "_outputTriangles", outputTriangles);
        
        pixelGrassComputeShader.GetKernelThreadGroupSizes(idPyramidKernel, out uint threadGroupSizex, out uint threadGroupSizey, out uint threadGroupSizez);
        dispatchSize.x = Mathf.CeilToInt(Mathf.CeilToInt(sourceMesh.triangles.Length / 3) / (float)threadGroupSizex);
        dispatchSize.y = Mathf.CeilToInt(_NumOfMeshesPerTriangle / (float)threadGroupSizey);
        dispatchSize.z = Mathf.CeilToInt(1 / (float)threadGroupSizez);

        material.SetBuffer("_outputTriangles", outputTriangles);
    }

    public Bounds TransformBounds(Bounds _localBounds)
    {
        var center = transform.TransformPoint(_localBounds.center);

        // transform the local extents' axes
        var extents = _localBounds.extents;
        var axisX = transform.TransformVector(extents.x, 0, 0);
        var axisY = transform.TransformVector(0, extents.y, 0);
        var axisZ = transform.TransformVector(0, 0, extents.z);

        // sum their absolute value to get the world extents
        extents.x = Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x);
        extents.y = Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y);
        extents.z = Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z);

        return new Bounds { center = center, extents = extents };
    }

    void DrawBounds(Bounds b, float delay = 0)
    {
        // bottom
        var p1 = new Vector3(b.min.x, b.min.y, b.min.z);
        var p2 = new Vector3(b.max.x, b.min.y, b.min.z);
        var p3 = new Vector3(b.max.x, b.min.y, b.max.z);
        var p4 = new Vector3(b.min.x, b.min.y, b.max.z);

        Debug.DrawLine(p1, p2, Color.blue, delay);
        Debug.DrawLine(p2, p3, Color.red, delay);
        Debug.DrawLine(p3, p4, Color.yellow, delay);
        Debug.DrawLine(p4, p1, Color.magenta, delay);

        // top
        var p5 = new Vector3(b.min.x, b.max.y, b.min.z);
        var p6 = new Vector3(b.max.x, b.max.y, b.min.z);
        var p7 = new Vector3(b.max.x, b.max.y, b.max.z);
        var p8 = new Vector3(b.min.x, b.max.y, b.max.z);

        Debug.DrawLine(p5, p6, Color.blue, delay);
        Debug.DrawLine(p6, p7, Color.red, delay);
        Debug.DrawLine(p7, p8, Color.yellow, delay);
        Debug.DrawLine(p8, p5, Color.magenta, delay);

        // sides
        Debug.DrawLine(p1, p5, Color.white, delay);
        Debug.DrawLine(p2, p6, Color.gray, delay);
        Debug.DrawLine(p3, p7, Color.green, delay);
        Debug.DrawLine(p4, p8, Color.cyan, delay);
    }

}
