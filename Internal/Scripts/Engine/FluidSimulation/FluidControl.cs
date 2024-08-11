using Deform;
using System.Collections;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using UnityEngine;
using VoxelSystem;
using VoxelSystem.Demo;
using static UnityEngine.ParticleSystem;

//General fluid control, can be used instead of a mesh based system.
public class FluidControl : MonoBehaviour
{

    public bool DebugModeOn = false;
    public bool fluidOn = true;
    public float controlStrength = 0.025155f;
    public float controlRadius = 0.1208333f;
    public float smoothingRadius = 1.25f;
    public float controlNorm = 0.75f;
    public float kelvin = 273;

    public Voxel_t[] voxels_t;
    public Vector3[] points;

    protected GPUVoxelData Voxels;
    protected Kernel setupKernel, updateKernel;

    protected const string kSetupKernelKey = "Setup", kUpdateKernelKey = "Update";
    protected const string kVoxelBufferKey = "_VoxelBuffer", kVoxelCountKey = "_VoxelCount";
    protected const string kParticleBufferKey = "_ParticleBuffer", kParticleCountKey = "_ParticleCount";
    protected const string kUnitLengthKey = "_UnitLength";


    protected enum MeshType
    {
        Volume, Surface
    };

    void Start()
    {
        FluidSimulationManager.Instance.fluidControlledObjects.Add(this);

        UpdateFluidObjectValues();
    }

    protected virtual void UpdateFluidObjectValues()
    {
        int keyIndex = FluidSimulationManager.Instance.fluidControlledObjects.IndexOf(this);
        //Set Values
        if (fluidOn)
        {
            FluidSimulationManager.Instance._fluidObjectsArray[keyIndex].kelvin = kelvin;
            FluidSimulationManager.Instance._fluidObjectsArray[keyIndex].controlRadius = controlRadius;
            FluidSimulationManager.Instance._fluidObjectsArray[keyIndex].controlStrength = controlStrength;
            FluidSimulationManager.Instance._fluidObjectsArray[keyIndex].smoothingRadius = smoothingRadius;
            FluidSimulationManager.Instance._fluidObjectsArray[keyIndex].controlNorm = controlNorm;
        }
        else
        {
            FluidSimulationManager.Instance._fluidObjectsArray[keyIndex].kelvin = UnigmaSpaceTime.Instance.GlobalTemperature; // Set room temperature.
            FluidSimulationManager.Instance._fluidObjectsArray[keyIndex].controlRadius = 0;
            FluidSimulationManager.Instance._fluidObjectsArray[keyIndex].controlStrength = 0;
            FluidSimulationManager.Instance._fluidObjectsArray[keyIndex].smoothingRadius = 0;
            FluidSimulationManager.Instance._fluidObjectsArray[keyIndex].controlNorm = 1;
        }

    }

    protected virtual void MeshToPoints(GPUVoxelData voxelData)
    {

        if (fluidOn)
        {
            //First we get the total count of all the voxels from the buffer. We want to save on performance by checking if this needs a new array or not.
            int voxelCount = voxelData.Buffer.count;
            voxels_t = new Voxel_t[voxelCount];
            points = new Vector3[voxelCount];

            //Once we have the size, we can call on the GPU to get the voxel data off the GPU to the CPU.
            voxelData.Buffer.GetData(voxels_t);
        }
    }

    protected virtual void TransformPoints(GPUVoxelData voxelData)
    {
        if (fluidOn)
        {
            int voxelCount = voxelData.Buffer.count;
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
        UpdateFluidObjectValues();
    }

    private void OnDrawGizmos()
    {
        if (DebugModeOn)
        {
            for (int i = 0; i < points.Length; i++)
            {
                Vector3 pos = points[i];

                Gizmos.color = Color.green;

                Gizmos.DrawSphere(pos, 0.025f);
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
