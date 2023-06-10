using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IsometricDepthNormalObject : UnigmaPostProcessingObjects
{
    private void Start()
    {
        material = Resources.Load("Materials/IsometricDepthNormals/IsometricDepthNormals") as Material;

        Debug.Log(material.name);
        renderer = GetComponent<Renderer>();

        //Add to this component to all children
        foreach (Transform child in transform)
        {
            child.gameObject.AddComponent<IsometricDepthNormalObject>();
        }
    }
}
