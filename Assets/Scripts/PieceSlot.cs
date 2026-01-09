// StateSlot.cs
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PieceSlot : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Auto-filled from GameObject name if left empty")]
    private string stateID;

    [SerializeField] private Material darkMaterial;

    private Material originalMaterial;
    private Renderer meshRenderer;
    private bool isSolved = false;

    // 1. EDITOR HELPER: Runs when you add the script or right-click -> Reset
    // This lets you see the ID in the inspector before the game starts.
    private void Reset()
    {
        stateID = CleanName(gameObject.name);
    }

    void Awake()
    {
        // 2. RUNTIME FAILSAFE: If you forgot to check the inspector, this grabs the name now.
        if (string.IsNullOrEmpty(stateID))
        {
            stateID = CleanName(gameObject.name);
        }

        meshRenderer = GetComponent<Renderer>();
        if (meshRenderer != null)
        {
            originalMaterial = meshRenderer.material;
            meshRenderer.material = darkMaterial;
        }
    }

    public bool CheckMap(string incomingPieceID)
    {
        // If it's already solved, we reject any new attempts
        if (isSolved) return false;

        // Compare the cleaned IDs
        if (incomingPieceID == stateID)
        {
            HandleSuccess();
            return true; // <--- Return SUCCESS
        }
        else
        {
            HandleFailure();
            return false; // <--- Return FAILURE
        }
    }

    private void HandleSuccess()
    {
        isSolved = true;
        if (meshRenderer != null) meshRenderer.material = originalMaterial;
        GameManager.Instance.IncreasePointCount();
        GetComponent<Collider>().enabled = false;
    }

    private void HandleFailure()
    {
        GameManager.Instance.DecreasePointCount();
    }

    // Helper to remove spaces or unwanted chars if your naming convention varies
    private string CleanName(string rawName)
    {
        return rawName.Trim();
    }
}