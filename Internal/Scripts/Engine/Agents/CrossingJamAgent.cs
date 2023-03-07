using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrossingJamAgent : MonoBehaviour
{
    // Start is called before the first frame update
    private Rigidbody body;
    public int path; //Which path out of the paths to choose from.
    public PathNode target; //current node of interests.
    public PathNode destination; //terminating node.
    private MapWaypoints map;
    public float threshold = 0.1f;

    public bool pathFinder = true; //Determines if it searches for it's path based on destination.
    private PathNode[] objPath; //This path is used when pathfinder is set to true.
    private int pathIndex = 0;
    private bool destinationReached = false;
    public float speed = 10f;
    private float delta = 0.0f;
    private float _moveSpeed = 0.0f;
    private ConeCast detectionCone;
    void Start()
    {
        body = GetComponent<Rigidbody>();
        detectionCone = GetComponent<ConeCast>();
        map = GameObject.FindWithTag("Paths").GetComponent<MapWaypoints>();
        if (pathFinder)
            objPath = getPath();
        if (objPath == null)
            objPath = map.paths[path].nodes;
        if (target == null)
            target = objPath[pathIndex];
        _moveSpeed = speed;
    }

    // Update is called once per frame
    void Update()
    {

        if(!destinationReached)
            traverseGraph();
        if (Input.GetKey("space"))
        {
            _moveSpeed = stop(_moveSpeed);
        }
        else if (detectedObjectWithName("StopSign"))
        {
            _moveSpeed = stop(_moveSpeed);
        }
        else
            _moveSpeed = speed;

        move(target.transform.position - transform.position, _moveSpeed);
    }

    public void move(Vector3 direction, float m_speed)
    {
        delta += Time.deltaTime*10f;
        body.velocity = Vector3.Lerp(body.velocity, direction * m_speed*Time.deltaTime, delta);
        if (delta > 1)
            delta = 0.0f;
    }

    public float stop(float m_speed)
    {
        return Vector2.Lerp(new Vector2(m_speed, m_speed), new Vector2(0,0), Time.deltaTime*2f).x;
    }

    void getTarget(PathNode node)
    {
        if (pathIndex < objPath.Length)
        {
            pathIndex += 1;
            target = objPath[pathIndex];
        }
    }

    void traverseGraph()
    {
        if (Vector3.Distance(transform.position, target.transform.position) < threshold)
        {
            if (target.key != destination.key)
            {
                getTarget(target);
            }
            else
            {
                destinationReached = true;
            }
        }
    }

    PathNode[] getPath()
    {
        Stack<PathNode> tempStack = new Stack<PathNode>();
        Stack<PathNode> finalPath = new Stack<PathNode>();

        PathNode currHead = target;
        tempStack.Push(currHead);


        Dictionary<string, PathNode> visted = new Dictionary<string, PathNode>();
        while (tempStack.Count > 0)
        {
            
            currHead = tempStack.Pop();
            finalPath.Push(currHead);
            visted.Add(currHead.key, currHead);

            if (currHead.key == destination.key)
            {
                break;
            }

            bool allVisted = true;
            foreach (PathNode node in currHead.connections)
            {
                if (!visted.ContainsKey(node.key))
                {
                    allVisted = false;
                    break;
                }

            }
            if (allVisted)
                finalPath.Pop();
            foreach (PathNode node in currHead.connections)
            {
                if(!visted.ContainsKey(node.key))
                    tempStack.Push(node);
            }
        }

        //Return the final path.
        PathNode curr = null;
        List<PathNode> finalPathArray = new List<PathNode>();
        while (finalPath.Count > 0)
        {
            curr = finalPath.Pop();
            finalPathArray.Add(curr);

        }
        finalPathArray.Reverse(0, finalPathArray.Count);
        return finalPathArray.ToArray();
    }

    bool detectedObjectWithName(string name)
    {
        if (detectionCone == null || detectionCone.enabled == false)
            return false;
        if (detectionCone.objectsInView == null)
            return false;
        foreach (GameObject obj in detectionCone.objectsInView)
        {
            if (obj.name == name)
                return true;
        }
        return false;
    }
}
