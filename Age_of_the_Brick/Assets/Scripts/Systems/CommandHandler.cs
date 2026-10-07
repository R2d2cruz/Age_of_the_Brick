using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles player-issued commands such as movement, attack, and interactions
/// for the currently selected units.
/// </summary>
public class CommandHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SelectionHandler selectionHandler;

    [Header("Layer Masks")]
    [SerializeField] private LayerMask groundMask;  // capa del suelo
    [SerializeField] private LayerMask targetMask;  // capa de enemigos, recursos, etc.

    [Header("Player Settings")]
    [SerializeField] private int localPlayerId = 1; // 🔹 el jugador actual

    private Camera mainCamera;

    /// <summary>
    /// Gets the main camera reference.
    /// </summary>
    private void Awake()
    {
        mainCamera = Camera.main;
    }

    /// <summary>
    /// Checks for right-click input each frame and processes commands accordingly.
    /// </summary>
    private void Update()
    {
        // 👇 Nueva forma de detectar click derecho
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            HandleCommand();
        }
    }

    /// <summary>
    /// Determines whether the player's click is on a target (enemy, resource, etc.)
    /// or on the ground, and issues the appropriate command to the selected units.
    /// </summary>
    private void HandleCommand()
    {
        // Dynamically obtain the ID of the current local player
        int activeLocalPlayerId = PlayerManager.Instance != null
            ? PlayerManager.Instance.GetLocalPlayerId()
            : localPlayerId;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = mainCamera.ScreenPointToRay(mousePos);

        // 🔹 Click on a selectable object (enemy, resource, building, etc.)
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, targetMask))
        {
            Selectable target = hit.collider.GetComponentInParent<Selectable>();
            if (target != null)
            {
                foreach (var obj in selectionHandler.SelectedObjects)
                {
                    if (obj.IsOwnedBy(activeLocalPlayerId)) // 🔹 only my units
                    {
                        Unit unit = obj.GetComponent<Unit>();
                        if (unit != null)
                        {
                            unit.ExecuteContextCommand(target);
                        }
                    }
                }
                return;
            }
        }

        // 🔹 Click on the ground → move
        if (Physics.Raycast(ray, out RaycastHit groundHit, Mathf.Infinity, groundMask))
        {
            Vector3 destination = groundHit.point;
            foreach (var obj in selectionHandler.SelectedObjects)
            {
                if (obj.IsOwnedBy(activeLocalPlayerId)) // ✅ only my units
                {
                    Unit unit = obj.GetComponent<Unit>();
                    if (unit != null)
                    {
                        unit.MoveTo(destination);
                    }
                }
            }
        }
    }
}
