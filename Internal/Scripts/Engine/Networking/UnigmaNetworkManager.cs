using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.Events;
using PlayFab;
using PlayFab.Networking;

public class UnigmaNetworkManager : NetworkManager
{

    public static bool IsServerOn = false;
    public static UnigmaNetworkManager Instance { get; private set; }

    public PlayerEvent OnPlayerAdded = new PlayerEvent();
    public PlayerEvent OnPlayerRemoved = new PlayerEvent();

    public static Dictionary<string, GameObject> Players = new Dictionary<string, GameObject>();

    public int MaxConnections = 100;
    public int Port = 7777; // overwritten by the code in AgentListener.cs

    public List<UnityNetworkConnection> Connections
    {
        get { return _connections; }
        private set { _connections = value; }
    }
    private List<UnityNetworkConnection> _connections = new List<UnityNetworkConnection>();

    public class PlayerEvent : UnityEvent<string> { }

    // Use this for initialization
    public override void Awake()
    {
        base.Awake();
        Instance = this;
        NetworkServer.RegisterHandler<ReceiveAuthenticateMessage>(OnReceiveAuthenticate);
    }
    
    public override void Update()
    {
        base.Update();
    }
    
    public override void OnStartServer()
    {
        Debug.Log("server started");
        IsServerOn = true;
    }

    public override void OnStartClient()
    {
        Debug.Log("client started");
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        Transform startPos = GetStartPosition();
        GameObject player = startPos != null
            ? Instantiate(playerPrefab, startPos.position, startPos.rotation)
            : Instantiate(playerPrefab);

        // instantiating a "Player" prefab gives it the name "Player(clone)"
        // => appending the connectionId is WAY more useful for debugging!
        player.name = $"{playerPrefab.name} [connId={conn.connectionId}]";
        NetworkServer.AddPlayerForConnection(conn, player);

        Players.Add(conn.connectionId.ToString(), player);
        OnPlayerAdded.Invoke(numPlayers.ToString());
    }


    public void StartListen()
    {
        this.GetComponent<TelepathyTransport>().port = (ushort)Port;
        NetworkServer.Listen(MaxConnections);
    }

    public override void OnApplicationQuit()
    {
        base.OnApplicationQuit();
        NetworkServer.Shutdown();
    }

    private void OnReceiveAuthenticate(NetworkConnectionToClient nconn, ReceiveAuthenticateMessage message)
    {
        var conn = _connections.Find(c => c.ConnectionId == nconn.connectionId);
        if (conn != null)
        {
            conn.PlayFabId = message.PlayFabId;
            conn.IsAuthenticated = true;
            OnPlayerAdded.Invoke(message.PlayFabId);
        }
    }

    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        base.OnServerConnect(conn);

        Debug.LogWarning("Client Connected");
        var uconn = _connections.Find(c => c.ConnectionId == conn.connectionId);
        if (uconn == null)
        {
            _connections.Add(new UnityNetworkConnection()
            {
                Connection = conn,
                ConnectionId = conn.connectionId,
                //LobbyId = PlayFabMultiplayerAgentAPI.SessionConfig.SessionId
            });
        }
    }

    public override void OnServerError(NetworkConnectionToClient conn, Exception ex)
    {
        base.OnServerError(conn, ex);

        Debug.Log(string.Format("Unity Network Connection Status: exception - {0}", ex.Message));
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        base.OnServerDisconnect(conn);
        IsServerOn = false;
        var uconn = _connections.Find(c => c.ConnectionId == conn.connectionId);
        if (uconn != null)
        {
            if (!string.IsNullOrEmpty(uconn.PlayFabId))
            {
                OnPlayerRemoved.Invoke(uconn.PlayFabId);
            }
            _connections.Remove(uconn);
        }
    }
    
    [Serializable]
    public class UnityNetworkConnection
    {
        public bool IsAuthenticated;
        public string PlayFabId;
        public string LobbyId;
        public int ConnectionId;
        public NetworkConnection Connection;
    }

    public class CustomGameServerMessageTypes
    {
        public const short ReceiveAuthenticate = 900;
        public const short ShutdownMessage = 901;
        public const short MaintenanceMessage = 902;
    }

    public struct ReceiveAuthenticateMessage : NetworkMessage
    {
        public string PlayFabId;
    }

    public struct ShutdownMessage : NetworkMessage { }

    [Serializable]
    public struct MaintenanceMessage : NetworkMessage
    {
        public DateTime ScheduledMaintenanceUTC;
    }
}
