using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.MultiplayerModels;
using PlayFab.Networking;
using Mirror;

public class ClientStartup : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        LoginWithCustomIDRequest request = new LoginWithCustomIDRequest()
        {
            TitleId = PlayFabSettings.TitleId,
            CreateAccount = true,
            CustomId = SystemInfo.deviceUniqueIdentifier
        };
        PlayFabClientAPI.LoginWithCustomID(request, OnPlayFabLoginSuccesss, OnLoginError);
    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnPlayFabLoginSuccesss(LoginResult loginResult)
    {
        Debug.Log("Log in success: " + loginResult);
        RequestMultiplayerServer();
    }

    private void RequestMultiplayerServer()
    {
        Debug.Log("[ClientStartUp].RequestMultiplayerServer");
        RequestMultiplayerServerRequest requestData = new RequestMultiplayerServerRequest
        {
            BuildId = "9f301f73-4575-4f1b-827c-7d7e06b08280",
            SessionId = "ba67d671-512a-4e7d-a38c-2329ce181911",
            PreferredRegions = new List<string> { "EastUs" },
        };
        PlayFabMultiplayerAPI.RequestMultiplayerServer(requestData, OnRequestMultiplayerServer, OnRequestMultiplayerServerFailure);

    }

    private void OnRequestMultiplayerServer(RequestMultiplayerServerResponse response)
    {
        if (response == null)
        {
            Debug.Log("[ClientStartUp].OnRequestMultiplayerServer: response is null");
            return;
        }

        //Print out user IP and port
        Debug.Log("[ClientStartUp].OnRequestMultiplayerServer: IP: " + response.IPV4Address + " Port: " + response.Ports[0].Num);

        //Setting the network address and port
        UnityNetworkServer.Instance.networkAddress = response.IPV4Address;
        UnityNetworkServer.Instance.GetComponent<kcp2k.KcpTransport>().Port = (ushort)response.Ports[0].Num;

        //Start the client
        UnityNetworkServer.Instance.StartClient();
    }

    private void OnRequestMultiplayerServerFailure(PlayFabError error)
    {
        Debug.Log("[ClientStartUp].OnRequestMultiplayerServerFailure");
        Debug.Log(error);
        Debug.Log(error.GenerateErrorReport());
    }

    void OnLoginError(PlayFabError playFabError)
    {
        Debug.Log("Log in failed: " + playFabError.ErrorMessage);
    }
}
