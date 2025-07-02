// GridCellData.cs
using UnityEngine; // Needed for Vector3Int

// This class represents the *data* for a single grid cell.
// It is a plain C# class, NOT a MonoBehaviour.
// It will be instantiated and managed by the GridManager.
[System.Serializable] // Make it serializable so it can be viewed in the Inspector if needed for debugging
public class GridCellData
{
    public Vector3Int Coordinates { get; private set; }
    public bool IsWalkable { get; set; }
    public bool IsOccupied { get; set; } // True if a unit is currently on this cell
    public float WorldHeight { get; set; } // The actual Y-coordinate of the ground at this cell's center

    /// <summary>
    /// Constructor for a GridCellData object.
    /// </summary>
    /// <param name="coords">The grid coordinates (e.g., Vector3Int(0,0,0) or Vector3Int(-5,0,3)).</param>
    /// <param name="isWalkable">Whether units can move onto this cell.</param>
    /// <param name="worldHeight">The Y-coordinate of the cell in world space.</param>
    public GridCellData(Vector3Int coords, bool isWalkable, float worldHeight)
    {
        Coordinates = coords;
        IsWalkable = isWalkable;
        WorldHeight = worldHeight;
        IsOccupied = false; // Cells start unoccupied by default
    }
}