using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelSystem;
using VoxelSystem.Demo;

/*
 * This script attaches to the game object of choice and uses the gameobject information to create control particles.
 */

public class FluidControllerObject : FluidControl
{
    [SerializeField] new protected MeshFilter mesh;
    public GPUVoxelSkinnedMesh voxelSkinnedMesh;
    private void Start()
    {

        voxelizer = Resources.Load<ComputeShader>("Voxelizer/Shaders/Voxelizer");
        particleUpdate = Resources.Load<ComputeShader>("Voxelizer/Demo/Shaders/GPUVoxelParticleSystem/GPUVoxelSkinnedMesh");

        Debug.Log(particleUpdate == null);
        Debug.Log(voxelizer == null);

        setupKernel = new Kernel(particleUpdate, kSetupKernelKey);
        updateKernel = new Kernel(particleUpdate, kUpdateKernelKey);


        FluidSimulationManager.Instance.fluidControlledObjects.Add(transform.name, this);
    }

    private void Update()
    {
        UpdateControlPoints();
        
    }

    void UpdateControlPoints()
    {
        if (isSkinnedMesh)
        {
            MeshToPoints(voxelSkinnedMesh.data);
        }
        else
        {
            //MeshToPoints(Voxels);
        }
    }
}
