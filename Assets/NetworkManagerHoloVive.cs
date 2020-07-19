using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

// Custom NetworkManager to handle spawning the prefabs representing the Vive-tracked objects.
public class NetworkManagerHoloVive : NetworkManager
{
    public NetworkIdentity viveHeadset;
    public NetworkIdentity viveLeftController;
    public NetworkIdentity viveRightController;
    public NetworkIdentity viveControllerInputManager;
    
    public NetworkIdentity holoHeadset;
    public NetworkIdentity worldAnchorInverter;

    public NetworkIdentity sessionManager;

    public override void OnStartServer()
    {
        base.OnStartServer();
    }
    
    public override void OnServerDisconnect(NetworkConnection conn)
    {
        Debug.Log("OnServerDisconnect");

        NetworkIdentity[] objectsToHandle = new NetworkIdentity[] { viveHeadset, viveLeftController, viveRightController, viveControllerInputManager, sessionManager, holoHeadset, worldAnchorInverter };

        foreach (NetworkIdentity objectToHandle in objectsToHandle)
        {
            if (objectToHandle.connectionToClient?.identity == conn.identity)
            {
                Debug.Log("OnServerDisconnect, removing client authority from object " + objectToHandle.gameObject.name);
                objectToHandle.RemoveClientAuthority();
            }
        }
        
        // call base functionality (actually destroys the player)
        base.OnServerDisconnect(conn);
    }

    public override void OnStopClient()
    {
        base.OnStopClient();

        Debug.Log("OnStopClient");

        if (viveControllerInputManager != null)
        {
            viveControllerInputManager.GetComponent<ViveControllerInputManager>().DoClearAnySpawnedFreeModePose();
        }
    }
}
