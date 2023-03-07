using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrossingJamPath
{
    public LineRenderer path;
    public GameObject parent;
    public Dictionary<string, AgentPhysics> objectsOnPath;
    public bool hasObjects = true;
    public CrossingJamPath()
    {
        objectsOnPath = new Dictionary<string, AgentPhysics>();
        hasObjects = true;
    }

    public void addObjectToPath(AgentPhysics ped)
    {
        if(!objectsOnPath.ContainsKey(ped.ToString()))
            objectsOnPath.Add(ped.ToString(), ped);
    }

    public bool checkPathsActive()
    {
        foreach (KeyValuePair<string, AgentPhysics> ped in objectsOnPath)
        {
            if (ped.Value.path == path)
                return true;
        }
        return false;
    }
}
