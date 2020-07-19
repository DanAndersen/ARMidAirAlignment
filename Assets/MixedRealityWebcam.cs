using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MixedRealityWebcam : MonoBehaviour
{
    private bool camAvailable;
    private WebCamTexture camTexture;
    private Texture defaultBackground;
    
    public RawImage background;

    public Material CompositeMaterial;

    public Camera VirtualContentCamera;
    public RenderTexture VirtualContentRenderTexture;

    // Start is called before the first frame update
    void Start()
    {
        defaultBackground = background.texture;

        WebCamDevice[] devices = WebCamTexture.devices;

        if (devices.Length == 0)
        {
            Debug.Log("no camera detected");
            camAvailable = false;
            return;
        }

        for (int i = 0; i < devices.Length; i++)
        {
            Debug.Log("webcam device " + i + ": " + devices[i].name);
        }

        for (int i = 0; i < devices.Length; i++)
        {
            if (devices[i].name == "HTC Vive") // the camera on the Vive headset, we don't want this one
            {
                continue;
            }
            camTexture = new WebCamTexture(devices[i].name);

            break;
        }

        if (camTexture == null)
        {
            Debug.Log("unable to find camera");
        }

        camTexture.Play();
        background.texture = camTexture;

        CompositeMaterial.SetTexture("_WebcamTex", camTexture);

        camAvailable = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (!camAvailable)
        {
            return;
        }

        float scaleY = camTexture.videoVerticallyMirrored ? -1f : 1f;
        background.rectTransform.localScale = new Vector3(1f, scaleY, 1f);

        int orient = -camTexture.videoRotationAngle;

        background.rectTransform.localEulerAngles = new Vector3(0, 0, orient);



        float vc_width = VirtualContentRenderTexture.width;
        float vc_height = VirtualContentRenderTexture.height;
        float vc_rt_ratio = vc_width / vc_height;

        float vc_vfov = VirtualContentCamera.fieldOfView * Mathf.Deg2Rad; // vertical FOV
        float vc_hfov = vc_vfov * vc_rt_ratio;


        float vc_fx = vc_width / (2 * Mathf.Tan(vc_hfov / 2));
        float vc_fy = vc_height / (2 * Mathf.Tan(vc_vfov / 2));


        float vc_cx = VirtualContentRenderTexture.width / 2.0f;
        float vc_cy = VirtualContentRenderTexture.height / 2.0f;

        // set the virtual-content camera parameters

        CompositeMaterial.SetFloat("_VC_Width", VirtualContentRenderTexture.width);
        CompositeMaterial.SetFloat("_VC_Height", VirtualContentRenderTexture.height);

        CompositeMaterial.SetFloat("_VC_Fx", vc_fx);
        CompositeMaterial.SetFloat("_VC_Fy", vc_fy);
        CompositeMaterial.SetFloat("_VC_Cx", vc_cx);
        CompositeMaterial.SetFloat("_VC_Cy", vc_cy);
    }
}
