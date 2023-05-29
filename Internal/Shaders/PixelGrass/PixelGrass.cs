using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PixelGrass : MonoBehaviour
{
    
    [SerializeField] private Mesh sourceMesh = default;
    [SerializeField] private Material material = default;
    [SerializeField] private ComputeShader pixelGrassComputeShader = default;
    [SerializeField] public float height = 1;
    [SerializeField] public float width = 1;

    //Ensure the data is laid out sequentially
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct SourceVertex
    {
        public Vector3 position;
    }

    private struct OutputVertex
    {
        Vector3 position;
        Vector3 normal;
        Vector2 uv;
    };

    private bool initialized;
    private ComputeBuffer sourceVertexBuffer;
    private ComputeBuffer sourceTriBuffer;
    private ComputeBuffer outputTriangles;
    private ComputeBuffer argsBuffer;

    private int idPyramidKernel;
    private int idTriToVertKernal;
    private int dispatchSize;
    private Bounds localBounds;

    private const int SOURCE_VERT_STRIDE = sizeof(float) * (3);
    private const int SOURCE_TRI_STRIDE = sizeof(int);
    private const int OUTPUT_TRI_STRIDE = sizeof(float) * (3 + (3+3+2) * 6);
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

        //Create compute buffer.
        sourceVertexBuffer = new ComputeBuffer(sourceVertices.Length, SOURCE_VERT_STRIDE, ComputeBufferType.Structured, ComputeBufferMode.Immutable);
        sourceTriBuffer = new ComputeBuffer(tris.Length, SOURCE_TRI_STRIDE, ComputeBufferType.Structured, ComputeBufferMode.Immutable);
        
        outputTriangles = new ComputeBuffer(numTriangles, OUTPUT_TRI_STRIDE, ComputeBufferType.Append);
        argsBuffer = new ComputeBuffer(1, ARGS_STRIDE, ComputeBufferType.IndirectArguments);

        //Set data on the compute buffer
        sourceVertexBuffer.SetData(sourceVertices);
        sourceTriBuffer.SetData(tris);
        argsBuffer.SetData(new int[] { 0, 1, 0, 0 });
        outputTriangles.SetCounterValue(0);

        idPyramidKernel = pixelGrassComputeShader.FindKernel("Main");

        pixelGrassComputeShader.SetBuffer(idPyramidKernel, "_sourceVertices", sourceVertexBuffer);
        pixelGrassComputeShader.SetBuffer(idPyramidKernel, "_sourceTriangles", sourceTriBuffer);
        pixelGrassComputeShader.SetBuffer(idPyramidKernel, "_outputTriangles", outputTriangles);
        pixelGrassComputeShader.SetInt("_NumOfTriangles", numTriangles);

        pixelGrassComputeShader.SetBuffer(idTriToVertKernal, "_IndirectArgsBuffer", argsBuffer);

        //place on graphics shader.
        material.SetBuffer("_outputTriangles", outputTriangles);
        
        

        //Calculate dipatch size.
        pixelGrassComputeShader.GetKernelThreadGroupSizes(idPyramidKernel, out uint threadGroupSize, out _, out _);

        dispatchSize = Mathf.CeilToInt(numTriangles / (float)threadGroupSize);
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
        argsBuffer.SetData(argsBufferInitialized);

        Bounds bounds = TransformBounds(localBounds);

        DrawBounds(bounds, 1);
        //update for this frame, position and height.
        pixelGrassComputeShader.SetMatrix("_LocalToWorldMatrix", transform.localToWorldMatrix);
        pixelGrassComputeShader.SetFloat("_Height", height);
        pixelGrassComputeShader.SetFloat("_Width", width);

        //Finally, dispatch the shader.
        pixelGrassComputeShader.Dispatch(idPyramidKernel, dispatchSize, 1, 1);
        
        //Render the generated mesh.
        Graphics.DrawProceduralIndirect(material, bounds, MeshTopology.Triangles, argsBuffer, 0, null, null, UnityEngine.Rendering.ShadowCastingMode.Off, true, gameObject.layer);
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
