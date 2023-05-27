using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PyramidCompute : MonoBehaviour
{
    [SerializeField] private Mesh sourceMesh = default;
    [SerializeField] private Material material = default;
    [SerializeField] private ComputeShader computeShader = default;
    [SerializeField] private ComputeShader triToVerts = default;
    [SerializeField] public float height = 1;
    [SerializeField] private float frequency = 1;

    //Ensure the data is laid out sequentially
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct SourceVertex
    {
        public Vector3 position;
        public Vector2 uv;
    }

    private bool initialized;
    private ComputeBuffer sourceVertexBuffer;
    private ComputeBuffer sourceTriBuffer;
    private ComputeBuffer resultVertexBuffer;
    private ComputeBuffer argsBuffer;

    private int idPyramidKernel;
    private int idTriToVertKernal;
    private int dispatchSize;
    private Bounds localBounds;

    private const int SOURCE_VERT_STRIDE = sizeof(float) * (3 + 2);
    private const int SOURCE_TRI_STRIDE = sizeof(int);
    private const int RESULT_VERT_STRIDE = sizeof(float) * (3 + (3 + 2) * 3);
    private const int ARGS_STRIDE = sizeof(int) * 4;

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
    
    public void OnEnable()
    {
        if (initialized)
        {
            OnDisable();
        }
        initialized = true;

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
                position = positions[i],
                uv = uvs[i]
            };
        }
        int numTriangles = tris.Length / 3;

        //Create compute buffer.
        sourceVertexBuffer = new ComputeBuffer(sourceVertices.Length, SOURCE_VERT_STRIDE, ComputeBufferType.Structured, ComputeBufferMode.Immutable);
        sourceVertexBuffer.SetData(sourceVertices);
        sourceTriBuffer = new ComputeBuffer(tris.Length, SOURCE_TRI_STRIDE, ComputeBufferType.Structured, ComputeBufferMode.Immutable);
        sourceTriBuffer.SetData(tris);
        resultVertexBuffer = new ComputeBuffer(numTriangles * 3, RESULT_VERT_STRIDE, ComputeBufferType.Append);
        argsBuffer = new ComputeBuffer(1, ARGS_STRIDE, ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(new int[] { 0, 1, 0, 0 });

        //Set data on the compute buffer
        sourceVertexBuffer.SetData(sourceVertices);
        sourceTriBuffer.SetData(tris);
        resultVertexBuffer.SetCounterValue(0);

        //Send buffers to shader.
        idPyramidKernel = computeShader.FindKernel("Main");
        idTriToVertKernal = triToVerts.FindKernel("Main");

        computeShader.SetBuffer(idPyramidKernel, "_sourceVertices", sourceVertexBuffer);
        computeShader.SetBuffer(idPyramidKernel, "_sourceTriangles", sourceTriBuffer);
        computeShader.SetBuffer(idPyramidKernel, "_outputTriangles", resultVertexBuffer);
        computeShader.SetInt("_NumOfTriangles", numTriangles);

        triToVerts.SetBuffer(idTriToVertKernal, "_IndirectArgsBuffer", argsBuffer);

        //place on graphics shader.
        material.SetBuffer("_outputTriangles", resultVertexBuffer);

        //Calculate dipatch size.
        computeShader.GetKernelThreadGroupSizes(idPyramidKernel, out uint threadGroupSize, out _, out _);

        dispatchSize = Mathf.CeilToInt(numTriangles / (float)threadGroupSize);

        localBounds = sourceMesh.bounds;
        localBounds.Expand(height);
    }

    private void OnDisable()
    {
        if (initialized)
        {
            sourceVertexBuffer.Release();
            sourceTriBuffer.Release();
            resultVertexBuffer.Release();
            argsBuffer.Release();
        }
        initialized = false;
    }

    private void LateUpdate()
    {
        //Clear the draw buffer.
        resultVertexBuffer.SetCounterValue(0);

        Bounds bounds = TransformBounds(localBounds);

        //update for this frame, position and height.
        computeShader.SetMatrix("_LocalToWorldMatrix", transform.localToWorldMatrix);
        computeShader.SetFloat("_Height", height);

        //Finally, dispatch the shader.
        computeShader.Dispatch(idPyramidKernel, dispatchSize, 1, 1);

        //Copy the result buffer to the args buffer
        ComputeBuffer.CopyCount(resultVertexBuffer, argsBuffer, 0);

        //After copy set tris to verts.

        triToVerts.Dispatch(idTriToVertKernal, 1, 1, 1);

        //Debug.Log("Details about generated mesh: " + resultVertexBuffer + " ; " + argsBuffer); 
        //Render the generated mesh.
        Graphics.DrawProceduralIndirect(material, bounds, MeshTopology.Triangles, argsBuffer, 0, null, null, UnityEngine.Rendering.ShadowCastingMode.Off, false, gameObject.layer);
    }
}
