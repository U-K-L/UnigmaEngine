using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FluidContainer : MonoBehaviour
{
    // Start is called before the first frame update
    public enum ContainerType
    {
        Box,
        Sphere
    }
    public int numOfParticles = 16;
    public ContainerType containerType;
    public Vector3 containerSize = Vector3.one;
    private FluidSimulationManager _fluidSimManager;
    private bool SimCreated = false;
    public FluidSimulationManager.particlePhases phase;
    public FluidSimulationManager.particleTypes type;
    public float kelvin = 273.0f;
    void Start()
    {
        _fluidSimManager = FindObjectOfType<FluidSimulationManager>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!SimCreated)
        {
            _fluidSimManager.AddParticles(transform.position, numOfParticles, containerSize, (int)containerType, (int)phase, (int)type, kelvin);
            SimCreated = true;
        }
    }
}
