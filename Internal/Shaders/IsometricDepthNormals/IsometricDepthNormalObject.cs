using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IsometricDepthNormalObject : MonoBehaviour
{
    private Material _depthNormalsMaterial;
    private void Start()
    {
        _depthNormalsMaterial = Resources.Load("Materials/IsometricDepthNormals/IsometricDepthNormals.mat", typeof(Material)) as Material;

        //Add to this component to all children
        foreach (Transform child in transform)
        {
            child.gameObject.AddComponent<IsometricDepthNormalObject>();
        }
    }
}
