using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FluidEmitter : MonoBehaviour
{
    private FluidSimulationManager _fluidSimManager;
    public int numOfParticlesSpawned = 16;
    public Vector4 force = new Vector4(0, 0, 0, 0.33f);
    public Vector3 radius = Vector3.one;
    public FluidSimulationManager.particlePhases phase = 0;
    public FluidSimulationManager.particleTypes type = 0;
    // Start is called before the first frame update
    void Start()
    {
        _fluidSimManager = FindObjectOfType<FluidSimulationManager>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.Z))
        {
            _fluidSimManager.ShootParticles(transform.position, numOfParticlesSpawned, force, radius, (int)phase, (int)type);
        }
    }
}
