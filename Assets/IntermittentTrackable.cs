using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class IntermittentTrackable : NetworkBehaviour
{
    [System.Serializable]
    public struct State
    {
        public bool IsOn;
        public bool IsTracked;
        public bool GotPose;
    }

    // This is for objects like the controllers/trackers of either the Holo or the Vive coordinate systems.
    // It contains a flag to determine if the object is currently being tracked.
    // This helps us handle the case where we had moved around a controlled in the past, 
    // but the tracking currently isn't working, so we can't rely on the (stale) pose for any calibration.

    [SyncVar]
    public IntermittentTrackable.State DeviceState = new State();
    
    // Note that there is a distinction between having a current active pose, and the device being on or off.

    public bool DeviceHasValidPose()
    {
        return DeviceState.IsOn && DeviceState.IsTracked && DeviceState.GotPose;
    }
    
    [Command]
    public void CmdSet(IntermittentTrackable.State newState)
    {
        DeviceState = newState;
    }

    public void Set(IntermittentTrackable.State newState)
    {
        if (!newState.Equals(DeviceState))
        {
            CmdSet(newState);
        }
    }
}
