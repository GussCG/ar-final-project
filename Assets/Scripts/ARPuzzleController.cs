using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class ARPuzzleController : MonoBehaviour
{
    [Header("UI Setup")]
    [Tooltip("Assign the Canvas that holds your UI here")]
    public GraphicRaycaster uiRaycaster; // 1. Need this to "see" the UI manually
    public EventSystem eventSystem;      // 2. Need this to create pointer data

    [Tooltip("Drag your 3 fixed UI Images here")]
    public List<Image> uiSlots;

    [Header("Puzzle Data")]
    public List<Sprite> puzzleAssetDeck;

    [Header("AR Configuration")]
    public float distanceFromCamera = 0.4f;
    public LayerMask mapLayerMask;
    public GameObject floatingCursorPrefab;

    [Header("Input System")]
    [Tooltip("Bind to <Pointer>/position (Value -> Vector2)")]
    public InputActionReference screenPositionInput;

    [Tooltip("Bind to <Pointer>/press (Button) or <Touch>/press")]
    public InputActionReference pressInput; // 3. Need this to detect the "Click"

    // --- Runtime State ---
    private Sprite[] _slotContents;
    private int _currentDraggingSlotIndex = -1;
    private GameObject _currentFloatingObject;
    private Camera _arCamera;
    private bool _isDragging = false;

    private void OnEnable()
    {
        if (screenPositionInput != null) screenPositionInput.action.Enable();
        if (pressInput != null) pressInput.action.Enable();
    }

    private void OnDisable()
    {
        if (screenPositionInput != null) screenPositionInput.action.Disable();
        if (pressInput != null) pressInput.action.Disable();
    }

    void Start()
    {
        _arCamera = Camera.main;
        _slotContents = new Sprite[uiSlots.Count];

        // Auto-find EventSystem if not assigned
        if (eventSystem == null) eventSystem = EventSystem.current;

        FillSlots();
    }

    void Update()
    {
        // 1. INPUT: Read values
        bool isPressed = pressInput.action.WasPressedThisFrame();
        bool isReleased = pressInput.action.WasReleasedThisFrame();

        // 2. LOGIC: Handle Pickup (Initial Touch)
        if (isPressed && !_isDragging)
        {
            TryPickupUI();
        }

        // 3. LOGIC: Handle Dragging
        if (_isDragging && _currentFloatingObject != null)
        {
            HandleDragging();

            if (isReleased)
            {
                DropPiece();
            }
        }
    }

    // --- MANUAL UI DETECTION ---

    private void TryPickupUI()
    {
        // Get screen position
        Vector2 screenPos = screenPositionInput.action.ReadValue<Vector2>();

        // Set up the Pointer Event for the UI system
        PointerEventData pointerData = new PointerEventData(eventSystem);
        pointerData.position = screenPos;

        // Shoot the ray
        List<RaycastResult> results = new List<RaycastResult>();
        uiRaycaster.Raycast(pointerData, results);

        // Check if we hit any of our slots
        foreach (RaycastResult result in results)
        {
            // Check if the hit object is one of our UI slots
            for (int i = 0; i < uiSlots.Count; i++)
            {
                if (result.gameObject == uiSlots[i].gameObject)
                {
                    // Found it! Pick it up.
                    OnSlotTouched(i);
                    return; // Stop looking
                }
            }
        }
    }

    // --- INVENTORY MANAGEMENT ---

    private void FillSlots()
    {
        for (int i = 0; i < uiSlots.Count; i++)
        {
            if (_slotContents[i] == null && puzzleAssetDeck.Count > 0)
            {
                Sprite nextPiece = puzzleAssetDeck[0];
                puzzleAssetDeck.RemoveAt(0);

                _slotContents[i] = nextPiece;
                uiSlots[i].sprite = nextPiece;
                uiSlots[i].enabled = true;
                uiSlots[i].preserveAspect = true;
            }
            else if (_slotContents[i] == null)
            {
                uiSlots[i].enabled = false;
            }
        }
    }

    // --- INTERACTION LOGIC ---

    public void OnSlotTouched(int slotIndex)
    {
        if (_isDragging) return;
        if (_slotContents[slotIndex] == null) return;

        _isDragging = true;
        _currentDraggingSlotIndex = slotIndex;

        // Visual pickup
        uiSlots[slotIndex].enabled = false;

        // Spawn 3D Object
        Vector3 spawnPos = GetWorldPositionFromInput();
        _currentFloatingObject = Instantiate(floatingCursorPrefab, spawnPos, Quaternion.identity);

        Renderer rend = _currentFloatingObject.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material.mainTexture = _slotContents[slotIndex].texture;
        }
    }

    private void HandleDragging()
    {
        Vector3 targetPos = GetWorldPositionFromInput();
        _currentFloatingObject.transform.position = Vector3.Lerp(_currentFloatingObject.transform.position, targetPos, Time.deltaTime * 15f);
        _currentFloatingObject.transform.LookAt(_arCamera.transform);
    }

    private void DropPiece()
    {
        _isDragging = false;
        bool success = false;
        string heldID = "";

        if (_currentDraggingSlotIndex != -1 && _slotContents[_currentDraggingSlotIndex] != null)
        {
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
            FillSlots();
        }
        else
        {
            Debug.Log("Failed.");
            uiSlots[_currentDraggingSlotIndex].enabled = true;
        }

        _currentDraggingSlotIndex = -1;
    }

    private Vector3 GetWorldPositionFromInput()
    {
        Vector2 screenPos = screenPositionInput.action.ReadValue<Vector2>();
        return _arCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, distanceFromCamera));
    }
}