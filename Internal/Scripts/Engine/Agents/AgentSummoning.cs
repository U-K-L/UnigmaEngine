using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentSummoning : AgentPhysics
{
    //private Rigidbody _mbody;
    //private bool moving = true;
    private int pathIndex = 0;
    public float _tol = 0.1f;
    //public float speed = 0.5f;
    private float _moveSpeed = 0.0f;
    public static Dictionary<string, AgentSummoning> summonings = new Dictionary<string, AgentSummoning>();
    public string type;
    private AgentAI agentAI;
    private Agent_Summoner _summoner;
    void Start()
    {
        if (type != "Summoner")
        {
            if (key == null)
            {
                key = type + ", {" + summonings.Count + "}";
            }
            transform.parent.name = key;
            transform.name = transform.parent.name;
            summonings.Add(key, this);
        }
        agentAI = GetComponent<AgentAI>();
        BeginAgent();

    }

    // Update is called once per frame
    void Update()
    {
        UpdateAgent();
        if (fullPath != null)
            followPath();
        else
            _moveSpeed = stop(_moveSpeed);
    }

    void followPath()
    {

        if (fullPath.Count > 0)
            checkIfDestinationReached();
        else if (fullPath.Count == 0)
        {
            reachedDestination();
        }
        _moveSpeed = speed;
    }

    public bool checkIfDestinationReached()
    {
        while (Vector3.Distance(destination, transform.position) < _tol && fullPath.Count > 0)
        {
            pathIndex += 1;
            destination = fullPath.Dequeue();

        }
        if (Vector3.Distance(destination, transform.position) < _tol)
        {
            
            return true;
        }

        return false;
    }

    public float stop(float m_speed)
    {
        return Vector2.Lerp(new Vector2(m_speed, m_speed), new Vector2(0, 0), Time.deltaTime).x;
    }

    public void reachedDestination()
    {
        path = null;
        if (state == StateMachine.moving)
            setStateIdle();
        pathIndex = 0;
    }
    public void moveAway()
    {
        Vector3 randomDirection = Random.onUnitSphere;
        StartCoroutine(pushRandom(randomDirection));
        moving = false;
    }
    
    public override void Seeking(Vector3 target)
    {
        
        agentAI.movementState = AgentAI.MovementStateMachine.seeking;
        setStateMoving();
        
    }

    public override void Seeking()
    {
        agentAI.movementState = AgentAI.MovementStateMachine.seeking;
        setStateMoving();

    }

    public float getSpeed()
    {
        return _moveSpeed;
    }
    IEnumerator pushRandom(Vector3 rndmDir)
    {
        float counter = 0;
        while (counter < 2)
        {
            counter += Time.deltaTime;
            _mbody.AddForce(rndmDir * 10f);
            yield return new WaitForSeconds(Time.deltaTime);
        }
        moving = true;
        yield return new WaitForSeconds(Time.deltaTime);
    }
    
    public void SetSummoner(Agent_Summoner summoner)
    {
        _summoner = summoner;
    }

    public Agent_Summoner GetSummoner()
    {
        return _summoner;
    }

}
