using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Align
{
    public enum InterfaceType
    {
        _AxesSmallNoSphere = 0,
        _AxesSmallWithSphere = 1,
        _AxesLargeNoSphere = 2,
        _AxesLargeWithSphere = 3,
        _ProjectorNoSphere = 4,
        _ProjectorWithSphere = 5,
        _ProngsNoSphere = 6,
        _ProngsWithSphere = 7,
        _ProngsDynamicNoSphere = 8,
        _ProngsDynamicWithSphere = 9,
    }

    public enum ControllerTargetPointLocation
    {
        ControllerTip = 0,
        GripCenter = 1,
    }

    public enum ControllerWireframeModelVisibility
    {
        Hidden = 0,
        Visible = 1,
    }

    public enum WhichTargetPoint
    {
        Left = 0,
        Right = 1,
    }

    public enum InterfaceScale
    {
        Small = 0,
        Large = 1,
    }

    public class Constants
    {
        public const float HoloFocalDistanceMeters = 2.0f;

        public const float HoloFOVHorizontalDegrees = 30.0f;
        public const float HoloFOVVerticalDegrees = 17.5f;

        public const float DefaultAxesLengthToWidthRatio = 50.0f;
    }

    public class Helpers
    {
        public static Quaternion GetLocalRotationForDynamicProngs(SpawnedPoseTargetPoint targetPoint)
        {
            bool needToFlip = false;

            Transform tpTransform = targetPoint.transform;
            Transform viveOrigin = ObjectManager.Instance.ViveOrigin; // the coordinate system of the Vive. The floor plane will be relative to this.
            
            Vector3 tpUp_world = tpTransform.forward; // the +Z direction ends up being the "up" of the controller
            Vector3 tpUp_vive = viveOrigin.InverseTransformDirection(tpUp_world);

            if (tpUp_vive.y < 0)
            {
                needToFlip = true;
                Debug.Log("need to flip");
            }
            
            Vector3 tpForward_world = -tpTransform.up; // note that the -Y direction ends up being the "forward" of the controller

            Vector3 rayDir_world = tpForward_world;

            if (needToFlip)
            {
                rayDir_world = tpUp_world;
            }
            
            Vector3 rayDir_vive = viveOrigin.InverseTransformDirection(rayDir_world);
            
            Vector3 groundPlaneNormal_vive = Vector3.up;

            Vector3 rayDir_ProjectedOntoGroundPlane_vive = Vector3.ProjectOnPlane(rayDir_vive, groundPlaneNormal_vive).normalized;

            float tp_to_quad_distance = GetProjectorProngsQuadDistance();

            Vector3 tpPosition_world = tpTransform.position;
            
            Vector3 tpPosition_vive = viveOrigin.InverseTransformPoint(tpPosition_world);
            Vector3 tpNewForwardEndpoint_vive = tpPosition_vive + tp_to_quad_distance * rayDir_ProjectedOntoGroundPlane_vive;

            // raise it up to user head height
            tpNewForwardEndpoint_vive.y = ObjectManager.Instance.SessionManager.UserHeightToFloor;

            Vector3 new_tpForward_vive = (tpNewForwardEndpoint_vive - tpPosition_vive).normalized;

            Vector3 new_tpForward_world = viveOrigin.TransformDirection(new_tpForward_vive);
            
            float degrees = Vector3.SignedAngle(tpForward_world, new_tpForward_world, tpTransform.right);

            Debug.Log("degrees: " + degrees);
            
            Quaternion localRotation_tp = Quaternion.AngleAxis(degrees, Vector3.right);

            return localRotation_tp;
        }

        public static float GetProjectorProngsQuadDistance()
        {
            // goal is for user to hold the controller at distance ObjectManager.Instance.SessionManager.UserArmHinged
            // and to see the quad at Constants.HoloFocalDistanceMeters

            return Constants.HoloFocalDistanceMeters - ObjectManager.Instance.SessionManager.UserArmHinged;
            //return ObjectManager.Instance.SessionManager.UserArmHinged;
        }

        public static float GetProjectorProngsFOVDegrees(float quadDistance)
        {
            // given a target-point to quad distance of quadDistance, and a desired user-to-quad distance of quadDistance+ObjectManager.Instance.SessionManager.UserArmHinged,
            // what should be the FOV of the projection onto the quad so that when the user sees the quad, it just fills up the user's AR HMD FOV?

            float userToQuadDist = quadDistance + ObjectManager.Instance.SessionManager.UserArmHinged;

            float hmd_fov_radians = Constants.HoloFOVVerticalDegrees * Mathf.Deg2Rad;

            float half_x = userToQuadDist * Mathf.Tan(hmd_fov_radians / 2);

            float quad_fov_radians = 2 * Mathf.Atan2(half_x, quadDistance);

            float quad_fov_degrees = quad_fov_radians * Mathf.Rad2Deg;

            return quad_fov_degrees;
        }

        public static float GetAxesLengthForDistance(float headToTargetDistanceMeters)
        {
            float holoMinFOVDegrees = Mathf.Min(Constants.HoloFOVHorizontalDegrees, Constants.HoloFOVVerticalDegrees);

            float holoMinFOVRadians = Mathf.Deg2Rad * holoMinFOVDegrees;

            float axesLength = 2.0f * headToTargetDistanceMeters * Mathf.Tan(holoMinFOVRadians / 2.0f);

            return axesLength;
        }
        
        public static void SetProjectorFOV(Projector targetProjector, float projectorFOVDegrees)
        {
            targetProjector.fieldOfView = projectorFOVDegrees;
        }
        
        internal static void SetProjectionQuadSize(Transform transform, float projectorFOVDegrees, float projectionQuadDistance)
        {
            transform.localPosition = new Vector3(0.0f, -projectionQuadDistance, 0.0f);

            float projectorFOVRadians = Mathf.Deg2Rad * projectorFOVDegrees;

            float quadSize = Mathf.Tan(projectorFOVRadians / 2.0f) * projectionQuadDistance * 2.0f;

            transform.localScale = new Vector3(quadSize, quadSize, quadSize);
        }

        internal static float GetProjectQuadDistanceForHeadLevel(Transform camera, Transform alignmentPoint, float minDistance, float maxDistance)
        {
            Plane headLevelPlane = new Plane(Vector3.up, camera.position);

            Ray alignmentRay = new Ray(alignmentPoint.position, -alignmentPoint.up); // NOTE: the projection quad is shifted along the -Y direction as shown in SetProjectionQuadSize()

            float enter = minDistance;
            if (headLevelPlane.Raycast(alignmentRay, out enter))
            {
                return Mathf.Clamp(enter, minDistance, maxDistance);
            } else
            {
                return minDistance;
            }
        }
    }
}

