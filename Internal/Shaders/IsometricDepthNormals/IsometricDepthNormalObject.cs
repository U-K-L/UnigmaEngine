using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IsometricDepthNormalObject : UnigmaPostProcessingObjects
{
    [Header("Isometric Depth Normals")]
    public float _fadeThreshold = 100000;
    public float _normalAmp = 1;
    public float _depthAmp = 1;
    public bool _writeToTexture = true;

    [HideInInspector]
    public Material material = default;

    public bool ready;
    private void Awake()
    {
        Material mat = Resources.Load("Materials/IsometricDepthNormals/IsometricDepthNormals") as Material;
        material = Instantiate(mat);
        material.name = material.name + " " + gameObject.name;
        materials.Add("IsometricDepthNormals", material);
        renderer = GetComponent<Renderer>();

        //Add to this component to all children
        foreach (Transform child in transform)
        {
            if (child.gameObject.GetComponent<IsometricDepthNormalObject>() != null)
            {
                continue;
            }
            IsometricDepthNormalObject iso = child.gameObject.AddComponent<IsometricDepthNormalObject>();
            iso._fadeThreshold = _fadeThreshold;
            iso._normalAmp = _normalAmp;
            iso._depthAmp = _depthAmp;
        }
        ready = true;
    }

    private void Update()
    {
        material.SetFloat("_Fade", _fadeThreshold);
        material.SetFloat("_NormalAmount", _normalAmp);
        material.SetFloat("_DepthAmount", _depthAmp);
    }
}
