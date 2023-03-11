using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

public class AgentAIThoughtsThreaded
{
    public bool running = false;
    public Thread thread;
    System.Random rnd = new System.Random();
    public Dictionary<string, AgentAI> agents = new Dictionary<string, AgentAI>();
    public void Start()
    {
        double rand = rnd.NextDouble() * 100.0;
        Thread.Sleep(500 + (int)rand);
        running = true;
        Running();
    }

    void Running()
    {
        
        while (running)
        {
            Update();
            double rand = rnd.NextDouble() * 10.0;
            Thread.Sleep(60 + (int)rand);
        }

    }

    public void Update()
    {
        Debug.Log("Agent is searching thoughts");
        SearchThoughts();
    }

    void SearchThoughts()
    {
        string[] keys = agents.Keys.ToArray();
        for (int i = 0; i < keys.Length; i++)
        {
            string key = keys[i];
            DetermineThoughts(agents[key]);
        }
    }

    void DetermineThoughts(AgentAI agent)
    {
        string[] keys;
        lock (agent.visionFieldThreaded.GameObjectsInCone)
            keys = agent.visionFieldThreaded.GameObjectsInCone.Keys.ToArray();
        for (int i = 0; i < keys.Length; i++)
        {
            string key = keys[i];
            if (agent.visionFieldThreaded.GameObjectsInCone.ContainsKey(key))
            {
                DetermineResponse(agent.visionFieldThreaded.GameObjectsInCone[key], agent);
            }
        }
    }

    void DetermineResponse(IntelligentObject Iobj, AgentAI agent)
    {
        Debug.Log(Iobj.type + " Agent is determining thoughts." );
        if (Iobj.type == "Summoner_Intelligent")
        {
            agent.thinkingState = AgentAI.ThinkingStateMachine.thinking;
            (string, string) task = ("Thought", "The Queen! I found the Queen!");
            agent.tasks.Enqueue(task);
        }

        if (Iobj.type == "Flee")
        {
            
            agent.thinkingState = AgentAI.ThinkingStateMachine.thinking;
            (string, string) task = ("Flee", Iobj.position.x.ToString() + "," + Iobj.position.y.ToString() + "," + Iobj.position.z.ToString());
            agent.tasks.Enqueue(task);
        }

        if (Iobj.type == "CPU")
        {
            agent.thinkingState = AgentAI.ThinkingStateMachine.thinking;
            (string, string) task = ("GenerativeThought", Iobj.Iname + ", HP is " + Iobj.HP.ToString());
            agent.tasks.Enqueue(task);
        }
    }
}
