using System.Collections;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using UnityEngine;
using VoxelSystem;
using VoxelSystem.Demo;
using static UnityEngine.ParticleSystem;

public class FluidControl : MonoBehaviour
{
    // Start is called before the first frame update
    public GPUVoxelSkinnedMesh voxelSkinnedMesh;
    public VoxelSystem.Demo.VParticle_t[] vParticle;
    public Voxel_t[] voxels_t;
    public FluidSimulationManager fluidSimulationManager;

    public GameObject dummyMesh;
    private Vector3[] points;

    GPUVoxelData Voxels;

    public bool fluidOn = true;

    enum MeshType
    {
        Volume, Surface
    };

    [SerializeField] MeshType type = MeshType.Volume;

    [SerializeField] new protected MeshFilter mesh;
    [SerializeField] protected ComputeShader voxelizer, particleUpdate;
    [SerializeField] protected int count = 64;
    void Start()
    {
        mesh = dummyMesh.GetComponent<MeshFilter>();
        Voxels = GPUVoxelizer.Voxelize(voxelizer, mesh.mesh, count, (type == MeshType.Volume));
        points = new Vector3[Voxels.Buffer.count];
    }

    // Update is called once per frame
    void Update()
    {
        /*
        Matrix4x4 localToWorld = dummyMesh.transform.localToWorldMatrix;
        fluidSimulationManager.NumOfControlParticles = mesh.mesh.vertexCount;
        for (int i = 0; i < mesh.mesh.vertexCount; i++)
        {
            Vector3 pos = mesh.mesh.vertices[i];
            pos = localToWorld.MultiplyPoint3x4(pos);
            points[i] = pos;
            fluidSimulationManager.controlParticlesPositions[i] = pos;
        }
        */

        /*
        if (vParticle == null)
            vParticle = new VoxelSystem.Demo.VParticle_t[voxelSkinnedMesh.particleBuffer.count];
        voxelSkinnedMesh.particleBuffer.GetData(vParticle);

        fluidSimulationManager.NumOfControlParticles = vParticle.Length;
        for (int i = 0; i < vParticle.Length; i++)
        {
            fluidSimulationManager.controlParticlesPositions[i] = vParticle[i].position;
        }
        */

        if (fluidOn)
        {
            int voxelCount = Voxels.Buffer.count;
            if (voxels_t == null)
                voxels_t = new Voxel_t[voxelCount];
            Voxels.Buffer.GetData(voxels_t);

            fluidSimulationManager.NumOfControlParticles = voxelCount;


            Matrix4x4 localToWorld = dummyMesh.transform.localToWorldMatrix;
            for (int i = 0; i < voxelCount; i++)
            {
                Vector3 pos = voxels_t[i].position;
                pos = localToWorld.MultiplyPoint3x4(pos);
                points[i] = pos;
                fluidSimulationManager.controlParticlesPositions[i] = pos;
            }
        }
        else
        {
            fluidSimulationManager.NumOfControlParticles = 0;
        }

    }

    private void OnDrawGizmos()
    {
        
        for (int i = 0; i < points.Length; i++)
        {
            Vector3 pos = points[i];

            Gizmos.color = Color.green;

            Gizmos.DrawSphere(pos, 0.1f);
        }
        

        /*
        for (int i = 0; i < vParticle.Length; i++)
        {
            Vector3 pos = vParticle[i].position;

            Gizmos.color = Color.green;

            Gizmos.DrawSphere(pos, 0.1f);
        }
        */

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
