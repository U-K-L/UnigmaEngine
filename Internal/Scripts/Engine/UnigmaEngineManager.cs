using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnigmaEngineManager : MonoBehaviour
{
    public UnigmaPhysicsManager UnigmaPhysics;

    public static UnigmaEngineManager Instance;
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
