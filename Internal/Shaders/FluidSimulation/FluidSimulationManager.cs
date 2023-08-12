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
    public Material ShaderMaterial;
    public Transform fluidSimTransform;

    private void Awake()
    {
        _fluidSimulationCompute = Resources.Load<ComputeShader>("FluidSimCompute");
        _cam = Camera.main;
        //Create the texture for compute shader.
        _rtTarget = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        
        
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


        //uint threadsX, threadsY, threadsZ;
        //_fluidSimulationCompute.GetKernelThreadGroupSizes(0, out threadsX, out threadsY, out threadsZ);
        //_fluidSimulationCompute.Dispatch(0, Mathf.CeilToInt(Screen.width / threadsX), Mathf.CeilToInt(Screen.width / threadsY), (int)threadsZ);

        Graphics.Blit(source, destination, ShaderMaterial);
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

        GetComponent<Camera>().AddCommandBuffer(CameraEvent.AfterForwardOpaque, fluidCommandBuffers);

    }

}
