using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Shapes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Video;
using System;

public class BubbleGumTransition : ImmediateModeShapeDrawer
{
    public Material material;
    public float slider = 0;
    // Start is called before the first frame update
    void Start()
    {
        //1 is closed. 0 is open
        material.SetFloat("_Transition", 1);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (slider < 0.98f)
                CloseAnimationPlay();
            else
                OpenAnimationPlay();
        }
    }

    public void CloseAnimationPlay()
    {
        StopAllCoroutines();
        StartCoroutine(CloseAnimation());
    }

    public void OpenAnimationPlay()
    {
        StopAllCoroutines();
        StartCoroutine(OpenAnimation());
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(source, destination, material);
    }

    IEnumerator CloseAnimation()
    {
        while (slider < 0.99f)
        {
            if(slider > 0.125f)
                slider += Mathf.Lerp(slider, slider + Time.deltaTime*0.1f, Time.deltaTime * 100f);
            else
                slider = Mathf.Lerp(slider, slider + Time.deltaTime*2f, Time.deltaTime * 100f);
            material.SetFloat("_Transition", slider);
            yield return new WaitForSeconds(0.02f);
        }
        slider = 1;
        material.SetFloat("_Transition", slider);

    }

    IEnumerator OpenAnimation()
    {
        while (slider > 0.001f)
        {
            slider = Mathf.Lerp(slider, slider - Time.deltaTime*10, Time.deltaTime*100);
            slider = Mathf.Max(0, slider);
            material.SetFloat("_Transition", slider);
            yield return new WaitForSeconds(0.02f);
        }
        slider = 0;
        material.SetFloat("_Transition", slider);

    }
}
