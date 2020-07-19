using System;
using System.Collections;
using System.Collections.Generic;
using Align;
using UnityEngine;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Linq;
using System.Text;
using System.IO;
using Mirror;

public class SessionLabels
{
    public const string StartedSession = "StartedSession";

    public const string DidInitialAlignment = "DidInitialAlignment";
    public const string DidAlignment = "DidAlignment";

    public const string Pose = "Pose";
    public const string Spawn = "Spawn";

    public const string StoppedSession = "StoppedSession";
}

public class SessionEvent
{
    public string Label { get; set; }
    public float Time { get; set; }
}

public class StartedSessionEvent : SessionEvent
{
    public string InterfaceType { get; set; }
    public string TargetPointLocation { get; set; }
    public int NumAlignmentsInSession { get; set; }
    public string WhichController { get; set; }
    public string WhichHeadset { get; set; }
    public float UserHeightToFloor { get; set; }
    public float UserArmOutstretched { get; set; }
    public float UserArmHinged { get; set; }

    public StartedSessionEvent()
    {
        Label = SessionLabels.StartedSession;
    }
}

public class SpawnEvent : SessionEvent
{
    public int PoseIndex { get; set; }
    public DevicePose SpawnedPose { get; set; }

    public SpawnEvent()
    {
        Label = SessionLabels.Spawn;
    }
}

public class PoseEvent : SessionEvent
{
    public DevicePose Headset { get; set; }
    public DevicePose Controller { get; set; }
    public DevicePose UnusedHeadset { get; set; }
    public DevicePose UnusedController { get; set; }
    public float DistMeters { get; set; }
    public float DistDegrees { get; set; }
    public float HeadPitchDegrees { get; set; }

    public PoseEvent()
    {
        Label = SessionLabels.Pose;
    }
}

public class DidAlignmentEvent : PoseEvent
{
    public DidAlignmentEvent()
    {
        Label = SessionLabels.DidAlignment;
    }
}

public class DevicePose
{
    public float[] Position { get; set; }
    public float[] Rotation { get; set; }

    public DevicePose(Vector3 pos, Quaternion rot)
    {
        Position = new float[] { pos.x, pos.y, pos.z };
        Rotation = new float[] { rot.x, rot.y, rot.z, rot.w };
    }
}

public class UnityPose
{
    public Vector3 pos { get; set; }
    public Quaternion rot { get; set; }

    public UnityPose(Vector3 p, Quaternion r)
    {
        pos = p;
        rot = r;
    }
}

public class SessionEventConverter : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        JObject jObject = JObject.Load(reader);
        string label = (string)jObject["Label"];

        SessionEvent target = null;
        if (label == SessionLabels.Pose)
        {
            target = (SessionEvent)new PoseEvent();
        } else if (label == SessionLabels.DidAlignment)
        {
            target = (SessionEvent)new DidAlignmentEvent();
        }
        else if (label == SessionLabels.Spawn)
        {
            target = (SessionEvent)new SpawnEvent();
        } else if (label == SessionLabels.StartedSession)
        {
            target = (SessionEvent)new StartedSessionEvent();
        }
        else
        {
            target = new SessionEvent();
        }
        
        serializer.Populate(jObject.CreateReader(), target);
        return target;
    }

    public override bool CanConvert(Type objectType)
    {
        return typeof(SessionEvent).IsAssignableFrom(objectType);
    }
}

public static class ThreadSafeRandom
{
    [ThreadStatic] private static System.Random Local;

    public static System.Random ThisThreadsRandom
    {
        get { return Local ?? (Local = new System.Random(unchecked(Environment.TickCount * 31 + System.Threading.Thread.CurrentThread.ManagedThreadId))); }
    }
}

static class MyExtensions
{
    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = ThreadSafeRandom.ThisThreadsRandom.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}

public class SessionManager : NetworkBehaviour
{
    public static string SessionFolderName
    {
        get
        {
#if !UNITY_EDITOR && UNITY_WSA
            return Windows.Storage.ApplicationData.Current.RoamingFolder.Path;
#else
            return Application.persistentDataPath;
#endif
        }
    }

    List<SessionEvent> CurrentSessionEvents = new List<SessionEvent>();

    public void Log_OnSessionStarted()
    {
        CurrentSessionEvents.Clear();

        CurrentSessionEvents.Add(
            new StartedSessionEvent() {
                Time = Time.time,
                InterfaceType = ParameterInterfaceType.ToString(),
                NumAlignmentsInSession = ParameterNumAlignmentsInSession,
                TargetPointLocation = ParameterControllerTargetPointLocation.ToString(),
                UserArmHinged = UserArmHinged,
                UserArmOutstretched = UserArmOutstretched,
                UserHeightToFloor = UserHeightToFloor,
                WhichController = ParameterWhichController.ToString(),
                WhichHeadset = ParameterWhichHeadset.ToString(),
            });
    }

    public void Log_OnInitialAlignment()
    {
        CurrentSessionEvents.Add(new SessionEvent() { Label = SessionLabels.DidInitialAlignment, Time = Time.time });
    }

    public void Log_OnAlignment()
    {
        // grab a pose event, and then get the data and put it into a DidAlignment event
        PoseEvent pe = GetLogPoseData();

        Debug.Log("Log_OnAlignment, distDegrees = " + pe.DistDegrees + ", distMeters = " + pe.DistMeters);

        CurrentSessionEvents.Add(
            new DidAlignmentEvent() {
                Time = Time.time,
                Controller = pe.Controller,
                DistDegrees = pe.DistDegrees,
                DistMeters = pe.DistMeters,
                HeadPitchDegrees = pe.HeadPitchDegrees,
                Headset = pe.Headset,
                UnusedController = pe.UnusedController,
                UnusedHeadset = pe.UnusedHeadset,
            });
    }

    public void Log_OnSessionStopped()
    {
        CurrentSessionEvents.Add(new SessionEvent() { Label = SessionLabels.StoppedSession, Time = Time.time });

        Debug.Log("TODO... save and process the session events");

        Debug.Log("CurrentSessionEvents has " + CurrentSessionEvents.Count + " entries");

        string json = JsonConvert.SerializeObject(CurrentSessionEvents);

        string sessionLabel = System.DateTime.Now.ToString("yyyyMMddHHmmss");
        Debug.Log("sessionLabel: " + sessionLabel);

        string outputSessionLogPath = System.IO.Path.Combine(SessionFolderName, string.Format("{0}_session.json", sessionLabel));

        File.WriteAllText(outputSessionLogPath, json);

        Debug.Log("wrote session data to " + outputSessionLogPath);
    }

    public float ComputeHeadPitchDegrees(Transform headset)
    {
        // the horizontal plane is the XZ plane
        return Mathf.Rad2Deg * Mathf.Asin(headset.forward.y);
    }

    public void Log_OnPoseSpawned()
    {
        Debug.Log("start Log_OnPoseSpawned");

        Transform spawnedPoseParent = viveControllerInputManager.CurrentSpawnedPoseParent?.transform;

        if (spawnedPoseParent == null)
        {
            Debug.LogError("attempted to do Log_OnPoseSpawned when the spawnedPoseParent was null");
            return;
        }
        CurrentSessionEvents.Add(new SpawnEvent()
        {
            Time = Time.time,
            PoseIndex = CurrentAlignmentIndex,
            SpawnedPose = new DevicePose(spawnedPoseParent.position, spawnedPoseParent.rotation),
        });
    }

    public PoseEvent GetLogPoseData()
    {
        Transform unusedHeadset = null;
        Transform headset = null;
        if (ParameterWhichHeadset == WhichHeadsetForSpawningPoses.HoloLens)
        {
            headset = ObjectManager.Instance.HoloHeadset;
            unusedHeadset = ObjectManager.Instance.ViveHeadset;
        }
        else if (ParameterWhichHeadset == WhichHeadsetForSpawningPoses.Vive)
        {
            headset = ObjectManager.Instance.ViveHeadset;
            unusedHeadset = ObjectManager.Instance.HoloHeadset;
        }
        else
        {
            Debug.LogError("unhandled headset for spawning poses: " + ParameterWhichHeadset);
            return new PoseEvent();
        }

        Transform controllerParent = ObjectManager.Instance.ViveControllerInputManager.CurrentControllerParent?.transform;

        if (controllerParent == null)
        {
            Debug.LogError("attempted to do Log_OnPose() when the current controller parent was null");
            return new PoseEvent();
        }

        Transform unusedControllerParent = null;
        if (controllerParent == ObjectManager.Instance.ViveControllerInputManager.LeftControllerParent?.transform)
        {
            unusedControllerParent = ObjectManager.Instance.ViveControllerInputManager.RightControllerParent?.transform;
        } else
        {
            unusedControllerParent = ObjectManager.Instance.ViveControllerInputManager.LeftControllerParent?.transform;
        }

        if (headset == null)
        {
            Debug.LogError("attempted to do Log_OnPose() when the current headset was null");
            return new PoseEvent();
        }

        Transform spawnedPoseParent = ObjectManager.Instance.ViveControllerInputManager.CurrentSpawnedPoseParent?.transform;

        if (spawnedPoseParent == null)
        {
            Debug.LogError("attempted to do Log_OnPose() when the current spawnedPoseParent was null");
            return new PoseEvent();
        }

        float errorTranslationMeters = Vector3.Distance(controllerParent.position, spawnedPoseParent.position);
        float errorRotationDegrees = Quaternion.Angle(controllerParent.rotation, spawnedPoseParent.rotation);

        // we are using world positions/rotations because the coordinate systems should be aligned
        float headPitchDegrees = ComputeHeadPitchDegrees(headset);

        return new PoseEvent()
        {
            Time = Time.time,
            Headset = new DevicePose(headset.position, headset.rotation),
            Controller = new DevicePose(controllerParent.position, controllerParent.rotation),
            DistMeters = errorTranslationMeters,
            DistDegrees = errorRotationDegrees,
            HeadPitchDegrees = headPitchDegrees,
            UnusedController = new DevicePose(unusedControllerParent.position, unusedControllerParent.rotation),
            UnusedHeadset = new DevicePose(unusedHeadset.position, unusedHeadset.rotation),
        };
    }

    public void Log_OnPose()
    {
        PoseEvent pe = GetLogPoseData();

        // we are using world positions/rotations because the coordinate systems should be aligned
        CurrentSessionEvents.Add(pe);
    }




    // =====================================================

    /*
    void TestSerialization()
    {
        Debug.Log("Starting test of serialization");

        List<SessionEvent> events = new List<SessionEvent>();

        events.Add(new SessionEvent() { Label = "starting_session", Time = 0.0f });
        events.Add(new SessionEvent() { Label = "started_session", Time = 0.1f });
        events.Add(new PoseEvent() { Time = 0.2f,
            Headset = new DevicePose(Vector3.zero, Quaternion.identity),
            Controller = new DevicePose(Vector3.one, new Quaternion(1, 2, 3, 4)),
            DistMeters = 0.1f,
            DistDegrees = 30.0f,
            HeadPitchDegrees = 0.0f,
        });
        events.Add(new SessionEvent() { Label = "stopping_session", Time = 0.3f });
        events.Add(new SessionEvent() { Label = "stopped_session", Time = 0.4f });
        
        string json = JsonConvert.SerializeObject(events, Formatting.Indented);

        Debug.Log("object to json: \n" + json);

        var deserialized = JsonConvert.DeserializeObject<List<SessionEvent>>(json, new SessionEventConverter());

        string json2 = JsonConvert.SerializeObject(deserialized, Formatting.Indented);

        Debug.Log("object to json to object to json: \n" + json2);

        Debug.Log("ending test of serialization");
    }
    */




    [SerializeField]
    private ViveControllerInputManager viveControllerInputManager;

    public Transform holoHeadset;
    public Transform viveHeadset;

    public enum State
    {
        Ready,
        Starting,
        Running,
        Stopping,
    }

    internal State CurrentState = State.Ready;

    public enum WhichHeadsetForSpawningPoses
    {
        Vive,
        HoloLens,
    }

    public enum PoseSpawningScheme
    {
        InPlane,
    }

    // ========================================================================
    [SyncVar(hook = nameof(SetParameterControllerVisibility))]
    internal Align.ControllerWireframeModelVisibility ParameterControllerVisibility = Align.ControllerWireframeModelVisibility.Visible;

    public Align.ControllerWireframeModelVisibility ParameterControllerVisibility_Visible = Align.ControllerWireframeModelVisibility.Visible;

    void SetParameterControllerVisibility(Align.ControllerWireframeModelVisibility newParameterControllerVisibility)
    {
        Debug.Log("SetParameterInterfaceType");
        ParameterControllerVisibility = newParameterControllerVisibility;
        ParameterControllerVisibility_Visible = newParameterControllerVisibility;

        viveControllerInputManager.CurrentControllerVisibility = ParameterControllerVisibility;
    }
    
    [SyncVar(hook = nameof(SetParameterInterfaceType))]
    internal Align.InterfaceType ParameterInterfaceType = Align.InterfaceType._AxesSmallNoSphere;

    public Align.InterfaceType ParameterInterfaceType_Visible = Align.InterfaceType._AxesSmallNoSphere;

    void SetParameterInterfaceType(Align.InterfaceType newParameterInterfaceType)
    {
        Debug.Log("SetParameterInterfaceType");
        ParameterInterfaceType = newParameterInterfaceType;
        ParameterInterfaceType_Visible = newParameterInterfaceType;

        viveControllerInputManager.CurrentAlignmentInterfaceType = ParameterInterfaceType;
    }

    [SyncVar(hook = nameof(SetParameterControllerTargetPointLocation))]
    internal Align.ControllerTargetPointLocation ParameterControllerTargetPointLocation = Align.ControllerTargetPointLocation.GripCenter;

    public Align.ControllerTargetPointLocation ParameterControllerTargetPointLocation_Visible = Align.ControllerTargetPointLocation.GripCenter;

    void SetParameterControllerTargetPointLocation(Align.ControllerTargetPointLocation newParameterControllerTargetPointLocation)
    {
        Debug.Log("SetParameterControllerTargetPointLocation");
        ParameterControllerTargetPointLocation = newParameterControllerTargetPointLocation;
        ParameterControllerTargetPointLocation_Visible = newParameterControllerTargetPointLocation;

        viveControllerInputManager.CurrentControllerTargetPointLocation = ParameterControllerTargetPointLocation;
    }

    public int ParameterNumAlignmentsInSession = 10;

    public Align.WhichTargetPoint ParameterWhichController = Align.WhichTargetPoint.Right;

    public WhichHeadsetForSpawningPoses ParameterWhichHeadset = WhichHeadsetForSpawningPoses.Vive;

    [SyncVar(hook = nameof(SetUserHeightToFloor))]
    public float UserHeightToFloor = 0.0f;

    void SetUserHeightToFloor(float newUserHeightToFloor)
    {
        Debug.Log("SetUserHeightToFloor");
        UserHeightToFloor = newUserHeightToFloor;
    }

    [SyncVar(hook = nameof(SetUserArmOutstretched))]
    public float UserArmOutstretched = 0.0f;

    void SetUserArmOutstretched(float newUserArmOutstretched)
    {
        Debug.Log("SetUserArmOutstretched");
        UserArmOutstretched = newUserArmOutstretched;
    }

    [SyncVar(hook = nameof(SetUserArmHinged))]
    public float UserArmHinged = 0.0f;

    void SetUserArmHinged(float newUserArmHinged)
    {
        Debug.Log("SetUserArmHinged");
        UserArmHinged = newUserArmHinged;
    }

    private const int WAITING_FOR_USER = -1;

    [SerializeField]
    private int CurrentAlignmentIndex = WAITING_FOR_USER;

    [SerializeField]
    public float SecondsBetweenPoseLogs = 0.05f;


    private UnityPose PregeneratedInitialSessionPose;
    private List<UnityPose> PregeneratedSessionPoses = new List<UnityPose>();

    private void InitPregeneratedPoses()
    {
        // first, set the unity random seed, so the generated poses will all be the same
        UnityEngine.Random.InitState(12345);

        PregeneratedInitialSessionPose = GenerateSessionPose(WAITING_FOR_USER);

        PregeneratedSessionPoses.Clear();
        for (int i = 0; i < ParameterNumAlignmentsInSession; i++)
        {
            PregeneratedSessionPoses.Add(GenerateSessionPose(i));
        }

        // now, set a new seed based on the timestamp, and then shuffle the pregenerated poses.
        // this way, we use the same poses, but they are randomized in order.

        UnityEngine.Random.InitState((int)(Time.time * 1000));

        PregeneratedSessionPoses.Shuffle();

        string s = "generated poses:\n";
        foreach (var pose in PregeneratedSessionPoses)
        {
            s += "\t" + pose.pos + "\t" + pose.rot + "\n";
        }
        Debug.Log(s);
    }




    // ========================================================================

    // Returns true if the user should be able to freely change the parameters, and should be able to both create and align poses
    public bool IsInFreeMode()
    {
        if (CurrentState == State.Ready)
        {
            return true;
        }

        return false;
    }

    // ========================================================================

    /// <summary>
    /// The horizontal offset in pixels to draw the HUD runtime GUI at.
    /// </summary>
    public int offsetX;

    /// <summary>
    /// The vertical offset in pixels to draw the HUD runtime GUI at.
    /// </summary>
    public int offsetY;

    // ========================================================================

    // Start is called before the first frame update
    void Start()
    {
        //TestSerialization();

        viveControllerInputManager = FindObjectOfType<ViveControllerInputManager>();
        if (viveControllerInputManager == null)
        {
            Debug.LogError("SessionManager: viveControllerInputManager cannot be null");
        }

        if (holoHeadset == null)
        {
            Debug.LogError("SessionManager: holoHeadset cannot be null");
        }

        if (viveHeadset == null)
        {
            Debug.LogError("SessionManager: viveHeadset cannot be null");
        }
    }

    float timeLastPoseLog = 0.0f;



    private void FixedUpdate()
    {
        if (CurrentState == State.Running)
        {
            if (CurrentAlignmentIndex != WAITING_FOR_USER)
            {
                // here we are not waiting for the first alignment from the user (the alignment that starts the session)
                // so we should be tracking per-frame pose information here

                // don't log pose every frame. have it as a controllable parameter.
                var time = Time.time; // current seconds since start of game

                if ((time - timeLastPoseLog) >= SecondsBetweenPoseLogs)
                {
                    Log_OnPose();

                    timeLastPoseLog = time;
                }
            }
        }

        if (ParameterInterfaceType_Visible != ParameterInterfaceType)
        {
            ParameterInterfaceType = ParameterInterfaceType_Visible;
            RpcUpdateParameterInterfaceType(ParameterInterfaceType_Visible);
        }

        if (ParameterControllerTargetPointLocation_Visible != ParameterControllerTargetPointLocation)
        {
            ParameterControllerTargetPointLocation = ParameterControllerTargetPointLocation_Visible;
            RpcUpdateParameterControllerTargetPointLocation(ParameterControllerTargetPointLocation_Visible);
        }

        if (ParameterControllerVisibility_Visible != ParameterControllerVisibility)
        {
            ParameterControllerVisibility = ParameterControllerVisibility_Visible;
            RpcUpdateParameterControllerVisibility(ParameterControllerVisibility_Visible);
        }
    }

    private void GetPregeneratedPose(int index, out Vector3 positionInViveSpace, out Quaternion rotationInViveSpace)
    {
        if (index == WAITING_FOR_USER)
        {
            UnityPose initialPose = PregeneratedInitialSessionPose;

            positionInViveSpace = initialPose.pos;
            rotationInViveSpace = initialPose.rot;
        } else
        {
            UnityPose pose = PregeneratedSessionPoses[index];

            positionInViveSpace = pose.pos;
            rotationInViveSpace = pose.rot;
        }
    }

    private UnityPose GenerateSessionPose(int index)
    {
        Vector3 positionInViveSpace;
        Quaternion rotationInViveSpace;

        float maxHeight = UserHeightToFloor + 0.15f * UserArmOutstretched;
        float minHeight = Mathf.Max(UserHeightToFloor - 0.5f * UserArmOutstretched, 0.25f);

        float default_z = 0.0f; // how far forward the vertical plane is

        if (index == WAITING_FOR_USER)
        {
            // generate a neutral pose for the initial pose
            float x = 0.0f;
            float y = (minHeight + maxHeight) / 2.0f;
            float z = default_z;

            Vector3 pos = new Vector3(x, y, z);

            Vector3 lookTarget = pos + new Vector3(0, 0, 1); // all pointing in +Z world direction

            // - the controller's "upwards" is (+Y)
            // - the controller's "forward" is (+Z)

            Quaternion neutral_rot = Quaternion.LookRotation(Vector3.up, -(lookTarget - pos));

            positionInViveSpace = pos;
            rotationInViveSpace = neutral_rot;
        }
        else
        {
            // generate a randomly perturbed pose 

            // poses will be spawned in a vertical plane, all facing one direction away from the user, with minor perturbation
            float perturbation = 0.05f;

            float x = UnityEngine.Random.Range(-0.5f, 0.5f) + UnityEngine.Random.Range(-perturbation, perturbation);
            float y = UnityEngine.Random.Range(minHeight, maxHeight) + UnityEngine.Random.Range(-perturbation, perturbation);
            float z = default_z + UnityEngine.Random.Range(-perturbation, perturbation);

            Vector3 perturbed_pos = new Vector3(x, y, z);

            Vector3 lookTarget = perturbed_pos + new Vector3(0, 0, 1); // all pointing in +Z world direction

            // - the controller's "upwards" is (+Y)
            // - the controller's "forward" is (+Z)

            Quaternion neutral_rot = Quaternion.LookRotation(Vector3.up, -(lookTarget - perturbed_pos));

            Quaternion wiggleRotation = Quaternion.AngleAxis(UnityEngine.Random.Range(15.0f, 45.0f), UnityEngine.Random.onUnitSphere);

            Quaternion perturbed_rot = neutral_rot * wiggleRotation;

            positionInViveSpace = perturbed_pos;
            rotationInViveSpace = perturbed_rot;
        }

        return new UnityPose(positionInViveSpace, rotationInViveSpace);
    }

    private void PlaceInitialPose()
    {
        CurrentAlignmentIndex = WAITING_FOR_USER;

        Vector3 initialPosePositionInViveSpace = new Vector3();
        Quaternion initialPoseRotationInViveSpace = new Quaternion();
        GetPregeneratedPose(CurrentAlignmentIndex, out initialPosePositionInViveSpace, out initialPoseRotationInViveSpace);

        viveControllerInputManager.RpcPlaceTargetObjectAtCustomPose(ParameterWhichController, initialPosePositionInViveSpace, initialPoseRotationInViveSpace);
    }

    internal void DoAlignment(WhichTargetPoint whichTargetPoint)
    {
        if (CurrentAlignmentIndex == WAITING_FOR_USER)
        {
            RpcClearText();
            Log_OnInitialAlignment(); // this is where we start measuring completion time... this alignment doesn't actually count
        } else
        {
            Log_OnAlignment(); // this alignment does count
        }

        viveControllerInputManager.RpcAlignWithTargetObject(whichTargetPoint);

        CurrentAlignmentIndex += 1;
        Debug.Log("updated CurrentAlignmentIndex to " + CurrentAlignmentIndex);


        if (CurrentAlignmentIndex >= ParameterNumAlignmentsInSession)
        {
            StopSession();
        } else
        {
            Vector3 nextPosePositionInViveSpace = new Vector3();
            Quaternion nextPoseRotationInViveSpace = new Quaternion();
            GetPregeneratedPose(CurrentAlignmentIndex, out nextPosePositionInViveSpace, out nextPoseRotationInViveSpace);

            viveControllerInputManager.RpcPlaceTargetObjectAtCustomPose(ParameterWhichController, nextPosePositionInViveSpace, nextPoseRotationInViveSpace);
        }
    }

    private void StartSession()
    {
        Debug.Log("Starting session...");

        // before starting the session, we need to clear out any existing free-mode spawned pose
        if (IsInFreeMode() && viveControllerInputManager.CurrentFreeModeControlState == ViveControllerInputManager.FreeModeControlState.AligningWithTargetObject)
        {
            Debug.Log("in StartSession(), calling RpcClearAnySpawnedFreeModePose");
            viveControllerInputManager.RpcClearAnySpawnedFreeModePose();
        }

        CurrentState = State.Starting;

        // clear out 
        viveControllerInputManager.CurrentAlignmentInterfaceType = ParameterInterfaceType;
        viveControllerInputManager.CurrentControllerTargetPointLocation = ParameterControllerTargetPointLocation;

        Log_OnSessionStarted();

        InitPregeneratedPoses();

        PlaceInitialPose();

        RpcFadeInShowText("Align controller with the mid-air pose to start the session.");

        CurrentState = State.Running;

        Debug.Log("Session started.");
    }

    [ClientRpc]
    void RpcClearText()
    {
        ObjectManager.Instance.BillboardText.ClearText();
    }

    [ClientRpc]
    void RpcFadeInShowText(string s)
    {
        ObjectManager.Instance.BillboardText.FadeInShow(s);
    }

    [ClientRpc]
    void RpcFadeInShowFadeOutText(string s)
    {
        ObjectManager.Instance.BillboardText.FadeInShowFadeOut(s);
    }

    private void StopSession()
    {
        Debug.Log("Stopping session...");

        CurrentState = State.Stopping;

        Log_OnSessionStopped();

        Debug.Log("in StopSession(), calling RpcClearAnySpawnedFreeModePose");
        viveControllerInputManager.RpcClearAnySpawnedFreeModePose();

        CurrentState = State.Ready;

        RpcFadeInShowFadeOutText("Session finished.");

        Debug.Log("Session stopped.");
    }

    public float ComputeUserHeightToFloor()
    {
        Transform headset = (ParameterWhichHeadset == WhichHeadsetForSpawningPoses.HoloLens) ? ObjectManager.Instance.HoloHeadset : ObjectManager.Instance.ViveHeadset;

        Transform viveOrigin = ObjectManager.Instance.ViveOrigin;

        // we know that vive origin will be located on the floor
        return viveOrigin.InverseTransformPoint(headset.position).y;
    }

    public float ComputeHeadToArm()
    {
        Transform headset = (ParameterWhichHeadset == WhichHeadsetForSpawningPoses.HoloLens) ? ObjectManager.Instance.HoloHeadset : ObjectManager.Instance.ViveHeadset;

        // using controller parent, not the alignment point here
        Transform controller = (ParameterWhichController == WhichTargetPoint.Left) ? ObjectManager.Instance.ViveLeftControllerParent : ObjectManager.Instance.ViveRightControllerParent;

        Transform viveOrigin = ObjectManager.Instance.ViveOrigin;

        return Vector3.Distance(Vector3.ProjectOnPlane(headset.position, viveOrigin.up), Vector3.ProjectOnPlane(controller.position, viveOrigin.up));
    }

    [ClientRpc]
    void RpcUpdateParameterInterfaceType(Align.InterfaceType value)
    {
        Debug.Log("RpcUpdateParameterInterfaceType");
        ParameterInterfaceType = value;
    }

    [ClientRpc]
    void RpcUpdateParameterControllerVisibility(Align.ControllerWireframeModelVisibility value)
    {
        Debug.Log("RpcUpdateParameterControllerVisibility");
        ParameterControllerVisibility = value;
    }

    [ClientRpc]
    void RpcUpdateParameterControllerTargetPointLocation(Align.ControllerTargetPointLocation value)
    {
        Debug.Log("RpcUpdateParameterControllerTargetPointLocation");
        ParameterControllerTargetPointLocation = value;
    }

    [ClientRpc]
    void RpcUpdateUserHeightToFloor(float value)
    {
        Debug.Log("RpcUpdateUserHeightToFloor");
        UserHeightToFloor = value;
    }

    [ClientRpc]
    void RpcUpdateUserArmOutstretched(float value)
    {
        Debug.Log("RpcUpdateUserArmOutstretched");
        UserArmOutstretched = value;
    }

    [ClientRpc]
    void RpcUpdateUserArmHinged(float value)
    {
        Debug.Log("RpcUpdateUserArmHinged");
        UserArmHinged = value;
    }

    private GameObject _hololensCursorObject = null;

    [ClientRpc]
    void RpcToggleHololensCursor()
    {
        Debug.Log("RpcToggleHololensCursor");

        if (_hololensCursorObject == null)
        {
            _hololensCursorObject = GameObject.Find("DefaultCursor(Clone)");
        }

        if (_hololensCursorObject != null)
        {
            if (_hololensCursorObject.activeSelf)
            {
                _hololensCursorObject.SetActive(false);
            } else
            {
                _hololensCursorObject.SetActive(true);
            }
        }
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10 + offsetX, 40 + offsetY, 215, 9999));

        if (CurrentState == State.Ready)
        {
            if (GUILayout.Button("Update user height to floor (" + UserHeightToFloor + ")"))
            {
                RpcUpdateUserHeightToFloor(ComputeUserHeightToFloor());
                //UserHeightToFloor = ComputeUserHeightToFloor();
            }
            if (GUILayout.Button("Update user arm outstretched (" + UserArmOutstretched + ")"))
            {
                RpcUpdateUserArmOutstretched(ComputeHeadToArm());
                //UserArmOutstretched = ComputeHeadToArm();
            }
            if (GUILayout.Button("Update user arm hinged (" + UserArmHinged + ")"))
            {
                RpcUpdateUserArmHinged(ComputeHeadToArm());
                //UserArmHinged = ComputeHeadToArm();
            }
            if (GUILayout.Button("Start session"))
            {
                Debug.Log("pressed start session button");
                StartSession();
            }
        } else if (CurrentState == State.Running)
        {
            if (GUILayout.Button("Stop session"))
            {
                Debug.Log("pressed stop session button");
                StopSession();
            }
        }

        if (GUILayout.Button("Toggle hololens cursor"))
        {
            RpcToggleHololensCursor();
        }
        
        GUILayout.EndArea();
    }

    // given an existing Transform for a 6-dof pose (the parent object),
    // and knowledge about the user's height and arm poses,
    // find a location at user headset height, in front of the pose,
    // so that when the user is aligning with the pose (at a comfortable arm angle),
    // the aligned pose is about 2m in front of the user.
    public Vector3 GetTargetPointOffsetForAlignmentPoint(Transform spawnedPoseParentTransform)
    {
        float userHeadsetHeight = UserHeightToFloor;

        float idealHeadToTargetDistance = Constants.HoloFocalDistanceMeters; // 2 meters is the focal distance of the HoloLens

        float idealHeadToHandDistance = UserArmHinged;

        float idealHandToTargetDistance = idealHeadToTargetDistance - idealHeadToHandDistance;

        Transform viveOrigin = ObjectManager.Instance.ViveOrigin;

        Vector3 spawnedPoseParentPositionViveSpace = viveOrigin.InverseTransformPoint(spawnedPoseParentTransform.position);

        Vector3 spawnedPoseParentForwardDirectionViveSpace = viveOrigin.InverseTransformDirection(-spawnedPoseParentTransform.up); // note that the "-Y" direction of the pose is actually the direction pointing away from the controller's backside

        Vector3 targetPointPositionViveSpace = spawnedPoseParentPositionViveSpace + idealHandToTargetDistance * spawnedPoseParentForwardDirectionViveSpace;
        targetPointPositionViveSpace.y = userHeadsetHeight;

        Vector3 targetPointPositionWorld = viveOrigin.TransformPoint(targetPointPositionViveSpace);

        Vector3 targetPointRelativeToPoseParent = spawnedPoseParentTransform.InverseTransformPoint(targetPointPositionWorld);

        return targetPointRelativeToPoseParent;
    }
    
}
