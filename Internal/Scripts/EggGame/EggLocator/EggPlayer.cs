/*
 * Created by U.K.L. on 2/4/2023
 * 
 * Handles the local player client wise. Works offline. Also works with AI.
 */

using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EggPlayer : NetworkBehaviour
{
    // Start is called before the first frame update
    public int ID = 0; //Which player this is.
    private bool isAI = false; //If the player is an AI.
    private PlayerCursor _playerCursor;
    private CameraController cameraController;

    //This should be a party list governed by a seperate menu and save file
    public int[] party;
    private Dictionary<string, EggLocatorUnit> units = new Dictionary<string, EggLocatorUnit>();
    private EggLocatorUnit currentUnit;
    private BlockGraph _blockGraph;
    private EggGameManager gameManager;

    public bool isPlayer = true;

    public enum StateMachine { controllable, locked, selectingTile, selectingUnit };
    public StateMachine state;

    public bool playerBegin = false;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
    public void CreateParty()
    {
         party = new int[1];
    }
    void Start()
    {
        StartPlayer();
    }

    public void StartPlayer()
    {
        _blockGraph = GameObject.FindGameObjectWithTag("GameManager").GetComponent<BlockGraph>();
        gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<EggGameManager>();

        if (!isAI)
        {
            _playerCursor = gameObject.AddComponent<PlayerCursor>();
            cameraController = GameObject.FindGameObjectWithTag("CameraController").GetComponent<CameraController>();
        }
        playerBegin = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (playerBegin)
        {
            if (!isAI && !isServer && isPlayer)
            {
                if (state == StateMachine.controllable || state == StateMachine.selectingTile)
                    UpdateMouseControls();
                UpdateCameraControls();

            }
            if (currentUnit == null && gameManager)
                SetInitialCurrentUnit();
        }
    }

    void UpdateMouseControls()
    {
        if (Input.GetMouseButtonDown(0))
        {
            ChoosePressButtonAction();
        }

    }

    void ChoosePressButtonAction()
    {
        //Checks what type of object is currently selected.
        GameObject selectedObject = _playerCursor.GetCursorPointsToObject();
        if (selectedObject != null)
        {

            if (selectedObject.tag == "Player")
            {
                EggLocatorUnit unit = selectedObject.GetComponent<EggLocatorUnit>();
                AgentPhysics agent = selectedObject.transform.gameObject.GetComponent<AgentPhysics>();
                if (unit && agent)
                    SelectUnit(unit, agent);
            }
            else
            {
                //If object is a block
                Debug.Log("clicked and jumped!!?");
                UnitJumpsToSelectedBlock();
                SetControllableState();
                //UnitJumpsToPosition();

            }
        }
    }
    
    void SelectUnit(EggLocatorUnit unit, AgentPhysics agent)
    {
        if (gameManager.GetID() != int.Parse(unit.owner) && gameManager.ID != -1)
            return;
        if (_playerCursor.getSelectedObjects().ContainsKey(agent.ToString()))
        {
            _playerCursor.removeObjectFromList(agent);
            unit.isSelected = false;
            SelectAllUnits(unit.name, ID);
        }
        else
        {
            _playerCursor.addObjectToList(agent);
            unit.isSelected = true;
            SelectAllUnits(unit.name, ID);
            SetSelectingTileState();
        }
        //Call onclick method for player.
        unit.OnClick();
        currentUnit = unit;
        
    }

    [Command]
    public void SelectAllUnits(string key, int id)
    {
        //id + 1 because it runs on a server authority game, where server is player 0.
        id = id + 1;
        Debug.Log("Command server to select all units");
        if(gameManager.matchNetworkEgg != null)
            gameManager.matchNetworkEgg.SelectAllUnitsClient(key, id);
    }

    //Client Sent determines if this was called from client or if it is called from server.
    void UnitJumpsToSelectedBlock(bool ClientSent = true)
    {
        //Gets the block that the player has selected.
        GameObject block = _playerCursor.GetCursorPointsToObject();
        if (block != null)
        {
            BlockEntity blockEntity = block.GetComponent<BlockEntity>();
            if (blockEntity != null)
            {
                Dictionary<string, AgentPhysics> selectedObjects = _playerCursor.getSelectedObjects();
                List<string> keys = new List<string>(selectedObjects.Keys);

                for (int i = 0; i < keys.Count; i++)
                {
                    AgentPhysics agent = selectedObjects[keys[i]];
                    if (agent != null)
                    {
                        EggLocatorUnit unit = agent.GetComponent<EggLocatorUnit>();
                        if (unit != null)
                        {
                            //unit.JumpToBlock(blockEntity);
                            gameManager.SetJumpCommand(unit, blockEntity);
                            _playerCursor.removeObjectFromList(agent);
                            unit.isSelected = false;
                            _blockGraph.RemoveAllIndicators();
                            if (ClientSent)
                                UpdateUnitsJumpServer(unit.name, blockEntity.hashKey);
                        }
                    }
                }
            }
        }
    }



    void UpdateCameraControls()
    {

        Vector3 pivot;
        pivot = _playerCursor.GetRayCastedHit();
        if (currentUnit != null)
        {
            pivot = currentUnit.transform.position;
            if (state == StateMachine.selectingTile)
            {
                pivot = _playerCursor.GetRayCastedHit();
            }
        }
        if (Input.GetButton("Drag_Desktop"))
        {
            pivot = _playerCursor.GetRayCastedHit();
            cameraController.SetPivot(pivot);
        }
        else if (Input.GetButton("Reset"))
        {
            cameraController._camState = CameraController.CameraState.Resetting;
            cameraController.SetPivot(pivot);
        }//Determines what to do depending on state.
        else if (cameraController._camState == CameraController.CameraState.Tracking)
        {
            cameraController.SetPivot(pivot);
        }
        else if (cameraController._camState == CameraController.CameraState.Controlled)
        {
            pivot = _playerCursor.GetRayCastedHit();
            cameraController.SetPivot(pivot);
        }
        else if (cameraController._camState == CameraController.CameraState.Resetting)
        {
            cameraController.SetPivot(pivot);
        }

    }

    public void Reset()
    {
        units.Clear();
    }

    public void AddUnitToParty(EggLocatorUnit unit)
    {
        units.Add(unit.name, unit);
    }

    private void SetInitialCurrentUnit()
    {
        //Loop through all units in globalunit list
        foreach (KeyValuePair<string, EggLocatorUnit> unitPair in gameManager.GlobalUnits)
        {
            EggLocatorUnit unit = unitPair.Value;
            Debug.Log(gameManager.ID);
            Debug.Log(unit.owner);
            if (gameManager.GetID() == int.Parse(unit.owner))
                currentUnit = unit;
            if (isServer || UnigmaNetworkManager.IsServerOn == false)
                currentUnit = unit;
        }


    }

    public bool IsUnitsDead()
    {
        foreach (KeyValuePair<string, EggLocatorUnit> unitPair in units)
        {
            EggLocatorUnit unit = unitPair.Value;
            if (unit != null)
            {
                if (!unit.dead)
                    return false;
            }
        }
        return true;
    }

    public void SetLockedState()
    {
        state = StateMachine.locked;
    }

    public void SetSelectingTileState()
    {
        state = StateMachine.selectingTile;
    }

    public void SetControllableState()
    {
        state = StateMachine.controllable;
    }

    [Command]
    public void DropBlock(string key)
    {
        gameManager.matchNetworkEgg.DropBlockClients(key);
        gameManager.matchNetworkEgg.DropBlockServer(key);
    }


    [Command]
    public void UpdateUnitsJumpServer(string unitKey, string blockKey)
    {
        Debug.Log("Command server to make all units jump!");
        gameManager.matchNetworkEgg.UpdateUnitsJumpClient(unitKey, blockKey);
        gameManager.matchNetworkEgg.UpdateUnitsJumpServer(unitKey, blockKey);

    }
}
