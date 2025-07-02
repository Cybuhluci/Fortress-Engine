using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlacementSystem : MonoBehaviour
{
    [Header("Indicators")]
    [SerializeField] private GameObject MouseIndicator;
    [SerializeField] private GameObject CellIndicator; // For general mouse hover
    [SerializeField] private GameObject moveRangeHighlightPrefab; // NEW: Prefab for movement range highlights

    [Header("Dependencies")]
    [SerializeField] private InputManager InputManager;
    [SerializeField] private Grid grid; // Unity's built-in Grid component
    [SerializeField] private GameManager GameManager;
    [SerializeField] private GridManager gridmanager; // Used by UpdateMovementHighlights

    [Header("LayerMasks")]
    [SerializeField] private LayerMask PlacementLayermask; // This should be your ground layer
    [SerializeField] private LayerMask UnitLayermask;

    // List to keep track of active movement range highlights
    private List<GameObject> activeMoveHighlights = new List<GameObject>();
    [SerializeField] private float highlightVerticalOffset = 0.01f; // Vertical offset for highlights
    [SerializeField] private float mouseIndicatorVerticalOffset = 0.02f; // NEW: Vertical offset for mouse indicator

    private void Awake()
    {
        // Basic error checking for dependencies
        if (InputManager == null) Debug.LogError("PlacementSystem: InputManager not assigned!");
        if (grid == null) Debug.LogError("PlacementSystem: Grid not assigned!");
        if (GameManager == null) Debug.LogError("PlacementSystem: GameManager not assigned!");
        if (gridmanager == null) Debug.LogError("PlacementSystem: GridManager not assigned!"); // Added check for gridmanager
        if (moveRangeHighlightPrefab == null) Debug.LogWarning("PlacementSystem: Move Range Highlight Prefab not assigned. Movement range won't be visualized.");

        // Ensure indicators are off by default
        MouseIndicator.SetActive(false);
        CellIndicator.SetActive(false);
        ClearMovementHighlights(); // Ensure no highlights are active at start
    }

    private void Update()
    {
        // Get the current mouse ray and hit info
        Vector2 mouseScreenPosition = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        Ray ray = Camera.main.ScreenPointToRay(mouseScreenPosition);

        RaycastHit hit;
        // Use PlacementLayermask for the main raycast to detect ground
        bool hitGround = Physics.Raycast(ray, out hit, 100, PlacementLayermask);
        bool hitUnit = Physics.Raycast(ray, out _, 100, UnitLayermask); // Only check if a unit was hit, no need for hit info here

        // --- 1. Update visual mouse/cell indicators ---

        // MouseIndicator now follows the raycast hit point
        if (hitGround)
        {
            MouseIndicator.SetActive(true);
            // Place the mouse indicator slightly above the hit point to prevent clipping
            MouseIndicator.transform.position = new Vector3(hit.point.x, hit.point.y + mouseIndicatorVerticalOffset, hit.point.z);
        }
        else
        {
            // If the ray doesn't hit the ground, hide the mouse indicator
            MouseIndicator.SetActive(false);
        }


        if (hitGround && !hitUnit) // Only show cell indicator if hovering over ground AND not over a unit
        {
            CellIndicator.SetActive(true);
            Vector3 mouseWorldPosition = hit.point;
            Vector3Int gridPosition = grid.WorldToCell(mouseWorldPosition);
            CellIndicator.transform.position = grid.CellToWorld(gridPosition);
        }
        else
        {
            CellIndicator.SetActive(false); // Hide if not hovering over ground or if hovering over a unit
        }

        // --- 2. Update Movement Range Highlights ---
        UpdateMovementHighlights();

        // --- 3. Handle Mouse Clicks for Selection and Movement ---
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            HandleLeftClick(ray, hit); // Pass the ray and ground hit info
        }
    }

    private void HandleLeftClick(Ray clickRay, RaycastHit groundHitInfo)
    {
        // First, check if we clicked on a Unit
        Unit clickedUnit = GameManager.GetUnitAtMousePosition(clickRay);

        if (clickedUnit != null) // We clicked on a Unit
        {
            Debug.Log($"Clicked on Unit: {clickedUnit.UnitName}");
            GameManager.SelectUnit(clickedUnit); // Tell GameManager to select this unit
        }
        else if (GameManager.IsUnitSelected && groundHitInfo.collider != null && (PlacementLayermask & (1 << groundHitInfo.collider.gameObject.layer)) != 0) // A unit is selected AND we clicked on valid ground
        {
            Vector3Int targetGridPos = grid.WorldToCell(groundHitInfo.point);

            if (GameManager.GetCurrentReachableCells() != null && GameManager.GetCurrentReachableCells().ContainsKey(targetGridPos))
            {
                Debug.Log($"Attempting to move {GameManager.SelectedUnit.UnitName} to grid: {targetGridPos}");
                GameManager.MoveSelectedUnitTo(targetGridPos); // Let GameManager handle the move
            }
            else
            {
                Debug.Log("Clicked on valid ground, but it's not a reachable cell for the selected unit. Deselecting.");
                GameManager.DeselectUnit(); // Deselect if invalid move or simply clicked elsewhere
            }
        }
        else // No unit is selected, and we clicked on empty space (or invalid ground)
        {
            Debug.Log("Clicked on empty space, no unit selected, or invalid click. Deselecting any active unit.");
            GameManager.DeselectUnit(); // Ensure nothing is selected
        }
    }

    private void UpdateMovementHighlights()
    {
        ClearMovementHighlights();

        if (GameManager.IsUnitSelected)
        {
            Dictionary<Vector3Int, int> reachableCells = GameManager.GetCurrentReachableCells();
            if (reachableCells != null)
            {
                foreach (var cellData in reachableCells)
                {
                    Vector3Int cellCoords = cellData.Key;

                    Vector3 highlightWorldPos = gridmanager.GetWorldCoordinatesFromGrid(cellCoords);

                    GameObject highlight = Instantiate(moveRangeHighlightPrefab, highlightWorldPos, Quaternion.identity);

                    highlight.transform.position = new Vector3(highlight.transform.position.x, highlight.transform.position.y + highlightVerticalOffset, highlight.transform.position.z);

                    activeMoveHighlights.Add(highlight);
                }
            }
        }
    }

    private void ClearMovementHighlights()
    {
        foreach (GameObject highlight in activeMoveHighlights)
        {
            Destroy(highlight);
        }
        activeMoveHighlights.Clear();
    }
}