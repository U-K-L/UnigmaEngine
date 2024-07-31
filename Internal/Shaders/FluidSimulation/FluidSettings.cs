using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UnigmaFluidSettings", menuName = "Unigma/Engine/Fluid/FluidSettings", order = 1)]
public class FluidSettings : ScriptableObject
{
    public int MaxNumOfParticles;
    public int MaxNumOfControlParticles;

    public float BlurFallOff = 0.025f;
    public float BlurRadius = 5.0f;
    public float Viscosity = 1.0f;
    public float TimeStep = 0.02f;
    public float BoundsDamping = -0.3f;
    public float SizeOfParticle = 0.08552188f;
}