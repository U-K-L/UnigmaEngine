using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerspectiveCameraLerp : MonoBehaviour
{
    // Start is called before the first frame update
    private Matrix4x4 ortho;
    private Matrix4x4 perspective;

    public bool isOrtho = true;
    private float aspectRatio;
    private float orthographicSize;
    public Camera cam;
    public Camera perpCam;
    private bool transitioning = false;
    public Vector3 perspectiveOffsets;
    private Vector3 originalPosition;
    public Camera _childCam;

    public float durspeed = 1f;
    void Start()
    {
        originalPosition = transform.localPosition;
        orthographicSize = cam.orthographicSize;
        aspectRatio = cam.aspect;
    }

    private void Awake()
    {

        aspectRatio = Screen.width / Screen.height;
        perspective = perpCam.projectionMatrix;//Matrix4x4.Perspective(60, aspectRatio, 0.01f, 1000);
        ortho = cam.projectionMatrix;//Matrix4x4.Ortho(-orthographicSize * aspectRatio, orthographicSize * aspectRatio, -orthographicSize, orthographicSize, near, far);
        isOrtho = true;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        //SynchronizeCams();
        if (Input.GetKeyDown(KeyCode.A))
        {
            BeginTransition(durspeed);
        }
        if(!isOrtho&&transitioning)
            updateProjectionWarp();
        //else if (!isOrtho)
            //updateProjectionWarpStatic();
    }

    public void BeginTransition(float duration)
    {
        isOrtho = !isOrtho;
        if (isOrtho)
            BlendToMatrix(ortho, duration);
        else
        {
            transitioning = true;
            BlendToMatrix(perspective, duration);

        }
    }

    void updateProjectionWarp()
    {
        Matrix4x4 p = cam.projectionMatrix;
        p.m01 += Mathf.Sin(Time.time * 3.2F) * 0.0125F;
        p.m10 += Mathf.Abs(Mathf.Cos(Time.time * 1.5F)) * 0.0125F;
        p.m00 +=  Mathf.Abs(Mathf.Sin(Time.time * 2.5F)) * 0.295F;
        p.m32 +=  Mathf.Cos(Time.time * 2.5F) * 0.0225F;
        p.m33 +=  Mathf.Cos(Time.time * 1.5F) * 0.0125F;
        cam.projectionMatrix = p;
        //cam.transform.localPosition = perspectiveOffsets;
    }

    void updateProjectionWarpStatic()
    {
        Matrix4x4 p = perspective;
        p.m01 += Mathf.Sin(Time.time * 1.2F) * 0.0125F;
        p.m10 += 0.005f * Mathf.Abs(Mathf.Cos(Time.time * 1.5F)) * 0.0125F;
        p.m00 += 0.005f * Mathf.Abs(Mathf.Sin(Time.time * 1.5F)) * 0.0125F;
        p.m32 += 0.005f * Mathf.Cos(Time.time * 1.5F) * 0.0125F;
        p.m33 += 0.005f *Mathf.Cos(Time.time * 1.5F) * 0.0125F;
        cam.projectionMatrix = p;
        //cam.transform.localPosition = perspectiveOffsets;
    }

    static Matrix4x4 MatrixLerp(Matrix4x4 from, Matrix4x4 to, float time)
    {
        Matrix4x4 ret = new Matrix4x4();
        for (int i = 0; i < 16; i++)
        {
            ret[i] = Mathf.Lerp(from[i], to[i], time);
        }
        return ret;

    }

    private IEnumerator LerpFromTo(Matrix4x4 source, Matrix4x4 destination, float duration)
    {
        float startTime = Time.time;
        while (Time.time - startTime < duration)
        {
            cam.projectionMatrix = MatrixLerp(source, destination, (Time.time - startTime) / duration);
            yield return null;
        }
        transitioning = false;
        cam.projectionMatrix = destination;

        cam.orthographic = isOrtho;
        cam.nearClipPlane = 0.01f;
        cam.ResetProjectionMatrix();
        if (isOrtho)
        {

            cam.transform.localPosition = originalPosition;
        }


    }

    public Coroutine BlendToMatrix(Matrix4x4 target, float duration)
    {
        StopAllCoroutines();
        return StartCoroutine(LerpFromTo(cam.projectionMatrix, target, duration));
    }

    void SynchronizeCams()
    {
        _childCam.orthographicSize = cam.orthographicSize;
        _childCam.projectionMatrix = cam.projectionMatrix;
        _childCam.orthographic = cam.orthographic;
        _childCam.fieldOfView = cam.fieldOfView;
        _childCam.nearClipPlane = cam.nearClipPlane;
        _childCam.ResetProjectionMatrix();
    }

}
