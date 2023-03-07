using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentBillBoard : MonoBehaviour
{
    // Start is called before the first frame update
    public Vector3 offSet;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 Ceuler = Camera.main.transform.rotation.eulerAngles;
        Vector3 Teuler = gameObject.transform.rotation.eulerAngles;
        Vector3 swizzle = new Vector3(Teuler.x, Ceuler.y, Ceuler.z) + offSet;
        gameObject.transform.rotation = Quaternion.Euler(swizzle);
    }
}
