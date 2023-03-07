using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapWaypoints : MonoBehaviour
{
    [System.Serializable]
    public class Path
    {
        public PathNode[] nodes;
        public Material lineMaterial;
    }
    public Path[] paths;
    public bool debugPath = false;
    public float lineWidth = 0.01f;
    public Dictionary<string, LineRenderer> lineEdges;
    public Material lineMaterial;

    // Start is called before the first frame update
    void Awake()
    {
        lineEdges = new Dictionary<string, LineRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (debugPath)
        {
            enableLineRendering(true);
            debugPaths();
        }
        else
            enableLineRendering(false);
    }

    void debugPaths()
    {
        Dictionary<string, float> edges = new Dictionary<string, float>();
        foreach (Path path in paths)
        {
            foreach (PathNode node in path.nodes)
            {
                foreach (PathNode nodeNext in node.connections)
                {
                    string edgeF = node.key + nodeNext.key; //forwards
                    string edgeB = nodeNext.key + node.key; //backwards
                    if (edges.ContainsKey(edgeF) || edges.ContainsKey(edgeB))
                        continue;
                    createConnection(node, nodeNext, path.lineMaterial);
                    edges.Add(edgeF, 0);
                    edges.Add(edgeB, 0);
                }
            }
        }
    }

    void createConnection(PathNode node, PathNode nodeNext, Material mat = null)
    {
        string key = node.key + nodeNext.key;
        if (lineEdges.ContainsKey(key))
        {
            LineRenderer lineRenderer = lineEdges[key];
            lineRenderer.SetPosition(0, node.transform.position); //x,y and z position of the starting point of the line
            lineRenderer.SetPosition(1, nodeNext.transform.position); //x,y and z position of the end point of the line
        }
        else
        {
            LineRenderer lineRenderer = new GameObject(node.key + nodeNext.key).AddComponent<LineRenderer>();
            if (mat)
                lineRenderer.material = mat;
            else
                lineRenderer.material = lineMaterial;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;

            lineRenderer.positionCount = 2;
            lineRenderer.useWorldSpace = true;

            //For drawing line in the world space, provide the x,y,z values
            lineRenderer.SetPosition(0, node.transform.position); //x,y and z position of the starting point of the line
            lineRenderer.SetPosition(1, nodeNext.transform.position); //x,y and z position of the end point of the line

            lineEdges.Add(key, lineRenderer);
        }
    }

    void enableLineRendering(bool b)
    {
        foreach(KeyValuePair<string, LineRenderer> line in lineEdges)
        {
            line.Value.enabled = b;
        }
    }


}
