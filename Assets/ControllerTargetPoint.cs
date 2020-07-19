using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Align;
using TMPro;
using System;

public class ControllerTargetPoint : MonoBehaviour
{
    private SpawnedPoseTargetPoint CurrentSpawnedPoseTargetPoint = null;
    private InterfaceType CurrentAlignmentInterfaceType;
    private ControllerTargetPointLocation CurrentControllerTargetPointLocation;

    public Axes AxesGameObject;

    public Projector TargetProjector;

    public GameObject ProngQuad; // quad that contains the prong targets.

    // prong targets -- these are only used to determine WHERE the endpoints of the prong lines should be
    public LineRenderer ProngTarget1;
    public LineRenderer ProngTarget2;
    public LineRenderer ProngTarget3;

    // prong lines -- these go from the controller point and should stop at the prong target locations
    public LineRenderer ProngLine1;
    public LineRenderer ProngLine2;
    public LineRenderer ProngLine3;

    // this is a closed loop that connects the 3 prong target locations together
    public LineRenderer ProngTriangle;

    // text showing position/rotation error
    public TMP_Text DistanceText;

    public bool axesWireframe = false;

    public Transform Stick;

    public Transform RotationParent; // child of the target point, contains all visualizations inside it, we will do rotations of it based on dynamic prongs

    public GameObject RedBall;
    
    public void OnTargetSpawned(SpawnedPoseTargetPoint spawnedPoseTargetPoint, InterfaceType alignmentInterfaceType, ControllerTargetPointLocation controllerTargetPointLocation)
    {
        CurrentSpawnedPoseTargetPoint = spawnedPoseTargetPoint;

        RefreshVisualization(alignmentInterfaceType, controllerTargetPointLocation);
    }
    
    public void SetupProngs(bool withSphere)
    {
        float quadDistance = Helpers.GetProjectorProngsQuadDistance();
        float fovDegrees = Helpers.GetProjectorProngsFOVDegrees(quadDistance);

        ProngQuad.SetActive(true);
        Helpers.SetProjectionQuadSize(ProngQuad.transform, fovDegrees, quadDistance);
        
        ProngTarget1.gameObject.SetActive(true);
        ProngTarget2.gameObject.SetActive(true);
        ProngTarget3.gameObject.SetActive(true);

        ProngLine1.gameObject.SetActive(true);
        ProngLine2.gameObject.SetActive(true);
        ProngLine3.gameObject.SetActive(true);

        ProngLine1.SetPosition(1, transform.InverseTransformPoint(ProngTarget1.transform.position));
        ProngLine2.SetPosition(1, transform.InverseTransformPoint(ProngTarget2.transform.position));
        ProngLine3.SetPosition(1, transform.InverseTransformPoint(ProngTarget3.transform.position));

        bool withTriangle = true;
        if (withTriangle)
        {
            ProngTriangle.gameObject.SetActive(true);

            ProngTriangle.SetPosition(0, transform.InverseTransformPoint(ProngTarget1.transform.position));
            ProngTriangle.SetPosition(1, transform.InverseTransformPoint(ProngTarget2.transform.position));
            ProngTriangle.SetPosition(2, transform.InverseTransformPoint(ProngTarget3.transform.position));
        }
    }

    private void SetupProngsDynamic(bool withSphere)
    {
        SetupProngs(withSphere);

        RotationParent.localRotation = Helpers.GetLocalRotationForDynamicProngs(this.CurrentSpawnedPoseTargetPoint);
    }

    public void SetupProjector(bool withSphere)
    {
        float quadDistance = Helpers.GetProjectorProngsQuadDistance();
        float fovDegrees = Helpers.GetProjectorProngsFOVDegrees(quadDistance);

        TargetProjector.gameObject.SetActive(true);
        Helpers.SetProjectorFOV(TargetProjector, fovDegrees);

        RedBall.SetActive(true);
    }
    
    // nothing is done with the sphere on the controller target point
    public void SetupAxes(InterfaceScale interfaceScale, bool withSphere)
    {
        float axesLength = 1.0f;

        switch (interfaceScale)
        {
            case InterfaceScale.Small:
                axesLength = Helpers.GetAxesLengthForDistance(ObjectManager.Instance.SessionManager.UserArmHinged);
                break;
            case InterfaceScale.Large:
                axesLength = Helpers.GetAxesLengthForDistance(ObjectManager.Instance.SessionManager.UserArmOutstretched);
                break;
            default:
                Debug.LogError("unhandled interfaceScale: " + interfaceScale);
                break;
        }
        
        AxesGameObject.gameObject.SetActive(true);
        AxesGameObject.SetAxesSize(axesLength, axesLength / Constants.DefaultAxesLengthToWidthRatio, axesWireframe);
    }






    public void RefreshVisualization(InterfaceType newAlignmentInterfaceType, ControllerTargetPointLocation newControllerTargetPointLocation)
    {
        CurrentAlignmentInterfaceType = newAlignmentInterfaceType;
        CurrentControllerTargetPointLocation = newControllerTargetPointLocation;

        // hide all UIs, then enable the one we want
        HideAllInterfaces();
        
        switch (CurrentAlignmentInterfaceType)
        {
            case InterfaceType._AxesSmallNoSphere:
                SetupAxes(InterfaceScale.Small, false);
                break;
            case InterfaceType._AxesSmallWithSphere:
                SetupAxes(InterfaceScale.Small, true);
                break;
            case InterfaceType._AxesLargeNoSphere:
                SetupAxes(InterfaceScale.Large, false);
                break;
            case InterfaceType._AxesLargeWithSphere:
                SetupAxes(InterfaceScale.Large, true);
                break;
            case InterfaceType._ProjectorNoSphere:
                SetupProjector(false);
                break;
            case InterfaceType._ProjectorWithSphere:
                SetupProjector(true);
                break;
            case InterfaceType._ProngsNoSphere:
                SetupProngs(false);
                break;
            case InterfaceType._ProngsWithSphere:
                SetupProngs(true);
                break;
            case InterfaceType._ProngsDynamicNoSphere:
                SetupProngsDynamic(false);
                break;
            case InterfaceType._ProngsDynamicWithSphere:
                SetupProngsDynamic(true);
                break;
            default:
                Debug.LogError("ControllerTargetPoint: unhandled interface type " + CurrentAlignmentInterfaceType);
                break;
        }
    }

    

    public void OnAlignmentConfirmed()
    {
        HideAllInterfaces();

        CurrentSpawnedPoseTargetPoint = null;
    }

    public void HideAllInterfaces()
    {
        AxesGameObject.gameObject.SetActive(false);

        TargetProjector.gameObject.SetActive(false);

        ProngQuad.gameObject.SetActive(false);

        ProngTarget1.gameObject.SetActive(false);
        ProngTarget2.gameObject.SetActive(false);
        ProngTarget3.gameObject.SetActive(false);

        ProngLine1.gameObject.SetActive(false);
        ProngLine2.gameObject.SetActive(false);
        ProngLine3.gameObject.SetActive(false);

        DistanceText.gameObject.SetActive(false);

        ProngTriangle.gameObject.SetActive(false);

        RotationParent.localRotation = Quaternion.identity;

        RedBall.SetActive(false);
    }

    // Start is called before the first frame update
    void Start()
    {
        if (AxesGameObject == null)
        {
            Debug.LogError("AxesGameObject on ControllerTargetPoint should not be null");
        }

        if (TargetProjector == null)
        {
            Debug.LogError("TargetProjector on ControllerTargetPoint should not be null");
        }

        if (ProngQuad == null)
        {
            Debug.LogError("ProngQuad on ControllerTargetPoint should not be null");
        }

        if (ProngTarget1 == null || ProngTarget2 == null || ProngTarget3 == null)
        {
            Debug.LogError("ProngTargets on ControllerTargetPoint should not be null");
        }

        if (ProngLine1 == null || ProngLine2 == null || ProngLine3 == null)
        {
            Debug.LogError("ProngLines on ControllerTargetPoint should not be null");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (DistanceText.gameObject.activeInHierarchy)
        {
            if (CurrentSpawnedPoseTargetPoint != null)
            {
                float dist_cm = 100.0f * Vector3.Distance(transform.position, CurrentSpawnedPoseTargetPoint.transform.position);
                float dist_deg = Quaternion.Angle(transform.rotation, CurrentSpawnedPoseTargetPoint.transform.rotation);

                DistanceText.text = dist_cm + "cm\n" + dist_deg + "°";
            }
            else
            {
                DistanceText.text = "";
            }
        }
    }
}
