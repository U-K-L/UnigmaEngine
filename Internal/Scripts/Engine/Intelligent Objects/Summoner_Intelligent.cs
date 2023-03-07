using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Summoner_Intelligent : IntelligentObject
{
    // Start is called before the first frame update
    void Start()
    {
        Iname = gameObject.name;
    }

    // Update is called once per frame
    void Update()
    {
        position = transform.position;
    }
}
