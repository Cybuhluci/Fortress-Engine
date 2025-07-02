// Unit.cs
using UnityEngine;

public class Unit : MonoBehaviour
{
    // === Existing User Properties ===
    public Vector3Int GridCoordinates { get; private set; }
    public string UnitName = "Scout";
    public bool IsSelected { get; private set; }
    public int MoveRange = 5; // Added MoveRange, set to a default value

    [SerializeField] private GameObject selectionHighlight;
    [SerializeField] private float verticalOffset = 0.05f; // NEW: Small offset to prevent clipping

    // === NEW/MODIFIED Properties for Grid Interaction ===
    private GridManager _gridManager; // Reference to the GridManager

    // --- MonoBehaviour Lifecycle ---
    private void Awake()
    {
        // Find the GridManager early. This ensures it's available when Start() runs.
        _gridManager = FindFirstObjectByType<GridManager>();
        if (_gridManager == null)
        {
            Debug.LogError("Unit: GridManager not found in scene! Unit cannot register its position or move.");
            enabled = false; // Disable unit script if no GridManager to prevent further errors
            return; // Exit Awake if GridManager is missing
        }

        if (selectionHighlight != null)
        {
            selectionHighlight.SetActive(false); // Ensure highlight is off at start
        }
    }

    private void Start()
    {
        // This is crucial: Register the unit's initial position with the GridManager.
        // It's called in Start() because GridManager's own Awake() (where it builds the grid)
        // should have completed by this time, ensuring the grid data is ready.
        RegisterUnitOnGrid();
    }

    // === NEW/MODIFIED Grid Interaction Methods ===

    /// <summary>
    /// Registers the unit's current world position with the GridManager
    /// and marks its corresponding grid cell as occupied. This method is typically called once at Start.
    /// </summary>
    public void RegisterUnitOnGrid()
    {
        if (_gridManager == null)
        {
            Debug.LogError("Unit: Cannot register on grid, GridManager is null. Check Awake() for errors.");
            return;
        }

        // Convert the unit's current world position to grid coordinates using the GridManager
        GridCoordinates = _gridManager.WorldToGridCoordinates(transform.position);

        // Inform the GridManager that this cell is now occupied by this unit
        _gridManager.SetCellOccupied(GridCoordinates, true);

        // Optional but Recommended: Snap the unit's actual world position to the center of its grid cell.
        // This ensures perfect visual alignment and consistency with grid logic,
        // reducing potential floating-point issues when converting between world and grid coords.
        transform.position = _gridManager.GetWorldCoordinatesFromGrid(GridCoordinates);
        // Apply your existing vertical offset after snapping to cell center
        transform.position = new Vector3(transform.position.x, transform.position.y + verticalOffset, transform.position.z);


        Debug.Log($"Unit '{UnitName}' registered at grid coordinates: {GridCoordinates}");
    }


    /// <summary>
    /// Moves the unit to a new grid cell, handling grid occupation updates in the GridManager.
    /// This should be the primary method used for actual in-game movement of the unit.
    /// </summary>
    /// <param name="newCoords">The target grid coordinates for the unit.</param>
    public void MoveToCell(Vector3Int newCoords)
    {
        if (_gridManager == null)
        {
            Debug.LogError("Unit: Cannot move, GridManager is null. Check Awake() for errors.");
            return;
        }

        // 1. Unoccupy the unit's current (old) cell in the GridManager
        _gridManager.SetCellOccupied(GridCoordinates, false);

        // 2. Update the unit's internal grid coordinates to the new position
        GridCoordinates = newCoords;

        // 3. Occupy the new cell in the GridManager
        _gridManager.SetCellOccupied(GridCoordinates, true);

        // 4. Update the unit's world position to the center of the new grid cell
        transform.position = _gridManager.GetWorldCoordinatesFromGrid(newCoords);
        // Apply your existing vertical offset after moving
        transform.position = new Vector3(transform.position.x, transform.position.y + verticalOffset, transform.position.z);

        Debug.Log($"Unit '{UnitName}' moved to grid coordinates: {newCoords}");
    }

    // === Existing User Methods (kept as-is) ===

    // This method sets the unit's *visual* position and its internal GridCoordinates.
    // However, it does NOT interact with the GridManager's 'IsOccupied' status.
    // For actual gameplay movement that affects grid logic, `MoveToCell` is recommended.
    // Use this `SetPosition` carefully, perhaps only for initial manual placement in editor,
    // as `RegisterUnitOnGrid` in Start() will overwrite its coordinate if not handled.
    public void SetPosition(Vector3Int newGridPos, Vector3 worldPos)
    {
        GridCoordinates = newGridPos;
        transform.position = new Vector3(worldPos.x, worldPos.y + verticalOffset, worldPos.z);
        Debug.Log($"Unit '{UnitName}' manually set visual position to: {newGridPos}");
    }

    public void Select()
    {
        IsSelected = true;
        if (selectionHighlight != null)
        {
            selectionHighlight.SetActive(true);
        }
        Debug.Log($"{UnitName} selected!");
    }

    public void Deselect()
    {
        IsSelected = false;
        if (selectionHighlight != null)
        {
            selectionHighlight.SetActive(false);
        }
        Debug.Log($"{UnitName} deselected.");
    }
}