using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectManager : Singleton<ObjectManager>
{
    public Transform HoloHeadset;
    public Transform ViveHeadset;

    public Transform ViveLeftControllerParent;
    public Transform ViveRightControllerParent;

    public Transform ViveLeftControllerAlignmentPoint;
    public Transform ViveRightControllerAlignmentPoint;

    public ViveControllerInputManager ViveControllerInputManager;

    public Transform HoloOrigin;
    public Transform ViveOrigin;

    public BillboardText BillboardText;

    public SessionManager SessionManager;
}
