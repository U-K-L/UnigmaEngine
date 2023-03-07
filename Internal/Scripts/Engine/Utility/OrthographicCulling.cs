using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrthographicCulling : MonoBehaviour
{
    // Start is called before the first frame update
    private Camera _camera;
    void Start()
    {
        _camera = this.GetComponent<Camera>();
    }

    // Update is called once per frame
    void Update()
    {

    }
}
