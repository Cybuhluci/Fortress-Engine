// GridManager.cs
using UnityEngine;
using System.Collections.Generic; // Needed for Dictionary and Queue

public class GridManager : MonoBehaviour
{
    // === Inspector-assignable fields ===
    [Header("Core Grid Dependencies")]
    [Tooltip("Reference to Unity's built-in Grid component. Crucial for converting between world positions and grid coordinates.")]
    [SerializeField] private UnityEngine.Grid unityGridComponent;

    [Tooltip("Cell size in world units. Should match your UnityEngine.Grid's Cell Size for accurate drawing and conversions.")]
    [SerializeField] private float cellSize = 1f; // Used for Gizmos, should align with Unity Grid's cell size

    // === Internal grid data ===
    // We now use a Dictionary for dynamic, sparse grids. Keys are Vector3Int coordinates, values are GridCellData objects.
    private Dictionary<Vector3Int, GridCellData> gridCells;

    // === Directions for BFS/A* (these remain the same) ===
    private static readonly Vector3Int[] cardinalDirections = new Vector3Int[]
    {
        new Vector3Int(1, 0, 0),  // Right
        new Vector3Int(-1, 0, 0), // Left
        new Vector3Int(0, 0, 1),  // Forward (Z-axis in 3D world)
        new Vector3Int(0, 0, -1)  // Backward (Z-axis in 3D world)
    };

    private static readonly Vector3Int[] diagonalDirections = new Vector3Int[]
    {
        new Vector3Int(1, 0, 1),   // Right-Forward
        new Vector3Int(1, 0, -1),  // Right-Backward
        new Vector3Int(-1, 0, 1),  // Left-Forward
        new Vector3Int(-1, 0, -1)  // Left-Backward
    };

    // --- MonoBehaviour Lifecycle ---
    private void Awake()
    {
        // Ensure the Unity Grid Component is assigned
        if (unityGridComponent == null)
        {
            Debug.LogWarning("GridManager: Unity Grid Component not assigned. Attempting to find one in parents. Please assign it in the Inspector for proper functionality.");
            unityGridComponent = GetComponentInParent<UnityEngine.Grid>();
            if (unityGridComponent == null)
            {
                Debug.LogError("GridManager: No UnityEngine.Grid Component found on this GameObject or its parents. Grid functionality will be severely limited!");
                // If no grid is found, disable this component to prevent errors
                enabled = false;
                return;
            }
        }

        // Initialize the grid by scanning the scene for placed GridCellVisuals
        InitializeGrid();
    }

    // --- Grid Initialization ---
    /// <summary>
    /// Initializes the grid by finding all GridCellVisual components placed in the scene
    /// and populating the internal dictionary with their data.
    /// </summary>
    private void InitializeGrid()
    {
        gridCells = new Dictionary<Vector3Int, GridCellData>();

        // Find all active GridCellVisual GameObjects in the scene
        // This is why it's important to place GridCellVisuals manually or through a level builder
        GridCellVisual[] foundGridCellVisuals = FindObjectsByType<GridCellVisual>(FindObjectsSortMode.None);

        foreach (GridCellVisual cellVisual in foundGridCellVisuals)
        {
            // Create a GridCellData instance using the data from the visual component
            GridCellData newCellData = new GridCellData(
                cellVisual.GridCoordinates,
                cellVisual.IsWalkable,
                cellVisual.WorldHeight
            );

            // Add the new cell data to our dictionary.
            // Check for duplicates to prevent errors if two visuals are at the same grid coordinate.
            if (!gridCells.ContainsKey(newCellData.Coordinates))
            {
                gridCells.Add(newCellData.Coordinates, newCellData);
            }
            else
            {
                Debug.LogWarning($"GridManager: Duplicate GridCellVisual found at coordinates {newCellData.Coordinates} (GameObject: {cellVisual.name}). Only the first one found will be used for this coordinate. Please ensure unique cell placements.");
            }
        }
        Debug.Log($"GridManager: Initialized {gridCells.Count} dynamic cells from GridCellVisuals placed in the scene.");
    }

    // --- Public Grid Access Methods ---
    /// <summary>
    /// Gets the GridCellData object for the given world-aligned grid coordinates.
    /// Returns null if no cell (GridCellVisual prefab) exists at these coordinates.
    /// </summary>
    public GridCellData GetGridCell(Vector3Int coordinates)
    {
        if (gridCells.TryGetValue(coordinates, out GridCellData cell))
        {
            return cell;
        }
        return null; // Cell does not exist at these coordinates in the custom grid
    }

    /// <summary>
    /// Converts world-aligned grid coordinates to a world space center position.
    /// Relies on the assigned Unity Grid component for accuracy.
    /// </summary>
    public Vector3 GetWorldCoordinatesFromGrid(Vector3Int gridCoords)
    {
        if (unityGridComponent != null)
        {
            return unityGridComponent.GetCellCenterWorld(gridCoords);
        }
        // Fallback or error if unityGridComponent is not assigned
        Debug.LogError("GridManager: GetWorldCoordinatesFromGrid called without unityGridComponent assigned! Returning approximate position.");
        return new Vector3(gridCoords.x * cellSize + cellSize / 2f, 0, gridCoords.z * cellSize + cellSize / 2f);
    }

    /// <summary>
    /// Converts a world position to world-aligned grid coordinates.
    /// Relies on the assigned Unity Grid component for accuracy.
    /// </summary>
    public Vector3Int WorldToGridCoordinates(Vector3 worldPos)
    {
        if (unityGridComponent != null)
        {
            return unityGridComponent.WorldToCell(worldPos);
        }
        // Fallback or error if unityGridComponent is not assigned
        Debug.LogError("GridManager: WorldToGridCoordinates called without unityGridComponent assigned! Returning approximate coordinates.");
        return new Vector3Int(Mathf.FloorToInt(worldPos.x / cellSize), 0, Mathf.FloorToInt(worldPos.z / cellSize));
    }

    /// <summary>
    /// Sets the occupied status of a specific grid cell.
    /// </summary>
    /// <param name="coordinates">The grid coordinates of the cell to update.</param>
    /// <param name="occupied">True if the cell is now occupied, false otherwise.</param>
    public void SetCellOccupied(Vector3Int coordinates, bool occupied)
    {
        GridCellData cell = GetGridCell(coordinates); // Use the new GetGridCell
        if (cell != null)
        {
            cell.IsOccupied = occupied;
            // Debug.Log($"Cell {coordinates} occupied status set to: {occupied}"); // Optional debug
        }
        else
        {
            Debug.LogWarning($"Attempted to set occupied status for non-existent cell {coordinates}. Please ensure the cell visual prefab is placed.");
        }
    }

    // --- Pathfinding / Reachable Cells (BFS) ---
    /// <summary>
    /// Calculates all reachable grid cells from a starting position within a given move range,
    /// considering cardinal (cost 1) and diagonal (cost 2) movement.
    /// </summary>
    /// <param name="startPos">The starting grid coordinates of the unit.</param>
    /// <param name="moveRange">The maximum movement cost the unit has.</param>
    /// <param name="unitToIgnore">The unit whose movement range is being calculated (its own occupied cell should not block). Can be null.</param>
    /// <returns>A dictionary where keys are reachable grid coordinates and values are their minimum movement cost.</returns>
    public Dictionary<Vector3Int, int> GetReachableCells(Vector3Int startPos, int moveRange, Unit unitToIgnore)
    {
        Dictionary<Vector3Int, int> minCostToReach = new Dictionary<Vector3Int, int>();
        Queue<KeyValuePair<Vector3Int, int>> queue = new Queue<KeyValuePair<Vector3Int, int>>();

        // Ensure the starting cell exists and is walkable
        GridCellData startCell = GetGridCell(startPos);
        if (startCell == null || !startCell.IsWalkable)
        {
            Debug.LogWarning($"GetReachableCells: Starting cell ({startPos}) is not walkable or does not exist in the grid. Cannot calculate reachable cells.");
            return minCostToReach; // Return empty dictionary
        }

        minCostToReach[startPos] = 0;
        queue.Enqueue(new KeyValuePair<Vector3Int, int>(startPos, 0));

        while (queue.Count > 0)
        {
            KeyValuePair<Vector3Int, int> current = queue.Dequeue();
            Vector3Int currentCoords = current.Key;

            // Explore Cardinal Directions
            foreach (Vector3Int dir in cardinalDirections)
            {
                ProcessNeighbor(currentCoords, dir, 1, moveRange, unitToIgnore, minCostToReach, queue);
            }

            // Explore Diagonal Directions
            foreach (Vector3Int dir in diagonalDirections)
            {
                ProcessNeighbor(currentCoords, dir, 1, moveRange, unitToIgnore, minCostToReach, queue);
            }
        }
        return minCostToReach;
    }

    // Helper method for GetReachableCells to avoid code duplication
    private void ProcessNeighbor(Vector3Int currentCoords, Vector3Int neighborDir, int costToAdd, int moveRange, Unit unitToIgnore,
                                 Dictionary<Vector3Int, int> minCostToReach, Queue<KeyValuePair<Vector3Int, int>> queue)
    {
        Vector3Int neighborCoords = currentCoords + neighborDir;
        int newCost = minCostToReach[currentCoords] + costToAdd; // Cost to reach this neighbor

        // Try to get the GridCellData for the neighbor. If it's null, the cell doesn't exist in our custom grid.
        GridCellData neighborCell = GetGridCell(neighborCoords);

        // If neighborCell is null, it means there is no GridCellVisual prefab placed at these coordinates.
        // This implicitly acts as an "out of bounds" or "unwalkable" check for your custom grid shape.
        if (neighborCell == null)
        {
            // Debug.Log($"Neighbor ({neighborCoords}) does not exist in the grid (no visual prefab placed)."); // Optional debug
            return;
        }

        // Check if the neighbor cell is walkable and not occupied by another unit.
        // The 'unitToIgnore' parameter allows the unit whose path is being calculated to "pass through" its own occupied cell.
        bool isOccupiedByOtherUnit = neighborCell.IsOccupied && (unitToIgnore == null || neighborCell.Coordinates != unitToIgnore.GridCoordinates);

        if (!neighborCell.IsWalkable || isOccupiedByOtherUnit)
        {
            // Debug.Log($"Neighbor ({neighborCoords}): Not walkable or occupied by other unit."); // Optional debug
            return; // Cannot move to this cell
        }

        // If a shorter path to this neighbor has not been found yet, or the current path is cheaper
        if (newCost <= moveRange && (!minCostToReach.ContainsKey(neighborCoords) || newCost < minCostToReach[neighborCoords]))
        {
            minCostToReach[neighborCoords] = newCost;
            queue.Enqueue(new KeyValuePair<Vector3Int, int>(neighborCoords, newCost));
            // Debug.Log($"Adding neighbor ({neighborCoords}) to queue with cost {newCost}."); // Optional debug
        }
    }

    // --- Debugging / Visualization in Editor ---
    private void OnDrawGizmos()
    {
        // This method automatically gets called by Unity to draw gizmos in the editor
        // We will now draw only the cells that are part of our dynamically loaded grid
        DebugDrawGrid();
    }

    public void DebugDrawGrid()
    {
        if (gridCells == null || gridCells.Count == 0) return;

        // Iterate through all the GridCellData objects stored in our dictionary
        foreach (GridCellData cell in gridCells.Values)
        {
            Vector3 worldCenter = GetWorldCoordinatesFromGrid(cell.Coordinates);
            worldCenter.y = cell.WorldHeight + 0.05f; // Draw slightly above ground for visibility

            Color color = Color.white;
            if (!cell.IsWalkable)
            {
                color = Color.red; // Unwalkable cells are red
            }
            else if (cell.IsOccupied)
            {
                color = Color.blue; // Occupied cells are blue
            }
            else
            {
                color = Color.green; // Walkable, unoccupied cells are green
            }

            // Ensure we use the correct cell size from the Unity Grid for drawing
            float currentCellSize = unityGridComponent != null ? unityGridComponent.cellSize.x : cellSize;

            // Draw a wire square on the ground to represent the cell
            Debug.DrawLine(worldCenter + Vector3.left * currentCellSize / 2f + Vector3.back * currentCellSize / 2f,
                           worldCenter + Vector3.right * currentCellSize / 2f + Vector3.back * currentCellSize / 2f, color, 1f);
            Debug.DrawLine(worldCenter + Vector3.right * currentCellSize / 2f + Vector3.back * currentCellSize / 2f,
                           worldCenter + Vector3.right * currentCellSize / 2f + Vector3.forward * currentCellSize / 2f, color, 1f);
            Debug.DrawLine(worldCenter + Vector3.right * currentCellSize / 2f + Vector3.forward * currentCellSize / 2f,
                           worldCenter + Vector3.left * currentCellSize / 2f + Vector3.forward * currentCellSize / 2f, color, 1f);
            Debug.DrawLine(worldCenter + Vector3.left * currentCellSize / 2f + Vector3.forward * currentCellSize / 2f,
                           worldCenter + Vector3.left * currentCellSize / 2f + Vector3.back * currentCellSize / 2f, color, 1f);

            // Draw a small ray upwards to visualize the cell's center and height
            Debug.DrawRay(worldCenter, Vector3.up * 0.1f, color, 1f);
        }
    }
}