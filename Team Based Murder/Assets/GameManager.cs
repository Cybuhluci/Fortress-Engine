using UnityEngine;
using System.Collections.Generic; // Required for Dictionary

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; } // Singleton pattern

    [Header("Dependencies")]
    [SerializeField] private GridManager gridManager; // Reference to your GridManager
    [SerializeField] private LayerMask UnitLayermask; // LayerMask for units

    [Header("Game State")]
    public Unit SelectedUnit { get; private set; }

    // Stores the reachable cells for the currently selected unit, along with their movement cost
    private Dictionary<Vector3Int, int> currentReachableCells;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        // Basic validation
        if (gridManager == null)
        {
            Debug.LogError("GameManager: GridManager reference not set in Inspector!");
        }
    }

    /// <summary>
    /// Selects a unit, deselecting any previously selected unit, and calculates its reachable cells.
    /// </summary>
    public void SelectUnit(Unit unit)
    {
        if (SelectedUnit != null)
        {
            SelectedUnit.Deselect(); // Deselect previously selected unit
            ClearReachableCells(); // Clear highlights for previous unit
        }

        SelectedUnit = unit;
        if (SelectedUnit != null)
        {
            SelectedUnit.Select(); // Select the new unit

            // Calculate and store reachable cells for the newly selected unit
            currentReachableCells = gridManager.GetReachableCells(
                SelectedUnit.GridCoordinates,
                // Assuming your Unit script will have a 'MoveRange' property
                // You'll need to add 'public int MoveRange = 5;' (or similar) to Unit.cs
                SelectedUnit.MoveRange,
                SelectedUnit
            );

            Debug.Log($"Calculated {currentReachableCells.Count} reachable cells for {SelectedUnit.UnitName}.");
            // At this point, PlacementSystem (or a dedicated highlighter) would query these cells
            // and display the movement range.
        }
    }

    /// <summary>
    /// Deselects the currently selected unit and clears its reachable cells.
    /// </summary>
    public void DeselectUnit()
    {
        if (SelectedUnit != null)
        {
            SelectedUnit.Deselect();
            SelectedUnit = null;
            ClearReachableCells(); // Clear highlights when unit is deselected
        }
    }

    /// <summary>
    /// Moves the currently selected unit to a target grid position if valid,
    /// and updates the grid manager's occupancy.
    /// </summary>
    /// <param name="targetGridPos">The grid position to move the unit to.</param>
    public void MoveSelectedUnitTo(Vector3Int targetGridPos)
    {
        if (!IsUnitSelected) return; // No unit selected to move

        // 1. Check if the target cell is actually reachable
        if (!currentReachableCells.ContainsKey(targetGridPos))
        {
            Debug.LogWarning($"Attempted to move {SelectedUnit.UnitName} to an unreachable cell: {targetGridPos}");
            return; // Not a valid move target
        }

        // 2. Update occupancy in GridManager (old cell becomes free, new cell becomes occupied)
        gridManager.SetCellOccupied(SelectedUnit.GridCoordinates, false); // Old position is now free
        gridManager.SetCellOccupied(targetGridPos, true);               // New position is occupied

        // 3. Update the unit's position and internal grid coordinates
        Vector3 targetWorldPos = gridManager.GetWorldCoordinatesFromGrid(targetGridPos);
        SelectedUnit.SetPosition(targetGridPos, targetWorldPos); // This teleports for now

        Debug.Log($"Moved {SelectedUnit.UnitName} from {SelectedUnit.GridCoordinates} to {targetGridPos}.");

        // After moving, you typically deselect the unit or transition to another action state
        DeselectUnit();
    }

    /// <summary>
    /// Returns the dictionary of currently reachable cells for the selected unit.
    /// Used by visualizers (like PlacementSystem) to show movement range.
    /// </summary>
    /// <returns>A dictionary of reachable grid positions and their costs, or null if no unit is selected.</returns>
    public Dictionary<Vector3Int, int> GetCurrentReachableCells()
    {
        return currentReachableCells;
    }

    // Clears the stored reachable cells
    private void ClearReachableCells()
    {
        if (currentReachableCells != null)
        {
            currentReachableCells.Clear();
        }
    }

    // Public property to check if a unit is currently selected
    public bool IsUnitSelected => SelectedUnit != null;

    // Helper method to check if a raycast hit a unit (used by PlacementSystem)
    public Unit GetUnitAtMousePosition(Ray ray)
    {
        RaycastHit hit;
        // Use a shorter distance for unit detection than for ground, if units are close to camera
        if (Physics.Raycast(ray, out hit, 100f, UnitLayermask))
        {
            return hit.collider.GetComponent<Unit>();
        }
        return null;
    }
}