using Deform;
using System.Collections;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using UnityEngine;
using VoxelSystem;
using VoxelSystem.Demo;
using static UnityEngine.ParticleSystem;

public class FluidControl : MonoBehaviour
{
    public bool isSkinnedMesh = false;
    public bool DebugModeOn = false;
    public bool useMesh = true;
    public bool fluidOn = true;

    public Voxel_t[] voxels_t;
    public Vector3[] points;

    protected GPUVoxelData Voxels;
    protected Kernel setupKernel, updateKernel;




    protected const string kSetupKernelKey = "Setup", kUpdateKernelKey = "Update";
    protected enum MeshType
    {
        Volume, Surface
    };

    [SerializeField] protected MeshType meshType = MeshType.Volume;
    protected ComputeShader voxelizer, particleUpdate;
    [SerializeField] protected int voxelResolution = 12;
    void Start()
    {

    }

    protected virtual void MeshToPoints(GPUVoxelData voxelData)
    {

        if (fluidOn)
        {
            //First we get the total count of all the voxels from the buffer. We want to save on performance by checking if this needs a new array or not.
            int voxelCount = voxelData.Buffer.count;
            if (voxels_t == null && !isSkinnedMesh)
            {
                voxels_t = new Voxel_t[voxelCount];
                points = new Vector3[voxelCount];
            }
            else
            {
                voxels_t = new Voxel_t[voxelCount];
                points = new Vector3[voxelCount];
            }

            //Once we have the size, we can call on the GPU to get the voxel data off the GPU to the CPU.
            voxelData.Buffer.GetData(voxels_t);
            Matrix4x4 localToWorld = transform.localToWorldMatrix;
            for (int i = 0; i < voxelCount; i++)
            {
                Vector3 pos = voxels_t[i].position;
                pos = localToWorld.MultiplyPoint3x4(pos);
                points[i] = pos;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnDrawGizmos()
    {
        if (DebugModeOn)
        {
            for (int i = 0; i < points.Length; i++)
            {
                Vector3 pos = points[i];

                Gizmos.color = Color.green;

                Gizmos.DrawSphere(pos, 0.1f);
            }
        }

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

    void ReleaseBuffers()
    {
        Voxels.Dispose();
    }
}
