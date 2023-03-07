using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fusion_Skill : AgentSkill
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //Get queue of agents.
    //Each agent in queue is stacked to prev agent.
    //Add component stackable.
    public override void execute(Player_Controller_Agents controller, AgentPhysics agent)
    {
        

        Dictionary<string, AgentPhysics> selectedObjects = controller.selectedObjects;
        BaseStacker baseObj = agent.gameObject.AddComponent(typeof(BaseStacker)) as BaseStacker;
        Stack<AgentPhysics> stack = baseObj.stack;
        stack.Push(agent);
        foreach (KeyValuePair<string, AgentPhysics> Entry in selectedObjects)
        {
            AgentPhysics ped = Entry.Value;
            if (!baseObj.stack.Contains(ped))
            {
                ped.transform.rotation = Quaternion.Euler(Vector3.zero);
                Vector3 rotation = ped.transform.rotation.eulerAngles;
                rotation.y = agent.transform.rotation.eulerAngles.y;
                ped.transform.rotation = Quaternion.Euler(rotation);
                AgentPhysics baseOfObj = stack.Peek();

                createStack(baseOfObj, ped);
                baseObj.stack.Push(ped);

            }

        }
        controller.removeAllObjectFromList();
        agent.setCurrentSkills(options);
    }

    void createStack(AgentPhysics baseOBJ, AgentPhysics stackableOBJ)
    {

        //Creates the stackable point.
        Stackable stackObj = stackableOBJ.gameObject.AddComponent(typeof(Stackable)) as Stackable;
        GameObject point = new GameObject();
        //Sets the position.
        point.name = "point";
        point.AddComponent(typeof(ConstrainedPoint));
        point.transform.parent = baseOBJ.gameObject.transform;
        point.transform.localPosition = Vector3.zero;
        point.transform.position += baseOBJ.gameObject.transform.up*0.3f;
        stackObj._baseObj = point.transform;
        //Handles rigidbody and other settings.
        Rigidbody rb = stackableOBJ.gameObject.GetComponent<Rigidbody>();

        rb.isKinematic = true;
        stackableOBJ.gameObject.layer = 17;

        //Other settings.
        stackObj.centerOfMass = new Vector3(0, -0.62f, 0);
        stackObj.frequency = 2.95f;
        stackObj.half_life = 2.25f;
        stackObj.elasticity = 0.01f;
        stackableOBJ.forceSmear = true;
    }
}
