/*
 * Created by U.K.L. on 2/4/2023
 * 
 * Handles the server side of the game. Handles the game state and the players.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class MatchNetworkEgg : NetworkBehaviour
{
    // Start is called before the first frame update
    private EggGameManager eggGameManager;

    public SyncDictionary<string, GameObject> players = new SyncDictionary<string, GameObject>();
    public SyncDictionary<string, GameObject> units = new SyncDictionary<string, GameObject>();
    private void Start()
    {
        players.Callback += OnPlayersUpdated;
        StartGameManager();
        UnigmaNetworkManager.Instance.OnPlayerAdded.AddListener(OnPlayerAdded);
        

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void StartGameManager()
    {
        eggGameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<EggGameManager>();
        eggGameManager.matchNetworkEgg = this;
    }

    [ClientRpc]
    void CreatingAgentsOnTheClients()
    {
        
        Debug.Log("Agents being created, but we only have: " + players.Count);
        if (eggGameManager == null)
            eggGameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<EggGameManager>();
        if (eggGameManager != null)
        {
            UnigmaNetworkManager.IsServerOn = true;
            //eggGameManager.MultiplayerCreateAgents();
        }
    }

    void CreateAgentsOnTheServer()
    {

        Debug.Log("Agents being created, but we only have: " + players.Count);
        if (eggGameManager == null)
            eggGameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<EggGameManager>();
        if (eggGameManager != null)
        {
            UnigmaNetworkManager.IsServerOn = true;
            //eggGameManager.MultiplayerCreateAgents();
        }
    }

    [ClientRpc]
    void SetPlayerID(int id)
    {
        if (eggGameManager == null)
            eggGameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<EggGameManager>();
        if (eggGameManager != null)
        {
            if(eggGameManager.ID == 0)
                eggGameManager.ID = id;
        }
    }

    void OnPlayerAdded(string id)
    {
        SetPlayerID(int.Parse(id));

        //Starts the match if all players are connected.
        if (int.Parse(id) >= 2)
        {
            Debug.Log(id);
            AddPlayersStartMatch();
        }
    }

    void AddPlayersStartMatch()
    {
        foreach (KeyValuePair<string, GameObject> pair in UnigmaNetworkManager.Players)
        {
            if (players.ContainsKey(pair.Key))
            {
                Debug.Log("Player already in dictionary");
                continue;
            }

            players.Add(pair.Key, pair.Value);
        }
        CreatingAgentsOnTheClients();
        CreateAgentsOnTheServer();
        //LoadMultiplayerPlayers();
        //LoadMultiplayerServer();
    }

    void OnPlayersUpdated(SyncDictionary<string, GameObject>.Operation op, string key, GameObject item)
    {
        switch (op)
        {
            case SyncIDictionary<string, GameObject>.Operation.OP_ADD:
                UpdateClientsPlayers(key, item);
                UpdateServerPlayers(key, item);
                break;
        }
    }

    [ClientRpc]
    void UpdateClientsPlayers(string key, GameObject item)
    {
        if(eggGameManager == null)
            eggGameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<EggGameManager>();

        if (eggGameManager != null)
        {
            if (!eggGameManager.ManagerPlayers.ContainsKey(key))
            {
                item.GetComponent<EggPlayer>().ID = (int)item.GetComponent<NetworkIdentity>().netId - 2;
                eggGameManager.ManagerPlayers.Add(key, item);
            }
        }
    }

    [ClientRpc]
    void LoadMultiplayerPlayers()
    {
        EggGameMaster.Instance.Multiplayer();
    }

    void LoadMultiplayerServer()
    {
        EggGameMaster.Instance.Multiplayer();
    }

    void UpdateServerPlayers(string key, GameObject item)
    {
        if (eggGameManager == null)
            eggGameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<EggGameManager>();

        if (eggGameManager != null)
        {
            if (!eggGameManager.ManagerPlayers.ContainsKey(key))
            {
                item.GetComponent<EggPlayer>().ID = (int)item.GetComponent<NetworkIdentity>().netId - 2;
                eggGameManager.ManagerPlayers.Add(key, item);
            }
        }
    }


    [Command]
    public void AddUnitToGlobalDictionary(GameObject unit)
    {
        units.Add(unit.name, unit);
    }



    [ClientRpc]
    public void SelectAllUnitsClient(string key, int id)
    {
        Debug.Log("Everyone select this unit: " + key + " From player: " + id);
        if (id == eggGameManager.ID)
            return;

        if (eggGameManager.GlobalUnits.ContainsKey(key))
        {
            EggLocatorUnit unit = eggGameManager.GlobalUnits[key].GetComponent<EggLocatorUnit>();
            unit.SetCrouchingState();
        }

    }

    [ClientRpc]
    public void DropBlockClients(string key)
    {
        eggGameManager._blockGraph.BlockMap[key].DropBlock();
    }

    //For the server.
    public void DropBlockServer(string key)
    {
        eggGameManager._blockGraph.BlockMap[key].DropBlock();
    }

    [ClientRpc]
    public void UpdateUnitsJumpClient(string unitKey, string blockKey)
    {
        eggGameManager.GlobalUnits[unitKey].GetComponent<EggLocatorUnit>().JumpToBlock(eggGameManager._blockGraph.BlockMap[blockKey]);
    }

    public void UpdateUnitsJumpServer(string unitKey, string blockKey)
    {
        eggGameManager.GlobalUnits[unitKey].GetComponent<EggLocatorUnit>().JumpToBlock(eggGameManager._blockGraph.BlockMap[blockKey]);
    }
}
