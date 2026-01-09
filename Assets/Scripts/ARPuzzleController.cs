using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ARPuzzleController : MonoBehaviour
{
    [Header("UI Setup")]
    [Tooltip("Drag your 3 fixed UI Images here")]
    public List<Image> uiSlots;

    [Header("Puzzle Data")]
    [Tooltip("Drag all your map sprites here. The Sprite Name MUST match the Map ID.")]
    public List<Sprite> puzzleAssetDeck;

    [Header("AR Configuration")]
    public float distanceFromCamera = 0.4f;
    public LayerMask mapLayerMask;
    public GameObject floatingCursorPrefab;

    // --- Runtime State ---
    // Tracks which sprite (if any) is currently assigned to each slot index
    private Sprite[] _slotContents;
    private int _currentDraggingSlotIndex = -1; // -1 means nothing is being dragged
    private GameObject _currentFloatingObject;
    private Camera _arCamera;
    private bool _isDragging = false;

    void Start()
    {
        _arCamera = Camera.main;
        _slotContents = new Sprite[uiSlots.Count];

        // 1. Initial Setup: Bind listeners to the UI Slots
        for (int i = 0; i < uiSlots.Count; i++)
        {
            // Capture the index 'i' for the lambda closure
            int index = i;
            AddEventTrigger(uiSlots[i], index);
        }

        // 2. Fill the slots with the first available assets
        FillSlots();
    }

    void Update()
    {
        if (_isDragging && _currentFloatingObject != null)
        {
            HandleDragging();
        }
    }

    // --- INVENTORY MANAGEMENT ---

    private void FillSlots()
    {
        // Simple logic: Fill empty slots with the next available assets from the deck
        for (int i = 0; i < uiSlots.Count; i++)
        {
            // If slot is empty and we have assets left
            if (_slotContents[i] == null && puzzleAssetDeck.Count > 0)
            {
                // Pop the first asset from the deck
                Sprite nextPiece = puzzleAssetDeck[0];
                puzzleAssetDeck.RemoveAt(0);

                // Assign to logic and UI
                _slotContents[i] = nextPiece;
                uiSlots[i].sprite = nextPiece;
                uiSlots[i].enabled = true; // Turn the UI ON
                uiSlots[i].preserveAspect = true; // Optional: Keep aspect ratio
            }
            else if (_slotContents[i] == null)
            {
                // No assets left for this slot
                uiSlots[i].enabled = false; // Turn the UI OFF
            }
        }
    }

    // --- INPUT BINDING ---

    private void AddEventTrigger(Image slotImage, int slotIndex)
    {
        // Ensure the slot handles raycasts
        EventTrigger trigger = slotImage.gameObject.GetComponent<EventTrigger>();
        if (trigger == null) trigger = slotImage.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerDown;

        // When touched, call OnSlotTouched with the specific index
        entry.callback.AddListener((data) => { OnSlotTouched(slotIndex); });

        trigger.triggers.Add(entry);
    }

    // --- INTERACTION LOGIC ---

    public void OnSlotTouched(int slotIndex)
    {
        if (_isDragging) return;
        if (_slotContents[slotIndex] == null) return; // Ignore empty slots

        _isDragging = true;
        _currentDraggingSlotIndex = slotIndex;

        // 1. Visually "Pick up" the item
        uiSlots[slotIndex].enabled = false; // Turn UI OFF (Hide it)

        // 2. Spawn the 3D representation
        Vector3 spawnPos = GetWorldPositionFromInput();
        _currentFloatingObject = Instantiate(floatingCursorPrefab, spawnPos, Quaternion.identity);

        // 3. Apply the texture from the sprite we are holding
        Renderer rend = _currentFloatingObject.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material.mainTexture = _slotContents[slotIndex].texture;
        }
    }

    private void HandleDragging()
    {
        // Move floating object
        Vector3 targetPos = GetWorldPositionFromInput();
        _currentFloatingObject.transform.position = Vector3.Lerp(_currentFloatingObject.transform.position, targetPos, Time.deltaTime * 15f);
        _currentFloatingObject.transform.LookAt(_arCamera.transform);

        // Detect Release
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
            heldID = _slotContents[_currentDraggingSlotIndex].name; // ID comes from Sprite Name
        }

        if (_currentFloatingObject != null)
        {
            // Raycast Logic
            Vector3 rayDir = (_currentFloatingObject.transform.position - _arCamera.transform.position).normalized;
            Ray aimRay = new Ray(_arCamera.transform.position, rayDir);

            if (Physics.Raycast(aimRay, out RaycastHit hit, 20f, mapLayerMask))
            {
                PieceSlot slot = hit.collider.GetComponent<PieceSlot>();
                if (slot != null)
                {
                    // Check logic
                    success = slot.CheckMap(heldID);
                }
            }

            Destroy(_currentFloatingObject);
        }

        // --- OUTCOME HANDLING ---

        if (success)
        {
            Debug.Log("Success!");
            // 1. Clear the data for this slot (it is used up)
            _slotContents[_currentDraggingSlotIndex] = null;

            // 2. (Optional) Immediately try to refill this empty slot from the deck
            FillSlots();
        }
        else
        {
            Debug.Log("Failed. Returning to inventory.");
            // Return the item to the UI (Turn UI ON)
            uiSlots[_currentDraggingSlotIndex].enabled = true;
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
