using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Experimental.Utilities;
using Microsoft.MixedReality.Toolkit.Input;
using System;
using TMPro;
using System.Text;

public class AnchorMarker : MonoBehaviour
{
    [SerializeField]
    public TMP_Text InfoText;

    [SerializeField]
    public Material AnchorAttached;

    [SerializeField]
    public Material AnchorDetached;

    [SerializeField]
    public Renderer CapsuleRenderer;

    [SerializeField]
    public Collider CapsuleCollider;

    [SerializeField]
    public GameObject Axes;

    public string Id = null;

    public bool IsAttached { get; private set; }

    private WorldAnchorManager worldAnchorManager;

    public void Init(string id, WorldAnchorManager worldAnchorManager)
    {
        this.Id = id;
        this.worldAnchorManager = worldAnchorManager;
        this.gameObject.name = "AnchorMarker [" + this.Id + "]";
    }

    private void Start()
    {
        
    }

    public bool IsAnchorManagerQueueEmpty()
    {
#if UNITY_WSA
        return (worldAnchorManager.GetNumQueuedAnchorOperations() == 0);
#else
        return true;
#endif
    }

    public void AttachAnchor()
    {
        if (Id == null)
        {
            Debug.LogError("attempting to attach anchor when its Id isn't set yet");
        }

        worldAnchorManager.AttachAnchor(gameObject, Id);
        CapsuleRenderer.material = AnchorAttached;
        Debug.Log("+Anchor attached for: " + this.gameObject.name + " - AnchorID: " + Id);
        IsAttached = true;
    }

    public void DetachAnchor()
    {
        if (Id == null)
        {
            Debug.LogError("attempting to detach anchor when its Id isn't set yet");
        }

        worldAnchorManager.RemoveAnchor(gameObject);
        CapsuleRenderer.material = AnchorDetached;
        Debug.Log("-Anchor detached for: " + this.gameObject.name + " - AnchorID: " + Id);
        IsAttached = false;
    }

    public override bool Equals(object obj)
    {

        if (obj == null || GetType() != obj.GetType())
        {
            return false;
        }

        AnchorMarker otherMarker = obj as AnchorMarker;
        return Id.Equals(otherMarker.Id);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public void Update()
    {
        if (InfoText != null)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("ID: ");
            sb.Append(Id);
            sb.AppendLine();

            Vector3 pos = transform.position;
            
            sb.Append("Pos X: ");
            sb.Append(pos.x);
            sb.AppendLine();

            sb.Append("Pos Y: ");
            sb.Append(pos.y);
            sb.AppendLine();

            sb.Append("Pos Z: ");
            sb.Append(pos.z);
            sb.AppendLine();

            Vector3 rot = transform.eulerAngles;

            sb.Append("Rot X: ");
            sb.Append(rot.x);
            sb.AppendLine();

            sb.Append("Rot Y: ");
            sb.Append(rot.y);
            sb.AppendLine();

            sb.Append("Rot Z: ");
            sb.Append(rot.z);
            sb.AppendLine();

            InfoText.text = sb.ToString();
        }
    }

    public void SetVisibilityTo(bool value)
    {
        Axes.SetActive(value);
        CapsuleRenderer.enabled = value;
        InfoText.enabled = value;
        CapsuleCollider.enabled = value;
    }
}
