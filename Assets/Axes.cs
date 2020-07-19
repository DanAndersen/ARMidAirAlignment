using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Axes : MonoBehaviour
{
    public AxesSingle AxesSingleX;
    public AxesSingle AxesSingleY;
    public AxesSingle AxesSingleZ;
    
    public void SetAxesSize(float length, float width, bool axesWireframe)
    {
        AxesSingleX.SetAxesSize(length, width, axesWireframe);
        AxesSingleY.SetAxesSize(length, width, axesWireframe);
        AxesSingleZ.SetAxesSize(length, width, axesWireframe);
    }
}
