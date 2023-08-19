using RayFire;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

public class FluidSimulationManager : MonoBehaviour
{
    ComputeShader _fluidSimulationCompute;
    ComputeBuffer _textureBuffer;
    ComputeBuffer _fluidSimParticles;
    int numParticles;
    Camera _cam;
    RenderTexture _rtTarget;
    RenderTexture _tempTarget;
    RenderTexture _fluidNormalBuffer;
    RenderTexture _fluidDepthBuffer;

    Shader _fluidNormalShader;
    Shader _fluidDepthShader;
    Shader _fluidCompositeShader;
    Material _fluidSimMaterialDepthHori;
    Material _fluidSimMaterialDepthVert;
    Material _fluidSimMaterialNormal;
    Material _fluidSimMaterialComposite;
    public Transform fluidSimTransform;

    public Color DeepWaterColor = Color.white;
    public Color ShallowWaterColor = Color.white;
    public float DepthMaxDistance = 100;
    public Vector2 BlurScale;
    public float BlurFallOff = 0.25f;
    public float BlurRadius = 5.0f;
    private void Awake()
    {
        _fluidSimulationCompute = Resources.Load<ComputeShader>("FluidSimCompute");
        
        _fluidNormalShader = Resources.Load<Shader>("FluidNormalBuffer");
        _fluidDepthShader = Resources.Load<Shader>("FluidBilateralFilter");
        _fluidCompositeShader = Resources.Load<Shader>("FluidComposition");
        
        //Create the material for the fluid simulation.
        _fluidSimMaterialDepthHori = new Material(_fluidDepthShader);
        _fluidSimMaterialDepthVert = new Material(_fluidDepthShader);
        _fluidSimMaterialNormal = new Material(_fluidNormalShader);
        _fluidSimMaterialComposite = new Material(_fluidCompositeShader);


        _cam = Camera.main;
        //Create the texture for compute shader.
        _rtTarget = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        _tempTarget = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        _fluidNormalBuffer = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        _fluidDepthBuffer = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);


        _rtTarget.enableRandomWrite = true;
        _rtTarget.Create();
        _fluidSimulationCompute.SetTexture(0, "Result", _rtTarget);
        CreateFluidCommandBuffers();


    }

    void Update()
    {
        //Draw mesh instantiaonation.
        //Graphics.DrawMeshInstancedIndirect()

    }

    //Temporarily attach this simulation to camera!!!
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        _fluidSimulationCompute.SetMatrix("_CameraToWorld", _cam.cameraToWorldMatrix);
        _fluidSimulationCompute.SetMatrix("_CameraInverseProjection", _cam.projectionMatrix.inverse);
        _fluidSimulationCompute.SetMatrix("_ParentTransform", fluidSimTransform.localToWorldMatrix);


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
        _fluidSimulationCompute.GetKernelThreadGroupSizes(0, out threadsX, out threadsY, out threadsZ);

        fluidCommandBuffers.SetComputeTextureParam(_fluidSimulationCompute, 0, "Result", _rtTarget);
        fluidCommandBuffers.DispatchCompute(_fluidSimulationCompute, 0, Mathf.CeilToInt(Screen.width / threadsX), Mathf.CeilToInt(Screen.width / threadsY), (int)threadsZ);

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

}
