using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIAgentController : MonoBehaviour
{
    EggPlayer _player;
    EggGameManager gameManager;

    public float ReactionSpeed = 2f;
    
    // Start is called before the first frame update
    void Start()
    {
        _player = GetComponent<EggPlayer>();
        gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<EggGameManager>();
        StartCoroutine(Thinking());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator Thinking()
    {
        while (true)
        {
            yield return new WaitForSeconds(ReactionSpeed);
            DetermineAction();
        }
    }

    void DetermineAction()
    {
        if (!_player)
            return;

        EggLocatorUnit unit = _player.GetCurrentUnit();

        if (!unit)
            return;

        List<BlockEntity> blocks = ListPossibleMoves();

        if (blocks == null)
            return;

        //Go towards closes opponent 50% of the time, go towards a random block 50% of the time.
        //This is a placeholder algorithm for more complex functions including eventual RL.
        GoTowardsClosesOpponent(blocks);
    }

    void GoTowardsClosesOpponent(List<BlockEntity> blocks)
    {
        if (!_player)
            return;

        //go through all the units
        float minDist = Mathf.Infinity;
        EggLocatorUnit closestUnit = null;
        foreach (KeyValuePair<string, EggLocatorUnit> UnitPair in gameManager.GlobalUnits)
        {
            EggLocatorUnit unit = UnitPair.Value;
            //if the unit is not on our team
            if (int.Parse(unit.owner) != 1)
            {
                float dist = Vector3.Distance(unit.transform.position, _player.GetCurrentUnit().transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    closestUnit = unit;
                }
            }
        }

        //Now find the block that gets you closes to that unit.
        minDist = Mathf.Infinity;
        BlockEntity closesBlock = null;
        foreach (BlockEntity block in blocks)
        {
            float dist = Vector3.Distance(block.transform.position, closestUnit.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closesBlock = block;
            }
        }

        Debug.Log(closesBlock);
        gameManager.SetJumpCommand(_player.GetCurrentUnit(), closesBlock);
    }

    List<BlockEntity> ListPossibleMoves()
    {
        if (!_player)
            return null;

        EggLocatorUnit unit = _player.GetCurrentUnit();
        
        if (!unit)
            return null;
        
        List<BlockEntity> blocks = unit.BlockGraph.GetBlocksLevelSetByDistance(unit.GetCurrentBlock(), unit.MoveRange);

        return blocks;
    }
}
