using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.SpatialAwareness;
using UnityEngine.XR.WSA.Persistence;
using System.Text;
using Newtonsoft.Json;
using System;
using Microsoft.MixedReality.Toolkit.Experimental.Utilities;

public class AnchorPlacer : MonoBehaviour, IMixedRealityPointerHandler
{
    private const string MARKERS_FILENAME = "markers";

    private const string ANCHOR_VIVE_ORIGIN = "anchor_vive_origin";
    private List<string> AllowedAnchorMarkersIds = new List<string>(new string[] { ANCHOR_VIVE_ORIGIN });
    private AnchorMarker MarkerViveOrigin = null; // the anchor

    [SerializeField]
    private GameObject ViveOrigin = null; // the game object that should be childed to the anchor when/if it is spawned


    [SerializeField]
    private GameObject WorldAnchorMarker; // the prefab
    
    private List<string> PersistedAnchorMarkersIds;
    private List<GameObject> Markers = new List<GameObject>();

    private WorldAnchorManager worldAnchorManager;

    public AnchorMarker GetMarkerViveOrigin()
    {
        return MarkerViveOrigin;
    }

    private void OnEnable()
    {
        CoreServices.InputSystem?.PushFallbackInputHandler(this.gameObject);
    }

    private void OnDisable()
    {
        CoreServices.InputSystem?.PopFallbackInputHandler();
    }

    private void Start()
    {
        if (WhichDeviceManager.Instance.IsHoloLens())
        {
            if (ViveOrigin == null)
            {
                Debug.LogError("ViveOrigin cannot be null");
            }

            worldAnchorManager = GameObject.FindObjectOfType<WorldAnchorManager>();
            if (worldAnchorManager == null)
            {
                Debug.LogError("worldAnchorManager cannot be null");
            }

            LoadAnchorMarkers();
        }
    }

    private void Update()
    {
        if (ViveOrigin != null && MarkerViveOrigin != null)
        {
            // Update the position every frame like this, so we don't have to have ViveOrigin and all its associated objects as a child of the marker.
            // We do this because if it's a child, then pressing a button will activate the hand-manipulation of the marker).
            ViveOrigin.transform.SetPositionAndRotation(MarkerViveOrigin.transform.position, MarkerViveOrigin.transform.rotation);
        }
    }

    private void LoadAnchorMarkers()
    {
        PersistedAnchorMarkersIds = ReadMarkersFromFile();
        WorldAnchorStore.GetAsync(WorldAnchorStoreLoaded);
    }

    private void WorldAnchorStoreLoaded(WorldAnchorStore store)
    {
        Debug.Log("AnchorPlacer: WorldAnchorStoreLoaded");

        string[] anchorIdsInStore = store.GetAllIds();

        List<string> persistedAnchorMarkerIdsToRemove = new List<string>();

        foreach (string persistedMarkerId in PersistedAnchorMarkersIds)
        {
            bool markerIdInAnchorStore = Array.Exists(anchorIdsInStore, id => id == persistedMarkerId);

            bool markerIdShouldBeSpawned = markerIdInAnchorStore && (persistedMarkerId == ANCHOR_VIVE_ORIGIN);
            
            if (markerIdShouldBeSpawned)
            {
                Debug.Log("Reload Marker from store with id [" + persistedMarkerId + "]");
                GameObject markerClone = (GameObject)Instantiate(WorldAnchorMarker, Vector3.zero, Quaternion.identity);
                AnchorMarker anchorMarker = markerClone.GetComponent<AnchorMarker>();
                anchorMarker.Init(persistedMarkerId, worldAnchorManager);
                anchorMarker.AttachAnchor();
                Markers.Add(anchorMarker.gameObject);

                MarkerViveOrigin = anchorMarker;
            }
            else
            {
                Debug.Log("Marker with id [" + persistedMarkerId + "] was not found in AnchorStore");

                persistedAnchorMarkerIdsToRemove.Add(persistedMarkerId);
            }
        }

        foreach (string persistedMarkerIdToRemove in persistedAnchorMarkerIdsToRemove)
        {
            Debug.Log("Removing marker with id [" + persistedMarkerIdToRemove + "]");
            PersistedAnchorMarkersIds.Remove(persistedMarkerIdToRemove);
        }

        SaveMarkersToFile();
    }

    public void OnPointerClicked(MixedRealityPointerEventData eventData)
    {
        //Debug.Log("OnPointerClicked");
    }

    public void OnPointerDown(MixedRealityPointerEventData eventData)
    {
        //Debug.Log("OnPointerDown");
    }

    public void OnPointerDragged(MixedRealityPointerEventData eventData)
    {
        //Debug.Log("OnPointerDragged");
    }

    public void OnPointerUp(MixedRealityPointerEventData eventData)
    {
        if (WhichDeviceManager.Instance.IsHoloLens())
        {
            bool shouldCreateMarkerHere = (MarkerViveOrigin == null); // only create a new marker if we don't already have a loaded marker
            string newMarkerId = ANCHOR_VIVE_ORIGIN;

            if (shouldCreateMarkerHere)
            {
                AnchorMarker newCreatedMarker = CreateNewMarker(newMarkerId);

                Debug.Log("newly created marker has id [" + newCreatedMarker.Id + "]");
                PersistedAnchorMarkersIds.Add(newCreatedMarker.Id);
                Markers.Add(newCreatedMarker.gameObject);

                MarkerViveOrigin = newCreatedMarker;

                SaveMarkersToFile();
            }
            eventData.Use();
        }
    }

    private AnchorMarker CreateNewMarker(string newMarkerId)
    {
        Vector3 hitPoint = Camera.main.transform.position + Camera.main.transform.forward;
        Vector3 directionToCamera = -Camera.main.transform.forward;
        
        directionToCamera.y = 0f;

        GameObject marker = (GameObject)Instantiate(WorldAnchorMarker, hitPoint, Quaternion.LookRotation(directionToCamera));
        AnchorMarker anchorMarker = marker.GetComponent<AnchorMarker>();
        anchorMarker.Init(newMarkerId, worldAnchorManager);

        anchorMarker.AttachAnchor();
        return anchorMarker;
    }

    private string GetMarkersPath()
    {
        string path = string.Format("{0}/{1}.json", Application.persistentDataPath, MARKERS_FILENAME);
        return path;
    }

    public List<string> ReadMarkersFromFile()
    {
        string path = GetMarkersPath();

        if (UnityEngine.Windows.File.Exists(path))
        {
            byte[] data = UnityEngine.Windows.File.ReadAllBytes(path);
            string json = Encoding.ASCII.GetString(data);
            Debug.Log("Data loaded from file " + path);
            return JsonConvert.DeserializeObject<List<string>>(json);
        } else
        {
            Debug.Log("No existing data file found, initialize empty marker list");
            return new List<string>();
        }
    }

    public void SaveMarkersToFile()
    {
        string path = GetMarkersPath();

        string json = JsonConvert.SerializeObject(PersistedAnchorMarkersIds, Formatting.Indented);
        byte[] data = Encoding.ASCII.GetBytes(json);

        UnityEngine.Windows.File.WriteAllBytes(path, data);
        Debug.Log("Data saved to file " + path);
    }

    public float AdjustPositionStepMeters = 0.01f;
    public float AdjustRotationStepDegrees = 0.1f;

    private IEnumerator AdjustMarkerPosition(Vector3 delta)
    {
        if (MarkerViveOrigin != null)
        {
            MarkerViveOrigin.DetachAnchor();

            while (!MarkerViveOrigin.IsAnchorManagerQueueEmpty())
            {
                yield return null;
            }

            MarkerViveOrigin.transform.position = MarkerViveOrigin.transform.position + delta;

            while (!MarkerViveOrigin.IsAnchorManagerQueueEmpty())
            {
                yield return null;
            }

            MarkerViveOrigin.AttachAnchor();

            while (!MarkerViveOrigin.IsAnchorManagerQueueEmpty())
            {
                yield return null;
            }
        }
        else
        {
            Debug.LogError("Cannot AdjustMarkerPosition when there is no MarkerViveOrigin");
        }
    }

    private IEnumerator AdjustMarkerRotation(Quaternion delta)
    {
        if (MarkerViveOrigin != null)
        {
            MarkerViveOrigin.DetachAnchor();

            while (!MarkerViveOrigin.IsAnchorManagerQueueEmpty())
            {
                yield return null;
            }
            
            MarkerViveOrigin.transform.rotation = MarkerViveOrigin.transform.rotation * delta;

            while (!MarkerViveOrigin.IsAnchorManagerQueueEmpty())
            {
                yield return null;
            }

            MarkerViveOrigin.AttachAnchor();

            while (!MarkerViveOrigin.IsAnchorManagerQueueEmpty())
            {
                yield return null;
            }
        }
        else
        {
            Debug.LogError("Cannot AdjustMarkerRotation when there is no MarkerViveOrigin");
        }
    }

    public void AdjustMarkerPosition_X_neg()
    {
        StartCoroutine(AdjustMarkerPosition(new Vector3(-AdjustPositionStepMeters, 0, 0)));
    }

    public void AdjustMarkerPosition_X_pos()
    {
        StartCoroutine(AdjustMarkerPosition(new Vector3(AdjustPositionStepMeters, 0, 0)));
    }

    public void AdjustMarkerPosition_Y_neg()
    {
        StartCoroutine(AdjustMarkerPosition(new Vector3(0, -AdjustPositionStepMeters, 0)));
    }

    public void AdjustMarkerPosition_Y_pos()
    {
        StartCoroutine(AdjustMarkerPosition(new Vector3(0, AdjustPositionStepMeters, 0)));
    }

    public void AdjustMarkerPosition_Z_neg()
    {
        StartCoroutine(AdjustMarkerPosition(new Vector3(0, 0, -AdjustPositionStepMeters)));
    }

    public void AdjustMarkerPosition_Z_pos()
    {
        StartCoroutine(AdjustMarkerPosition(new Vector3(0, 0, AdjustPositionStepMeters)));
    }

    public void AdjustMarkerRotation_X_neg()
    {
        StartCoroutine(AdjustMarkerRotation(Quaternion.AngleAxis(-AdjustRotationStepDegrees, Vector3.right)));
    }

    public void AdjustMarkerRotation_X_pos()
    {
        StartCoroutine(AdjustMarkerRotation(Quaternion.AngleAxis(AdjustRotationStepDegrees, Vector3.right)));
    }

    public void AdjustMarkerRotation_Y_neg()
    {
        StartCoroutine(AdjustMarkerRotation(Quaternion.AngleAxis(-AdjustRotationStepDegrees, Vector3.up)));
    }

    public void AdjustMarkerRotation_Y_pos()
    {
        StartCoroutine(AdjustMarkerRotation(Quaternion.AngleAxis(AdjustRotationStepDegrees, Vector3.up)));
    }

    public void AdjustMarkerRotation_Z_neg()
    {
        StartCoroutine(AdjustMarkerRotation(Quaternion.AngleAxis(-AdjustRotationStepDegrees, Vector3.forward)));
    }

    public void AdjustMarkerRotation_Z_pos()
    {
        StartCoroutine(AdjustMarkerRotation(Quaternion.AngleAxis(AdjustRotationStepDegrees, Vector3.forward)));
    }
}
