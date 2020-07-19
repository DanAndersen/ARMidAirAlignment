using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Align;

public class SpawnedPoseTargetPoint : MonoBehaviour
{
    private ControllerTargetPoint CurrentControllerTargetPoint = null;
    private InterfaceType CurrentAlignmentInterfaceType;
    private ControllerTargetPointLocation CurrentControllerTargetPointLocation;

    public Axes AxesGameObject;

    public GameObject ProjectionQuad;

    public Projector FixedTargetProjector;
    
    public LineRenderer ProngTarget1;
    public LineRenderer ProngTarget2;
    public LineRenderer ProngTarget3;

    // this is a closed loop that connects the 3 prong target locations together
    public LineRenderer ProngTriangle;

    public Transform AxesRadiusSphere;

    public Transform ProngSphere1;
    public Transform ProngSphere2;
    public Transform ProngSphere3;

    public Transform RotationParent; // child of the target point, contains all visualizations inside it, we will do rotations of it based on dynamic prongs

    public bool axesWireframe = true;

    public void OnTargetSpawned(ControllerTargetPoint controllerTargetPoint, InterfaceType alignmentInterfaceType, ControllerTargetPointLocation controllerTargetPointLocation)
    {
        this.transform.localPosition = controllerTargetPoint.transform.localPosition;

        CurrentControllerTargetPoint = controllerTargetPoint;

        RefreshVisualization(alignmentInterfaceType, controllerTargetPointLocation);
    }

    public void SetupProngs(bool withSphere)
    {
        float quadDistance = Helpers.GetProjectorProngsQuadDistance();
        float fovDegrees = Helpers.GetProjectorProngsFOVDegrees(quadDistance);

        ProjectionQuad.SetActive(true);
        Helpers.SetProjectionQuadSize(ProjectionQuad.transform, fovDegrees, quadDistance);
        
        ProngTarget1.gameObject.SetActive(true);
        ProngTarget2.gameObject.SetActive(true);
        ProngTarget3.gameObject.SetActive(true);

        if (withSphere)
        {
            ProngSphere1.gameObject.SetActive(true);
            ProngSphere2.gameObject.SetActive(true);
            ProngSphere3.gameObject.SetActive(true);

            ProngSphere1.position = ProngTarget1.transform.position;
            ProngSphere2.position = ProngTarget2.transform.position;
            ProngSphere3.position = ProngTarget3.transform.position;
        }

        bool withTriangle = true;
        if (withTriangle)
        {
            ProngTriangle.gameObject.SetActive(true);

            //ProngTriangle.SetPosition(0, transform.InverseTransformPoint(ProngTarget1.transform.position));
            //ProngTriangle.SetPosition(1, transform.InverseTransformPoint(ProngTarget2.transform.position));
            //ProngTriangle.SetPosition(2, transform.InverseTransformPoint(ProngTarget3.transform.position));
        }

        if (withSphere)
        {
            SetupRadiusSphere();
        }
    }

    private void SetupProngsDynamic(bool withSphere)
    {
        SetupProngs(withSphere);

        RotationParent.localRotation = Helpers.GetLocalRotationForDynamicProngs(this);
    }

    public void SetupProjector(bool withSphere)
    {
        float quadDistance = Helpers.GetProjectorProngsQuadDistance();
        float fovDegrees = Helpers.GetProjectorProngsFOVDegrees(quadDistance);

        ProjectionQuad.SetActive(true);
        Helpers.SetProjectionQuadSize(ProjectionQuad.transform, fovDegrees, quadDistance);
        
        FixedTargetProjector.gameObject.SetActive(true);
        Helpers.SetProjectorFOV(FixedTargetProjector, fovDegrees);

        if (withSphere)
        {
            SetupRadiusSphere();
        }
    }

    public void SetupRadiusSphere()
    {
        //Vector3 offset = Vector3.one * (axesLength/2);
        Vector3 offset = Vector3.zero;

        AxesRadiusSphere.gameObject.SetActive(true);
        AxesRadiusSphere.transform.localPosition = offset;
    }

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

        if (withSphere)
        {
            SetupRadiusSphere();
        }
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
                Debug.LogError("SpawnedPoseTargetPoint: unhandled interface type " + CurrentAlignmentInterfaceType);
                break;
        }
    }
    
    public void OnAlignmentConfirmed()
    {
        HideAllInterfaces();

        CurrentControllerTargetPoint = null;
    }

    private void HideAllInterfaces()
    {
        AxesGameObject.gameObject.SetActive(false);

        ProjectionQuad.SetActive(false);

        FixedTargetProjector.gameObject.SetActive(false);

        ProngTarget1.gameObject.SetActive(false);
        ProngTarget2.gameObject.SetActive(false);
        ProngTarget3.gameObject.SetActive(false);

        AxesRadiusSphere.gameObject.SetActive(false);

        ProngSphere1.gameObject.SetActive(false);
        ProngSphere2.gameObject.SetActive(false);
        ProngSphere3.gameObject.SetActive(false);

        ProngTriangle.gameObject.SetActive(false);

        RotationParent.localRotation = Quaternion.identity;
    }

    // Start is called before the first frame update
    void Start()
    {
        if (AxesGameObject == null)
        {
            Debug.LogError("AxesGameObject on SpawnedTargetObject should not be null");
        }

        if (ProjectionQuad == null)
        {
            Debug.LogError("ProjectionQuad on SpawnedTargetObject should not be null");
        }

        if (FixedTargetProjector == null)
        {
            Debug.LogError("FixedTargetProjector on SpawnedTargetObject should not be null");
        }

        if (ProngTarget1 == null || ProngTarget2 == null || ProngTarget3 == null)
        {
            Debug.LogError("ProngTargets on SpawnedTargetObject should not be null");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void FixedUpdate()
    {
        if (CurrentControllerTargetPoint != null)
        {
            if (AxesRadiusSphere.gameObject.activeSelf)
            {
                float radius = Vector3.Distance(CurrentControllerTargetPoint.transform.position, transform.position);
                AxesRadiusSphere.transform.localScale = Vector3.one * radius * 2;
            }

            if (ProngSphere1.gameObject.activeSelf)
            {
                float radius = Vector3.Distance(CurrentControllerTargetPoint.ProngTarget1.transform.position, ProngSphere1.transform.position);
                ProngSphere1.transform.localScale = Vector3.one * radius * 2;
            }

            if (ProngSphere2.gameObject.activeSelf)
            {
                float radius = Vector3.Distance(CurrentControllerTargetPoint.ProngTarget2.transform.position, ProngSphere2.transform.position);
                ProngSphere2.transform.localScale = Vector3.one * radius * 2;
            }

            if (ProngSphere3.gameObject.activeSelf)
            {
                float radius = Vector3.Distance(CurrentControllerTargetPoint.ProngTarget3.transform.position, ProngSphere3.transform.position);
                ProngSphere3.transform.localScale = Vector3.one * radius * 2;
            }
        }
    }
}
