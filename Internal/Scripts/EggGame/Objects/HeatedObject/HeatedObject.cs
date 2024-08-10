using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeatedObject : MonoBehaviour
{
    FluidControl _fluidControl;
    // Start is called before the first frame update
    void Start()
    {
        _fluidControl = GetComponent<FluidControl>();

        _fluidControl.points = new Vector3[1];
    }

    // Update is called once per frame
    void Update()
    {
        _fluidControl.points[0] = transform.position;
    }
}
