using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseStacker : MonoBehaviour
{
    // Start is called before the first frame update
    public Stack<AgentPhysics> stack;
    public Dictionary<string, AgentPhysics> agents;
    public void Awake()
    {
        stack = new Stack<AgentPhysics>();
        agents = new Dictionary<string, AgentPhysics>();
    }

    public void clearStack()
    {
        stack.Clear();
        agents.Clear();
    }

    public void removeConstraints()
    {
        while (stack.Count > 0)
        {
            AgentPhysics agent = stack.Pop();
            ConstrainedPoint point = agent.gameObject.GetComponentInChildren<ConstrainedPoint>();
            if(point != null)
                Destroy(point.gameObject);
            //remove component and enable rigid body.
            Stackable stackable = agent.gameObject.GetComponent<Stackable>();
            if (stackable != null)
                Destroy(stackable);
            Rigidbody rb = agent.gameObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                agent.gameObject.layer = 10;
            }
            agent.moveAway();
        }
        clearStack();
    }
}
