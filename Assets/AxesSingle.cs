using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AxesSingle : MonoBehaviour
{
    public MeshRenderer Cylinder;
    public MeshRenderer Cone;

    public MeshRenderer Spur1;
    public MeshRenderer Spur2;

    public void SetAxesSize(float length, float width, bool axesWireframe)
    {
        bool useCone = false;
        bool useSpur = true;

        Cylinder.transform.localPosition = new Vector3(0, 0, length / 2);
        Cylinder.transform.localScale = new Vector3(width, length/2, width);

        Cylinder.enabled = true;

        if (useCone)
        {
            Cone.transform.localPosition = new Vector3(0, 0, length);
            Cone.transform.localScale = Vector3.one * (width * 100);
            Cone.enabled = true;
        }
        else
        {
            Cone.enabled = false;
        }
        
        if (useSpur)
        {
            Spur1.transform.localPosition = new Vector3(0, 0, length);
            Spur2.transform.localPosition = new Vector3(0, 0, length);

            Spur1.transform.localScale = new Vector3(width, width*2, width);
            Spur2.transform.localScale = new Vector3(width, width*2, width);

            Spur1.enabled = true;
            Spur2.enabled = true;
        } else
        {
            Spur1.enabled = false;
            Spur2.enabled = false;
        }
    }
}
