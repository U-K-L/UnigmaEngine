using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class SelectionGlow : MonoBehaviour
{
    // Start is called before the first frame update
    public int _blurIterations = 1;
    public Material material;
    private static RenderTexture blurred;
    private static RenderTexture prepass;
    const int BoxDownPass = 0;
    const int BoxUpPass = 1;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnEnable()
    {
        prepass = new RenderTexture((int)(Screen.width), (int)(Screen.height), 0);
        blurred = new RenderTexture((int)(Screen.width), (int)(Screen.height), 0);
        Shader.SetGlobalTexture("_SelectionPrePassTex", prepass);
        Shader.SetGlobalTexture("_SelectionBlurredTex", blurred);
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(source, destination);
        Graphics.SetRenderTarget(prepass);
        Graphics.Blit(source, prepass, material);

        RenderTexture[] textures = new RenderTexture[16];
        uint width = (uint)source.width>>2;
        uint height = (uint)source.height>>2;

        textures[0] = RenderTexture.GetTemporary(source.width, source.height, 0, source.format);
        RenderTexture currentDestination = textures[0];
        Graphics.Blit(source, currentDestination, material, BoxDownPass);

        RenderTexture currentSource = currentDestination;
        int i = 1;
        for (; i < _blurIterations; i++)
        {
            width = width >> 2;
            height = height >> 2;
            if (height < 2)
                break;
            textures[i] = RenderTexture.GetTemporary((int)width, (int)height, 0, source.format);
            currentDestination = textures[i];
            Graphics.Blit(currentSource, currentDestination, material, BoxDownPass);
            currentSource = currentDestination;

        }

        for (i -= 2; i >= 0; i--)
        {
            currentDestination = textures[i];
            textures[i] = null;
            Graphics.Blit(currentSource, currentDestination, material, BoxUpPass);
            RenderTexture.ReleaseTemporary(currentSource);
            currentSource = currentDestination;
        }
        Graphics.Blit(currentSource, blurred, material, BoxUpPass);

        RenderTexture.ReleaseTemporary(currentSource);
    }
}
