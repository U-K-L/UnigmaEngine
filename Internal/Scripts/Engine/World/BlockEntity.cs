using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockEntity : MonoBehaviour
{
    // Start is called before the first frame update
    public string hashKey; //used to find where this object is.
    public GameObject _indicator;
    public Vector3 KeyPos;
    private Dictionary<string, AgentPhysics> _agents;
    public Dictionary<string, BlockEntity> neighbors;

    private BoxCollider _collider;
    public enum StateMachine { normal, broken, occupied, airborne, disconnected };
    public StateMachine state;

    public bool blockFallen = false;
    public Dictionary<string, BlockEntity> connectedBlocks;

    public BlockGraph blockGraph;
    private EggGameManager eggGameManager;

    public Material material;

    public int _maxDurability = 1;
    public int _currentDurability = 1;

    private bool isShaking = false;
    public void Start()
    {
        _currentDurability = _maxDurability;
        GameObject notNull = GameObject.FindGameObjectWithTag("GameManager");
        if (notNull)
        {
            eggGameManager = notNull.GetComponent<EggGameManager>();
        }
        connectedBlocks = new Dictionary<string, BlockEntity>();
        Rigidbody body = GetComponent<Rigidbody>();
        if (body == null)
        {
            body = gameObject.AddComponent<Rigidbody>();
        }
        body.isKinematic = true;
        body.useGravity = false;
        body.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
        //remove mesh collider
        if (GetComponent<MeshCollider>() != null)
        {
            Destroy(GetComponent<MeshCollider>());
        }
        //Do the same for box collider
        if (GetComponent<BoxCollider>() == null)
        {
            _collider = gameObject.AddComponent<BoxCollider>();
            _collider.size = new Vector3(0.95f, 1f, 0.95f);
            _collider.isTrigger = false;
        }
        hashKey = transform.name;

        material = GetComponent<Renderer>().material;
    }


    public void CreateReferences()
    {
        //Checks if neighbors exist if so clear it if not create it
        if (neighbors == null)
        {
            neighbors = new Dictionary<string, BlockEntity>();
        }
        else
        {
            neighbors.Clear();
        }
        hashKey = transform.name;
    }
    
    public void setupBlock()
    {
        _agents = new Dictionary<string, AgentPhysics>();
    }
    // Update is called once per frame
    void Update()
    {
        //DebugCurrentUnitsOnMe();
        if (_currentDurability == 0 && _maxDurability > 0 && isShaking == false)
        {
            SetShaking();
        }


    }

    void SetShaking()
    {
        material.SetFloat("_ShakingOn", 0.097f);
        isShaking = true;
    }

    private void FixedUpdate()
    {

    }

    public void startDescend()
    {
        StopAllCoroutines();
        StartCoroutine(descendBlock());
    }
    
    IEnumerator descendBlock()
    {
        Vector3 offsetTarget = new Vector3(0, 20, 0);
        Vector3 finalPos = transform.position + offsetTarget;
        float speed = 10f;
        while (Vector3.Distance(transform.position, finalPos) >= 0.01)
        {

            transform.position = Vector3.Lerp(transform.position, finalPos, Time.deltaTime * speed);
            yield return new WaitForSeconds(.02f);
        }
    }

    public Dictionary<string, AgentPhysics> GetAgentStack()
    {
        return _agents;
    }

    public void AddAgent(AgentPhysics agent)
    {
        if (_agents == null || agent == null)
            return;
        _agents.Add(agent.key, agent);
    }

    public bool FindAgent(AgentPhysics agent)
    {
        if (_agents == null || agent == null)
            return false;
        return _agents.ContainsKey(agent.key);
    }

    public void RemoveAgent(AgentPhysics agent)
    {
        if (_agents == null  || agent == null)
            return;
        _agents.Remove(agent.key);
    }

    public void ClearAllAgents()
    {
        if (_agents == null)
            return;
        _agents.Clear();
    }

    public bool HasAgentOnMe()
    {
        if (_agents == null)
            return false;
        if (_agents.Count > 0)
            return true;
        return false;
    }

    public bool HasAgentOnMeNotPlayer()
    {
        bool ans = HasAgentOnMe();
        if (ans == true)
        {
            foreach (KeyValuePair<string, AgentPhysics> agent in _agents)
            {
                if (agent.Value != null)
                {
                    if (agent.Value.GetComponent<EggLocatorUnit>().isPlayer)
                        return false;
                }
            }
        }
        return ans;
    }

    public void DebugCurrentUnitsOnMe()
    {
        foreach (KeyValuePair<string, AgentPhysics> agent in _agents)
        {
            string agentName = agent.Value.name;
            Debug.Log(this.name + ": " + agentName);
        }
    }

    public void DetermineBlockDrop()
    {
        Debug.Log("Is the server active? " + UnigmaNetworkManager.IsServerOn);
        if (UnigmaNetworkManager.IsServerOn)
        {
            DropBlockServer();
        }
        else {
            DropBlock();
        }

    }
    
    private void DropBlockServer()
    {
        NetworkClient.localPlayer.gameObject.GetComponent<EggPlayer>().DropBlock(hashKey);
    }

    public void DropBlock()
    {
        if (_currentDurability > 0)
        {
            _currentDurability--;
            return;
        }
        Rigidbody body = transform.GetComponent<Rigidbody>();
        body.isKinematic = false;
        body.useGravity = true;
        SetBlockFell();
    }

    public void SetBlockFell()
    {
        blockFallen = true;
    }

    public bool GetIsFallen()
    {
        return blockFallen;
    }

    public void SetIndicator(GameObject indicator)
    {
        if (blockFallen == true)
            return;
        if (_indicator == null)
        {
            GameObject indicatorInstance = Instantiate(indicator, transform);
            _indicator = indicatorInstance;
            indicatorInstance.transform.name = "Indicator: " + transform.name;
            indicatorInstance.transform.localPosition = Vector3.zero;
            indicatorInstance.transform.localScale = _indicator.transform.localScale * 0.35f;
            indicatorInstance.transform.localPosition += new Vector3(transform.localScale.x / 2, 0, -1 * transform.localScale.z / 2) + ( Vector3.up + new Vector3(0, + (0.075f / transform.localScale.y), 0));
        }
    }

    public void RemoveIndicator()
    {
        Destroy(_indicator);
        _indicator = null;
    }

    public Vector3 CenterOfBlock()
    {
        return transform.position + new Vector3(transform.localScale.x / 2, transform.localScale.y, -1*transform.localScale.z / 2);
    }

    public void CalculateBlockDrop()
    {
        //SetBlockFell();
        Rigidbody body = transform.GetComponent<Rigidbody>();
        body.isKinematic = false;
        body.useGravity = true;
        state = StateMachine.airborne;
        foreach (KeyValuePair<string, BlockEntity> block in connectedBlocks)
        {
            Debug.Log(block.Value.name);
            block.Value.CalculateBlockDrop();
        }
        connectedBlocks.Clear();
    }

    void OnCollisionEnter(Collision collision)
    {
        BlockEntity block = collision.gameObject.GetComponent<BlockEntity>();
        EggLocatorUnit unit = collision.gameObject.GetComponent<EggLocatorUnit>();
        if (block != null)
        {
            CollidedWithOtherBlock(block);
            
        }
        //Checks if it is the player
        else if (unit != null)
        {
            //checks if the player is above the block and has been in the air for a certain amount of time.
            Debug.Log(collision.contacts[0].normal.y);
            if (collision.contacts[0].normal.y > 0.0f  && unit._agent.IsTrulyAirborne())
            {
                collision.gameObject.GetComponent<EggLocatorUnit>().CrashedIntoBlock(this);
            }
            /*
            if (collision.gameObject.GetComponent<EggLocatorUnit>().isPlayer)
            {
                state = StateMachine.occupied;
            }
            */
        }
    }

    void CollidedWithOtherBlock(BlockEntity block)
    {
        Debug.Log("Collided other block!");
        state = StateMachine.normal;
        if (connectedBlocks.ContainsKey(block.hashKey) == false)
            connectedBlocks.Add(block.hashKey, block);

        RaycastHit[] hits;
        hits = Physics.RaycastAll(CenterOfBlock(), -transform.up, 1);

        for (int i = 0; i < hits.Length; i++)
        {
            BlockEntity blockobj = hits[i].collider.gameObject.GetComponent<BlockEntity>();
            if (blockobj != null)
            {
                Debug.Log(blockobj.hashKey);
                if (connectedBlocks.ContainsKey(blockobj.hashKey) == false)
                {
                    Debug.Log(blockobj.name);
                    connectedBlocks.Add(blockobj.hashKey, blockobj);
                }
            }
        }

    }

    private void OnDrawGizmos()
    {
        Debug.DrawRay(CenterOfBlock() + new Vector3(0, -transform.localScale.y, 0), -transform.up, Color.red, 1);

    }
}
