using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.Experimental.Utilities;
using Microsoft.MixedReality.Toolkit.Utilities;

[RequireComponent(typeof(StabilizationPlaneModifier))]
public class StabilizationPlaneOverrideModifier : MonoBehaviour
{
    public bool OverrideStabilizationPlaneDistance = true;

    public Transform[] Transforms;

    private StabilizationPlaneModifier stabilizationPlaneModifier;

    private Transform _initialTargetOverride = null;
    private StabilizationPlaneModifier.StabilizationPlaneMode _initialMode;

    // Start is called before the first frame update
    void Start()
    {
        stabilizationPlaneModifier = GetComponent<StabilizationPlaneModifier>();

        _initialTargetOverride = stabilizationPlaneModifier.TargetOverride;
        _initialMode = stabilizationPlaneModifier.mode;
    }

    private void LateUpdate()
    {
        if (OverrideStabilizationPlaneDistance)
        {
            if (Transforms != null)
            {
                Transform selectedTransform = null;
                float selected_zCameraToTarget = Mathf.Infinity;

                float nearClipPlane = CameraCache.Main.nearClipPlane;

                foreach (var t in Transforms)
                {
                    if (t.gameObject.activeSelf)
                    {
                        Vector3 targetWorldPosition = t.position;

                        Vector3 targetPositionInCameraSpace = Camera.main.transform.InverseTransformPoint(targetWorldPosition);

                        float zCameraToTarget = targetPositionInCameraSpace.z;

                        if (zCameraToTarget > nearClipPlane && zCameraToTarget < selected_zCameraToTarget)
                        {
                            selectedTransform = t;
                            selected_zCameraToTarget = zCameraToTarget;
                        }
                    }
                }

                if (selectedTransform != null)
                {
                    stabilizationPlaneModifier.TargetOverride = selectedTransform;
                    stabilizationPlaneModifier.mode = StabilizationPlaneModifier.StabilizationPlaneMode.TargetOverride;
                } else
                {
                    stabilizationPlaneModifier.TargetOverride = _initialTargetOverride;
                    stabilizationPlaneModifier.mode = _initialMode;
                }
            }
        } else
        {
            stabilizationPlaneModifier.TargetOverride = _initialTargetOverride;
            stabilizationPlaneModifier.mode = _initialMode;
        }
    }
}
