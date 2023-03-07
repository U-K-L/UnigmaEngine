using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        EggLocatorUnit unit = other.GetComponentInParent<EggLocatorUnit>();
        if (unit)
        {
            unit.Die();
            Debug.Log("DEAD!! " + unit.name);
        }
        
        
    }
}
