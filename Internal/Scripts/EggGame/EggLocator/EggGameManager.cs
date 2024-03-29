﻿/*
 * Created by U.K.L. on 2/4/2023
 * 
 * Handles the client side of the game. Handles the game state and the players for the clients
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using static UnityEngine.UI.CanvasScaler;
using UnityEngine.SceneManagement;

public class EggGameManager : MonoBehaviour
{
    public int ID = 0;
    
    public BlockGraph _blockGraph;
    public EggGameCommandManager _commandManager;

    public bool MapLoaded = false;
    public bool MatchReady = false;
    public bool AgentsCreated = false;
    public bool AgentsSetup = false;

    private List<BlockEntity> startingBlocks = null;
    public int startingBlockIndex = 0;
    
    private EggStage current_stage;

    public List<GoldenEgg> goldenEggList = new List<GoldenEgg>();
    public int EggsToWin = 1;

    public GameObject[] players;

    public Dictionary<string, GameObject> ManagerPlayers = new Dictionary<string, GameObject>();
    public MatchNetworkEgg matchNetworkEgg;
    public Dictionary<string, EggLocatorUnit> GlobalUnits = new Dictionary<string, EggLocatorUnit>();

    public EggStageManager stageManager;

    [HideInInspector]
    public CameraController _cam;

    [HideInInspector]
    public bool matchStart = false;
    // Start is called before the first frame update
    void Start()
    {
        StartGameManager();
    }

    public void StartGameManager()
    {
        _commandManager = GetComponent<EggGameCommandManager>();
        _cam = GameObject.FindGameObjectWithTag("CameraController").GetComponent<CameraController>();
        players = new GameObject[2];
        matchStart = true;
    }

    // Update is called once per frame
    void Update()
    {
        MatchReady = EggGameMaster.Instance.MatchReady();
        if (MatchReady)
        {
            if (!AgentsCreated && EggGameMaster.Instance != null)
            {
                Debug.Log("creating stage");
                if(current_stage == null)
                    current_stage = InstantiateStage();
                CreateStage(current_stage);
            }


            if (Input.GetKey(KeyCode.R))
                Reset();
        }
    }

    EggStage InstantiateStage()
    {
        if (EggGameMaster.Instance.currentStage != null)
        {
            try
            {
                EggStage stagePrefab = Instantiate(EggGameMaster.Instance.currentStage.stage_prefab, stageManager.transform).GetComponent<EggStage>();
                Debug.Log("current stage: " + stagePrefab.gameObject.name);
                _blockGraph = GetComponent<BlockGraph>();
                Debug.Log("current stage: " + _blockGraph.name);
                _blockGraph.CreateGraph(stagePrefab.gameObject);
                
                return stagePrefab;
            }
            catch (System.Exception e)
            {
                Debug.Log(e);
            }
            
        }
        else
        {
            Debug.Log("Seems the stage did not load properly");
        }
        return null;
    }

    void CreateStartingBlocks(EggStage stage)
    {
        startingBlocks = new List<BlockEntity>();

        for (int i = 0; i < stage.startingBlocks.Length; i++)
        {
            BlockEntity block = stage.startingBlocks[i].GetComponent<BlockEntity>();
            if(block)
                startingBlocks.Add(block);
        }

    }

    void CreateStage(EggStage stage)
    {
        
        if (EggGameMaster.Instance.gameMode == EggGameMaster.GameMode.Singleplayer)
        {
            SingleplayerCreateAgents();
        }
        else if (EggGameMaster.Instance.gameMode == EggGameMaster.GameMode.Multiplayer)
        {
            if(!AgentsSetup)
                SetUpAgents();
            if(AllPlayersReady())
                MultiplayerCreateAgents();
        }
    }

    bool AllPlayersReady()
    {
        foreach (KeyValuePair<string, GameObject> pair in ManagerPlayers)
        {
            if(!pair.Value.GetComponent<EggPlayer>().playerBegin)
                return false;
        }
        return true;
    }

    void ClearStage()
    {
        foreach (Transform child in stageManager.transform)
        {
            GameObject.Destroy(child.gameObject);
        }
        MatchReady = false;
        AgentsCreated = false;
        matchStart = false;
        current_stage = null;
        foreach (GameObject player in players)
        {
            if (player != null)
            {
                player.GetComponent<EggPlayer>().Reset();
            }
        }
        GlobalUnits.Clear();
        _commandManager.StopExecutingCommands();
        EggGameMaster.Instance._matchReady = false;
    }

    void SetUpAgents()
    {
        foreach (KeyValuePair<string, GameObject> pair in ManagerPlayers)
        {
            if (pair.Value == null)
            {
                Debug.Log("Player is null");
                continue;
            }
            GameObject player = pair.Value;
            player.GetComponent<EggPlayer>().CreateParty();
            players[player.GetComponent<EggPlayer>().ID] = player;
        }
        AgentsSetup = true;
    }

    public void MultiplayerCreateAgents()
    {
        Debug.Log("Amount of players: " + ManagerPlayers.Count);

        AgentsCreated = true;
        createAgents();
        GetComponent<TimerUI>().StartTimer();
    }

    void SingleplayerCreateAgents()
    {
        Debug.Log("creating singleplayer");
        if (players[0] == null)
        {
            players[0] = Instantiate(GetComponentInChildren<UnigmaNetworkManager>().playerPrefab, this.transform);
            players[0].GetComponent<EggPlayer>().CreateParty();
            players[1] = Instantiate(GetComponentInChildren<UnigmaNetworkManager>().playerPrefab, this.transform);
            players[1].GetComponent<EggPlayer>().CreateParty();
            players[1].GetComponent<EggPlayer>().isPlayer = false;
            players[1].GetComponent<EggPlayer>().isAI = true;
        }
        AgentsCreated = true;
        createAgents();
        GetComponent<TimerUI>().StartTimer();
        ID = -1;
    }
    void UnitJumpsToPosition(EggLocatorUnit unit, Vector3 target)
    {
        if (unit != null)
        {
            unit.JumpToPosition(target);
            //_playerCursor.removeObjectFromList(agent);
            unit.isSelected = false;
            _blockGraph.RemoveAllIndicators();
        }
    }

    void UnitJumpsToSelectedBlock(EggLocatorUnit unit, BlockEntity block)
    {
        if (unit != null)
        {
            unit.JumpToBlock(block);
            //_playerCursor.removeObjectFromList(agent);
            unit.isSelected = false;
            _blockGraph.RemoveAllIndicators();
        }
    }

    public void SetJumpCommand(EggLocatorUnit unit, BlockEntity block)
    {
        Object[] objects = new Object[2];
        objects[0] = unit;
        objects[1] = block;
        _commandManager.AddCommand(unit, unit.name, "jump", unit.speed, objects);
    }

    public IEnumerator CommandJump(string id, Object[] objects)
    {
        //unit, block.
        EggLocatorUnit unit = (EggLocatorUnit)objects[0];
        BlockEntity block = (BlockEntity)objects[1];

        while (unit.isRecovering)
        {
            Lock();
            yield return new WaitForSeconds(0.02f);
        }
        if (unit.isRecovering == false)
            unit.JumpToBlock(block);
    }

    public void Unlock()
    {
        EggGameCommandManager.Unlock();
    }

    public void Lock()
    {
        EggGameCommandManager.Lock();
    }

    public void LockPlayers()
    {
        foreach (GameObject player in players)
        {
            player.GetComponent<EggPlayer>().SetLockedState();
        }
    }

    public void UnlockPlayers()
    {
        foreach (GameObject player in players)
        {
            player.GetComponent<EggPlayer>().SetControllableState();
        }
    }

    public void ClearAllCommands()
    {
        foreach(KeyValuePair<string, EggLocatorUnit> unit in GlobalUnits)
        {
            unit.Value.ClearCommandQueue();
        }
    }

    /*
    void UnitJumpsToPosition()
    {
        foreach (KeyValuePair<string, AgentPhysics> agentMap in _playerCursor.getSelectedObjects())
        {
            AgentPhysics agent = agentMap.Value;
            if (agent != null)
            {
                EggLocatorUnit unit = agent.GetComponent<EggLocatorUnit>();
                if (unit != null)
                {
                    unit.JumpToPosition(_playerCursor.GetRayCastedHit());
                    _playerCursor.removeObjectFromList(agent);
                    unit.isSelected = false;
                    _blockGraph.RemoveAllIndicators();
                }
            }
        }
    }

    void UnitJumpsToSelectedBlock()
    {
        //Gets the block that the player has selected.
        GameObject block = _playerCursor.GetCursorPointsToObject();
        if (block != null)
        {
            BlockEntity blockEntity = block.GetComponent<BlockEntity>();
            if (blockEntity != null)
            {
                foreach (KeyValuePair<string, AgentPhysics> agentMap in _playerCursor.getSelectedObjects())
                {
                    AgentPhysics agent = agentMap.Value;
                    if (agent != null)
                    {
                        EggLocatorUnit unit = agent.GetComponent<EggLocatorUnit>();
                        if (unit != null)
                        {
                            unit.JumpToBlock(blockEntity);
                            _playerCursor.removeObjectFromList(agent);
                            unit.isSelected = false;
                            _blockGraph.RemoveAllIndicators();
                        }
                    }
                }
            }
        }
    }

    void DebugBlock()
    {
        GameObject block = _playerCursor.GetCursorPointsToObject();
        if (block != null)
        {
            BlockEntity blockEntity = block.GetComponent<BlockEntity>();
            List<BlockEntity> blocks = _blockGraph.GetBlocksLevelSet(blockEntity, 1);
            _blockGraph.AddIndicators(blocks);

        }
       
    }
    */
    void createAgents()
    {
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] == null)
            {
                continue;
            }
            Debug.Log("Attempt to get the player");
            EggPlayer player = players[i].GetComponent<EggPlayer>();
            player.ID = i;
            if (player)
            {
                for (int j = 0; j < player.party.Length; j++)
                {
                    Debug.Log("Create units player");
                    createAgent(i, player.party[j]);
                }
            }
        }
        BubbleGumTransition bubble = _cam._cam.GetComponent<BubbleGumTransition>();
        bubble.OpenAnimationPlay();

        _commandManager.StartExecutingCommands();
    }

    void createAgent(int player, int index)
    {
        //Create player controlled character.
        GameObject characterObj = EggGameData.LoadCharacterFromIndex(index);
        characterObj = GameObject.Instantiate(characterObj, stageManager.transform);
        EggLocatorUnit Unit = characterObj.GetComponentInChildren<EggLocatorUnit>();
        //Temporary hack before making a proper character select screen.
        Debug.Log("Player is: " + player + " " + current_stage.startingBlocks[player].name);
        Unit.SetCurrentBlock(current_stage.startingBlocks[player].GetComponent<BlockEntity>());
        Debug.Log("Created");

        characterObj.transform.position = Unit.GetCurrentBlock().CenterOfBlock() + Vector3.up * 72.25f;
        //kanaloa.transform.position += new Vector3(0, 100, 0);
        Unit.isPlayer = true;
        Unit.owner = player.ToString();

        EggPlayer playerUnit = players[player].GetComponent<EggPlayer>();
        if (playerUnit)
        {
            Unit.name = Unit.name + "[" + player + "]";
            Unit.speed += index;
            playerUnit.AddUnitToParty(Unit);

            if (matchNetworkEgg != null)
            {
                if(matchNetworkEgg.isServer)
                    matchNetworkEgg.AddUnitToGlobalDictionary(Unit.gameObject);
                
            }
            GlobalUnits.Add(Unit.name, Unit);
        }

        //kanaloaUnitPlayer = Kanaloa_Unit;

        //cameraController.SetPivot(kanaloaUnitPlayer.transform.position);

        
        
    }

    /*
    public void AddEggToCount(GoldenEgg egg)
    {
        goldenEggList.Add(egg);
        if (goldenEggList.Count >= EggsToWin)
        {
            Victory();
        }
    }
    */
    IEnumerator Victory()
    {
        Debug.Log("You win!");
        GetComponent<TimerUI>().StopTimer();
        yield return new WaitForSeconds(2);
        Reset();
    }
    void Reset()
    {
        /*
        EggLocatorUnit Kanaloa_Unit = kanaloa.GetComponentInChildren<EggLocatorUnit>();
        kanaloa.transform.position = Kanaloa_Unit.GetCurrentBlock().CenterOfBlock() + Vector3.up * 72.25f;

        Destroy(current_stage);
        GameObject new_current_stage = Instantiate(Stage);
        _blockGraph.Reset();
        _blockGraph.CreateGraph(new_current_stage);
        current_stage = new_current_stage;
        */
        StopAllCoroutines();
        StartCoroutine(RestartMatch());
    }

    IEnumerator RestartMatch()
    {
        Camera cam = Camera.main;
        BubbleGumTransition bubble = cam.GetComponent<BubbleGumTransition>();
        bubble.CloseAnimationPlay();

        while (bubble.slider < 0.98f)
        {
            yield return null;
        }

        ClearStage();
        EggGameMaster.Instance.ReloadStage();
        
    }

    IEnumerator TransitionScenes()
    {
        Camera cam = Camera.main;
        BubbleGumTransition bubble = cam.GetComponent<BubbleGumTransition>();
        bubble.CloseAnimationPlay();

        while (bubble.slider < 0.98f)
        {
            yield return null;
        }

        EggGameMaster.Instance.TitleScreen();
    }

    public int GetID()
    {
        return ID - 1;
    }

    public void CheckVictory()
    {
        //Victory if all players on a team have been eliminated... goes to last team remaining.
        int victoryIndex = -1;
        int playersRemain = 0;
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] == null)
            {
                continue;
            }
            EggPlayer player = players[i].GetComponent<EggPlayer>();
            if (player)
            {
                if (player.IsUnitsDead())
                {
                    Debug.Log(player.name + " is dead");
                }
                else
                {
                    victoryIndex = i;
                    playersRemain++;
                }
            }
        }

        if (playersRemain == 1)
        {
            Debug.Log("Player " + victoryIndex + " wins!");
            StartCoroutine(Victory());
        }
    }
}
