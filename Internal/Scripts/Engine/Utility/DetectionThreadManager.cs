using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

public static class DetectionThreadManager
{
    public static Stack<ConeDectionThread> threads = new Stack<ConeDectionThread>();
    public static int numOfActionsInThread;

    public static Dictionary<string, string> AllThreads = new Dictionary<string, string>();
    public static void CreateDetectionThread(ConeDectionThread coneThread, ConeDetectionMultithreaded coneDetection, string key)
    {
        coneThread.detector = coneDetection;
        int numOfThreads = threads.Count;
        if (numOfActionsInThread % 32 == 0)
        {
            coneThread.thread = new Thread(new ThreadStart(coneThread.Start));
            coneThread.thread.Name = "Detection Thread: " + numOfThreads;
            coneThread.thread.Start();
            coneThread.coneDetectors.Add(key, coneDetection);
            threads.Push(coneThread);
            numOfActionsInThread = 0;
        }
        else
        {
            coneThread.thread = threads.Peek().thread;
            threads.Peek().coneDetectors.Add(key, coneDetection);

        }

        if (coneThread != null)
        {
            
        }
        numOfActionsInThread++;
    }

    public static void EndThreads()
    {
        foreach (ConeDectionThread thr in threads)
        {
            thr.running = false;
        }
    }
}
