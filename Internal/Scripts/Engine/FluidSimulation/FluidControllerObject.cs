using Deform;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using VoxelSystem;
using VoxelSystem.Demo;

/*
 * This script attaches to the game object of choice and uses the gameobject information to create control particles.
 */

public class FluidControllerObject : FluidControl
{
    public bool isSkinnedMesh = false;
    public bool useMesh = true;
    [SerializeField] new protected MeshFilter _meshFilter;
    [SerializeField] new public SkinnedMeshRenderer renderer;
    protected Renderer _renderer;
    protected MaterialPropertyBlock block;


    [SerializeField] protected MeshType meshType = MeshType.Volume;
    protected ComputeShader voxelizer, particleUpdate;
    [SerializeField] protected int voxelResolution = 12;
    protected ComputeBuffer particleBuffer;
    private void Start()
    {
        FluidSimulationManager.Instance.fluidControlledObjects.Add(this);

        voxelizer = Resources.Load<ComputeShader>("Voxelizer/Shaders/Voxelizer");
        particleUpdate = Resources.Load<ComputeShader>("Voxelizer/Demo/Shaders/GPUVoxelParticleSystem/GPUVoxelSkinnedMesh");

        setupKernel = new Kernel(particleUpdate, kSetupKernelKey);
        updateKernel = new Kernel(particleUpdate, kUpdateKernelKey);

        if (isSkinnedMesh)
            SetUpSkinnedMesh();
        else
            SetUpMesh();

        UpdateFluidObjectValues();

    }
    void SetUpMesh()
    {
        //Find mesh filter.
        if (_meshFilter == null)
            _meshFilter = GetComponent<MeshFilter>();

        Mesh localMesh = _meshFilter.mesh;

        Voxels = GPUVoxelizer.Voxelize(voxelizer, localMesh, voxelResolution, (meshType == MeshType.Volume));
        var pointMesh = BuildPoints(Voxels);
        particleBuffer = new ComputeBuffer(pointMesh.vertexCount, Marshal.SizeOf(typeof(VParticle_t)));


        Compute(setupKernel, Voxels, Time.deltaTime);
        MeshToPoints(Voxels);
    }

    void SetUpSkinnedMesh()
    {
        var mesh = Sample();

        Voxels = GPUVoxelizer.Voxelize(voxelizer, mesh, voxelResolution, (meshType == MeshType.Volume));
        var pointMesh = BuildPoints(Voxels);
        particleBuffer = new ComputeBuffer(pointMesh.vertexCount, Marshal.SizeOf(typeof(VParticle_t)));

        //GetComponent<MeshFilter>().sharedMesh = pointMesh;
        /*
        block = new MaterialPropertyBlock();
        _renderer = GetComponent<Renderer>();
        _renderer.GetPropertyBlock(block);


        block.SetBuffer(kParticleBufferKey, particleBuffer);
        _renderer.SetPropertyBlock(block);
        */

        Compute(setupKernel, Voxels, Time.deltaTime);
    }

    private void Update()
    {
        CreateControlPoints();
        TransformControlPoints();
        UpdateFluidObjectValues();

    }

    void CreateControlPoints()
    {
        //Checks if a mesh is being used.
        //If animated skinnedMesh update each frame the skinned mesh.
        //If normal static mesh do not update unless deformed.
        if (isSkinnedMesh)
        {
            Voxels.Dispose();

            var mesh = Sample();
            Voxels = GPUVoxelizer.Voxelize(voxelizer, mesh, voxelResolution, (meshType == MeshType.Volume));

            Compute(updateKernel, Voxels, Time.deltaTime);
            MeshToPoints(Voxels);
        }
    }

    void TransformControlPoints()
    {
        TransformPoints(Voxels);
    }

    Mesh Sample()
    {
        var mesh = new Mesh();
        renderer.BakeMesh(mesh);
        return mesh;
    }

    void Compute(Kernel kernel, GPUVoxelData data, float dt)
    {
        particleUpdate.SetBuffer(kernel.Index, kVoxelBufferKey, data.Buffer);
        particleUpdate.SetInt(kVoxelCountKey, data.Buffer.count);
        particleUpdate.SetFloat(kUnitLengthKey, data.UnitLength);

        particleUpdate.SetBuffer(kernel.Index, kParticleBufferKey, particleBuffer);
        particleUpdate.SetInt(kParticleCountKey, particleBuffer.count);

        particleUpdate.Dispatch(kernel.Index, particleBuffer.count / (int)kernel.ThreadX + 1, (int)kernel.ThreadY, (int)kernel.ThreadZ);
    }

    Mesh BuildPoints(GPUVoxelData data)
    {
        var count = data.Width * data.Height * data.Depth;
        var mesh = new Mesh();
        mesh.indexFormat = (count > 65535) ? IndexFormat.UInt32 : IndexFormat.UInt16;
        mesh.vertices = new Vector3[count];
        var indices = new int[count];
        for (int i = 0; i < count; i++) indices[i] = i;
        mesh.SetIndices(indices, MeshTopology.Points, 0);
        mesh.RecalculateBounds();
        return mesh;
    }
}
