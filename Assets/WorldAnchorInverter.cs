using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

// The goal of this script is to make it so that the coordinate systems appear merged on the Vive Desktop.
// This is so we can record a session on the Vive Desktop and compare the poses of the HoloLens headset and the poses of the controllers, and do it all in world space.

// This prefab is spawned by the HoloLens when:
// - it is connected to the network
// - there is a world anchor that exists in the scene

// The HoloLens client player has authority over this object, so it will be destroyed when the HoloLens client disconnects.

// Every frame:
// - on the HoloLens (local player), it will take the transform of the world anchor, and set its own transform to be the inverse of that.
// - the network transform will sync that transform to the WorldAnchorInverter that is on the Vive Desktop.
// - on the Vive Desktop, it will update the pose of its own HoloOrigin to match the transform of the WorldAnchorInverter.

public class WorldAnchorInverter : NetworkBehaviour
{
    public AnchorPlacer anchorPlacer;

    public GameObject holoOrigin;

    private AnchorMarker worldAnchorOnHoloLens;

    private void Start()
    {
        holoOrigin = GameObject.Find("HoloOrigin");

        anchorPlacer = GameObject.Find("AnchorPlacer").GetComponent<AnchorPlacer>();

        if (holoOrigin == null)
        {
            Debug.LogError("WorldAnchorInverter: holoOrigin cannot be null");
        }

        if (anchorPlacer == null)
        {
            Debug.LogError("WorldAnchorInverter: anchorPlacer cannot be null");
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
    }





    /// <summary>
    /// Extract translation from transform matrix.
    /// </summary>
    /// <param name="matrix">Transform matrix. This parameter is passed by reference
    /// to improve performance; no changes will be made to it.</param>
    /// <returns>
    /// Translation offset.
    /// </returns>
    public static Vector3 ExtractTranslationFromMatrix(ref Matrix4x4 matrix)
    {
        Vector3 translate;
        translate.x = matrix.m03;
        translate.y = matrix.m13;
        translate.z = matrix.m23;
        return translate;
    }

    /// <summary>
    /// Extract rotation quaternion from transform matrix.
    /// </summary>
    /// <param name="matrix">Transform matrix. This parameter is passed by reference
    /// to improve performance; no changes will be made to it.</param>
    /// <returns>
    /// Quaternion representation of rotation transform.
    /// </returns>
    public static Quaternion ExtractRotationFromMatrix(ref Matrix4x4 matrix)
    {
        Vector3 forward;
        forward.x = matrix.m02;
        forward.y = matrix.m12;
        forward.z = matrix.m22;

        Vector3 upwards;
        upwards.x = matrix.m01;
        upwards.y = matrix.m11;
        upwards.z = matrix.m21;

        return Quaternion.LookRotation(forward, upwards);
    }

    /// <summary>
    /// Extract scale from transform matrix.
    /// </summary>
    /// <param name="matrix">Transform matrix. This parameter is passed by reference
    /// to improve performance; no changes will be made to it.</param>
    /// <returns>
    /// Scale vector.
    /// </returns>
    public static Vector3 ExtractScaleFromMatrix(ref Matrix4x4 matrix)
    {
        Vector3 scale;
        scale.x = new Vector4(matrix.m00, matrix.m10, matrix.m20, matrix.m30).magnitude;
        scale.y = new Vector4(matrix.m01, matrix.m11, matrix.m21, matrix.m31).magnitude;
        scale.z = new Vector4(matrix.m02, matrix.m12, matrix.m22, matrix.m32).magnitude;
        return scale;
    }

    /// <summary>
    /// Extract position, rotation and scale from TRS matrix.
    /// </summary>
    /// <param name="matrix">Transform matrix. This parameter is passed by reference
    /// to improve performance; no changes will be made to it.</param>
    /// <param name="localPosition">Output position.</param>
    /// <param name="localRotation">Output rotation.</param>
    /// <param name="localScale">Output scale.</param>
    public static void DecomposeMatrix(ref Matrix4x4 matrix, out Vector3 localPosition, out Quaternion localRotation, out Vector3 localScale)
    {
        localPosition = ExtractTranslationFromMatrix(ref matrix);
        localRotation = ExtractRotationFromMatrix(ref matrix);
        localScale = ExtractScaleFromMatrix(ref matrix);
    }

    /// <summary>
    /// Set transform component from TRS matrix.
    /// </summary>
    /// <param name="transform">Transform component.</param>
    /// <param name="matrix">Transform matrix. This parameter is passed by reference
    /// to improve performance; no changes will be made to it.</param>
    public static void SetTransformFromMatrix(Transform transform, ref Matrix4x4 matrix)
    {
        transform.localPosition = ExtractTranslationFromMatrix(ref matrix);
        transform.localRotation = ExtractRotationFromMatrix(ref matrix);
        transform.localScale = ExtractScaleFromMatrix(ref matrix);
    }


    // EXTRAS!

    /// <summary>
    /// Identity quaternion.
    /// </summary>
    /// <remarks>
    /// <para>It is faster to access this variation than <c>Quaternion.identity</c>.</para>
    /// </remarks>
    public static readonly Quaternion IdentityQuaternion = Quaternion.identity;
    /// <summary>
    /// Identity matrix.
    /// </summary>
    /// <remarks>
    /// <para>It is faster to access this variation than <c>Matrix4x4.identity</c>.</para>
    /// </remarks>
    public static readonly Matrix4x4 IdentityMatrix = Matrix4x4.identity;

    /// <summary>
    /// Get translation matrix.
    /// </summary>
    /// <param name="offset">Translation offset.</param>
    /// <returns>
    /// The translation transform matrix.
    /// </returns>
    public static Matrix4x4 TranslationMatrix(Vector3 offset)
    {
        Matrix4x4 matrix = IdentityMatrix;
        matrix.m03 = offset.x;
        matrix.m13 = offset.y;
        matrix.m23 = offset.z;
        return matrix;
    }










    private void Update()
    {
        if (hasAuthority && WhichDeviceManager.Instance.IsHoloLens())
        {
            // this code will only run on the HoloLens

            // first, check if we have our world anchor (do it here, in case we connected to the network before the world anchor was set up)

            if (worldAnchorOnHoloLens == null)
            {
                worldAnchorOnHoloLens = anchorPlacer.GetMarkerViveOrigin();
            }

            // now, only proceed if we know it's actually not null

            if (worldAnchorOnHoloLens != null)
            {
                // on the HoloLens, the world position of the world anchor represents where the Vive origin is relative to the HoloLens.

                Matrix4x4 mat = worldAnchorOnHoloLens.transform.localToWorldMatrix;
                Matrix4x4 inverseMat = mat.inverse;

                // adjust the world-space transform of the WorldAnchorInverter to be these inverse position/rotation values.
                transform.position = ExtractTranslationFromMatrix(ref inverseMat);
                transform.rotation = ExtractRotationFromMatrix(ref inverseMat);
            }
        }

        if (!hasAuthority && WhichDeviceManager.Instance.IsVive())
        {
            // this is on the vive. the local pose of the WorldAnchorInverter represents where the HoloOrigin should be relative to the Vive's origin.
            holoOrigin.transform.SetPositionAndRotation(transform.position, transform.rotation);
        }
    }
}
