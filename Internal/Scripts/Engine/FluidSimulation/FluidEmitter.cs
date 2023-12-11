using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FluidEmitter : MonoBehaviour
{
    private FluidSimulationManager _fluidSimManager;
    public int numOfParticlesSpawned = 16;
    public Vector4 force = new Vector4(0, 0, 0, 0.33f);
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
            _fluidSimManager.ShootParticles(transform.position, numOfParticlesSpawned, force);
        }
    }
}
