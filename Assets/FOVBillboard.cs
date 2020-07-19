using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FOVBillboard : MonoBehaviour
{

    protected bool Following { get; set; } = true;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Following)
        {
            transform.SetPositionAndRotation(Camera.main.transform.position, Camera.main.transform.rotation);
        }
    }
}
