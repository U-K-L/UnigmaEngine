using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


public class AgentPhysics : MonoBehaviour
{
    //They key identifier for this agent.
    [HideInInspector]
    public string key;
    //Handles calculating paths.
    [HideInInspector]
    public NavMeshAgent navAgent;
    [HideInInspector]
    public LineRenderer path;
    protected private Vector3 destination;
    protected Queue<Vector3> fullPath; //A queue of all the paths the agent is requesting currently. Use this to move.
    
    //General settings.
    public Rigidbody _mbody;
    public Collider _collider;
    public MeshCollider _meshcollider;
    public SphereCollider _spherecollider;
    public SphereCollider _triggerCollider;
    public BoxCollider _boxCollider;
    protected bool moving = true;
    public float speed = 0.5f;
    protected float delta = 0.0f; //Time over a period for this agent.
    [SerializeField, Range(0f, 90f)]
    float maxGroundAngle = 60f;
    float minGroundDotProduct = 0.0f;

    //State machines.
    protected delegate void ExecutedAction(object[] arguments);
    ExecutedAction executeAction; //An action can only change once per fixed update.
    private object[] actionArguments; //Delegate arguments.
    public enum StateMachine { pause, idle, jumping, moving, airborne, crouching };
    protected StateMachine state;

    public Dictionary<string, AgentStateClasses> SubStates;
    
    //Custom time scale per agent.
    public float agentTimeScale;
    public float jumpRadius = 1f; 

    //Collision variables.
    protected bool onGround;
    public float fallMultiplier = 1;
    public int maxJumps = 1;
    private int jumpPhase = 0;
    public Vector3 contactNormal;

    //Prefabs for effects.
    public GameObject fallingPrefab; //prefab for falling.
    public GameObject dashingPrefab;
    private LandingSmoke _smoke;
    private DashingSmoke _dashingSmoke;
    private CameraShake _camControl;
    private SmearEffect _smear;
    [HideInInspector]
    public bool currentlyFollowed = false;
    public bool forceSmear = false;

    private bool velBoostOn = false;

    //State recording class.
    private class RecordedStates
    {
        public Vector3 position;
    }
    Stack<RecordedStates> undoStates;
    Stack<RecordedStates> redoStates;
    private float[] _stateTime = new float[6]; //How long agent was in a state.

    public AgentSkill[] skills; //The learned skills by the agent.
    protected AgentSkill[] currentSkills; //Temporary skills agent can use.

    //Performance settings
    [Header("Performance Settings")]
    public static bool SDFSmokeON = true;

    //Events that can be set by outside scripts to then be called when a specific thing happens.
    public delegate void EventHandler(object[] arguments);
    public EventHandler OnLanding;
    public EventHandler OnClick;
    public EventHandler OnColliding; //Collision stay.
    public EventHandler OnTriggered;


    //These methods should be overwritten in the child class, but are kept for sake of protection.
    private void Start()
    {
        BeginAgent();
    }

    private void Update()
    {
        UpdateAgent();

    }

    void UpdateStateTime()
    {
        _stateTime[(int)state] += Time.deltaTime;
    }

    public virtual void BeginAgent()
    {
        SubStates = new Dictionary<string, AgentStateClasses>();
        SubStates.Add("idle", new AgentStateClassesIdle());
        SubStates.Add("moving", new AgentStateClassesMoving());
        SubStates.Add("jumping", new AgentStateClassesJumping());
        SubStates.Add("airborne", new AgentStateClassesAirborne());
        SubStates.Add("pause", new AgentStateClassesPause());
        SubStates.Add("crouching", new AgentStateClassesCrouching());



        key = transform.name + AgentPhysicsManager.GetAllAgents().Count;
        currentSkills = skills;

        _collider = GetComponent<Collider>();
        _meshcollider = GetComponent<MeshCollider>();
        _spherecollider = GetComponent<SphereCollider>();
        _boxCollider = GetComponent<BoxCollider>();
        _mbody = GetComponent<Rigidbody>();
        _mbody.sleepThreshold = 0.0f;
        state = StateMachine.idle;
        executeAction = Idle;
        OnLanding = DoNothing;
        OnClick = DoNothing;
        OnColliding = DoNothing;
        OnTriggered = DoNothing;
        navAgent = GetComponent<NavMeshAgent>();
        fullPath = new Queue<Vector3>();
        if (SDFSmokeON)
        {
            _smoke = Instantiate(fallingPrefab).GetComponent<LandingSmoke>();
            _smoke.enabled = false;
            _smoke.gameObject.SetActive(false);
        }
        //_dashingSmoke = Instantiate(dashingPrefab).GetComponentInChildren<DashingSmoke>();

        //GameObject camWithShake = GameObject.FindWithTag("CameraController");
        CameraShake camWithShake = Camera.main.GetComponent<CameraShake>();
        if (camWithShake != null)
            _camControl = camWithShake;
        _smear = GetComponent<SmearEffect>();
        if(_smear != null)
            _smear.enabled = false;

        undoStates = new Stack<RecordedStates>();
        redoStates = new Stack<RecordedStates>();

        AgentPhysicsManager.AddAgentToList(this);

        OnValidate();
    }

    // Update is called every frame. NOTE: this is used instead of normal update so that classes can inherit and override.
    public virtual void UpdateAgent()
    {

        currentSkills = skills;
        UpdateStateTime();
        DetermineAction();
        Smear();
    }

    private void FixedUpdate()
    {
        fixUpdateAgent();

    }

    public void fixUpdateAgent()
    {
        if (onGround)
        {
            jumpPhase = 0;
            contactNormal.Normalize();
        }
        else
        {
            contactNormal = Vector3.up;
        }
        if(actionArguments != null)
            executeAction(actionArguments);
        onGround = false;
        contactNormal = Vector3.zero;
    }


    //Signiture: Jump(Vector3 target, float jumpHeight).
    public void Jump(object[] arguments)
    {
        float g = Physics.gravity.y;
        if (!onGround)
        {
            state = StateMachine.airborne;
        }
        //Get arguments.
        Vector3 target = Vector3.negativeInfinity;
        target = (Vector3)arguments[0];
        float jumpHeight = (float)arguments[1];

        //Do action.
        if (jumpPhase < maxJumps)
        {

            float drag = _mbody.drag;
            float jumpVel = Mathf.Sqrt(-2f * g * jumpHeight);
            float epsilon = 1.0f; //- (fallMultiplier*2f);
            float time = 0;//Mathf.Sqrt(2 * jumpHeight / Mathf.Abs(g));

            //If a target is given, find velocity needed to reach it. Otherwise just jump upwards.
            Vector3 currentPos = _collider.bounds.center;

            Vector3 dir = (target - currentPos).normalized;

            float distance = Vector3.Distance(currentPos, target);
            distance = Mathf.Clamp(distance, 0.0f, jumpRadius);

            Vector3 targetPositionConstrained = currentPos + dir * distance;

            Vector3 deltaPos = targetPositionConstrained - currentPos;

            float deltaHeight = Vector3.Distance(currentPos, targetPositionConstrained) + jumpHeight;
            drag = 1.0f - (drag * Time.fixedDeltaTime);

            time = Mathf.Sqrt(2 * deltaHeight / Mathf.Abs(g));


            jumpVel = Mathf.Sqrt(-2f * g * deltaHeight);
            
            Vector3 vel = ((deltaPos + 0.5f*Physics.gravity * time * time + ( _mbody.drag * drag * deltaPos * time) + ( 0.125f * Physics.gravity * Time.fixedDeltaTime * time * time ) + (deltaPos * time * 0.125f) ) / time) * epsilon + contactNormal * jumpVel;
            _mbody.velocity = vel;

        }

    }

    public void Airborne(object[] arguments)
    {
        AgentStateClasses subState = SubStates["airborne"];
        //Checks if landed.
        if (onGround)
        {
            state = StateMachine.idle;
            return;
        }
        
        //Checks if agent is falling.
        Vector3 vel = _mbody.velocity;
        if (vel.y < 0f)
        {
            //increase speed of fall.
            vel += 0.85f*Physics.gravity * Time.fixedDeltaTime;// * fallMultiplier;
            //Set state to falling.
            subState.state = 1; //Falling
        }
        else if (vel.y > 0f)
        {
            subState.state = 0; //Rising
        }
        _mbody.velocity = vel;
        
    }

    //Do utterly nothing, stop calculations.
    public void Pause(object[] arguments)
    {

    }


    public void Idle(object[] arguments)
    {
        
    }

    public void Crouching(object[] arguments)
    {
        
    }

    // Signiture:  Moving(Vector3 direction, float m_speed)
    public void Moving(object[] arguments)
    {
        //Get arguments.
        Vector3 direction = (Vector3)arguments[0];
        float m_speed = (float)arguments[1];
        if (velBoostOn)
        {
            m_speed *= 2.5f;
            
        }
        //Do action.
        Vector3 vel = _mbody.velocity;
        Vector3 xAxis = ProjectOnContactPlane(Vector3.right).normalized;
        Vector3 zAxis = ProjectOnContactPlane(Vector3.forward).normalized;

        float currentX = Vector3.Dot(xAxis, _mbody.velocity);
        float currentZ = Vector3.Dot(zAxis, _mbody.velocity);

        delta += Time.deltaTime * 10f;
        float x = Mathf.MoveTowards(currentX, (direction * m_speed * Time.deltaTime).x, delta);
        float z = Mathf.MoveTowards(currentZ, (direction * m_speed * Time.deltaTime).z, delta);
        vel += xAxis * (x - currentX) + zAxis * (z - currentZ);
        if (delta > 1)
            delta = 0.0f;

        //Store result.
        _mbody.velocity = vel;
        rototateTowardsMovement();
        if(_dashingSmoke)
            if (_dashingSmoke.on == false)
                SmokeEffects();
    }

    void rototateTowardsMovement()
    {
        if (_mbody.velocity.magnitude > 0)
        {
            Vector3 movementDir = _mbody.velocity.normalized;
            Quaternion toRotation = Quaternion.LookRotation(movementDir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotation, 720f * Time.deltaTime);
        }
    }

    IEnumerator VelocityBoost()
    {
        float timer = 0;
        while (timer < 0.1f)
        {
            timer += Time.deltaTime;
            yield return new WaitForSeconds(Time.deltaTime);
        }
        velBoostOn = false;
        StopCoroutine(DisableSmear());
        StartCoroutine(DisableSmear());
        //_dashingSmoke.TurnOn();
    }


    //The queue must clear for the positions to ensure no bugs.
    void Smear()
    {
        if (_smear == null)
            return;
        if (forceSmear)
        {
            _smear.enabled = true;
            _smear.sendFrameToGPU = true;
            return;
        }
        if (_mbody.velocity.magnitude > 6  && !_smear.enabled)
        {
            _smear.enabled = true;
            _smear._recentPositions.Clear();
        }

        
        if (_smear._recentPositions.Count > 0)
        {
            Vector3 _prevPos = _smear._recentPositions.Peek();
            float diff = Vector3.Distance(_prevPos, gameObject.transform.position);
            if (_mbody.velocity.magnitude <= 0.01f && diff < 0.01f)
            {
                StopCoroutine(DisableSmear());
                StartCoroutine(DisableSmear());
            }
        }


    }

    IEnumerator DisableSmear()
    {
        _smear.enabled = true;
        _smear.sendFrameToGPU = false;
        yield return new WaitForSeconds(Time.deltaTime * 8f); //Wait 8 frames.
        _smear.sendFrameToGPU = true;
        _smear.enabled = false;

    }

    void SmokeEffects()
    {
        if (SDFSmokeON == false)
            return;
        if (_dashingSmoke == null)
            return;
        _dashingSmoke.gameObject.transform.position = this.transform.position;

        _dashingSmoke.setRotation(_mbody.velocity);
        
    }

    public void setFullPath()
    {
        fullPath.Clear();
        //Add navmesh path
        navAgent.enabled = true;
        NavMeshPath navpath = new NavMeshPath();
        if (navAgent.CalculatePath(path.GetPosition(0), navpath))
        {
            for (int i = 0; i < navpath.corners.Length-1; i++)
            {
                fullPath.Enqueue(navpath.corners[i]);
            }
        }

        if (navpath.status == NavMeshPathStatus.PathComplete)
        {
            //Add drawing path.
            for (int i = 0; i < path.positionCount; i++)
            {
                fullPath.Enqueue(path.GetPosition(i));
            }
        }

        destination = fullPath.Dequeue();

        navAgent.enabled = false;
    }

    public void SetDestination(Vector3 target)
    {
        fullPath.Clear();
        //Add navmesh path
        navAgent.enabled = true;
        NavMeshPath navpath = new NavMeshPath();
        if (navAgent.CalculatePath(target, navpath))
        {
            for (int i = 0; i < navpath.corners.Length - 1; i++)
            {
                fullPath.Enqueue(navpath.corners[i]);
            }
        }

        /*
        if (navpath.status == NavMeshPathStatus.PathComplete)
        {
            //Add drawing path.
            for (int i = 0; i < path.positionCount; i++)
            {
                fullPath.Enqueue(path.GetPosition(i));
            }
        }
        */

        fullPath.Enqueue(target);
        destination = fullPath.Dequeue();

        navAgent.enabled = false;
    }

    //Handle collision data.
    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("This agent: " + this.name + " Has: " + collision.gameObject.name);
        HandleLanding(collision);
        CalculateCollision(collision);
    }

    private void OnTriggerEnter(Collider collider)
    {
        object[] objects = new object[1];
        objects[0] = collider;
        OnTriggered(objects);
    }

    void HandleLanding(Collision collision)
    {
        if (IsTrulyAirborne())
        {
            if (_smoke)
            {
                _smoke.enabled = true;
                _smoke.gameObject.SetActive(true);
                _smoke.gameObject.transform.localPosition = gameObject.transform.position;
            }

            /*
            if (currentlyFollowed && _camControl != null)
                _camControl.AddForce(0.000001f);
            */

            if (OnLanding != null)
            {
                //Debug.Log("How long unit been at this state: " + "State: " + state + " , " + _stateTime[(int)state].ToString("F7"));
                object[] objects = new object[1];
                objects[0] = collision;
                OnLanding(objects);
            }
        }
    }

    void OnCollisionStay(Collision collision)
    {
        CalculateCollision(collision);
        if (OnColliding != null)
        {
            OnColliding(null);
        }
    }


    void OnValidate()
    {
        minGroundDotProduct = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
    }

    //Checks the normals of the contact point to decide up vector.
    void CalculateCollision(Collision collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector3 normal = collision.GetContact(i).normal;
            //onGround |= normal.y >= minGroundDotProduct;
            if (normal.y >= minGroundDotProduct)
            {
                onGround = true;
                contactNormal += normal;
            }
            else
            {
                contactNormal = Vector3.up;
            }
        }
    }

    Vector3 ProjectOnContactPlane(Vector3 vector)
    {
        return vector - (contactNormal * Vector3.Dot(contactNormal, vector));
    }

    //Handles what to do each state
    private void DetermineAction()
    {
        if (state == StateMachine.pause)
        {
            executeAction = Pause;
        }
        if (state == StateMachine.idle)
        {
            executeAction = Idle;
        }
        if (state == StateMachine.crouching)
        {
            executeAction = Crouching;
        }
        if (state == StateMachine.jumping)
        {
            executeAction = Jump;
        }

        if (state == StateMachine.moving)
        {
            executeAction = Moving;
        }

        if (state == StateMachine.airborne)
        {
            executeAction = Airborne;
        }
    }

    //Set state actions so that transitions are feasible.
    public void setStateMoving()
    {
        if (state == StateMachine.moving)
            return;
        state = StateMachine.moving;
        velBoostOn = true;
        StopCoroutine(VelocityBoost());
        StartCoroutine(VelocityBoost());
        

    }

    public void setStatePause()
    {
        if (state == StateMachine.pause)
            return;
        ResetStateTime();
        state = StateMachine.pause;
    }

    public void setStateJumping()
    {
        if (state == StateMachine.jumping)
            return;
        ResetStateTime();
        state = StateMachine.jumping;
        jumpPhase += 1;
    }

    public void setStateIdle()
    {
        if (state == StateMachine.idle)
            return;
        ResetStateTime();
        state = StateMachine.idle;
    }

    public void setStateCrouching()
    {
        if (state == StateMachine.crouching)
            return;
        ResetStateTime();
        state = StateMachine.crouching;
    }

    public void setStateAirborne()
    {
        if (state == StateMachine.airborne)
            return;
        ResetStateTime();
        state = StateMachine.airborne;
    }

    private void ResetStateTime()
    {
        for (int i = 0; i < _stateTime.Length; i++)
        {
            _stateTime[i] = 0;
        }
    }

    public StateMachine getState()
    {
        return state;
    }

    public Vector3 getDestination()
    {
        return destination;
    }

    //Determines the arguments in the function for the delegate.
    public void SetActionArguments(object[] arguments)
    {
        actionArguments = arguments;
    }

    public AgentSkill[] getCurrentSkills()
    {
        foreach (AgentSkill skill in skills)
        {
            Debug.Log(skill.name);
        }
        return currentSkills;
    }


    public void setCurrentSkills(AgentSkill[] skillList)
    {
        currentSkills = skillList;
    }

    public virtual void Seeking(Vector3 target)
    {

    }

    public virtual void Seeking()
    {

    }

    public void moveAway()
    {

    }

    public void EnableColliders(bool enable)
    {
        if (_collider != null)
            _collider.enabled = enable;
        if (_meshcollider != null)
            _meshcollider.enabled = enable;
        if (_spherecollider != null)
            _spherecollider.enabled = enable;
        if (_triggerCollider != null)
            _triggerCollider.enabled = enable;
        if(_boxCollider != null)
            _boxCollider.enabled = enable;
    }

    public bool IsTrulyAirborne()
    {
        return !onGround && state == AgentPhysics.StateMachine.airborne && _stateTime[(int)AgentPhysics.StateMachine.airborne] > 0.02f;
    }

    private void OnDestroy()
    {
        AgentPhysicsManager.RemoveAgentFromList(this);
    }

    public void DoNothing(object[] arguments)
    {
        //Do nothing. Used for delegates.
    }

    public override string ToString()
    {
        return key;
    }
}
