// GridCellVisual.cs
using UnityEngine;

// This script will be attached to your actual grid cell GameObjects/Prefabs in the scene.
// It acts as the visual and editable representation of a grid cell.
public class GridCellVisual : MonoBehaviour
{
    // These properties define the cell's base state, editable in the Inspector
    [Header("Cell Properties")]
    [Tooltip("Is this cell walkable by units?")]
    [SerializeField] private bool _isWalkable = true;

    [Tooltip("The determined grid coordinates of this cell.")]
    [ReadOnlyInspector] // Custom attribute to make it read-only in Inspector
    [SerializeField] private Vector3Int _gridCoordinates;

    // Public properties to allow GridManager to read the cell's data
    public Vector3Int GridCoordinates => _gridCoordinates;
    public bool IsWalkable => _isWalkable;
    public float WorldHeight => transform.position.y; // WorldHeight is simply the Y-position of the GameObject

    // Reference to Unity's Grid component, will be set by GridManager or manually if needed
    // This allows the visual cell to determine its own grid coordinates in Editor/Runtime
    private UnityEngine.Grid _unityGridComponent;

    // Call this in Awake or Start to initialize coordinates
    // and also in OnValidate for editor-time updates
    private void OnEnable()
    {
        // Find the main Unity Grid in the scene, usually a parent of the GridManager
        // Or set it manually in the Inspector if your hierarchy is different
        if (_unityGridComponent == null)
        {
            _unityGridComponent = FindFirstObjectByType<UnityEngine.Grid>();
            if (_unityGridComponent == null)
            {
                Debug.LogWarning($"GridCellVisual on {gameObject.name}: No UnityEngine.Grid found in scene. Grid coordinates might not be set correctly. Please ensure a UnityEngine.Grid component exists in your scene.");
            }
        }

        // Always update coordinates when enabled or instantiated
        UpdateGridCoordinates();
    }

    // Called when the script is loaded or a value is changed in the Inspector.
    // Useful for updating coordinates in the Editor as you move the prefab.
    private void OnValidate()
    {
        // Only attempt to find Grid if it's null, otherwise it can be slow in OnValidate
        if (_unityGridComponent == null)
        {
            _unityGridComponent = FindFirstObjectByType<UnityEngine.Grid>();
        }
        UpdateGridCoordinates();
    }

    // Calculates and sets the grid coordinates based on the GameObject's world position
    private void UpdateGridCoordinates()
    {
        if (_unityGridComponent != null)
        {
            // Use Unity's Grid to convert this GameObject's world position to grid coordinates
            _gridCoordinates = _unityGridComponent.WorldToCell(transform.position);
            // Optionally, snap the visual to the center of the cell for perfect alignment
            // transform.position = _unityGridComponent.GetCellCenterWorld(_gridCoordinates);
        }
    }

    // --- Editor-only Gizmos for Visualization ---
    private void OnDrawGizmos()
    {
        if (_unityGridComponent == null) return; // Need the grid to draw correctly

        // Get the world center of this cell using the stored grid coordinates
        Vector3 worldCenter = _unityGridComponent.GetCellCenterWorld(_gridCoordinates);

        // Adjust Y slightly for visibility above ground if needed
        worldCenter.y = WorldHeight + 0.05f;

        // Set Gizmo color based on walkability
        Gizmos.color = _isWalkable ? Color.green : Color.red;

        // Draw a wire cube representing the cell
        // Adjust size based on your actual unityGridComponent.cellSize
        // Using a slight offset so it doesn't clip with other geometry
        float cellSize = _unityGridComponent.cellSize.x; // Assuming uniform cell size
        Gizmos.DrawWireCube(worldCenter, new Vector3(cellSize * 0.9f, 0.1f, cellSize * 0.9f));

        // Draw a small sphere at the cell center to highlight its exact position
        Gizmos.DrawSphere(worldCenter, cellSize * 0.05f);
    }
}

// Custom ReadOnly attribute for Inspector fields
// (You can put this in a separate file named ReadOnlyInspectorAttribute.cs or directly in GridCellVisual.cs)
public class ReadOnlyInspectorAttribute : PropertyAttribute { }

#if UNITY_EDITOR
// Custom PropertyDrawer for the ReadOnly attribute
// This makes fields marked with [ReadOnlyInspector] uneditable in the Inspector
[UnityEditor.CustomPropertyDrawer(typeof(ReadOnlyInspectorAttribute))]
public class ReadOnlyPropertyDrawer : UnityEditor.PropertyDrawer
{
    public override float GetPropertyHeight(UnityEditor.SerializedProperty property, GUIContent label)
    {
        return UnityEditor.EditorGUI.GetPropertyHeight(property, label, true);
    }

    public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
    {
        GUI.enabled = false; // Disable GUI input
        UnityEditor.EditorGUI.PropertyField(position, property, label, true);
        GUI.enabled = true; // Re-enable GUI input
    }
}
#endif