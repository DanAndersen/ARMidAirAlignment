using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.WSA;

public class UIToggler : MonoBehaviour
{
    public GameObject[] GameObjectsToDisable;

    private bool _uiVisible = true;

    public int offsetX = 300;
    public int offsetY = 0;

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10 + offsetX, 40 + offsetY, 215, 9999));

        if (GUILayout.Button("Toggle UI"))
        {
            ToggleUI();
        }

        GUILayout.EndArea();
    }

    public void ToggleUI()
    {
        _uiVisible = !_uiVisible;

        Debug.Log("Toggling UI to " + _uiVisible);

        if (_uiVisible)
        {
            ShowUI();
        }
        else
        {
            HideUI();
        }
    }

    public void ToggleWorldAnchorUpdates()
    {
        AnchorMarker[] anchorMarkers = FindObjectsOfType<AnchorMarker>();
        foreach (var anchorMarker in anchorMarkers)
        {
            if (anchorMarker != null)
            {
                if (anchorMarker.IsAttached)
                {
                    anchorMarker.DetachAnchor();
                } else
                {
                    anchorMarker.AttachAnchor();
                }
            }
        }
    }

    private void ShowUI()
    {
        SetGameObjectsActiveTo(true);
        SetWorldAnchorVisibilityTo(true);
    }

    private void HideUI()
    {
        SetGameObjectsActiveTo(false);
        SetWorldAnchorVisibilityTo(false);
    }

    private void SetWorldAnchorVisibilityTo(bool value)
    {
        AnchorMarker[] anchorMarkers = FindObjectsOfType<AnchorMarker>();
        foreach (var anchorMarker in anchorMarkers)
        {
            if (anchorMarker != null)
            {
                anchorMarker.SetVisibilityTo(value);
            }
        }
    }

    private void SetGameObjectsActiveTo(bool value)
    {
        foreach (GameObject go in GameObjectsToDisable)
        {
            if (go != null)
            {
                go.SetActive(value);
            }
        }
    }
}
