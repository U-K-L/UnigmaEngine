using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnigmaPhysicsManager : MonoBehaviour
{
    public FluidSimulationManager UnigmaFluids;
    public UnigmaSpaceTime UnigmaSpaceTime;


    public static UnigmaPhysicsManager Instance;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

}