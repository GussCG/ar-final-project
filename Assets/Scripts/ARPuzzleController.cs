using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ARPuzzleController : MonoBehaviour
{
    [Header("UI Slots (Must be RAW IMAGE)")]
    public List<RawImage> uiSlots;

    [Header("Data (Drag your 3D Prefabs here)")]
    [Tooltip("The Prefab Name will be the ID. The Prefab visual will be the UI Icon.")]
    public List<GameObject> prefabDeck;

    [Header("AR Configuration")]
    public float distanceFromCamera = 0.4f;
    public LayerMask mapLayerMask;

    // --- Internal State ---
    private GameObject[] _slotContents; // Stores the prefab reference for each slot
    private int _currentDraggingSlotIndex = -1;
    private GameObject _currentFloatingObject;
    private Camera _arCamera;
    private bool _isDragging = false;

    // --- Snapshot Settings ---
    private int snapshotSize = 256;
    private Vector3 snapshotPos = new Vector3(0, -500, 0); // Far away from AR scene

    void Start()
    {
        _arCamera = Camera.main;
        _slotContents = new GameObject[uiSlots.Count];

        // 1. Setup Input Listeners
        for (int i = 0; i < uiSlots.Count; i++)
        {
            int index = i;
            AddEventTrigger(uiSlots[i], index);
        }

        // 2. Fill Slots
        FillSlots();
    }

    void Update()
    {
        if (_isDragging && _currentFloatingObject != null)
        {
            HandleDragging();
        }
    }

    // --- 1. INVENTORY SYSTEM ---

    private void FillSlots()
    {
        for (int i = 0; i < uiSlots.Count; i++)
        {
            // If slot is empty and we have prefabs left in the deck
            if (_slotContents[i] == null && prefabDeck.Count > 0)
            {
                // Pull next prefab
                GameObject nextPrefab = prefabDeck[0];
                prefabDeck.RemoveAt(0);

                // Assign Logic
                _slotContents[i] = nextPrefab;

                // GENERATE UI ICON AUTOMATICALLY
                Texture2D preview = GenerateRuntimePreview(nextPrefab);
                uiSlots[i].texture = preview;
                uiSlots[i].enabled = true;

                // Optional: Fix Aspect Ratio? RawImage works differently, 
                // but usually square snapshots look best.
            }
            else if (_slotContents[i] == null)
            {
                uiSlots[i].enabled = false;
            }
        }
    }

    // --- 2. RUNTIME SNAPSHOT GENERATOR ---
    // This function creates a mini studio, takes a photo of the prefab, and returns the texture.
    private Texture2D GenerateRuntimePreview(GameObject prefab)
    {
        // A. Setup "Studio"
        GameObject studioRoot = new GameObject("PreviewStudio");
        studioRoot.transform.position = snapshotPos;

        // B. Spawn Object
        GameObject subject = Instantiate(prefab, studioRoot.transform);
        subject.transform.localPosition = Vector3.zero;
        subject.transform.localRotation = Quaternion.identity; // Adjust if your prefabs are rotated

        // Ensure subject is on a layer the camera can see (Default is usually fine)
        // Strip Colliders so they don't mess with raycasts during the split second they exist
        foreach (var c in subject.GetComponentsInChildren<Collider>()) c.enabled = false;

        // C. Setup Camera
        GameObject camObj = new GameObject("PreviewCamera");
        camObj.transform.parent = studioRoot.transform;
        Camera snapCam = camObj.AddComponent<Camera>();
        snapCam.clearFlags = CameraClearFlags.Color;
        snapCam.backgroundColor = new Color(0, 0, 0, 0); // Transparent background
        snapCam.cullingMask = ~0; // Render everything (in this isolated spot)

        // D. Setup Lighting (Crucial for 3D models)
        GameObject lightObj = new GameObject("PreviewLight");
        lightObj.transform.parent = studioRoot.transform;
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.0f;
        lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);

        // E. Auto-Fit Camera to Object Bounds
        Bounds bounds = new Bounds(subject.transform.position, Vector3.zero);
        foreach (Renderer r in subject.GetComponentsInChildren<Renderer>())
            bounds.Encapsulate(r.bounds);

        float maxDim = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        float dist = maxDim / (2.0f * Mathf.Tan(0.5f * snapCam.fieldOfView * Mathf.Deg2Rad));

        // Position camera looking at center of bounds
        snapCam.transform.position = bounds.center + new Vector3(0, 0, -dist * 1.5f); // 1.5x buffer
        snapCam.transform.LookAt(bounds.center);

        // F. Render
        RenderTexture rt = RenderTexture.GetTemporary(snapshotSize, snapshotSize, 16);
        snapCam.targetTexture = rt;
        snapCam.Render();

        // G. Save to Texture2D
        RenderTexture.active = rt;
        Texture2D result = new Texture2D(snapshotSize, snapshotSize, TextureFormat.RGBA32, false);
        result.ReadPixels(new Rect(0, 0, snapshotSize, snapshotSize), 0, 0);
        result.Apply();

        // H. Cleanup
        RenderTexture.active = null;
        snapCam.targetTexture = null;
        RenderTexture.ReleaseTemporary(rt);
        Destroy(studioRoot); // Destroys camera, light, and subject

        return result;
    }

    // --- 3. INTERACTION LOGIC (Drag & Drop) ---

    private void AddEventTrigger(RawImage slotImage, int slotIndex)
    {
        EventTrigger trigger = slotImage.gameObject.GetComponent<EventTrigger>();
        if (trigger == null) trigger = slotImage.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerDown;
        entry.callback.AddListener((data) => { OnSlotTouched(slotIndex); });
        trigger.triggers.Add(entry);
    }

    public void OnSlotTouched(int slotIndex)
    {
        if (_isDragging) return;
        if (_slotContents[slotIndex] == null) return;

        _isDragging = true;
        _currentDraggingSlotIndex = slotIndex;
        GameObject prefabRef = _slotContents[slotIndex];

        // Hide UI
        uiSlots[slotIndex].enabled = false;

        // Spawn Floating Object
        Vector3 spawnPos = GetWorldPositionFromInput();
        _currentFloatingObject = Instantiate(prefabRef, spawnPos, Quaternion.identity); // Use Prefab directly!

        // Strip Colliders from the cursor so rays pass through
        foreach (var c in _currentFloatingObject.GetComponentsInChildren<Collider>()) c.enabled = false;

        // Optional: Rotate to face camera?
        // _currentFloatingObject.transform.LookAt(_arCamera.transform);
    }

    private void HandleDragging()
    {
        Vector3 targetPos = GetWorldPositionFromInput();
        _currentFloatingObject.transform.position = Vector3.Lerp(_currentFloatingObject.transform.position, targetPos, Time.deltaTime * 15f);

        if ((Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended) || Input.GetMouseButtonUp(0))
        {
            DropPiece();
        }
    }

    private void DropPiece()
    {
        _isDragging = false;
        bool success = false;
        string heldID = "";

        if (_currentDraggingSlotIndex != -1 && _slotContents[_currentDraggingSlotIndex] != null)
        {
            // ID IS THE PREFAB NAME
            heldID = _slotContents[_currentDraggingSlotIndex].name;
        }

        if (_currentFloatingObject != null)
        {
            Vector3 rayDir = (_currentFloatingObject.transform.position - _arCamera.transform.position).normalized;
            Ray aimRay = new Ray(_arCamera.transform.position, rayDir);

            if (Physics.Raycast(aimRay, out RaycastHit hit, 20f, mapLayerMask))
            {
                PieceSlot slot = hit.collider.GetComponent<PieceSlot>();
                if (slot != null)
                {
                    success = slot.CheckMap(heldID);
                }
            }
            Destroy(_currentFloatingObject);
        }

        if (success)
        {
            Debug.Log("Success!");
            _slotContents[_currentDraggingSlotIndex] = null;
            FillSlots(); // Auto-refill from deck
        }
        else
        {
            Debug.Log("Failed.");
            uiSlots[_currentDraggingSlotIndex].enabled = true; // Show UI again
        }

        _currentDraggingSlotIndex = -1;
    }

    private Vector3 GetWorldPositionFromInput()
    {
        Vector2 screenPos = Input.mousePosition;
        if (Input.touchCount > 0) screenPos = Input.GetTouch(0).position;
        return _arCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, distanceFromCamera));
    }
}