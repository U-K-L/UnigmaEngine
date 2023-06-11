using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IsometricDepthNormalObject : UnigmaPostProcessingObjects
{
    public float _fadeThreshold = 100000;
    public float _normalAmp = 1;
    public float _depthAmp = 1;
    private void Start()
    {
        Material mat = Resources.Load("Materials/IsometricDepthNormals/IsometricDepthNormals") as Material;
        material = Instantiate(mat);
        material.name = material.name + " " + gameObject.name;
        renderer = GetComponent<Renderer>();

        //Add to this component to all children
        foreach (Transform child in transform)
        {
            child.gameObject.AddComponent<IsometricDepthNormalObject>();
        }
    }

    private void Update()
    {
        Debug.Log("threshold: " + _fadeThreshold);
        material.SetFloat("_Fade", _fadeThreshold);
        material.SetFloat("_NormalAmount", _normalAmp);
        material.SetFloat("_DepthAmount", _depthAmp);
    }
}
