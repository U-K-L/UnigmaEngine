using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AgentPhysicsManager
{
    private static Dictionary<string, AgentPhysics> _agents = new Dictionary<string, AgentPhysics>();

    public static Dictionary<string, AgentPhysics> GetAllAgents()
    {
        return _agents;
    }

    public static void AddAgentToList(AgentPhysics agent)
    {
        _agents.Add(agent.key, agent);
    }

    public static void RemoveAgentFromList(AgentPhysics agent)
    {
        _agents.Remove(agent.key);
    }

    public static void RemoveAllAgentsFromList(AgentPhysics agent)
    {
        _agents.Clear();
    }
}
