using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorChangeIndicator : MonoBehaviour
{
    // Start is called before the first frame update
    public float closenessToEgg = 0;
    private Renderer _render;
    void Start()
    {
        _render = GetComponent<Renderer>();
    }

    // Update is called once per frame
    void Update()
    {
        Color pinkish = new Vector4(1f, 0.85f, 0.95f, 0f);
        Color cold = new Vector4(0.35f, 0.65f, 0.85f, 0f);
        Color lerpedColor = Color.Lerp(cold, pinkish, closenessToEgg);
        _render.material.color = lerpedColor;
    }
}
