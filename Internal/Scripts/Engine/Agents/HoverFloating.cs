using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HoverFloating : MonoBehaviour
{
    // Start is called before the first frame update
    [Range(0.0f, 10.0f)] public float Hover = 1.0f;
    [Range(0.0f, 10.0f)] public float Omega = 1.0f;

    private Vector3 hoverCenter;
    private Quaternion hoverRot;
    private float hoverPhase;
    private Rigidbody body;
    void Start()
    {
        hoverCenter = transform.position;
        hoverRot = transform.rotation;

        Random.InitState((int)transform.position.z*100);
        hoverPhase = Random.value * 1000.0f;
        body = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        hoverPhase += Omega * Time.deltaTime;
        Vector3 hoverVec =
            0.05f * Mathf.Sin(1.37f * hoverPhase) * Vector3.right
          + 0.05f * Mathf.Sin(1.93f * hoverPhase + 1.234f) * Vector3.forward
          + 0.04f * Mathf.Sin(0.97f * hoverPhase + 4.321f) * Vector3.up;
        hoverVec *= Hover;
        Quaternion hoverQuat = Quaternion.FromToRotation(Vector3.up, hoverVec + Vector3.up);
        body.velocity += hoverVec;
        transform.rotation = hoverRot * hoverQuat;
    }
}
