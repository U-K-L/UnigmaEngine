using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnigmaRendererObject : MonoBehaviour
{
    private void Awake()
    {
        gameObject.AddComponent<IsometricDepthNormalObject>();
        OutlineColor outlineObj = gameObject.AddComponent<OutlineColor>();

        outlineObj.useShader = true;
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
