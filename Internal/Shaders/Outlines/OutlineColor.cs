using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OutlineColor : UnigmaPostProcessingObjects
{
    [Header("Isometric Outline Outter Colors")]
    public Color _outlineColor = Color.white;
    [Header("Isometric Outline Inner Colors")]
    public Color _outlineInnerColor = Color.white;
    [Header("Texture for line thickness. Bighter is thicker.")]
    public Texture2D _thicknessTexture = default;
    public Vector4 _ThicknessTexture_ST = new Vector4(1,1,1,1);

    public bool useShader = false;

    //[HideInInspector]
    public Material material = default;
    Material currentMaterial;
    private void Awake()
    {
        Material currentMaterial = GetComponent<Renderer>().sharedMaterial;
        Material mat = Resources.Load("Materials/OutlineColors/OutlineColors") as Material;
        material = Instantiate(mat);
        material.name = material.name + " " + gameObject.name;
        materials.Add("OutlineColors", material);
        renderer = GetComponent<Renderer>();

        //Add to this component to all children
        foreach (Transform child in transform)
        {
            if (child.gameObject.GetComponent<OutlineColor>() != null)
            {
                continue;
            }
            OutlineColor ot = child.gameObject.AddComponent<OutlineColor>();
            ot._outlineColor = _outlineColor;
            ot._outlineInnerColor = _outlineInnerColor;
            ot._thicknessTexture = _thicknessTexture;
            ot._ThicknessTexture_ST = _ThicknessTexture_ST;
        }
    }

    private void Update()
    {
        if(useShader)
            SetPropertiesViaMaterial();
        else
            SetPropertiesViaScript();
    }

    private void SetPropertiesViaScript()
    {
        material.SetColor("_OutlineColor", _outlineColor);
        material.SetColor("_OutlineInnerColor", _outlineInnerColor);
        material.SetTexture("_ThicknessTexture", _thicknessTexture);
        material.SetVector("_ThicknessTexture_ST", _ThicknessTexture_ST);
    }

    private void SetPropertiesViaMaterial()
    {
        if (currentMaterial != null)
        {
            material.SetColor("_OutlineColor", currentMaterial.GetColor("_OutlineColor"));
            material.SetColor("_OutlineInnerColor", currentMaterial.GetColor("_OutlineInnerColor"));
            material.SetTexture("_ThicknessTexture", currentMaterial.GetTexture("_ThicknessTexture"));
            material.SetVector("_ThicknessTexture_ST", currentMaterial.GetVector("_ThicknessTexture_ST"));
        }
        else
            currentMaterial = GetComponent<Renderer>().sharedMaterial;
    }
}
