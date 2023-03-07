using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShockWave : MonoBehaviour
{
    public Material material;
    public Transform target;
    public float velocity;
    float vel;
    public float effectSize;
    public float distance;

    enum StateMachine{
        Shrinking,
        Expanding,
        Halt
    }

    StateMachine state;
    Camera cam;
    // Start is called before the first frame update
    void Start()
    {
        vel = velocity;
        state = StateMachine.Shrinking;
        cam = GetComponent<Camera>();
        cam.depthTextureMode |= DepthTextureMode.Depth;
        cam.depthTextureMode |= DepthTextureMode.DepthNormals;
        material.SetFloat("_WaveDistance", 0);
        distance = 0;

    }

    // Update is called once per frame
    void Update()
    {
        if(state != StateMachine.Halt)
            updateShockWave();
    }

    void updateShockWave()
    {
        float dt = vel*Time.deltaTime;
        if(state == StateMachine.Shrinking)
        {
            distance = Mathf.Lerp(distance, -effectSize, dt);
            if (distance < -effectSize * 0.7f)
            {
                state = StateMachine.Expanding;
                distance = 0.8f;
            }


        }
        else if(state == StateMachine.Expanding)
        {
            distance = Mathf.Lerp(distance, effectSize, dt);
            if (distance > effectSize * 0.9f)
                state = StateMachine.Halt;
        }


        material.SetFloat("_WaveDistance", distance);
        vel += vel * vel * 0.0001f;
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        var projectionMatrix = GL.GetGPUProjectionMatrix(cam.projectionMatrix, false);
        material.SetMatrix("unity_ViewToWorldMatrix", cam.cameraToWorldMatrix);
        material.SetMatrix("unity_InverseProjectionMatrix", projectionMatrix.inverse);



        material.SetVector("_Center", new Vector4(target.position.x, target.position.y, target.position.z, 0.0f));
        Graphics.Blit(source, destination, material);
    }
}
