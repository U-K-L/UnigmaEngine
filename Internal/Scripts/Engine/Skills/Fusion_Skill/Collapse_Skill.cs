using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Collapse_Skill : AgentSkill
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public override void execute(Player_Controller_Agents controller, AgentSummoning agent)
    {
        BaseStacker baseObj = agent.GetComponent<BaseStacker>();
        baseObj.removeConstraints();
        Destroy(baseObj);
        agent.setCurrentSkills(agent.skills);
        controller.removeAllObjectFromList();
    }
}
