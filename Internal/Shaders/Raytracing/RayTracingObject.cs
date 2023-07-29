using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayTracingObject : MonoBehaviour
{
    public Color color = Vector4.one;
    public float emission = 0.25f;
    public float smoothness = 0.05f;
    public float transparency = 1.0f;
    public float absorbtion = 0.0f;
    public float celShaded = 0f;
}
