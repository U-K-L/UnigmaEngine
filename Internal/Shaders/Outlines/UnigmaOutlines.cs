using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnigmaOutlines : MonoBehaviour
{
    // Start is called before the first frame update
    public Material material = default;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnRenderImage(RenderTexture src, RenderTexture dst)
    {

        Graphics.Blit(src, dst, material);
    }
}
