using System.Collections;
using System.Collections.Generic;
using UnigmaEngine;
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
    public bool updateMaterial = true;

    //[HideInInspector]
    public Material _originalMaterial = default;
    Material currentMaterial;
    private void Awake()
    {
        Material currentMaterial = GetComponent<Renderer>().sharedMaterial;
        Material mat = Resources.Load("Materials/OutlineColors/OutlineColors") as Material;
        //material = Instantiate(mat);
        //material.name = material.name + " " + gameObject.name;
        //materials.Add("OutlineColors", material);
        renderer = GetComponent<Renderer>();

        _originalMaterial = renderer.material;

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

        if (GetComponent<UnigmaSprite>())
            updateMaterial = false;
    }

    private void Update()
    {
        if (!updateMaterial)
            return;
        if(useShader)
            SetPropertiesViaMaterial();
        else
            SetPropertiesViaScript();
    }

    private void SetPropertiesViaScript()
    {
        if (_originalMaterial.HasColor("_OutlineColor"))
            _originalMaterial.SetColor("_OutlineColor", _outlineColor);

        if (_originalMaterial.HasColor("_OutlineInnerColor"))
            _originalMaterial.SetColor("_OutlineInnerColor", _outlineInnerColor);

        if (_originalMaterial.HasTexture("_ThicknessTexture"))
            _originalMaterial.SetTexture("_ThicknessTexture", _thicknessTexture);

        if (_originalMaterial.HasVector("_ThicknessTexture_ST"))
            _originalMaterial.SetVector("_ThicknessTexture_ST", _ThicknessTexture_ST);
    }

    private void SetPropertiesViaMaterial()
    {
        if (currentMaterial != null)
        {
            if(currentMaterial.HasColor("_OutlineColor"))
                _originalMaterial.SetColor("_OutlineColor", currentMaterial.GetColor("_OutlineColor"));

            if (currentMaterial.HasColor("_OutlineInnerColor"))
                _originalMaterial.SetColor("_OutlineInnerColor", currentMaterial.GetColor("_OutlineInnerColor"));

            if (currentMaterial.HasTexture("_ThicknessTexture"))
                _originalMaterial.SetTexture("_ThicknessTexture", currentMaterial.GetTexture("_ThicknessTexture"));

            if (currentMaterial.HasVector("_ThicknessTexture_ST"))
                _originalMaterial.SetVector("_ThicknessTexture_ST", currentMaterial.GetVector("_ThicknessTexture_ST"));
        }
        else
            currentMaterial = GetComponent<Renderer>().sharedMaterial;
    }
}
