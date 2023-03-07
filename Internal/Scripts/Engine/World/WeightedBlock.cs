using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeightedBlock : MonoBehaviour
{
    List<GameObject> currentCollisions = new List<GameObject>();
    Rigidbody body;
    private Vector3 stablePos;
    void Start()
    {
        body = GetComponent<Rigidbody>();
        stablePos = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void FixedUpdate()
    {
        if(transform.position != stablePos)
            enforceContraint();
        /*
        if (transform.position.y > stablePos.y)
            transform.position = stablePos;
        */

    }

    void OnCollisionEnter(Collision col)
    {
        /*
        currentCollisions.Add(col.gameObject);
        if (transform.position == stablePos)
            applyForce(col);
        */


    }

    void OnCollisionExit(Collision col)
    {

        //currentCollisions.Remove(col.gameObject);
    }

    void enforceContraint()
    {
        Vector3 directional_force = stablePos - transform.position;
        body.AddForce(directional_force * body.mass, ForceMode.Impulse);
    }

    void applyForce(Collision col)
    {

        Vector3 direction = col.GetContact(0).point - transform.position;
        // * col.gameObject.GetComponent<Rigidbody>().mass * 0.00001f
        body.AddForce(Vector3.up, ForceMode.Force);
        Debug.Log("Force Applied");
    }
}
