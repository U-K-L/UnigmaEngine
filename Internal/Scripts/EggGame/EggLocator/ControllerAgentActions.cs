using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControllerAgentActions : MonoBehaviour
{
    // Start is called before the first frame update
    private PlayerCursorEggGame _cursor;
    private Stack<AgentPhysics> _agentStack;
    private int maxSelected = 1;
    private EggLocatorGrid _grid;
    void Start()
    {
        _grid = GameObject.FindGameObjectWithTag("Stage").GetComponent<EggLocatorGrid>();
        _cursor = GetComponentInChildren<PlayerCursorEggGame>();
        _agentStack = new Stack<AgentPhysics>();
    }

    // Update is called once per frame
    void Update()
    {
        if (_grid == null)
        {
            _grid = GameObject.FindGameObjectWithTag("Stage").GetComponent<EggLocatorGrid>();
        }
        UpdateInputs();
    }

    void UpdateInputs()
    {
        if (Input.GetMouseButtonDown(0))
        {

            if (_cursor.GetCursorPointsToObject() != null)
            {
                AgentPhysics agentPhysics = _cursor.GetCursorPointsToObject().GetComponent<AgentPhysics>();
                if (agentPhysics != null)
                {
                    if (agentPhysics != null)
                        SelectAgent(agentPhysics);
                }
                else if (_cursor.GetCursorPointsToObject().GetComponent<BlockEntity>() != null)
                    CheckIfValidMove();

            }

        }
    }

    void CheckIfValidMove()
    {
        
        GameObject blockObj = _cursor.GetCursorPointsToObject();
        if (blockObj == null)
            return;
        BlockEntity block = blockObj.GetComponent<BlockEntity>();
        if (block == null)
            return;

        if (block._indicator != null)
            MoveSelectedObject(block);
    }

    //Moves the selected agent to the block
    void MoveSelectedObject(BlockEntity block)
    {
        //Get the agents that are selected.
        Dictionary<string, AgentPhysics> selectedObjs = _cursor.getSelectedObjects();

        if (_cursor.GetRayCastedPoint().x > Vector3.negativeInfinity.x)
        {
            //For each of the agents that are selected move them to the block.
            foreach (KeyValuePair<string, AgentPhysics> entry in selectedObjs)
            {
                //Get the agent and block needed, as well as the rigid body.
                AgentPhysics agent = entry.Value;
                EggLocatorUnit unit = agent.transform.GetComponent<EggLocatorUnit>();

                
                //Set the agent to jump.
                agent.SetActionArguments(new object[] { block.transform.position, 4f });
                agent.setStateJumping();

                //Performs the movement.
                unit.PerformMoveAbility();

                //Set the new block as the one the agent stands on.
                unit.SetCurrentBlock(block);
                
                /*
                if (unit.isPlayer)
                    unit.dropsBlock = true; //Makes the next block fall automatically when standing too long.
                */

                //Remove all indicators.
                _grid.RemoveAllLocators();
            }
            RemoveAllObjectsFromList();
        }
    }

    void SelectAgent(AgentPhysics agent)
    {
        EggLocatorUnit unit = agent.GetComponent<EggLocatorUnit>();
        _grid.RemoveAllLocators();
        
        //If the same object.
        if (_cursor.getSelectedObjects().ContainsKey(agent.ToString()))
        {
            _cursor.removeObjectFromList(agent);

        }
        else
        {
            //Add object, add to stack.
            
            if (unit.isPlayer)
            {
                _cursor.addObjectToList(agent);
                _agentStack.Push(agent);
                unit.FindPossibleBlocks();
            }
        }

    }

    private void RemoveAllObjectsFromList()
    {
        _cursor.removeAllObjectFromList();
    }
}
