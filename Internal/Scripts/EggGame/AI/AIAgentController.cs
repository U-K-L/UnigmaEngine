using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIAgentController : MonoBehaviour
{
    EggPlayer _player;
    EggGameManager gameManager;
    
    // Start is called before the first frame update
    void Start()
    {
        _player = GetComponent<EggPlayer>();
        gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<EggGameManager>();
    }

    // Update is called once per frame
    void Update()
    {
        ChooseBlock();
    }

    void ChooseBlock()
    {
        if (!_player)
            return;

        EggLocatorUnit unit = _player.GetCurrentUnit();

        if (!unit)
            return;

        List<BlockEntity> blocks = ListPossibleMoves();

        if (blocks == null)
            return;

        gameManager.SetJumpCommand(unit, blocks[0]);
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
