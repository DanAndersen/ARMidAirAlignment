using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.UI;
using TMPro;


public class NetworkManagerHUDHolo : MonoBehaviour
{
    public NetworkManager manager;

    public Button buttonLanHost;
    public Button buttonLanClient;
    public Button buttonLanServerOnly;
    public Button buttonCancelConnectionAttempt;
    public Button buttonClientReady;
    public Button buttonStop;

    public TMP_InputField inputNetworkAddress;

    public TMP_Text textMsg;

    private void Awake()
    {
        if (manager == null)
        {
            Debug.LogError("NetworkManager cannot be null");
        }

        inputNetworkAddress.text = manager.networkAddress;
    }

    private void Start()
    {
        inputNetworkAddress.text = manager.networkAddress;
    }

    // Update is called once per frame
    void Update()
    {
        bool buttonLanHostActive = false;
        bool buttonLanClientActive = false;
        bool buttonLanServerOnlyActive = false;
        bool buttonCancelConnectionAttemptActive = false;
        bool buttonClientReadyActive = false;
        bool buttonStopActive = false;
        bool inputNetworkAddressActive = false;
        string textMsgString = "";


        if (!NetworkClient.isConnected && !NetworkServer.active)
        {
            if (!NetworkClient.active)
            {
                // LAN Host
                if (Application.platform != RuntimePlatform.WebGLPlayer)
                {
                    buttonLanHostActive = true;
                }

                // LAN Client + IP
                buttonLanClientActive = true;

                inputNetworkAddressActive = true;
                manager.networkAddress = inputNetworkAddress.text;

                // LAN Server Only
                if (Application.platform == RuntimePlatform.WebGLPlayer)
                {
                    textMsgString += "(WebGL cannot be server)\n";
                }
                else
                {
                    buttonLanServerOnlyActive = true;
                }
            }
            else
            {
                textMsgString += ("Connecting to " + manager.networkAddress + "...\n");
                buttonCancelConnectionAttemptActive = true;
            }
        }
        else
        {
            // server / client status message
            if (NetworkServer.active)
            {
                textMsgString += ("Server: active. Transport: " + Transport.activeTransport + "\n");
            }
            if (NetworkClient.isConnected)
            {
                textMsgString += ("Client: address=" + manager.networkAddress + "\n");
            }
        }

        // client ready
        if (NetworkClient.isConnected && !ClientScene.ready)
        {
            buttonClientReadyActive = true;
        }

        // stop
        if (NetworkServer.active || NetworkClient.isConnected)
        {
            buttonStopActive = true;
        }

        // update UI
        
        buttonLanHost.gameObject.SetActive(buttonLanHostActive);
        buttonLanClient.gameObject.SetActive(buttonLanClientActive);
        buttonLanServerOnly.gameObject.SetActive(buttonLanServerOnlyActive);
        buttonCancelConnectionAttempt.gameObject.SetActive(buttonCancelConnectionAttemptActive);
        buttonClientReady.gameObject.SetActive(buttonClientReadyActive);
        buttonStop.gameObject.SetActive(buttonStopActive);

        inputNetworkAddress.gameObject.SetActive(inputNetworkAddressActive);

        textMsg.text = textMsgString;

    }

    public void OnClickLanHostButton()
    {
        manager.StartHost();
    }

    public void OnClickLanClientButton()
    {
        manager.StartClient();
    }

    public void OnClickLanServerOnlyButton()
    {
        manager.StartServer();
    }

    public void OnClickCancelConnectionAttemptButton()
    {
        manager.StopClient();
    }

    public void OnClickClientReadyButton()
    {
        ClientScene.Ready(NetworkClient.connection);

        if (ClientScene.localPlayer == null)
        {
            ClientScene.AddPlayer();
        }
    }

    public void OnClickStopButton()
    {
        manager.StopHost();
    }
}
