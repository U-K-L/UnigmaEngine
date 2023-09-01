using RayFire;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

public class FluidSimulationManager : MonoBehaviour
{
    ComputeShader _fluidSimulationCompute;
    ComputeBuffer _textureBuffer;
    ComputeBuffer _fluidSimParticles;
    int numParticles;
    int _UpdateParticlesKernel;
    int _CreateGrid;
    Camera _cam;
    RenderTexture _rtTarget;
    RenderTexture _densityMap;
    RenderTexture _tempTarget;
    RenderTexture _fluidNormalBuffer;
    RenderTexture _fluidDepthBuffer;

    Shader _fluidNormalShader;
    Shader _fluidDepthShader;
    Shader _fluidCompositeShader;
    Material _fluidSimMaterialDepthHori;
    Material _fluidSimMaterialDepthVert;
    Material _fluidSimMaterialNormal;
    public Material _fluidSimMaterialComposite;
    public Transform fluidSimTransform;

    public Color DeepWaterColor = Color.white;
    public Color ShallowWaterColor = Color.white;
    public float DepthMaxDistance = 100;
    public Vector2 BlurScale;
    public float BlurFallOff = 0.25f;
    public float BlurRadius = 5.0f;
    public Vector4 DepthScale = default;
    public float _Smoothness = 0.25f;
    public Transform _LightSouce;
    public Transform _LightScale;
    public int numOfParticles;
    public float _SizeOfParticle = 0.125f;

    ComputeBuffer _meshObjectBuffer;
    ComputeBuffer _verticesObjectBuffer;
    ComputeBuffer _indicesObjectBuffer;
    ComputeBuffer _particleBuffer;

    struct MeshObject
    {
        public Matrix4x4 localToWorld;
        public int indicesOffset;
        public int indicesCount;
        public Vector3 position;
        public Vector3 AABBMin;
        public Vector3 AABBMax;
        public Vector3 color;
        public float emission;
        public float smoothness;
        public float transparency;
        public float absorbtion;
        public float celShaded;
        public uint id;
    }
    
    struct Vertex
    {
        public Vector3 position;
        public Vector3 normal;
        public Vector2 uv;
    };

    struct Particles
    {
        Vector3 positions;
    };
    
    //Items to add to the raytracer.
    public LayerMask RayTracingLayers;

    private List<Renderer> _RayTracedObjects = new List<Renderer>();

    private List<MeshObject> meshObjects = new List<MeshObject>();
    private List<Vertex> Vertices = new List<Vertex>();
    private List<int> Indices = new List<int>();
    private List<Particles> _Particles;
    private void Awake()
    {
        _fluidSimulationCompute = Resources.Load<ComputeShader>("FluidSimCompute");
        
        _fluidNormalShader = Resources.Load<Shader>("FluidNormalBuffer");
        _fluidDepthShader = Resources.Load<Shader>("FluidBilateralFilter");
        
        //Create the material for the fluid simulation.
        _fluidSimMaterialDepthHori = new Material(_fluidDepthShader);
        _fluidSimMaterialDepthVert = new Material(_fluidDepthShader);
        _fluidSimMaterialNormal = new Material(_fluidNormalShader);


        _cam = Camera.main;
        //Create the texture for compute shader.
        _rtTarget = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        _densityMap = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        _tempTarget = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        _fluidNormalBuffer = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        _fluidDepthBuffer = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);

        _Particles = new List<Particles>(new Particles[numOfParticles]);

        _UpdateParticlesKernel = _fluidSimulationCompute.FindKernel("UpdateParticles");
        _CreateGrid = _fluidSimulationCompute.FindKernel("CreateGrid");

        _rtTarget.enableRandomWrite = true;
        _rtTarget.Create();
        _densityMap.enableRandomWrite = true;
        _densityMap.Create();
        _fluidNormalBuffer.enableRandomWrite = true;
        _fluidNormalBuffer.Create();
        _fluidSimulationCompute.SetTexture(_CreateGrid, "Result", _rtTarget);
        _fluidSimulationCompute.SetTexture(_CreateGrid, "DensityMap", _densityMap);
        _fluidSimulationCompute.SetTexture(_CreateGrid, "NormalMap", _fluidNormalBuffer);
        
        
        AddObjectsToList();
        CreateNonAcceleratedStructure();
        CreateFluidCommandBuffers();


    }

    void Update()
    {
        //Draw mesh instantiaonation.
        //Graphics.DrawMeshInstancedIndirect()
        UpdateNonAcceleratedRayTracer();

    }

    void AddObjectsToList()
    {
        foreach (var obj in FindObjectsOfType<Renderer>())
        {
            //Check if object in the RaytracingLayers.
            if (((1 << obj.gameObject.layer) & RayTracingLayers) != 0)
            {
                _RayTracedObjects.Add(obj);
            }
        }
    }

    void CreateNonAcceleratedStructure()
    {
        BuildTriangleList();
    }

    void BuildTriangleList()
    {
        Vertices.Clear();
        Indices.Clear();

        foreach (Renderer r in _RayTracedObjects)
        {
            MeshFilter mf = r.GetComponent<MeshFilter>();
            if (mf)
            {
                Mesh m = mf.sharedMesh;
                int startVert = Vertices.Count;
                int startIndex = Indices.Count;

                for (int i = 0; i < m.vertices.Length; i++)
                {
                    Vertex v = new Vertex();
                    v.position = m.vertices[i];
                    v.normal = m.normals[i];
                    v.uv = m.uv[i];
                    Vertices.Add(v);
                }
                var indices = m.GetIndices(0);
                Indices.AddRange(indices.Select(index => index + startVert));

                // Add the object itself
                meshObjects.Add(new MeshObject()
                {
                    localToWorld = r.transform.localToWorldMatrix,
                    indicesOffset = startIndex,
                    indicesCount = indices.Length
                });
            }
        }
        _meshObjectBuffer = new ComputeBuffer(meshObjects.Count, 144);
        _verticesObjectBuffer = new ComputeBuffer(Vertices.Count, 32);
        _indicesObjectBuffer = new ComputeBuffer(Indices.Count, 4);
        _verticesObjectBuffer.SetData(Vertices);
        _fluidSimulationCompute.SetBuffer(_CreateGrid, "_Vertices", _verticesObjectBuffer);
        _indicesObjectBuffer.SetData(Indices);
        _fluidSimulationCompute.SetBuffer(_CreateGrid, "_Indices", _indicesObjectBuffer);
    }

    void UpdateNonAcceleratedRayTracer()
    {
        //Build the BVH
        BuildBVH();
        UpdateParticles();
    }

    //To build the BVH we need to take all of the game objects in our raytracing list.
    //Afterwards place them in a tree with the root node containing all of the objects.
    //The bounding box is calculated by finding the min and max vertices for each axis.
    //If the ray intersects the box we search the triangles of that node, if not we traverse another node and ignore the children.
    unsafe void BuildBVH()
    {
        //First traverse through all of the objects and create their bounding boxes.

        //Get positions stored them to mesh objects.

        //Update position of mesh objects.
        for (int i = 0; i < _RayTracedObjects.Count; i++)
        {
            MeshObject meshobj = new MeshObject();
            RayTracingObject rto = _RayTracedObjects[i].GetComponent<RayTracingObject>();
            meshobj.localToWorld = _RayTracedObjects[i].GetComponent<Renderer>().localToWorldMatrix;
            meshobj.indicesOffset = meshObjects[i].indicesOffset;
            meshobj.indicesCount = meshObjects[i].indicesCount;
            meshobj.position = _RayTracedObjects[i].transform.position;
            meshobj.AABBMin = _RayTracedObjects[i].bounds.min;
            meshobj.AABBMax = _RayTracedObjects[i].bounds.max;
            meshobj.id = (uint)i;
            if (rto)
            {
                meshobj.color = new Vector3(rto.color.r, rto.color.g, rto.color.b);
                meshobj.emission = rto.emission;
                meshobj.smoothness = rto.smoothness;
                meshobj.transparency = rto.transparency;
                meshobj.absorbtion = rto.absorbtion;
                meshobj.celShaded = rto.celShaded;
            }
            meshObjects[i] = meshobj;
        }
        if (_meshObjectBuffer.count > 0)
        {
            _meshObjectBuffer.SetData(meshObjects);
            _fluidSimulationCompute.SetBuffer(_CreateGrid, "_MeshObjects", _meshObjectBuffer);
        }

        _meshObjectBuffer.SetData(meshObjects);
        _fluidSimulationCompute.SetBuffer(_CreateGrid, "_MeshObjects", _meshObjectBuffer);

    }

    void UpdateParticles()
    {
        //Create particle buffer.
        if (_particleBuffer == null)
        {
            _particleBuffer = new ComputeBuffer(numOfParticles, sizeof(float) * 3);
            _particleBuffer.SetData(_Particles);
        }

        //Update particles.
        uint threadsX, threadsY, threadsZ;
        _fluidSimulationCompute.GetKernelThreadGroupSizes(_UpdateParticlesKernel, out threadsX, out threadsY, out threadsZ);
        _fluidSimulationCompute.SetBuffer(_UpdateParticlesKernel, "_Particles", _particleBuffer);
        _fluidSimulationCompute.Dispatch(_UpdateParticlesKernel, (int)threadsX, (int)threadsY, (int)threadsZ);

        //Set particle buffer to shader.
        _fluidSimulationCompute.SetBuffer(_CreateGrid, "_Particles", _particleBuffer);
    }

    //Temporarily attach this simulation to camera!!!
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        //Guard clause, ensure there are objects to ray trace.
        if (_RayTracedObjects.Count == 0)
        {
            Debug.LogWarning("No objects to ray trace. Please add objects to the RayTracingLayers.");
            return;
        }
        
        _fluidSimulationCompute.SetMatrix("_CameraToWorld", _cam.cameraToWorldMatrix);
        _fluidSimulationCompute.SetMatrix("_CameraWorldToLocal", _cam.transform.worldToLocalMatrix);
        _fluidSimulationCompute.SetMatrix("_CameraInverseProjection", _cam.projectionMatrix.inverse);
        _fluidSimulationCompute.SetMatrix("_ParentTransform", fluidSimTransform.localToWorldMatrix);
        _fluidSimulationCompute.SetMatrix("_ParentTransformToLocal", fluidSimTransform.worldToLocalMatrix);
        _fluidSimulationCompute.SetVector("_DepthScale", DepthScale);
        _fluidSimulationCompute.SetFloat("_Smoothness", _Smoothness);
        _fluidSimulationCompute.SetVector("_LightSource", _LightSouce.position);
        _fluidSimulationCompute.SetVector("_LightScale", _LightScale.position);
        _fluidSimulationCompute.SetFloat("_SizeOfParticle", _SizeOfParticle);
        _fluidSimulationCompute.SetBool("_IsOrthographic", _cam.orthographic);



        Matrix4x4 m = GL.GetGPUProjectionMatrix(_cam.projectionMatrix, false);
        m[2, 3] = m[3, 2] = 0.0f; m[3, 3] = 1.0f;
        Matrix4x4 ProjectionToWorld = Matrix4x4.Inverse(m * _cam.worldToCameraMatrix) * Matrix4x4.TRS(new Vector3(0, 0, -m[2, 2]), Quaternion.identity, Vector3.one);
        Shader.SetGlobalMatrix("_ProjectionToWorld", ProjectionToWorld);
        Shader.SetGlobalMatrix("_CameraInverseProjection", _cam.projectionMatrix.inverse);

        //Change to horizonal / vertical scale.
        _fluidSimMaterialDepthHori.SetFloat("_ScaleX", BlurScale.x);
        _fluidSimMaterialDepthHori.SetFloat("_ScaleY", 0.0f);
        _fluidSimMaterialDepthHori.SetFloat("_BlurFallOff", BlurFallOff);
        _fluidSimMaterialDepthHori.SetFloat("_BlurRadius", BlurRadius);

        _fluidSimMaterialDepthVert.SetFloat("_ScaleX", 0.0f);
        _fluidSimMaterialDepthVert.SetFloat("_ScaleY", BlurScale.y);
        _fluidSimMaterialDepthVert.SetFloat("_BlurFallOff", BlurFallOff);
        _fluidSimMaterialDepthVert.SetFloat("_BlurRadius", BlurRadius);

        _fluidSimMaterialComposite.SetColor("_DeepWaterColor", DeepWaterColor);
        _fluidSimMaterialComposite.SetColor("_ShallowWaterColor", ShallowWaterColor);
        _fluidSimMaterialComposite.SetFloat("_DepthMaxDistance", DepthMaxDistance);
        _fluidSimMaterialComposite.SetTexture("_DensityMap", _densityMap);


        //uint threadsX, threadsY, threadsZ;
        //_fluidSimulationCompute.GetKernelThreadGroupSizes(0, out threadsX, out threadsY, out threadsZ);
        //_fluidSimulationCompute.Dispatch(0, Mathf.CeilToInt(Screen.width / threadsX), Mathf.CeilToInt(Screen.width / threadsY), (int)threadsZ);

        //Execute shaders on render target.
        //Graphics.Blit(_rtTarget, _fluidDepthBuffer, _fluidSimMaterialDepth);
        //Graphics.Blit(source, _rtTarget, _fluidSimMaterialDepthHori);
        Graphics.Blit(source, destination, _fluidSimMaterialComposite);
    }
    
    void CreateFluidCommandBuffers()
    {
        CommandBuffer fluidCommandBuffers = new CommandBuffer();
        fluidCommandBuffers.SetGlobalTexture("_UnigmaFluids", _rtTarget);

        fluidCommandBuffers.SetRenderTarget(_rtTarget);

        fluidCommandBuffers.ClearRenderTarget(true, true, new Vector4(0,0,0,0));

        uint threadsX, threadsY, threadsZ;
        _fluidSimulationCompute.GetKernelThreadGroupSizes(_CreateGrid, out threadsX, out threadsY, out threadsZ);

        fluidCommandBuffers.SetComputeTextureParam(_fluidSimulationCompute, _CreateGrid, "Result", _rtTarget);
        fluidCommandBuffers.DispatchCompute(_fluidSimulationCompute, _CreateGrid, Mathf.CeilToInt(Screen.width / threadsX), Mathf.CeilToInt(Screen.width / threadsY), (int)threadsZ);

        fluidCommandBuffers.SetGlobalTexture("_UnigmaFluidsDepth", _fluidDepthBuffer);

        fluidCommandBuffers.SetRenderTarget(_fluidDepthBuffer);

        //fluidCommandBuffers.ClearRenderTarget(true, true, new Vector4(0, 0, 0, 0));

        fluidCommandBuffers.Blit(_rtTarget, _fluidDepthBuffer, _fluidSimMaterialDepthHori);
        fluidCommandBuffers.Blit(_fluidDepthBuffer, _tempTarget);
        fluidCommandBuffers.Blit(_tempTarget, _fluidDepthBuffer, _fluidSimMaterialDepthVert);


        fluidCommandBuffers.SetGlobalTexture("_UnigmaFluidsNormals", _fluidNormalBuffer);

        fluidCommandBuffers.SetRenderTarget(_fluidNormalBuffer);

        //fluidCommandBuffers.ClearRenderTarget(true, true, new Vector4(0, 0, 0, 0));

        fluidCommandBuffers.Blit(_fluidDepthBuffer, _fluidNormalBuffer, _fluidSimMaterialNormal);

        GetComponent<Camera>().AddCommandBuffer(CameraEvent.AfterForwardOpaque, fluidCommandBuffers);

    }

    //Release all buffers and memory
    void ReleaseBuffers()
    {
        if (_verticesObjectBuffer != null)
            _verticesObjectBuffer.Release();
        if (_indicesObjectBuffer != null)
            _indicesObjectBuffer.Release();
        if (_meshObjectBuffer != null)
            _meshObjectBuffer.Release();
        if (_particleBuffer != null)
            _particleBuffer.Release();


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
}
