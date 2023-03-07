using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class AgentSkill : MonoBehaviour
{
    public string name;
    public Texture texture;
    public AgentSkill[] options;
    public VideoClip video;
    protected static Player_Controller_Agents drawing;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public virtual void execute(Player_Controller_Agents controller, AgentPhysics agent)
    {
        AgentSummoning summon = agent.gameObject.GetComponent<AgentSummoning>();
        if (summon != null)
            executeSkill(controller, summon);
        Agent_Summoner summoner = agent.gameObject.GetComponent<Agent_Summoner>();
        if (summoner != null)
            executeSkill(controller, summoner);
        executeSkill(controller, agent);
    }

    public virtual void execute(Player_Controller_Agents controller, Agent_Summoner agent)
    {
        AgentSummoning summon = agent.gameObject.GetComponent<AgentSummoning>();
        if (summon != null)
            executeSkill(controller, summon);
        AgentPhysics agentP = agent.gameObject.GetComponent<AgentPhysics>();
        if (agentP != null)
            executeSkill(controller, agentP);
        executeSkill(controller, agent);
    }

    public virtual void execute(Player_Controller_Agents controller, AgentSummoning agent)
    {
        AgentPhysics agentP = agent.gameObject.GetComponent<AgentPhysics>();
        if (agentP != null)
            executeSkill(controller, agentP);
        executeSkill(controller, agent);
    }

    public virtual void executeSkill(Player_Controller_Agents controller, AgentSummoning agent)
    {
        
    }

    public virtual void executeSkill(Player_Controller_Agents controller, Agent_Summoner agent)
    {
        
    }
    
    public virtual void executeSkill(Player_Controller_Agents controller, AgentPhysics agent)
    {
        
    }
}
