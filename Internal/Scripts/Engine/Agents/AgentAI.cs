using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

public class AgentAI : MonoBehaviour
{
    // Start is called before the first frame update
    [HideInInspector]
    public AgentSummoning agentSummoning;
    [HideInInspector]
    public ConeDetectionMultithreaded visionFieldThreaded; //What objects are currently in view.
    [HideInInspector]
    public ConeDetection visionField;
    [HideInInspector]
    public string Aname;
    public DialogueBox dialogueBox;
    private MagikaPP_Interpreter _mInterpreter;
    public bool _agentReady = false;
    public enum MovementStateMachine { idle, jump, seeking, fleeing, pursuit };
    public MovementStateMachine movementState;
    public enum ThinkingStateMachine { idle, thinking };
    public ThinkingStateMachine thinkingState;
    public string type;
    private Vector3 _currentTargetedGoal;
    private float _avoidRadius = 5.0f;
    private Transform _targetedGoal;
    //type, data
    public ConcurrentQueue<(string,string)> tasks = new ConcurrentQueue<(string, string)>();
    void Start()
    {
        visionField = GetComponent<ConeDetection>();
        visionFieldThreaded = GetComponent<ConeDetectionMultithreaded>();
        agentSummoning = GetComponent<AgentSummoning>();
        AgentAIThoughtsThreaded thought = new AgentAIThoughtsThreaded();
        AgentAIManager.CreateAIThread(thought, this, transform.parent.name);
        Aname = transform.name;

        _mInterpreter = new MagikaPP_Interpreter();
        _agentReady = true;
    }

    // Update is called once per frame
    void Update()
    {
        DebugVision();
        DetermineStates();
        ProcessTasks();
        if(_agentReady)
            ReadCommandTree();
    }

    //Movements:
    //Sets the arguments for the delegate functions.

    void Seek()
    {
        //Debug.Log("seeking");
        if(_targetedGoal != null)
            agentSummoning.Seeking(_targetedGoal.position);
        agentSummoning.SetActionArguments(new object[] { Vector3.Normalize(agentSummoning.getDestination() - transform.position), agentSummoning.getSpeed() });
    }

    void Fleeing()
    {
        if (Vector3.Distance(_currentTargetedGoal, transform.position) > _avoidRadius)
        {
            Debug.Log("stop fleeing");
            movementState = MovementStateMachine.idle;
            agentSummoning.setStateIdle();
            agentSummoning.reachedDestination();
            return;
        }
        agentSummoning.SetActionArguments(new object[] { Vector3.Normalize(transform.position - _currentTargetedGoal), agentSummoning.speed });
        agentSummoning.setStateMoving();
    }
    void ProcessTasks()
    {
        (string, string) task;
        if (tasks.Count > 0)
        {
            
            if (tasks.TryDequeue(out task))
            {
                DetermineAction(task);
            }
        }

    }
    void DetermineAction((string, string) task)
    {
        if (task.Item1 == "Thought")
        {
            dialogueBox.AddText(task.Item2);
        }

        if (task.Item1 == "Flee")
        {
            Debug.Log(task.Item2);
            string[] values = task.Item2.Split(',');
            _currentTargetedGoal.x = float.Parse(values[0]);
            _currentTargetedGoal.y = float.Parse(values[1]);
            _currentTargetedGoal.z = float.Parse(values[2]);
            movementState = MovementStateMachine.fleeing;
        }
    }

    void DetermineStates()
    {
        if (movementState == MovementStateMachine.jump)
        {
            agentSummoning.setStateJumping();
            movementState = MovementStateMachine.idle;
        }

        if (movementState == MovementStateMachine.seeking)
        {
            Seek();
        }

        if (movementState == MovementStateMachine.fleeing)
        {
            Fleeing();
        }
    }

    void DebugVision()
    {
        if (visionField != null && visionField.enabled)
        {
            foreach (GameObject obj in visionField.GameObjectsInCone)
            {
                Debug.Log(gameObject.name + " " + obj.name + " In View");
            }
        }

        if (visionFieldThreaded != null && visionFieldThreaded.enabled)
        {
            foreach (KeyValuePair<string, IntelligentObject> obj in visionFieldThreaded.GameObjectsInCone)
            {
                Debug.Log(gameObject.name + " " + obj.Value.Iname + " In View");
            }
        }

    }

    void ReadCommandTree()
    {
        if (_mInterpreter.GetCurrentNode() == null)
        {
            Debug.Log("End of command tree");
            return;
        }
        string type = _mInterpreter.Peek();
        if (type == "start")
        {
            CommandStart();            

        }

        if (type == "seek")
        {
            CommandSeek();
        }
        _mInterpreter.GetNextNode(this);
    }

    void CommandStart()
    {
        Debug.Log(Aname + " has started");
        
    }

    void CommandMessage()
    {

    }        
    void CommandSeek()
    {
        Debug.Log(Aname + " is seeking");
        object[] args = _mInterpreter.InterpretNode(this);
        SetTarget((Transform)args[2]);
        movementState = MovementStateMachine.seeking;
        agentSummoning.SetDestination(_targetedGoal.position);
    }

    public void SetTarget(Transform Ttransform)
    {
        _targetedGoal = Ttransform;
    }        
    void OnApplicationQuit()
    {
        AgentAIManager.EndThreads();
    }
}
