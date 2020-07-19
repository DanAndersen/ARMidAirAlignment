using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;

public class TestPlayerPrefabScript : NetworkBehaviour
{
    public GameObject worldAnchorInverterPrefab;

    [SyncVar]
    public bool isVive = false;

    [SyncVar]
    public bool isHolo = false;

    [SyncVar]
    public string PlayerLogInfo = "";

    private bool initedTrackableObjects = false;
    public NetworkTransform viveLeftController;
    public NetworkTransform viveRightController;
    public NetworkTransform viveHeadset;
    public NetworkTransform holoHeadset;
    
    public WorldAnchorInverter worldAnchorInverter;

    public ViveControllerInputManager viveControllerInputManager;

    public SessionManager sessionManager;

    public override void OnStartServer()
    {
        base.OnStartServer();

        InitTrackableObjects();
    }

    private void InitTrackableObjects()
    {
        try
        {
            {
                GameObject go = GameObject.Find("WorldAnchorInverter");
                if (go != null)
                {
                    worldAnchorInverter = go.GetComponent<WorldAnchorInverter>();
                }
            }

            {
                GameObject go = GameObject.Find("ViveControllerInputManager");
                if (go != null)
                {
                    viveControllerInputManager = go.GetComponent<ViveControllerInputManager>();
                }
            }

            {
                GameObject go = GameObject.Find("SessionManager");
                if (go != null)
                {
                    sessionManager = go.GetComponent<SessionManager>();
                }
            }

            {
                GameObject go = GameObject.Find("ViveLeftController");
                if (go != null)
                {
                    viveLeftController = go.GetComponent<NetworkTransform>();
                }
            }

            {
                GameObject go = GameObject.Find("ViveRightController");
                if (go != null)
                {
                    viveRightController = go.GetComponent<NetworkTransform>();
                }
            }

            {
                GameObject go = GameObject.Find("ViveHeadset");
                if (go != null)
                {
                    viveHeadset = go.GetComponent<NetworkTransform>();
                }
            }

            {
                GameObject go = GameObject.Find("HoloHeadset");
                if (go != null)
                {
                    holoHeadset = go.GetComponent<NetworkTransform>();
                }
            }
            
            bool ok = true;

            if (viveControllerInputManager == null)
            {
                Debug.LogError("couldn't find viveControllerInputManager");
                ok = false;
            }

            if (viveLeftController == null)
            {
                Debug.LogError("couldn't find viveLeftController");
                ok = false;
            }

            if (viveRightController == null)
            {
                Debug.LogError("couldn't find viveRightController");
                ok = false;
            }

            if (viveHeadset == null)
            {
                Debug.LogError("couldn't find viveHeadset");
                ok = false;
            }

            if (holoHeadset == null)
            {
                Debug.LogError("couldn't find holoHeadset");
                ok = false;
            }
            
            initedTrackableObjects = ok;
        }
        catch (Exception e)
        {
            PlayerLogInfo = e.Message + "\n" + e.Source + "\n" + e.StackTrace;
            Debug.LogError(e.Message + "\n" + e.Source + "\n" + e.StackTrace);
            throw e;
        }
    }

    [Command]
    public void CmdSetWhichDevice(bool isVive, bool isHolo, string logString)
    {
        this.isVive = isVive;
        this.isHolo = isHolo;
        this.PlayerLogInfo = logString;

        if (this.isVive)
        {
            AssignViveTrackablesToPlayerConnection(connectionToClient);
        }
        else if (this.isHolo)
        {
            AssignHoloTrackablesToPlayerConnection(connectionToClient);
        }
    }

    public void AssignViveTrackablesToPlayerConnection(NetworkConnection conn)
    {
        viveHeadset.GetComponent<NetworkIdentity>().AssignClientAuthority(conn);
        viveLeftController.GetComponent<NetworkIdentity>().AssignClientAuthority(conn);
        viveRightController.GetComponent<NetworkIdentity>().AssignClientAuthority(conn);
        
        viveControllerInputManager.GetComponent<NetworkIdentity>().AssignClientAuthority(conn);
        sessionManager.GetComponent<NetworkIdentity>().AssignClientAuthority(conn);
    }

    public void AssignHoloTrackablesToPlayerConnection(NetworkConnection conn)
    {
        holoHeadset.GetComponent<NetworkIdentity>().AssignClientAuthority(conn);

        worldAnchorInverter.GetComponent<NetworkIdentity>().AssignClientAuthority(conn);
    }

    // Update is called once per frame
    void Update()
    {
        if (!initedTrackableObjects)
        {
            InitTrackableObjects();
        }

        if (!isLocalPlayer)
        {
            // exit from update if this is not the local player
            return;
        }

        if (!isVive && !isHolo) // uninitialized
        {
            // do the initialization of which device this player represents.

            // need to detect whether this player is the HoloLens or the Vive.
            // we can check by querying the node states, and seeing if there is a Vive-only device.
            List<UnityEngine.XR.XRNodeState> nodeStates = new List<UnityEngine.XR.XRNodeState>();
            UnityEngine.XR.InputTracking.GetNodeStates(nodeStates);

            isVive = false;

            string logString = "";

            foreach (var nodeState in nodeStates)
            {
                logString += nodeState.nodeType + "\n";

                // we assume only a Vive has the corner lighthouse trackers
                if (nodeState.nodeType == UnityEngine.XR.XRNode.TrackingReference)
                {
                    isVive = true;
                }
            }

            PlayerLogInfo = logString;

            isHolo = !isVive;

            CmdSetWhichDevice(isVive, isHolo, logString); // tell the server to send this info to all clients

            if (isVive)
            {
                // hide the cube indicating the vive headset, when we're on the vive, so it doesn't occlude our view
                Renderer viveHeadsetRenderer = viveHeadset.GetComponent<Renderer>();
                if (viveHeadsetRenderer != null)
                {
                    viveHeadsetRenderer.enabled = false;
                }
            }

            if (isHolo)
            {
                // hide the cube indicating the holo headset, when we're on the hololens, so it doesn't occlude our view (in mixed-reality capture)
                Renderer holoHeadsetRenderer = holoHeadset.GetComponent<Renderer>();
                if (holoHeadsetRenderer != null)
                {
                    holoHeadsetRenderer.enabled = false;
                }
            }
        }

        // handle player input for movement.
        // in our case, use the main camera's transform to update the prefab's transform.
        transform.SetPositionAndRotation(Camera.main.transform.position, Camera.main.transform.rotation);

        if (isVive && !isHolo)
        {
            // update the pose of the Vive objects
            List<UnityEngine.XR.XRNodeState> nodeStates = new List<UnityEngine.XR.XRNodeState>();
            UnityEngine.XR.InputTracking.GetNodeStates(nodeStates);

            foreach (var nodeState in nodeStates)
            {
                if (nodeState.nodeType == UnityEngine.XR.XRNode.Head)
                {
                    Vector3 pos = new Vector3();
                    Quaternion rot = new Quaternion();
                    if (nodeState.TryGetPosition(out pos) && nodeState.TryGetRotation(out rot))
                    {
                        viveHeadset.transform.localPosition = pos;
                        viveHeadset.transform.localRotation = rot;
                    }
                }

                if (nodeState.nodeType == UnityEngine.XR.XRNode.LeftHand)
                {
                    Vector3 pos = new Vector3();
                    Quaternion rot = new Quaternion();
                    if (nodeState.TryGetPosition(out pos) && nodeState.TryGetRotation(out rot))
                    {
                        viveLeftController.transform.localPosition = pos;
                        viveLeftController.transform.localRotation = rot;
                    }
                }

                if (nodeState.nodeType == UnityEngine.XR.XRNode.RightHand)
                {
                    Vector3 pos = new Vector3();
                    Quaternion rot = new Quaternion();
                    if (nodeState.TryGetPosition(out pos) && nodeState.TryGetRotation(out rot))
                    {
                        viveRightController.transform.localPosition = pos;
                        viveRightController.transform.localRotation = rot;
                    }
                }
            }

            // now that the Vive poses are updated, handle input from the Vive controller buttons

            UpdateViveControllerInput();

        }
        else if (!isVive && isHolo)
        {
            // we are the HoloLens, update the HoloHeadset object.
            Vector3 holoHeadsetPos = Camera.main.transform.position;
            Quaternion holoHeadsetRot = Camera.main.transform.rotation;

            holoHeadset.transform.localPosition = holoHeadsetPos;
            holoHeadset.transform.localRotation = holoHeadsetRot;
        }
        else
        {
            Debug.LogError("error: this device is uninitialized. exactly one of isVive or isHolo should be true");
        }
    }

    // ========================================================================

    [Command]
    void CmdOnLeftControllerClick()
    {
        viveControllerInputManager.OnLeftControllerClick();
    }

    [Command]
    void CmdOnRightControllerClick()
    {
        viveControllerInputManager.OnRightControllerClick();
    }

    [Command]
    void CmdCycleAlignmentType(int step)
    {
        viveControllerInputManager.CycleAlignmentType(step);
    }

    [Command]
    void CmdCycleTargetPointLocation(int step)
    {
        viveControllerInputManager.CycleTargetPointLocation(step);
    }

    private void VibrateController(UnityEngine.XR.InputDeviceRole deviceRole)
    {
        List<UnityEngine.XR.InputDevice> devices = new List<UnityEngine.XR.InputDevice>();

        UnityEngine.XR.InputDevices.GetDevicesWithRole(deviceRole, devices);

        foreach (var device in devices)
        {
            UnityEngine.XR.HapticCapabilities capabilities;
            if (device.TryGetHapticCapabilities(out capabilities))
            {
                if (capabilities.supportsImpulse)
                {
                    uint channel = 0;
                    float amplitude = 0.5f;
                    float duration = 1.0f;
                    device.SendHapticImpulse(channel, amplitude, duration);
                }
            }
        }
    }

    private void UpdateViveControllerInput()
    {
        if (!isVive)
        {
            return; // make sure we only run this on the device with the Vive
        }

        // NOTE: these labels were set in the input Project Settings, with the positive/negative labels "joystick buton 14" and "joystick button 15".
        // See:
        // - https://docs.unity3d.com/Manual/OpenVRControllers.html
        // and 
        // - https://answers.unity.com/questions/1463656/what-is-unity-button-id.html

        if (Input.GetButtonUp("Left Vive Trigger"))
        {
            VibrateController(UnityEngine.XR.InputDeviceRole.LeftHanded);
            //Debug.Log("ButtonUp for Left Vive Trigger");
            CmdOnLeftControllerClick();
        }

        if (Input.GetButtonUp("Right Vive Trigger"))
        {
            VibrateController(UnityEngine.XR.InputDeviceRole.RightHanded);
            //Debug.Log("ButtonUp for Right Vive Trigger");
            CmdOnRightControllerClick();
        }

        if (ObjectManager.Instance.SessionManager.IsInFreeMode())
        {
            if (Input.GetButtonUp("Left Vive Trackpad Press"))
            {
                float x = Input.GetAxis("Left Vive Trackpad Horizontal Axis");
                float y = Input.GetAxis("Left Vive Trackpad Vertical Axis");

                if (Mathf.Abs(x) > Mathf.Abs(y))
                {
                    int step = (x > 0) ? 1 : -1;
                    CmdCycleAlignmentType(step);
                }
                else
                {
                    //int step = (y > 0) ? 1 : -1;
                    //CmdCycleTargetPointLocation(step);
                }
            }

            if (Input.GetButtonUp("Right Vive Trackpad Press"))
            {
                float x = Input.GetAxis("Right Vive Trackpad Horizontal Axis");
                float y = Input.GetAxis("Right Vive Trackpad Vertical Axis");

                if (Mathf.Abs(x) > Mathf.Abs(y))
                {
                    int step = (x > 0) ? 1 : -1;
                    CmdCycleAlignmentType(step);
                }
                else
                {
                    //int step = (y > 0) ? 1 : -1;
                    //CmdCycleTargetPointLocation(step);
                }
            }
        }
    }
}
