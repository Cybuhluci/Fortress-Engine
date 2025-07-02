using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    [SerializeField] private Camera SceneCamera;

    private Vector3 LastPosition;

    [SerializeField] private LayerMask PlacementLayermask;

    public Vector3 GetSelectedMapPosition()
    {
        if (Mouse.current == null)
        {
            Debug.LogWarning("No mouse device found. Cannot get selected map position.");
            return LastPosition;
        }

        Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();

        Ray ray = SceneCamera.ScreenPointToRay(mouseScreenPosition);

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100, PlacementLayermask))
        {
            LastPosition = hit.point;
        }

        return LastPosition;
    }
}