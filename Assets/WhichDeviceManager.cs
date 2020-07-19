using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WhichDeviceManager : MonoBehaviour
{
    private static WhichDeviceManager _instance = null;
    
    public static WhichDeviceManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = GameObject.FindObjectOfType<WhichDeviceManager>();

                if (_instance == null)
                {
                    GameObject container = new GameObject("WhichDeviceManager");
                    _instance = container.AddComponent<WhichDeviceManager>();
                }
            }

            return _instance;
        }
    }

    public void Start()
    {
        if (_whichDevice == DeviceType.Uninitialized)
        {
            DetermineWhichDevice();
        }
    }

    public enum DeviceType
    {
        Uninitialized,
        HoloLens,
        Vive
    }

    private DeviceType _whichDevice = DeviceType.Uninitialized;
    public DeviceType ThisDeviceType
    {
        get
        {
            if (_whichDevice == DeviceType.Uninitialized)
            {
                throw new System.Exception("WhichDeviceManager returned a ThisDeviceType of Uninitialized");
            }
            return _whichDevice;
        }
    }

    private void DetermineWhichDevice()
    {
        bool isVive = false;

        // need to detect whether this player is the HoloLens or the Vive.
        // we can check by querying the node states, and seeing if there is a Vive-only device.
        List<UnityEngine.XR.XRNodeState> nodeStates = new List<UnityEngine.XR.XRNodeState>();
        UnityEngine.XR.InputTracking.GetNodeStates(nodeStates);

        foreach (var nodeState in nodeStates)
        {
            // we assume only a Vive has the corner lighthouse trackers
            if (nodeState.nodeType == UnityEngine.XR.XRNode.TrackingReference)
            {
                isVive = true;
                break;
            }
        }

        Debug.Log("WhichDeviceManager: this device is vive? " + isVive);

        _whichDevice = isVive ? DeviceType.Vive : DeviceType.HoloLens;
    }

    public bool IsHoloLens()
    {
        return ThisDeviceType == DeviceType.HoloLens;
    }

    public bool IsVive()
    {
        return ThisDeviceType == DeviceType.Vive;
    }
}
