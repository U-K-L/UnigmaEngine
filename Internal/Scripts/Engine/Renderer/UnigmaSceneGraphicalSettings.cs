using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UnigmaSceneGraphicalSettings", menuName = "Unigma/Engine/Scene/GraphicalSettings", order = 2)]
public class UnigmaSceneGraphicalSettings : ScriptableObject
{
    [Tooltip("Control the influence of graphical factors. X = Albedo Map, Y = Global Illumination, Z = Reflections, W = Specular Map.")]
    public Vector4 GlobalIlluminationWeights; //X = Albedo Map, Y = Global Illumination, Z = Reflections, W = Specular Map.
}
