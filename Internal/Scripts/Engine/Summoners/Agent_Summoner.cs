using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Agent_Summoner : AgentPhysics
{
    private Dictionary<string, AgentSummoning> summonings;

    // Start is called before the first frame update
    void Start()
    {
        summonings = new Dictionary<string, AgentSummoning>();
        key = name;
        BeginAgent();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateAgent();
        foreach (KeyValuePair<string, AgentSummoning> agent in summonings)
        {
            Debug.Log(agent.Value.key);
        }
    }

    public Dictionary<string, AgentSummoning> GetSummonings()
    {
        return summonings;
    }
    
    public void AddSummoning(string key, AgentSummoning summoning)
    {
        summoning.SetSummoner(this);
        summonings.Add(key, summoning);
    }

    public void RemoveSummoning(string key, AgentSummoning summoning)
    {
        summoning.SetSummoner(null);
        summonings.Remove(key);
    }
}
