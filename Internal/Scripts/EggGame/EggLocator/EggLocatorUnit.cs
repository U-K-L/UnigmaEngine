/* Created by U.K.L. on 8/16/2022
 * 
 * The unit class for all actors within the game.
 * 
 */


using BoingKit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Collision = UnityEngine.Collision;

public class EggLocatorUnit : MonoBehaviour
{
    // Start is called before the first frame update
    public int move = 3; //How far can move to adjacent blocks.
    private BlockEntity _currentBlock; //The current block this unit stands on.
    private Dictionary<string, GameObject> _blockMatrix;
    private EggLocatorGrid _grid;
    private float timer;
    public AgentPhysics _agent;
    public bool dropsBlock = false;
    public bool isPlayer = false;
    private int currentMoves = 0;

    public bool decreaseMoveEachJump = false;
    public bool previousBlockWillFall = false;

    private bool _AIJUMPREADY = true;
    public bool dead = false;
    public bool isSelected = false;

    private AudioSource _audioJump;

    private BlockGraph _blockGraph;

    public float jumpRadius = 1f;
    public string owner = "";
    public int speed = 3;

    private Vector3 previousBlockPosition; //stores the last position of the previous block before it fell.
    private EggGameManager eggGameManager;

    //Remove this for a more formal recovering state within agent physics.
    public bool isRecovering = false;
    void Start()
    {
        GameObject notNull = GameObject.FindGameObjectWithTag("GameManager");
        if (notNull)
        {
            eggGameManager = notNull.GetComponent<EggGameManager>();
            _blockGraph = GameObject.FindGameObjectWithTag("GameManager").GetComponent<BlockGraph>();
        }

        GameObject jump = Resources.Load<GameObject>("Sounds/Jump/Jump");
        jump = Instantiate(jump, transform);
        _audioJump = jump.GetComponent<AudioSource>();
        _agent = GetComponent<AgentPhysics>();
        
        
        _agent.OnLanding += OnLanding;
        _agent.OnColliding += OnColliding;
        _agent.OnTriggered += OnTriggered;
        _agent.setStateIdle();
        _agent.GetComponent<AnimationControllerSprites>().SetAnimation("Airborne");
    }
    // Update is called once per frame
    void Update()
    {
        jumpRadius = move;
        //_agent.jumpRadius = jumpRadius;
        /*
        EnsureStageLoaded();
        //If this unit is capable of dropping blocks merely by standing idle then drop them.
        if (dropsBlock && currentMoves > 0)
            CalculateBlockDrop();
        
        //Determine what to do if dead.
        IsDead();

        //If nothing else needs to be done, search for the next block to move to.
        if (_blockMatrix != null && isPlayer == false && _AIJUMPREADY && _grid.gameStarted && _grid.gamestate == EggLocatorGrid.GameState.Playing)
            FindBlockToJumpTo();
        */
        if (_agent.getState() == AgentPhysics.StateMachine.airborne)
        {
            Debug.Log(_agent.SubStates["airborne"].GetCurrentStateAsString());
        }

        //Debug Jumping
        if (Input.GetKeyDown(KeyCode.J))
        {
            Debug.Log("Jumping");
            StartCoroutine(JumpToTargetNoBlockFall(transform.position + new Vector3(0, 40, 0)));
        }

    }

    //Ensures this unit has the stage loaded so it can know what blocks are available.
    void EnsureStageLoaded()
    {
        
    }

    public void FindPossibleBlocks(BlockEntity block = null)
    {
        
    }

    public void SetIndicator(int i, int j, int k)
    {

    }


    public void SetCurrentBlock(BlockEntity block)
    {
        timer = Time.time;
        _currentBlock = block;
        _currentBlock.RemoveAgent(_agent);
        _currentBlock.AddAgent(_agent);
    }

    public BlockEntity GetCurrentBlock()
    {
        return _currentBlock;
    }

    public void PerformMoveAbility()
    {
        
    }

    public void DecreaseMove()
    {
        
    }

    public void OnLanding(object[] arguments)
    {
        Collision collision = (Collision)arguments[0];
        for (int i = 0; i < collision.contactCount; i++)
        {
            GoldenEgg egg = collision.GetContact(i).otherCollider.transform.GetComponent<GoldenEgg>();
            BlockEntity block = collision.GetContact(i).otherCollider.transform.GetComponent<BlockEntity>();

            //Decide what to do depending on object landed with.
            //if (egg != null)
                //AddEggToCount(egg);
            if (block != null)
            {
                SetCurrentBlock(block);
            }
        }

        Debug.Log(gameObject.name + " landed on " + collision.gameObject.name);
        AdjustToBlock();
        if(eggGameManager)
            eggGameManager.Unlock();
    }

    public void OnTriggered(object[] arguments)
    {
        Collider collider = (Collider)arguments[0];
        EggLocatorUnit unit = collider.gameObject.GetComponent<EggLocatorUnit>();
        //Log state
        Debug.Log(name + " triggered " + _agent.getState() );
        if (unit != null)
            if (CanKnockBackUnit(unit) && _agent.IsTrulyAirborne() && !isRecovering)
                KnockOffUnit(unit);
    }

    void AdjustToBlock()
    {
        if (_currentBlock == null)
            return;
        Vector3 blockPosition = _currentBlock.CenterOfBlock();
        Vector3 unitPosition = transform.position;

        //Ensure the distance isn't too great, otherwise would look jarring.
        if (Vector3.Distance(blockPosition, unitPosition) < 1f)
        {
            //We only want to change X/Z not the Y component.
            transform.position = new Vector3(blockPosition.x, transform.position.y, blockPosition.z);
        }
        isRecovering = false;
    }

    public void OnColliding(object[] arguments)
    {
        
    }

    /*
    void AddEggToCount(GoldenEgg egg)
    {
        GameObject.FindGameObjectWithTag("GameManager").GetComponent<EggGameManager>().AddEggToCount(egg);
    }
    */
    
    public void KnockOffUnit(EggLocatorUnit unit)
    {
        //Get the proper bodies.
        unit.isRecovering = true;
        Debug.Log("KNOCK EM OUT: " + unit.name);
        AgentPhysics agent = unit._agent;
        Rigidbody body = agent.GetComponent<Rigidbody>();

        //agent.EnableColliders(false);
        //Calculate the direction to knock the unit.
        Vector3 direction = Vector3.Normalize(unit._currentBlock.transform.position - previousBlockPosition);

        //Get the target position.
        Vector3 targetPosition = agent.transform.position + new Vector3(direction.x, 0, direction.z) * 2.0f;

        StartCoroutine(KnockOffEffect());
        //Perform the jump
        agent.SetActionArguments(new object[] { targetPosition, 8f });
        agent.setStateJumping();
    }

    IEnumerator KnockOffUnit()
    {

        yield return new WaitForSeconds(0.75f);
        foreach (KeyValuePair<string, AgentPhysics> entry in _currentBlock.GetAgentStack())
        {
            if (entry.Key == _agent.key)
                continue;
            AgentPhysics agent = entry.Value;
            agent.transform.Find("container").gameObject.SetActive(true);
            Rigidbody body = agent.GetComponent<Rigidbody>();
            body.AddExplosionForce(6000.0f, agent.transform.position, 500.0f, 1.5f);
            Vector3 direction = agent.transform.position - Camera.main.transform.position;
            agent.GetComponent<EggLocatorUnit>().StartCoroutine("killSelf");
        }
        StartCoroutine(KnockOffEffect());
    }

    IEnumerator KnockOffEggs()
    {

        yield return new WaitForSeconds(0.75f);
        foreach (KeyValuePair<string, AgentPhysics> entry in _currentBlock.GetAgentStack())
        {
            if (entry.Key == _agent.key)
                continue;

        }
        StartCoroutine(KnockOffEffect());
    }

    public void CalculateBlockDrop()
    {
        float difference = Time.time - timer;
        if (difference > 5)
        {
            CurrentBlocksFall();
        }
    }
    IEnumerator KnockOffEffect()
    {
        if (eggGameManager)
        {
            eggGameManager._cam.perpCam.BeginTransition(0.01f);
            eggGameManager._cam.Dolly();
        }
        yield return new WaitForSeconds(0.3f);
        Time.timeScale = 0.08f;
        yield return new WaitForSeconds(0.12f);
        Time.timeScale = 1f;
        eggGameManager._cam.perpCam.BeginTransition(1f);
        yield return new WaitForSeconds(0.1f);
        eggGameManager._cam.EndDolly();
    }
    
    public IEnumerator killSelf()
    {
        yield return new WaitForSeconds(2f);
        EggLocatorGrid.TotalEggs += 1;
        Destroy(this.gameObject);
    }
    void IsDead()
    {
        if (transform.position.y < -20)
        {
            if (!isPlayer)
            {
                EggLocatorGrid.TotalEggs += 1;
                Destroy(this.gameObject);
            }
            dead = true;
        }
    }

    public void Die()
    {
        dead = true;
        eggGameManager.CheckVictory();
    }

    private void FindBlockToJumpTo()
    {

        //int i = Mathf.CeilToInt(Random.value * move);
        //StartCoroutine(JumpTo(_blockMatrix["6,0,0"].GetComponent<BlockEntity>()));
        
        List<BlockEntity> blocks = GetPossibleBlocks(_currentBlock);
        int i = Mathf.CeilToInt(Random.value * (blocks.Count-1));
        if (i < 0)
            i = 0;
        else if (i > blocks.Count)
            i = blocks.Count - 1;
        if (blocks[i] != null)
            StartCoroutine(JumpTo(blocks[i]));
    }


    public List<BlockEntity> GetPossibleBlocks(BlockEntity block = null)
    {

        List<BlockEntity> blocks = new List<BlockEntity>();
        if (_blockMatrix == null)
            return null;
        if (_currentBlock == null)
            return null;
        for (int i = 1; i <= move; i++)
        {
            for (int j = 0; j <= move; j++)
            {
                for (int k = 1; k <= move; k++)
                {
                    GetBlocks(i, j, k, blocks);
                }
            }
        }
        return blocks;
    }

    IEnumerator JumpToTarget(Vector3 target)
    {
        _AIJUMPREADY = false;
        float pauseTime = 0.15f;
        yield return new WaitForSeconds(pauseTime);
        _audioJump.Play();
        _agent.SetActionArguments(new object[] { target, 8f });
        _agent.setStateJumping();
        yield return new WaitForSeconds(pauseTime*3f);
        PerformMoveAbilityAI();
        _AIJUMPREADY = true;
    }

    IEnumerator JumpToTargetNoBlockFall(Vector3 target)
    {
        _AIJUMPREADY = false;
        float pauseTime = 0.15f;
        yield return new WaitForSeconds(pauseTime);
        _audioJump.Play();
        _agent.SetActionArguments(new object[] { target, 8f });
        _agent.setStateJumping();
        yield return new WaitForSeconds(pauseTime * 3f);
        _AIJUMPREADY = true;
    }

    IEnumerator JumpTo(BlockEntity block)
    {
        _AIJUMPREADY = false;
        float pauseTime = 0.15f;
        _agent.jumpRadius = Vector3.Distance(transform.position, block.CenterOfBlock());
        yield return new WaitForSeconds(pauseTime);
        _audioJump.Play();
        _agent.SetActionArguments(new object[] { block.CenterOfBlock(), 8f });
        _agent.setStateJumping();
        yield return new WaitForSeconds(pauseTime);
        PerformMoveAbilityAI();
        _AIJUMPREADY = true;
        //SetCurrentBlock(block);
    }

    IEnumerator Jump(Vector3 target)
    {
        _agent.setStateCrouching();
        yield return new WaitForSeconds(0.225f);
        _audioJump.Play();
        float jumpHeight = 12f;
        _agent.SetActionArguments(new object[] { _agent.transform.position + Vector3.up * jumpHeight, jumpHeight });
        _agent.setStateJumping();
    }

    public void EggGoesInvisible(object[] arguments)
    {
        transform.Find("container").gameObject.SetActive(false);
    }

    public void PerformMoveAbilityAI()
    {

        if (previousBlockWillFall)
        {
            CurrentBlocksFall();
        }


    }

    public void CurrentBlocksFall()
    {
        BlockEntity block = _currentBlock;
        previousBlockPosition = new Vector3(_currentBlock.transform.position.x, _currentBlock.transform.position.y, _currentBlock.transform.position.z);
        Debug.Log("This block falls: " + _agent.name);
        Debug.Log("This block falls: " + block.name);
        if (block)
        {
            block.DetermineBlockDrop();
        }
    }

    public void OnClick()
    {
        if (isSelected)
        {
            _agent.setStateCrouching();
            GetBlocksToJumpToByDistance();
        }
        else
        {
            _agent.setStateIdle();
            _blockGraph.RemoveAllIndicators();
        }
    }

    public void SetCrouchingState()
    {
        if (_agent.getState() != AgentPhysics.StateMachine.crouching)
        {
            _agent.setStateCrouching();
        }
        else
        {
            _agent.setStateIdle();
        }
    }

    public void GetBlocksToJumpTo()
    {
        BlockEntity block = _currentBlock;
        if (block != null)
        {
            List<BlockEntity> blocks = _blockGraph.GetBlocksLevelSet(block, move);
            _blockGraph.AddIndicators(blocks);
        }
    }

    public void GetBlocksToJumpToByDistance()
    {
        BlockEntity block = _currentBlock;
        if (block != null)
        {
            List<BlockEntity> blocks = _blockGraph.GetBlocksLevelSetByDistance(block, move);
            _blockGraph.AddIndicators(blocks);
        }
    }

    public void JumpToBlock(BlockEntity block)
    {
        StartCoroutine(JumpTo(block));
    }

    public void JumpToPosition(Vector3 pos)
    {
        StartCoroutine(JumpToTarget(pos));
    }

    public void CrashedIntoBlock(BlockEntity block)
    {
        Debug.Log("crashed dud!!!");
        _agent.SetActionArguments(new object[] { block.CenterOfBlock(), 4f });
        _agent.setStateJumping();
        block.DetermineBlockDrop();
    }

    public bool CanKnockBackUnit(EggLocatorUnit unit)
    {
        bool value = false;
        Rigidbody thisUnit = _agent._mbody;
        Rigidbody otherUnit = unit._agent._mbody;

        if (thisUnit.velocity.magnitude - otherUnit.velocity.magnitude > 0.1f)
        {
            value = true;
        }
        
        return value;
    }


    public void GetBlocks(int i, int j, int k, List<BlockEntity> blocks)
    {


        float x = _currentBlock.KeyPos.x;
        float z = _currentBlock.KeyPos.z;
        float xn = _currentBlock.KeyPos.x - i;
        float xp = _currentBlock.KeyPos.x + i;
        float zn = _currentBlock.KeyPos.z - k;
        float zp = _currentBlock.KeyPos.z + k;


        GameObject block = null;

        //Adjacent.
        if (_blockMatrix.ContainsKey("" + xn + "," + j + "," + z))
            block = _blockMatrix["" + xn + "," + j + "," + z];
        if (block != null)
        {

            BlockEntity entity = block.GetComponent<BlockEntity>();
            if (entity.HasAgentOnMeNotPlayer() == false && !entity.GetIsFallen())
                blocks.Add(entity);
        }

        if (_blockMatrix.ContainsKey("" + xp + "," + j + "," + z))
            block = _blockMatrix["" + xp + "," + j + "," + z];
        if (block != null)
        {

            BlockEntity entity = block.GetComponent<BlockEntity>();
            if (entity.HasAgentOnMeNotPlayer() == false && !entity.GetIsFallen())
                blocks.Add(entity);
        }

        if (_blockMatrix.ContainsKey("" + x + "," + j + "," + zp))
            block = _blockMatrix["" + x + "," + j + "," + zp];
        if (block != null)
        {

            BlockEntity entity = block.GetComponent<BlockEntity>();
            if (entity.HasAgentOnMeNotPlayer() == false && !entity.GetIsFallen())
                blocks.Add(entity);
        }

        if (_blockMatrix.ContainsKey("" + x + "," + j + "," + zn))
            block = _blockMatrix["" + x + "," + j + "," + zn];
        if (block != null)
        {

            BlockEntity entity = block.GetComponent<BlockEntity>();
            if (entity.HasAgentOnMeNotPlayer() == false && !entity.GetIsFallen())
                blocks.Add(entity);
        }

        //Diagonals
        xp -= 1;
        xn += 1;
        zp -= 1;
        zn += 1;
        if (_blockMatrix.ContainsKey("" + xp + "," + j + "," + zp))
            block = _blockMatrix["" + xp + "," + j + "," + zp];
        if (_currentBlock.hashKey == "" + xp + "," + j + "," + zp)
            return;
        if (block != null)
        {

            BlockEntity entity = block.GetComponent<BlockEntity>();
            if (entity.HasAgentOnMeNotPlayer() == false && !entity.GetIsFallen())
                blocks.Add(entity);
        }

        if (_blockMatrix.ContainsKey("" + xn + "," + j + "," + zp))
            block = _blockMatrix["" + xn + "," + j + "," + zp];
        if (_currentBlock.hashKey == "" + xn + "," + j + "," + zp)
            return;
        if (block != null)
        {

            BlockEntity entity = block.GetComponent<BlockEntity>();
            if (entity.HasAgentOnMeNotPlayer() == false && !entity.GetIsFallen())
                blocks.Add(entity);
        }

        if (_blockMatrix.ContainsKey("" + xp + "," + j + "," + zn))
            block = _blockMatrix["" + xp + "," + j + "," + zn];
        if (_currentBlock.hashKey == "" + xp + "," + j + "," + zn)
            return;
        if (block != null)
        {

            BlockEntity entity = block.GetComponent<BlockEntity>();
            if (entity.HasAgentOnMeNotPlayer() == false && !entity.GetIsFallen())
                blocks.Add(entity);
        }

        if (_blockMatrix.ContainsKey("" + xn + "," + j + "," + zn))
            block = _blockMatrix["" + xn + "," + j + "," + zn];
        if (_currentBlock.hashKey == "" + xn + "," + j + "," + zn)
            return;
        if (block != null)
        {

            BlockEntity entity = block.GetComponent<BlockEntity>();
            if (entity.HasAgentOnMeNotPlayer() == false && !entity.GetIsFallen())
                blocks.Add(entity);
        }
    }
}
