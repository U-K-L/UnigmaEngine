using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayTracer : MonoBehaviour
{
    public ComputeShader RayTracingShader;
    private RenderTexture _target;
    private Camera _cam;
    public Texture skyBox;
    void Awake()
    {
        _cam = GetComponent<Camera>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        InitializeRenderTexture();
        RayTracingShader.SetTexture(0, "_RayTracer", _target);
        RayTracingShader.SetMatrix("_CameraToWorld", _cam.cameraToWorldMatrix);
        RayTracingShader.SetMatrix("_CameraInverseProjection", _cam.projectionMatrix.inverse);
        RayTracingShader.SetTexture(0, "_SkyBoxTexture", skyBox);
        int threadGroupsX = Mathf.CeilToInt(Screen.width / 32.0f);
        int threadGroupsY = Mathf.CeilToInt(Screen.height / 32.0f);
        RayTracingShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);

        Graphics.Blit(_target, destination);
    }

    void InitializeRenderTexture()
    {
        if (_target == null || _target.width != Screen.width || _target.height != Screen.height)
        {
            if (_target != null)
                _target.Release();
            RenderTexture rt = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            _target = rt;
            _target.enableRandomWrite = true;
            _target.Create();
        }
    }
}
