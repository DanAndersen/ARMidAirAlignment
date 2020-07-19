using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Align;
using Mirror;

public class ViveControllerInputManager : NetworkBehaviour
{
    // when we are NOT in free mode (when we are in a real session, if the distance between the controller and the target is more than this distance, don't actually do the alignment.
    // this is to help avoid situations where a user might accidentally double-click
    public float SuppressAccidentalClickDistanceMeters = 0.25f;
    public bool EnableDistanceBasedClickSuppression = true; // if true, do the aforementioned accidental click suppression. otherwise, let all clicks pass.

    // when we are in a real session, and it has been longer than this many seconds since the last clic, suppress it so we don't do accidental double clicks.
    public float SuppressAccidentalClickTimeSeconds = 1.5f;
    public bool EnableTimeBasedClickSuppression = true; // if true, do the aforementioned accidental click suppression. otherwise, let all clicks pass.
    private float _lastClickTime = 0.0f;

    public Transform ViveOrigin;
    public ControllerParent LeftControllerParent;
    public ControllerParent RightControllerParent;

    public SpawnedPoseParent SpawnedPoseParentPrefab;

    internal ControllerParent CurrentControllerParent;
    internal SpawnedPoseParent CurrentSpawnedPoseParent;

    public LineRenderer Lasso;
    public LineRenderer TargetPlaneLasso;
    public Renderer TargetPlaneSphere;

    public LineRenderer BillboardBorder;

    public ParticleSystem BubbleParticleSystem;

    public Renderer LeftControllerWireframeModel;
    public Renderer RightControllerWireframeModel;

    public enum FreeModeControlState
    {
        PlacingTargetObject = 0,
        AligningWithTargetObject = 1,
    }

    [SyncVar]
    public FreeModeControlState CurrentFreeModeControlState;

    [SyncVar(hook = nameof(SetCurrentAlignmentInterfaceType))]
    public InterfaceType CurrentAlignmentInterfaceType;

    [SyncVar(hook = nameof(SetCurrentControllerTargetPointLocation))]
    public ControllerTargetPointLocation CurrentControllerTargetPointLocation;

    [SyncVar(hook = nameof(SetCurrentControllerVisibility))]
    public ControllerWireframeModelVisibility CurrentControllerVisibility;


    public override void OnStartServer()
    {
        base.OnStartServer();

        Debug.Log("ViveControllerInputManager: OnStartServer");

        CurrentFreeModeControlState = FreeModeControlState.PlacingTargetObject;
        SetCurrentAlignmentInterfaceType(CurrentAlignmentInterfaceType);
        SetCurrentControllerTargetPointLocation(CurrentControllerTargetPointLocation);
        SetCurrentControllerVisibility(CurrentControllerVisibility);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        Debug.Log("ViveControllerInputManager: OnStartClient");

        if (ViveOrigin == null)
        {
            Debug.LogError("ViveOrigin shouldn't be null");
        }

        if (LeftControllerParent == null)
        {
            Debug.LogError("LeftControllerParent shouldn't be null");
        }

        if (RightControllerParent == null)
        {
            Debug.LogError("RightControllerParent shouldn't be null");
        }

        if (SpawnedPoseParentPrefab == null)
        {
            Debug.LogError("SpawnedPoseParentPrefab shouldn't be null");
        }

        LeftControllerParent.TargetPoint.HideAllInterfaces();
        RightControllerParent.TargetPoint.HideAllInterfaces();
    }

    [ClientRpc]
    public void RpcPlaceTargetObjectAtCustomPose(WhichTargetPoint whichTargetPoint, Vector3 positionInViveSpace, Quaternion rotationInViveSpace)
    {
        ControllerParent controllerParent = whichTargetPoint == WhichTargetPoint.Left ? LeftControllerParent : RightControllerParent;

        CurrentSpawnedPoseParent = Instantiate(SpawnedPoseParentPrefab, ViveOrigin);
        CurrentSpawnedPoseParent.transform.localPosition = positionInViveSpace;
        CurrentSpawnedPoseParent.transform.localRotation = rotationInViveSpace;

        CurrentControllerParent = controllerParent;

        // callbacks
        CurrentControllerParent.TargetPoint.OnTargetSpawned(CurrentSpawnedPoseParent.TargetPoint, CurrentAlignmentInterfaceType, CurrentControllerTargetPointLocation);
        CurrentSpawnedPoseParent.TargetPoint.OnTargetSpawned(CurrentControllerParent.TargetPoint, CurrentAlignmentInterfaceType, CurrentControllerTargetPointLocation);

        CurrentFreeModeControlState = FreeModeControlState.AligningWithTargetObject;

        LeftControllerParent.TargetPoint.Stick.gameObject.SetActive(false);
        RightControllerParent.TargetPoint.Stick.gameObject.SetActive(false);

        if (!ObjectManager.Instance.SessionManager.IsInFreeMode())
        {
            ObjectManager.Instance.SessionManager.Log_OnPoseSpawned();
        }
    }

    public void DoClearAnySpawnedFreeModePose()
    {
        Debug.Log("start DoClearAnySpawnedFreeModePose");
        // callbacks
        if (CurrentControllerParent != null && CurrentControllerParent.TargetPoint != null)
        {
            CurrentControllerParent.TargetPoint.OnAlignmentConfirmed();
        }

        if (CurrentSpawnedPoseParent != null && CurrentSpawnedPoseParent.TargetPoint != null)
        {
            CurrentSpawnedPoseParent.TargetPoint.OnAlignmentConfirmed();
        }

        DestroyCurrentSpawnedPose();

        CurrentControllerParent = null;

        CurrentFreeModeControlState = FreeModeControlState.PlacingTargetObject;
    }

    [ClientRpc]
    public void RpcClearAnySpawnedFreeModePose()
    {
        Debug.Log("start RpcClearAnySpawnedFreeModePose");
        DoClearAnySpawnedFreeModePose();
    }

    private void DestroyCurrentSpawnedPose()
    {
        if (CurrentSpawnedPoseParent != null)
        {
            if (BubbleParticleSystem != null)
            {
                BubbleParticleSystem.transform.position = CurrentSpawnedPoseParent.TargetPoint.transform.position;
                BubbleParticleSystem.Play();
            }

            Destroy(CurrentSpawnedPoseParent.gameObject);
            CurrentSpawnedPoseParent = null;
        }
    }

    [ClientRpc]
    public void RpcAlignWithTargetObject(WhichTargetPoint whichTargetPoint)
    {
        ControllerParent controllerParent = whichTargetPoint == WhichTargetPoint.Left ? LeftControllerParent : RightControllerParent;

        if (CurrentControllerParent == controllerParent) // check to make sure we aren't using Left controller to align with Right-controller-spawned object, and vice versa
        {
            // callbacks
            CurrentControllerParent.TargetPoint.OnAlignmentConfirmed();
            CurrentSpawnedPoseParent.TargetPoint.OnAlignmentConfirmed();

            LeftControllerParent.TargetPoint.Stick.gameObject.SetActive(false);
            RightControllerParent.TargetPoint.Stick.gameObject.SetActive(false);

            // compute alignment
            float distanceMeters = Vector3.Distance(CurrentControllerParent.transform.position, CurrentSpawnedPoseParent.transform.position);
            float angleDegrees = Quaternion.Angle(CurrentControllerParent.transform.rotation, CurrentSpawnedPoseParent.transform.rotation);

            Debug.Log("distance: " + distanceMeters + " meters, angle: " + angleDegrees + " degrees");

            DestroyCurrentSpawnedPose();

            CurrentControllerParent = null;
            CurrentFreeModeControlState = FreeModeControlState.PlacingTargetObject;
        }
    }

    private void OnControllerClick(WhichTargetPoint whichTargetPoint)
    {
        if (ObjectManager.Instance.SessionManager.IsInFreeMode())
        {
            switch (CurrentFreeModeControlState)
            {
                case FreeModeControlState.PlacingTargetObject:

                    Transform controllerParent = whichTargetPoint == WhichTargetPoint.Left ? ObjectManager.Instance.ViveLeftControllerParent : ObjectManager.Instance.ViveRightControllerParent;

                    Vector3 positionInViveSpace = ViveOrigin.InverseTransformPoint(controllerParent.position);
                    Quaternion rotationInViveSpace = Quaternion.Inverse(ViveOrigin.localRotation) * controllerParent.rotation;

                    RpcPlaceTargetObjectAtCustomPose(whichTargetPoint, positionInViveSpace, rotationInViveSpace);

                    break;
                case FreeModeControlState.AligningWithTargetObject:
                    RpcAlignWithTargetObject(whichTargetPoint);
                    break;
                default:
                    Debug.LogError("Unhandled FreeModeControlState: " + CurrentFreeModeControlState);
                    break;
            }
        } else
        {
            if (ObjectManager.Instance.SessionManager.CurrentState == SessionManager.State.Running)
            {
                ControllerParent controllerParent = whichTargetPoint == WhichTargetPoint.Left ? LeftControllerParent : RightControllerParent;

                if (CurrentControllerParent == controllerParent) // check to make sure we aren't using Left controller to align with Right-controller-spawned object, and vice versa
                {
                    // if we're not in free mode, then a click of the controller only means we are aligning with an already-provided pose

                    // check our suppression distance, to help avoid double-clicks on accident
                    if (CurrentControllerParent != null && CurrentSpawnedPoseParent != null)
                    {
                        bool passedSuppressionCheck = (!EnableDistanceBasedClickSuppression) || (Vector3.Distance(CurrentControllerParent.TargetPoint.transform.position, CurrentSpawnedPoseParent.TargetPoint.transform.position) < SuppressAccidentalClickDistanceMeters);

                        float currentTime = Time.time;

                        passedSuppressionCheck = passedSuppressionCheck && ((!EnableTimeBasedClickSuppression) || ((currentTime - _lastClickTime) > SuppressAccidentalClickTimeSeconds));

                        if (passedSuppressionCheck)
                        {
                            _lastClickTime = currentTime;

                            ObjectManager.Instance.SessionManager.DoAlignment(whichTargetPoint);
                        }
                    }
                }
            }
        }



    }
    
    private Vector2 WorldSpaceToCameraProjectedAtDist(Vector3 worldPos, float plane_z)
    {
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos, Camera.MonoOrStereoscopicEye.Mono);
        screenPos.z = plane_z;

        Vector3 posAtDistanceWorld = Camera.main.ScreenToWorldPoint(screenPos, Camera.MonoOrStereoscopicEye.Mono);

        Vector3 posAtDistanceLocal = Camera.main.transform.InverseTransformPoint(posAtDistanceWorld);

        return new Vector2(posAtDistanceLocal.x, posAtDistanceLocal.y);
    }

    private Vector2 Perp(Vector2 pt)
    {
        return new Vector2(pt.y, -pt.x);
    }

    private int WhichSide(List<Vector2> s, Vector2 d, Vector2 p)
    {
        int positive = 0;
        int negative = 0;

        for (int i = 0; i < s.Count; i++)
        {
            var t = Vector2.Dot(d, s[i] - p);
            if (t > 0)
            {
                positive++;
            } else if (t < 0)
            {
                negative++;
            }
            if (positive > 0 && negative > 0)
            {
                return 0;
            }
        }
        return (positive > 0) ? 1 : -1;
    }

    private bool TestIntersection(List<Vector2> c0, List<Vector2> c1)
    {
        for (int i0 = 0; i0 < c0.Count; i0++)
        {
            int i1 = (i0 + c0.Count - 1) % c0.Count;

            Vector2 D = Perp(c0[i0] - c0[i1]);

            if (WhichSide(c1, D, c0[i0]) > 0)
            {
                return false;
            }
        }

        for (int i0 = 0; i0 < c1.Count; i0++)
        {
            int i1 = (i0 + c1.Count - 1) % c1.Count;

            Vector2 D = Perp(c1[i0] - c1[i1]);

            if (WhichSide(c0, D, c1[i0]) > 0)
            {
                return false;
            }
        }

        return true;
    }


    private const double Epsilon = 1e-10;
    public static bool IsZero(float x)
    {
        return Math.Abs(x) < Epsilon;
    }

    public static float Cross(Vector2 v1, Vector2 v2)
    {
        return v1.x * v2.y - v1.y * v2.x;
    }

    public static bool LineSegementsIntersect(Vector2 p, Vector2 p2, Vector2 q, Vector2 q2,
    out Vector2 intersection)
    {
        intersection = new Vector2();

        var r = p2 - p;
        var s = q2 - q;
        var rxs = Cross(r,s);
        var qpxr = Cross((q - p), r);

        // If r x s = 0 and (q - p) x r = 0, then the two lines are collinear.
        if (IsZero(rxs) && IsZero(qpxr))
        {
            // 2. If neither 0 <= (q - p) * r = r * r nor 0 <= (p - q) * s <= s * s
            // then the two lines are collinear but disjoint.
            // No need to implement this expression, as it follows from the expression above.
            return false;
        }

        // 3. If r x s = 0 and (q - p) x r != 0, then the two lines are parallel and non-intersecting.
        if (IsZero(rxs) && !IsZero(qpxr))
            return false;

        // t = (q - p) x s / (r x s)
        var t = Cross((q - p),s) / rxs;

        // u = (q - p) x r / (r x s)

        var u = Cross((q - p),r) / rxs;

        // 4. If r x s != 0 and 0 <= t <= 1 and 0 <= u <= 1
        // the two line segments meet at the point p + t r = q + u s.
        if (!IsZero(rxs) && (0 <= t && t <= 1) && (0 <= u && u <= 1))
        {
            // We can calculate the intersection point using either t or u.
            intersection = p + t * r;

            // An intersection was found.
            return true;
        }

        // 5. Otherwise, the two line segments are not parallel but do not intersect.
        return false;
    }



    private void Update()
    {
        // update the lasso

        if (CurrentControllerParent != null && CurrentSpawnedPoseParent != null)
        {
            Lasso.gameObject.SetActive(true);

            Lasso.SetPosition(0, CurrentControllerParent.TargetPoint.transform.position);
            Lasso.SetPosition(1, CurrentSpawnedPoseParent.TargetPoint.transform.position);

            float distance = Vector3.Distance(CurrentControllerParent.TargetPoint.transform.position, CurrentSpawnedPoseParent.TargetPoint.transform.position);

            float tiling_x = distance / Lasso.startWidth;

            Lasso.material.mainTextureScale = new Vector2(tiling_x, 1);
        }
        else
        {
            Lasso.gameObject.SetActive(false);
        }

        // update the target plane lasso

        if (CurrentControllerParent != null && CurrentSpawnedPoseParent != null)
        {
            if (CurrentSpawnedPoseParent.TargetPoint.ProjectionQuad.activeSelf)
            {
                float plane_z = 0.99f;

                float min_x = 0.0f;
                float min_y = 0.0f;
                float max_x = 0.0f;
                float max_y = 0.0f;

                for (int i = 0; i < BillboardBorder.positionCount; i++)
                {
                    Vector3 pt = BillboardBorder.GetPosition(i);

                    min_x = Mathf.Min(min_x, pt.x);
                    max_x = Mathf.Max(max_x, pt.x);
                    min_y = Mathf.Min(min_y, pt.y);
                    max_y = Mathf.Max(max_y, pt.y);
                }

                // in CCW order
                List<Vector2> view_points = new List<Vector2>() { new Vector2(min_x, min_y), new Vector2(max_x, min_y), new Vector2(max_x, max_y), new Vector2(min_x, max_y) };

                // in CCW order
                List<Vector2> quad_points = new List<Vector2>()
                {
                    WorldSpaceToCameraProjectedAtDist(CurrentSpawnedPoseParent.TargetPoint.ProjectionQuad.transform.TransformPoint(new Vector3(-0.33f, -0.33f, 0)), plane_z),
                    WorldSpaceToCameraProjectedAtDist(CurrentSpawnedPoseParent.TargetPoint.ProjectionQuad.transform.TransformPoint(new Vector3(0.33f, -0.33f, 0)), plane_z),
                    WorldSpaceToCameraProjectedAtDist(CurrentSpawnedPoseParent.TargetPoint.ProjectionQuad.transform.TransformPoint(new Vector3(0.33f, 0.33f, 0)), plane_z),
                    WorldSpaceToCameraProjectedAtDist(CurrentSpawnedPoseParent.TargetPoint.ProjectionQuad.transform.TransformPoint(new Vector3(-0.33f, 0.33f, 0)), plane_z),
                };
                
                bool quadInsideHoloFieldOfView = TestIntersection(view_points, quad_points);

                if (quadInsideHoloFieldOfView)
                {
                    // the center of the quad is inside the hololens view.

                    TargetPlaneLasso.gameObject.SetActive(false);
                    TargetPlaneSphere.gameObject.SetActive(false);
                } else
                {
                    // the center of the quad is outside the hololens view.

                    TargetPlaneLasso.gameObject.SetActive(true);
                    //TargetPlaneLasso.gameObject.SetActive(false);

                    TargetPlaneSphere.gameObject.SetActive(true);

                    Vector3 startPos = Camera.main.transform.position + Camera.main.transform.forward * plane_z;

                    Vector3 quadPosWorld = CurrentSpawnedPoseParent.TargetPoint.ProjectionQuad.transform.position;

                    Vector3 quadPosCameraSpace = Camera.main.transform.InverseTransformPoint(quadPosWorld);

                    bool isBehindCamera = (quadPosCameraSpace.z < 0);

                    // put it at the plane distance
                    Vector3 quadPosCameraSpaceAtPlaneDistance = quadPosCameraSpace / quadPosCameraSpace.z * plane_z;
                    if (isBehindCamera)
                    {
                        quadPosCameraSpaceAtPlaneDistance *= -1.0f;
                    }
                    
                    Vector3 quadPosAtDistanceLocal = quadPosCameraSpaceAtPlaneDistance;

                    for (int i = 0; i < view_points.Count; i++)
                    {
                        Vector2 view_point_a = view_points[i];
                        Vector2 view_point_b = view_points[(i + 1) % view_points.Count];

                        Vector2 intersection = Vector2.zero;
                        if (LineSegementsIntersect(view_point_a, view_point_b, Vector2.zero, new Vector2(quadPosAtDistanceLocal.x, quadPosAtDistanceLocal.y), out intersection))
                        {
                            quadPosAtDistanceLocal.x = intersection.x;
                            quadPosAtDistanceLocal.y = intersection.y;

                            break;
                        }
                    }


                    if (isBehindCamera)
                    {
                        quadPosAtDistanceLocal.z *= -1.0f;
                    }








                    Vector3 quadPosAtDistanceWorldClamped = Camera.main.transform.TransformPoint(quadPosAtDistanceLocal);
                    
                    Vector3 endPos = quadPosAtDistanceWorldClamped;

                    TargetPlaneLasso.SetPosition(0, startPos);
                    TargetPlaneLasso.SetPosition(1, endPos);

                    float distance = Vector3.Distance(startPos, endPos);

                    float tiling_x = distance / TargetPlaneLasso.startWidth;

                    TargetPlaneLasso.material.mainTextureScale = new Vector2(tiling_x, 1);

                    TargetPlaneSphere.transform.position = endPos;
                }
            }
            else
            {
                TargetPlaneLasso.gameObject.SetActive(false);
                TargetPlaneSphere.gameObject.SetActive(false);
            }
        }
        else
        {
            TargetPlaneLasso.gameObject.SetActive(false);
            TargetPlaneSphere.gameObject.SetActive(false);
        }
    }

    public void OnLeftControllerClick()
    {
        OnControllerClick(WhichTargetPoint.Left);
    }

    public void OnRightControllerClick()
    {
        OnControllerClick(WhichTargetPoint.Right);
    }

    void SetCurrentControllerTargetPointLocation(ControllerTargetPointLocation newLocation)
    {
        CurrentControllerTargetPointLocation = newLocation;

        Debug.Log("set CurrentControllerTargetPointLocation to " + CurrentControllerTargetPointLocation);

        Vector3 position = Vector3.zero;

        switch (CurrentControllerTargetPointLocation)
        {
            case ControllerTargetPointLocation.ControllerTip:
                position = new Vector3(0, -0.075f, 0.035f);
                break;
            case ControllerTargetPointLocation.GripCenter:
                position = new Vector3(0, 0, -0.075f);
                break;
            default:
                Debug.LogError("unhandled CurrentControllerTargetPointLocation: " + CurrentControllerTargetPointLocation);
                break;
        }

        LeftControllerParent.TargetPoint.transform.localPosition = position;
        RightControllerParent.TargetPoint.transform.localPosition = position;

        if (CurrentSpawnedPoseParent != null)
        {
            CurrentSpawnedPoseParent.TargetPoint.transform.localPosition = position;
        }

        UpdateControllerSticks(position);


        if (CurrentSpawnedPoseParent != null)
        {
            CurrentSpawnedPoseParent.TargetPoint.RefreshVisualization(CurrentAlignmentInterfaceType, CurrentControllerTargetPointLocation);
        }

        if (CurrentControllerParent != null)
        {
            CurrentControllerParent.TargetPoint.RefreshVisualization(CurrentAlignmentInterfaceType, CurrentControllerTargetPointLocation);
        }
    }

    private void UpdateControllerSticks(Vector3 targetLocalPosition)
    {
        LeftControllerParent.TargetPoint.Stick.gameObject.SetActive(false);
        RightControllerParent.TargetPoint.Stick.gameObject.SetActive(false);
    }

    public void CycleTargetPointLocation(int step)
    {
        int n = System.Enum.GetValues(typeof(ControllerTargetPointLocation)).Length; // num interfaces
        int r = (int)CurrentControllerTargetPointLocation + step;
        int new_idx = (r % n + n) % n; // mod function, handles negatives

        ControllerTargetPointLocation newLocation = (ControllerTargetPointLocation)(new_idx);

        CurrentControllerTargetPointLocation = newLocation;
    }

    void SetCurrentControllerVisibility(ControllerWireframeModelVisibility newVisibility)
    {
        CurrentControllerVisibility = newVisibility;

        Debug.Log("set CurrentControllerVisibility to " + CurrentControllerVisibility);

        switch (CurrentControllerVisibility)
        {
            case ControllerWireframeModelVisibility.Hidden:

                if (LeftControllerWireframeModel != null)
                {
                    LeftControllerWireframeModel.enabled = false;
                }

                if (RightControllerWireframeModel != null)
                {
                    RightControllerWireframeModel.enabled = false;
                }

                break;
            case ControllerWireframeModelVisibility.Visible:

                if (LeftControllerWireframeModel != null)
                {
                    LeftControllerWireframeModel.enabled = true;
                }

                if (RightControllerWireframeModel != null)
                {
                    RightControllerWireframeModel.enabled = true;
                }

                break;
            default:
                break;
        }
        
    }

    void SetCurrentAlignmentInterfaceType(InterfaceType newInterfaceType)
    {
        CurrentAlignmentInterfaceType = newInterfaceType;

        Debug.Log("set CurrentAlignmentInterfaceType to " + CurrentAlignmentInterfaceType);

        if (CurrentSpawnedPoseParent != null)
        {
            CurrentSpawnedPoseParent.TargetPoint.RefreshVisualization(CurrentAlignmentInterfaceType, CurrentControllerTargetPointLocation);
        }

        if (CurrentControllerParent != null)
        {
            CurrentControllerParent.TargetPoint.RefreshVisualization(CurrentAlignmentInterfaceType, CurrentControllerTargetPointLocation);
        }
    }

    public void CycleAlignmentType(int step)
    {
        int n = System.Enum.GetValues(typeof(InterfaceType)).Length; // num interfaces
        int r = (int)CurrentAlignmentInterfaceType + step;
        int new_idx = (r % n + n) % n; // mod function, handles negatives

        InterfaceType newInterfaceType = (InterfaceType)(new_idx);

        CurrentAlignmentInterfaceType = newInterfaceType;
    }
}
