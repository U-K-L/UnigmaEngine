using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathNode : MonoBehaviour
{
    public Dictionary<string, PathNode> pathNodes;
    public string key;
    public PathNode[] connections;
    private MeshRenderer mesh;
    private MapWaypoints path;
    public void Start()
    {
        mesh = GetComponent<MeshRenderer>();
        path = GetComponentInParent<MapWaypoints>();
        pathNodes = new Dictionary<string, PathNode>();
        foreach (PathNode node in connections)
        {
            pathNodes.Add(node.key, node);
        }
    }

    public void Update()
    {
        if (path.debugPath)
        {
            mesh.enabled = true;
        }
        else
        {
            mesh.enabled = false;
        }
    }
}
