using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public static class AgentAIManager
{
    public static Stack<AgentAIThoughtsThreaded> threads = new Stack<AgentAIThoughtsThreaded>();
    public static int numOfActionsInThread;
    public static Dictionary<string, string> AllThreads = new Dictionary<string, string>();
    public static Dictionary<string, GameObject> agents = new Dictionary<string, GameObject>();
    public static void CreateAIThread(AgentAIThoughtsThreaded agentThoughts, AgentAI agentAI, string key)
    {
        int numOfThreads = threads.Count;
        if (numOfActionsInThread % 32 == 0)
        {
            agentThoughts.thread = new Thread(new ThreadStart(agentThoughts.Start));
            agentThoughts.thread.Name = "Agent AI Thread: " + numOfThreads;
            agentThoughts.thread.Start();
            agentThoughts.agents.Add(key, agentAI);
            threads.Push(agentThoughts);
            numOfActionsInThread = 0;
        }
        else
        {
            agentThoughts.thread = threads.Peek().thread;
            threads.Peek().agents.Add(key, agentAI);

        }
        numOfActionsInThread++;
    }

    public static void EndThreads()
    {
        foreach (AgentAIThoughtsThreaded thr in threads)
        {
            thr.running = false;
        }
    }

}
