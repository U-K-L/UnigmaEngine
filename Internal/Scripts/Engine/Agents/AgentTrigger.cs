using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentTrigger : MonoBehaviour
{
    public Collider _triggerCollider;
    private AgentPhysics _agent;
    private void Awake()
    {
        _triggerCollider = GetComponent<Collider>();
        _agent = GetComponentInParent<AgentPhysics>();
    }

    private void OnTriggerEnter(Collider collider)
    {
        Debug.Log("triggered");
        object[] objects = new object[1];
        objects[0] = collider;
        _agent.OnTriggered(objects);
    }

    private void OnCollisionStay(Collision collision)
    {
        Debug.Log("collision stayed: " + collision.gameObject.name);
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("on collision: " + collision.gameObject.name);
    }
}
