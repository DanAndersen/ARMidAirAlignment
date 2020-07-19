using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestQuaternionMath : MonoBehaviour
{
    public Transform HMD;
    public Transform Controller;

    string Vec3ToString(Vector3 v)
    {
        return v.x + " " + v.y + " " + v.z;
    }

    string QuatToString(Quaternion q)
    {
        return q.x + " " + q.y + " " + q.z + " " + q.w;
    }

    Vector3 MyInverseTransformPoint(Transform t, Vector3 point)
    {
        return Quaternion.Inverse(t.rotation) * (point - t.position);
    }

    Vector3 MyTransformPoint(Transform t, Vector3 point)
    {
        return t.position + t.rotation * point;
    }

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("input:");
        Debug.Log("HMD position: " + Vec3ToString(HMD.position));
        Debug.Log("Controller position: " + Vec3ToString(Controller.position));
        Debug.Log("HMD rotation: " + QuatToString(HMD.rotation));
        Debug.Log("Controller rotation: " + QuatToString(Controller.rotation));

        Debug.Log("intended output:");
        Debug.Log("position of the Controller relative to the HMD: " + Vec3ToString(HMD.InverseTransformPoint(Controller.position)));
        Debug.Log("position of a point 1 meter on controller's +Z, relative to the world: " +
            Vec3ToString(Controller.TransformPoint(new Vector3(0, 0, 1))));
        Debug.Log("position of a point 1 meter on controller's +Z, relative to the HMD: " +
            Vec3ToString(HMD.InverseTransformPoint(Controller.TransformPoint(new Vector3(0, 0, 1)))));

        Debug.Log("my output:");
        Debug.Log("position of the Controller relative to the HMD: " + Vec3ToString(MyInverseTransformPoint(HMD, Controller.position)));
        Debug.Log("position of a point 1 meter on controller's +Z, relative to the world: " +
            Vec3ToString(MyTransformPoint(Controller, new Vector3(0, 0, 1))));
        Debug.Log("position of a point 1 meter on controller's +Z, relative to the HMD: " +
            Vec3ToString(MyInverseTransformPoint(HMD, MyTransformPoint(Controller, new Vector3(0, 0, 1)))));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
