using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

public class UnigmaCommandBuffers : MonoBehaviour
{
    private int buffersAdded = 0;
    private List<UnigmaPostProcessingObjects> _OutlineRenderObjects; //Objects part of this render.
    private List<Renderer> _OutlineNullObjects = default; //Objects not part of this render.
    public int _temporalReservoirsCount = 1;
    private Camera mainCam;

    public Texture2D SphericalMap;

    struct Sample
    {
        public Vector3 x0;
        public Vector3 x1;
        public Vector3 x2;
        public float weight;
    };

    struct Reservoir
    {
        public uint Y; //Index most important light.
        public float W; //light weight
        public float wSum; // weight summed.
        public float M; //Number of total lights for this reservoir.
        public float pHat;
        public Vector3 x1; //position of the hit point.
        public int age; //how many times used.
    };

    struct UnigmaLight
    {
        public Vector3 position;
        public float emission;
        public Vector3 area;
        public Vector3 color;
    };

    struct ReservoirPath
    {
        public float wSum; // weight summed.
        public float M; //Number of total lights for this reservoir.
        public Vector3 radiance;
        public Vector3 position;
        public Vector3 normal;

    };

    struct UnigmaDispatchInfo
    {
        public int FrameCount;
    };

    int _unigmaDispatchInfoStride = sizeof(int);
    int _reservoirStride = sizeof(float) * 6 + sizeof(float)*3;
    int _reservoirPathStride = sizeof(float) * 2 + sizeof(float) * 3*3;
    int _lightStride = sizeof(float) * 3*3 + sizeof(float);
    int _sampleStride = (sizeof(float) * 3) * 3 + sizeof(float);

    ComputeBuffer samplesBuffer;
    ComputeBuffer lightsBuffer;
    ComputeBuffer reservoirsBuffer;
    ComputeBuffer reservoirPathsBuffer;
    ComputeBuffer unigmaDispatchInfoBuffer;

    private List<Reservoir> reservoirs;
    private List<ReservoirPath> reservoirPaths;
    private List<Sample> samplesList;
    private List<UnigmaLight> lightList;
    private List<UnigmaDispatchInfo> unigmaDispatchInfos;

    private ComputeShader computeOutlineColors;
    private ComputeShader unigmaDispatchInfoComputeShader;
    private Material _nullMaterial = default;

    private bool BuffersReady = false;

    public static int _UnigmaFrameCount;

    [SerializeField]
    Material rayTracingDepthShadowMaterial;

    private RayTracingShader _DepthShadowsRayTracingShaderAccelerated;
    private RayTracingShader _RestirGlobalIllumRayTracingShaderAccelerated;
    private RayTracingShader _RestirSpatialShaderAccelerated;
    RayTracingAccelerationStructure _AccelerationStructure;

    public LayerMask RayTracingLayers;
    RenderTexture _DepthShadowsTexture;
    RenderTexture _UnigmaGlobalIllumination;
    private List<Renderer> _rayTracedObjects = new List<Renderer>();
    public Camera secondCam;
    // Start is called before the first frame update
    void Start()
    {
        samplesList = new List<Sample>();
        lightList = new List<UnigmaLight>();
        reservoirs = new List<Reservoir>();
        reservoirPaths = new List<ReservoirPath>();
        AddLightsToList();
        lightsBuffer = new ComputeBuffer(lightList.Count, _lightStride);
        unigmaDispatchInfos = new List<UnigmaDispatchInfo>();
        _nullMaterial = new Material(Shader.Find("Unigma/IsometricNull"));
        computeOutlineColors = Resources.Load("OutlineColorsBoxBlur") as ComputeShader;
        unigmaDispatchInfoComputeShader = Resources.Load("UnigmaDispatchInfo") as ComputeShader;
        Camera cam = GetComponent<Camera>();
        Camera.main.depthTextureMode = DepthTextureMode.MotionVectors;
        mainCam = Camera.main;
        cam.depthTextureMode = cam.depthTextureMode | DepthTextureMode.Depth;
        cam.depthTextureMode = cam.depthTextureMode | DepthTextureMode.DepthNormals;


        _DepthShadowsTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        _DepthShadowsTexture.enableRandomWrite = true;
        _DepthShadowsTexture.Create();

        _UnigmaGlobalIllumination = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        _UnigmaGlobalIllumination.enableRandomWrite = true;
        _UnigmaGlobalIllumination.Create();

        

        CreateAcceleratedStructure();
    }

    void CreateAcceleratedStructure()
    {
        int amountOfSamples = Screen.width * Screen.height;

        for(int i = 0; i < amountOfSamples; i++)
        {
            Sample s = new Sample();
            s.x0 = Vector3.zero;
            s.x1 = Vector3.zero;
            s.x2 = Vector3.zero;
            s.weight = 0;

            samplesList.Add(s);


        }

        for (int j = 0; j < amountOfSamples * _temporalReservoirsCount; j++)
        {
            Reservoir r = new Reservoir();
            r.W = 0;
            r.wSum = 0;
            r.Y = 0;
            r.M = 0;
            r.x1 = Vector3.zero;
            r.age = 0;

            reservoirs.Add(r);

            ReservoirPath rp = new ReservoirPath();
            rp.wSum = 0;
            rp.M = 0;
            rp.radiance = Vector3.zero;
            rp.normal = Vector3.zero;

            reservoirPaths.Add(rp);
        }

        for (int i = 0; i < 1; i++)
        {
            UnigmaDispatchInfo u = new UnigmaDispatchInfo();
            u.FrameCount = 0;

            unigmaDispatchInfos.Add(u);


        }

        samplesBuffer = new ComputeBuffer(amountOfSamples, _sampleStride);
        samplesBuffer.SetData(samplesList);

        reservoirsBuffer = new ComputeBuffer(amountOfSamples * _temporalReservoirsCount, _reservoirStride);
        reservoirsBuffer.SetData(reservoirs);

        reservoirPathsBuffer = new ComputeBuffer(amountOfSamples * _temporalReservoirsCount, _reservoirPathStride);
        reservoirPathsBuffer.SetData(reservoirPaths);

        unigmaDispatchInfoBuffer = new ComputeBuffer(1, _unigmaDispatchInfoStride);
        unigmaDispatchInfoBuffer.SetData(unigmaDispatchInfos);

        if (_DepthShadowsRayTracingShaderAccelerated == null)
            _DepthShadowsRayTracingShaderAccelerated = Resources.Load<RayTracingShader>("DepthShadowsRaytracer");

        if (_RestirGlobalIllumRayTracingShaderAccelerated == null)
            _RestirGlobalIllumRayTracingShaderAccelerated = Resources.Load<RayTracingShader>("ReStirGlobalIllumination");
        if (_RestirSpatialShaderAccelerated == null)
            _RestirSpatialShaderAccelerated = Resources.Load<RayTracingShader>("ReSTIRSpatialReuse");

        //Create GPU accelerated structure.
        var settings = new RayTracingAccelerationStructure.RASSettings();
        //settings.layerMask = RayTracingLayers;
        //Change this to manual after some work.
        settings.managementMode = RayTracingAccelerationStructure.ManagementMode.Manual;
        settings.rayTracingModeMask = RayTracingAccelerationStructure.RayTracingModeMask.Everything;

        _AccelerationStructure = new RayTracingAccelerationStructure(settings);

    }

    // Update is called once per frame
    void LateUpdate()
    {
        Matrix4x4 VP = GL.GetGPUProjectionMatrix(secondCam.projectionMatrix, true) * Camera.main.worldToCameraMatrix;
        Shader.SetGlobalMatrix("_Perspective_Matrix_VP", VP);
        //Shader.SetGlobalInt("_UnigmaFrameCount", _UnigmaFrameCount);
        lightList.Clear();
        AddLightsToList();

        Debug.Log("amount of lights: " + lightList.Count);
        
        lightsBuffer.SetData(lightList);

        foreach (Renderer r in _rayTracedObjects)
        {
            _AccelerationStructure.UpdateInstanceTransform(r);
        }

        if (BuffersReady == false)
        {
            BuffersReady = true;
            return;
        }
        if (buffersAdded < 1)
        {
            //Create isometric depth normals.
            _OutlineRenderObjects = new List<UnigmaPostProcessingObjects>();
            _OutlineNullObjects = new List<Renderer>();
            FindObjects("IsometricDepthNormalObject");
            AddObjectsToList();
            
            AddObjectsToAccerleration();


            CreateDepthShadowBuffers();
            CreateDepthNormalBuffers();
            buffersAdded += 1;
            /*
            _RestirGlobalIllumRayTracingShaderAccelerated.SetBuffer("_UnigmaDispatchInfo", unigmaDispatchInfoBuffer);
            unigmaDispatchInfoComputeShader.SetBuffer(0, "_UnigmaDispatchInfo", unigmaDispatchInfoBuffer);
            _RestirGlobalIllumRayTracingShaderAccelerated.SetBuffer("_samples", samplesBuffer);
            _RestirGlobalIllumRayTracingShaderAccelerated.SetBuffer("_unigmaLights", lightsBuffer);
            _RestirGlobalIllumRayTracingShaderAccelerated.SetBuffer("_reservoirs", reservoirsBuffer);
            _DepthShadowsRayTracingShaderAccelerated.SetBuffer("_Samples", samplesBuffer);
            */
        }

        if (buffersAdded < 2)
        {
            CreateOutlineColorBuffers();
            buffersAdded += 1;
        }
        //UpdateRayTracer();
        
    }


    void AddObjectsToList()
    {
        foreach (var obj in FindObjectsOfType<Renderer>())
        {
            //Check if object in the RaytracingLayers.
            if (((1 << obj.gameObject.layer) & RayTracingLayers) != 0)
            {
                _rayTracedObjects.Add(obj);
                //Debug.Log(obj.name);
            }
        }
    }

    void AddLightsToList()
    {
        int index = 0;
        foreach (Renderer obj in FindObjectsOfType<Renderer>())
        {
            
            //Check if object in the RaytracingLayers.
            if (((1 << obj.gameObject.layer) & RayTracingLayers) != 0)
            {
                if (!obj.material.HasFloat("_Emmittance"))
                    continue;
                if (obj.material.GetFloat("_Emmittance") > 0.01)
                {
                    UnigmaLight ulight = new UnigmaLight();
                    ulight.position = obj.GetComponent<BoxCollider>().bounds.min;
                    ulight.emission = obj.material.GetFloat("_Emmittance");
                    ulight.area = obj.transform.localScale;
                    Vector4 color = obj.material.GetColor("_Midtone"); 
                    ulight.color = new Vector3(color.x, color.y, color.z);
                    Debug.Log("Light: " + index + " : " + obj.name);
                    index += 1;
                    lightList.Add(ulight);
                }
            }
        }
        /*
        //Add spherical map.
        int width = SphericalMap.width;
        int height = SphericalMap.height;
        for (int i = 0; i < width; i++)
            for (int j = 0; j < height; j++)
            {
                float u = (float)i / (float)width;
                float v = (float)j / (float)height;
                UnigmaLight ulight = new UnigmaLight();
                Vector4 color = SphericalMap.GetPixel(i, j);
                ulight.color = new Vector3(color.x, color.y, color.z);
                ulight.position = SphericalMapping(new Vector2(u, v), 10);
                ulight.emission = Vector3.Magnitude(color);
                ulight.area = new Vector3(1, 1, 1);
                lightList.Add(ulight);
            }

        //Debug Light List
        for (int i = 0; i < lightList.Count; i++)
        {
            Debug.Log("Light: " + i + " : " + lightList[i].position);
        }
        */
    }

    void AddObjectsToAccerleration()
    {
        uint index = 0;
        foreach (Renderer r in _rayTracedObjects)
        {
            _AccelerationStructure.AddInstance(r, id:index);
            _AccelerationStructure.UpdateInstanceTransform(r); 
            index++;
        }
    }

    void CreateDepthShadowBuffers()
    {
        CommandBuffer depthShadowsCommandBuffer = new CommandBuffer();
        depthShadowsCommandBuffer.name = "DepthShadowsBuffer";
        depthShadowsCommandBuffer.SetRayTracingBufferParam(_RestirGlobalIllumRayTracingShaderAccelerated, "_UnigmaDispatchInfo", unigmaDispatchInfoBuffer);
        depthShadowsCommandBuffer.SetComputeBufferParam(unigmaDispatchInfoComputeShader, 0, "_UnigmaDispatchInfo", unigmaDispatchInfoBuffer);

        depthShadowsCommandBuffer.BuildRayTracingAccelerationStructure(_AccelerationStructure);

        depthShadowsCommandBuffer.SetGlobalTexture("_UnigmaDepthShadowsMap", _DepthShadowsTexture);
        depthShadowsCommandBuffer.SetRenderTarget(_DepthShadowsTexture);
        depthShadowsCommandBuffer.ClearRenderTarget(true, true, new Vector4(0,0,0,0));
        depthShadowsCommandBuffer.SetRayTracingTextureParam(_DepthShadowsRayTracingShaderAccelerated, "_UnigmaDepthShadowsMap", _DepthShadowsTexture);
        //depthShadowsCommandBuffer.SetBufferData(lightsBuffer, lightList);

        depthShadowsCommandBuffer.SetRayTracingShaderPass(_DepthShadowsRayTracingShaderAccelerated, "DepthShadowsRaytracingShaderPass");
        depthShadowsCommandBuffer.SetRayTracingBufferParam(_DepthShadowsRayTracingShaderAccelerated, "_Samples", samplesBuffer);
        depthShadowsCommandBuffer.SetRayTracingAccelerationStructure(_DepthShadowsRayTracingShaderAccelerated, "_RaytracingAccelerationStructure", _AccelerationStructure);
        depthShadowsCommandBuffer.DispatchRays(_DepthShadowsRayTracingShaderAccelerated, "DepthShadowsRaygenShader", (uint)Screen.width, (uint)Screen.height, 1);

        depthShadowsCommandBuffer.SetGlobalTexture("_UnigmaGlobalIllumination", _UnigmaGlobalIllumination);
        depthShadowsCommandBuffer.SetRenderTarget(_UnigmaGlobalIllumination);
        //depthShadowsCommandBuffer.ClearRenderTarget(true, true, new Vector4(0, 0, 0, 0));

        //First ReSTIR pass.
        depthShadowsCommandBuffer.SetRayTracingBufferParam(_RestirGlobalIllumRayTracingShaderAccelerated, "_samples", samplesBuffer);
        depthShadowsCommandBuffer.SetRayTracingBufferParam(_RestirGlobalIllumRayTracingShaderAccelerated, "_unigmaLights", lightsBuffer);
        depthShadowsCommandBuffer.SetRayTracingBufferParam(_RestirGlobalIllumRayTracingShaderAccelerated, "_reservoirs", reservoirsBuffer);
        depthShadowsCommandBuffer.SetRayTracingBufferParam(_RestirGlobalIllumRayTracingShaderAccelerated, "_reservoirPaths", reservoirPathsBuffer);

        depthShadowsCommandBuffer.SetRayTracingIntParam(_RestirGlobalIllumRayTracingShaderAccelerated, "_NumberOfLights", lightList.Count);
        depthShadowsCommandBuffer.SetRayTracingIntParam(_RestirGlobalIllumRayTracingShaderAccelerated, "_TemporalReservoirsCount", _temporalReservoirsCount);
        depthShadowsCommandBuffer.SetRayTracingTextureParam(_RestirGlobalIllumRayTracingShaderAccelerated, "_GlobalIllumination", _DepthShadowsTexture);
        depthShadowsCommandBuffer.SetRayTracingTextureParam(_RestirGlobalIllumRayTracingShaderAccelerated, "_CameraMotionVectorsTextureReSTIR", Shader.GetGlobalTexture("_CameraMotionVectorsTexture"));

        //depthShadowsCommandBuffer.SetRayTracingIntParam(_RestirGlobalIllumRayTracingShaderAccelerated, "_UnigmaFrameCount", _UnigmaFrameCount);
        depthShadowsCommandBuffer.SetRayTracingAccelerationStructure(_RestirGlobalIllumRayTracingShaderAccelerated, "_RaytracingAccelerationStructure", _AccelerationStructure);
        depthShadowsCommandBuffer.SetRayTracingShaderPass(_RestirGlobalIllumRayTracingShaderAccelerated, "GlobalIlluminationRaytracingShaderPass");
        

        //Reusepass
        depthShadowsCommandBuffer.SetRayTracingBufferParam(_RestirSpatialShaderAccelerated, "_samples", samplesBuffer);
        depthShadowsCommandBuffer.SetRayTracingBufferParam(_RestirSpatialShaderAccelerated, "_unigmaLights", lightsBuffer);
        depthShadowsCommandBuffer.SetRayTracingBufferParam(_RestirSpatialShaderAccelerated, "_reservoirPaths", reservoirPathsBuffer);

        depthShadowsCommandBuffer.SetRayTracingIntParam(_RestirSpatialShaderAccelerated, "_NumberOfLights", lightList.Count);
        depthShadowsCommandBuffer.SetRayTracingIntParam(_RestirSpatialShaderAccelerated, "_TemporalReservoirsCount", _temporalReservoirsCount);
        depthShadowsCommandBuffer.SetRayTracingTextureParam(_RestirSpatialShaderAccelerated, "_GlobalIllumination", _DepthShadowsTexture);
        depthShadowsCommandBuffer.SetRayTracingTextureParam(_RestirSpatialShaderAccelerated, "_CameraMotionVectorsTextureReSTIR", Shader.GetGlobalTexture("_CameraMotionVectorsTexture"));

        //depthShadowsCommandBuffer.SetRayTracingIntParam(_RestirGlobalIllumRayTracingShaderAccelerated, "_UnigmaFrameCount", _UnigmaFrameCount);
        depthShadowsCommandBuffer.SetRayTracingAccelerationStructure(_RestirSpatialShaderAccelerated, "_RaytracingAccelerationStructure", _AccelerationStructure);
        depthShadowsCommandBuffer.SetRayTracingShaderPass(_RestirSpatialShaderAccelerated, "GlobalIlluminationRaytracingShaderPass");

        
        //Dispatch
        int passCount = 1;
        for (int i = 0; i < passCount; i++)
        {
            depthShadowsCommandBuffer.SetRayTracingIntParam(_RestirGlobalIllumRayTracingShaderAccelerated, "_PassCount", i);
            depthShadowsCommandBuffer.SetRayTracingIntParam(_RestirSpatialShaderAccelerated, "_PassCount", i);
            depthShadowsCommandBuffer.DispatchRays(_RestirGlobalIllumRayTracingShaderAccelerated, "RestirGlobalIllumantionRayGen", (uint)Screen.width, (uint)Screen.height, 1);
            depthShadowsCommandBuffer.DispatchRays(_RestirSpatialShaderAccelerated, "RestirGlobalIllumantionRayGen", (uint)Screen.width, (uint)Screen.height, 1);
        }
        //depthShadowsCommandBuffer.DispatchCompute(unigmaDispatchInfoComputeShader, 0, 1, 1, 1);
        GetComponent<Camera>().AddCommandBuffer(CameraEvent.AfterForwardOpaque, depthShadowsCommandBuffer);


    }

    void UpdateRayTracer()
    {

        _AccelerationStructure.Build();
        Shader.SetGlobalTexture("_UnigmaDepthShadowsMap", _DepthShadowsTexture);
        RenderTexture.active = _DepthShadowsTexture;
        GL.Clear(true, true, new Vector4(0, 0, 0, 0));
        _DepthShadowsRayTracingShaderAccelerated.SetTexture("_UnigmaDepthShadowsMap", _DepthShadowsTexture);
        _DepthShadowsRayTracingShaderAccelerated.SetShaderPass("DepthShadowsRaytracingShaderPass");
        _DepthShadowsRayTracingShaderAccelerated.SetAccelerationStructure("_RaytracingAccelerationStructure", _AccelerationStructure);
        _DepthShadowsRayTracingShaderAccelerated.Dispatch("DepthShadowsRaygenShader", (int)Screen.width, (int)Screen.height, 1);

        Shader.SetGlobalTexture("_UnigmaGlobalIllumination", _UnigmaGlobalIllumination);
        RenderTexture.active = _UnigmaGlobalIllumination;



        _RestirGlobalIllumRayTracingShaderAccelerated.SetInt("_NumberOfLights", lightList.Count);
        _RestirGlobalIllumRayTracingShaderAccelerated.SetInt("_TemporalReservoirsCount", _temporalReservoirsCount);

        _RestirGlobalIllumRayTracingShaderAccelerated.SetTexture("_GlobalIllumination", _DepthShadowsTexture);

        _RestirGlobalIllumRayTracingShaderAccelerated.SetAccelerationStructure("_RaytracingAccelerationStructure", _AccelerationStructure);
        _RestirGlobalIllumRayTracingShaderAccelerated.SetShaderPass("GlobalIlluminationRaytracingShaderPass");
        _RestirGlobalIllumRayTracingShaderAccelerated.Dispatch("RestirGlobalIllumantionRayGen", (int)Screen.width, (int)Screen.height, 1);

        unigmaDispatchInfoComputeShader.Dispatch(0, 1, 1, 1);

        _UnigmaFrameCount += 1;
        _RestirGlobalIllumRayTracingShaderAccelerated.SetInt("_UnigmaFrameCount", _UnigmaFrameCount);
    }

    private void OnPostRender()
    {

        //depthShadowsCommandBuffer.SetRayTracingBufferParam(_RestirGlobalIllumRayTracingShaderAccelerated, "_UnigmaDispatchInfo", unigmaDispatchInfoBuffer);
        //depthShadowsCommandBuffer.SetComputeBufferParam(unigmaDispatchInfoComputeShader, 0, "_UnigmaDispatchInfo", unigmaDispatchInfoBuffer);

        //depthShadowsCommandBuffer.BuildRayTracingAccelerationStructure(_AccelerationStructure);

        //depthShadowsCommandBuffer.SetGlobalTexture("_UnigmaDepthShadowsMap", _DepthShadowsTexture);
        //depthShadowsCommandBuffer.SetRenderTarget(_DepthShadowsTexture);
        //depthShadowsCommandBuffer.ClearRenderTarget(true, true, new Vector4(0, 0, 0, 0));
        //depthShadowsCommandBuffer.SetRayTracingTextureParam(_DepthShadowsRayTracingShaderAccelerated, "_UnigmaDepthShadowsMap", _DepthShadowsTexture);


        //depthShadowsCommandBuffer.SetRayTracingShaderPass(_DepthShadowsRayTracingShaderAccelerated, "DepthShadowsRaytracingShaderPass");
        //depthShadowsCommandBuffer.SetRayTracingBufferParam(_DepthShadowsRayTracingShaderAccelerated, "_Samples", samplesBuffer);
        //depthShadowsCommandBuffer.SetRayTracingAccelerationStructure(_DepthShadowsRayTracingShaderAccelerated, "_RaytracingAccelerationStructure", _AccelerationStructure);
        //depthShadowsCommandBuffer.DispatchRays(_DepthShadowsRayTracingShaderAccelerated, "DepthShadowsRaygenShader", (uint)Screen.width, (uint)Screen.height, 1);

        //depthShadowsCommandBuffer.SetGlobalTexture("_UnigmaGlobalIllumination", _UnigmaGlobalIllumination);
        //depthShadowsCommandBuffer.SetRenderTarget(_UnigmaGlobalIllumination);
        //depthShadowsCommandBuffer.ClearRenderTarget(true, true, new Vector4(0, 0, 0, 0));
        //depthShadowsCommandBuffer.SetRayTracingBufferParam(_RestirGlobalIllumRayTracingShaderAccelerated, "_samples", samplesBuffer);
        //depthShadowsCommandBuffer.SetRayTracingBufferParam(_RestirGlobalIllumRayTracingShaderAccelerated, "_unigmaLights", lightsBuffer);
        //depthShadowsCommandBuffer.SetRayTracingBufferParam(_RestirGlobalIllumRayTracingShaderAccelerated, "_reservoirs", reservoirsBuffer);

        //depthShadowsCommandBuffer.SetRayTracingIntParam(_RestirGlobalIllumRayTracingShaderAccelerated, "_NumberOfLights", lightList.Count);
        //depthShadowsCommandBuffer.SetRayTracingIntParam(_RestirGlobalIllumRayTracingShaderAccelerated, "_TemporalReservoirsCount", _temporalReservoirsCount);
        //depthShadowsCommandBuffer.SetRayTracingTextureParam(_RestirGlobalIllumRayTracingShaderAccelerated, "_GlobalIllumination", _DepthShadowsTexture);

        //depthShadowsCommandBuffer.SetRayTracingIntParam(_RestirGlobalIllumRayTracingShaderAccelerated, "_UnigmaFrameCount", _UnigmaFrameCount);
        //depthShadowsCommandBuffer.SetRayTracingAccelerationStructure(_RestirGlobalIllumRayTracingShaderAccelerated, "_RaytracingAccelerationStructure", _AccelerationStructure);
        //depthShadowsCommandBuffer.SetRayTracingShaderPass(_RestirGlobalIllumRayTracingShaderAccelerated, "GlobalIlluminationRaytracingShaderPass");
        //depthShadowsCommandBuffer.DispatchRays(_RestirGlobalIllumRayTracingShaderAccelerated, "RestirGlobalIllumantionRayGen", (uint)Screen.width, (uint)Screen.height, 1);
        //depthShadowsCommandBuffer.DispatchCompute(unigmaDispatchInfoComputeShader, 0, 1, 1, 1);
        /*
        _UnigmaFrameCount += 1;
        if (_UnigmaFrameCount > int.MaxValue)
            _UnigmaFrameCount = 0;
        Shader.SetGlobalInt("_UnigmaFrameCount", _UnigmaFrameCount);
        Debug.Log(_UnigmaFrameCount);
        */
    }

    void CreateDepthNormalBuffers()
    {
        CommandBuffer outlineDepthBuffer = new CommandBuffer();
        outlineDepthBuffer.name = "OutlineDepthBuffer";
        RenderTexture rt = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
        RenderTexture posRT = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
        outlineDepthBuffer.SetGlobalTexture("_IsometricDepthNormal", rt);
        outlineDepthBuffer.SetGlobalTexture("_IsometricPositions", posRT);

        outlineDepthBuffer.SetRenderTarget(rt);

        outlineDepthBuffer.ClearRenderTarget(true, true, Vector4.zero);
        DrawIsometricDepthNormals(outlineDepthBuffer, 0);

        //Second pass
        outlineDepthBuffer.SetRenderTarget(posRT);
        outlineDepthBuffer.ClearRenderTarget(true, true, Vector4.zero);
        DrawIsometricDepthNormals(outlineDepthBuffer, 1);
        GetComponent<Camera>().AddCommandBuffer(CameraEvent.AfterForwardOpaque, outlineDepthBuffer);

    }

    void CreateOutlineColorBuffers()
    {
        CommandBuffer outlineColorBuffer = new CommandBuffer();
        outlineColorBuffer.name = "OutlineColorBuffer";
        RenderTexture rt = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
        RenderTexture innerRT = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
        RenderTexture tempRt = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
        rt.enableRandomWrite = true;
        tempRt.enableRandomWrite = true;
        outlineColorBuffer.SetGlobalTexture("_IsometricOutlineColor", rt);
        outlineColorBuffer.SetGlobalTexture("_IsometricInnerOutlineColor", innerRT);

        outlineColorBuffer.SetRenderTarget(rt);

        outlineColorBuffer.ClearRenderTarget(true, true, Vector4.zero);

        //Set outter colors
        DrawIsometricOutlineColor(outlineColorBuffer, 0);

        outlineColorBuffer.CopyTexture(rt, tempRt);
        computeOutlineColors.SetTexture(0, "_IsometricOutlineColor", rt);
        computeOutlineColors.SetTexture(0, "_TempTexture", tempRt);
        outlineColorBuffer.DispatchCompute(computeOutlineColors, 0, Mathf.CeilToInt(Screen.width / 8), Mathf.CeilToInt(Screen.height / 8), 1);
        outlineColorBuffer.CopyTexture(tempRt, rt);

        //Now set inner colors.
        outlineColorBuffer.SetRenderTarget(innerRT);
        outlineColorBuffer.ClearRenderTarget(true, true, Vector4.zero);
        DrawIsometricOutlineColor(outlineColorBuffer, 1);
        GetComponent<Camera>().AddCommandBuffer(CameraEvent.AfterForwardOpaque, outlineColorBuffer);
    }

    void DrawIsometricDepthNormals(CommandBuffer outlineDepthBuffer, int pass)
    {
        foreach (UnigmaPostProcessingObjects r in _OutlineRenderObjects)
        {
            IsometricDepthNormalObject iso = r.gameObject.GetComponent<IsometricDepthNormalObject>();
            if (iso != null)
                if (r.materials.ContainsKey("IsometricDepthNormals") && r.renderer.enabled == true && iso._writeToTexture)
                    outlineDepthBuffer.DrawRenderer(r.renderer, r.materials["IsometricDepthNormals"], 0, pass);
        }

        foreach (Renderer r in _OutlineNullObjects)
        {
            if (r.enabled == true)
                outlineDepthBuffer.DrawRenderer(r, _nullMaterial, 0, -1);
        }
    }

    void DrawIsometricOutlineColor(CommandBuffer outlineColor, int pass)
    {
        foreach (UnigmaPostProcessingObjects r in _OutlineRenderObjects)
        {
            OutlineColor cr = r.gameObject.GetComponent<OutlineColor>();
            if (cr != null)
                if (cr.materials.ContainsKey("OutlineColors") && cr.renderer.enabled == true)
                    outlineColor.DrawRenderer(cr.renderer, cr.materials["OutlineColors"], 0, pass);
        }

        foreach (Renderer r in _OutlineNullObjects)
        {
            if (r.enabled == true)
                outlineColor.DrawRenderer(r, _nullMaterial, 0, -1);
        }
    }

    // Find and store visible renderers to a list
    void FindObjects(string component)
    {
        // Retrieve all renderers in scene
        Renderer[] sceneRenderers = FindObjectsOfType<Renderer>();

        // Store only visible renderers
        _OutlineRenderObjects.Clear();
        _OutlineNullObjects.Clear();
        for (int i = 0; i < sceneRenderers.Length; i++)
            if (sceneRenderers[i].GetComponent(component))
                _OutlineRenderObjects.Add(sceneRenderers[i].gameObject.GetComponent(component) as UnigmaPostProcessingObjects);
            else
                _OutlineNullObjects.Add(sceneRenderers[i]);
    }

    //Release all buffers and memory
    void ReleaseBuffers()
    {
        Debug.Log("Buffers Released");
        if (samplesBuffer != null)
            samplesBuffer.Release();
        if (lightsBuffer != null)
            lightsBuffer.Release();
        if (reservoirsBuffer != null)
            reservoirsBuffer.Release();
        if (unigmaDispatchInfoBuffer != null)
            unigmaDispatchInfoBuffer.Release();
        CommandBuffer[] buffers = mainCam.GetCommandBuffers(CameraEvent.AfterForwardOpaque);
        foreach (CommandBuffer buffer in buffers)
        {
            buffer.Release();
        }

        _UnigmaGlobalIllumination.Release();

    }

    Vector3 SphericalMapping(Vector2 uv, float radius)
    {
        float theta = 2 * Mathf.PI * uv.x;
        float phi = Mathf.PI * uv.y;

        float x = Mathf.Cos(theta) * Mathf.Sin(phi) * radius;
        float y = Mathf.Sin(theta) * Mathf.Sin(phi) * radius;
        float z = -Mathf.Cos(phi) * radius;

        return new Vector3(x,y,z);
    }

    private void OnDrawGizmos()
    {

        int width = SphericalMap.width;
        int height = SphericalMap.height;
        for (int i = 0; i < width; i++)
            for (int j = 0; j < height; j++)
            {
                float u = (float)i / (float)width;
                float v = (float)j / (float)height;
                Gizmos.color = SphericalMap.GetPixel(i, j);
                Gizmos.DrawSphere(SphericalMapping(new Vector2(u, v), 10), 1f);
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
}
