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
        Vector2 mousePos = Mouse.current.position.ReadValue(); // 👈 Nueva forma de leer posición del mouse
        Ray ray = mainCamera.ScreenPointToRay(mousePos);

        // 🔹 Click en un objeto seleccionable (enemigo, recurso, edificio, etc.)
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, targetMask))
        {
            Selectable target = hit.collider.GetComponentInParent<Selectable>();
            if (target != null)
            {
                foreach (var obj in selectionHandler.SelectedObjects)
                {
                    if (obj.IsOwnedBy(localPlayerId)) // 🔹 solo mis unidades
                    {
                        Unit unit = obj.GetComponent<Unit>();
                        if (unit != null)
                        {
                            unit.ExecuteContextCommand(target); // Smart contextual command
                        }
                    }
                }
                return; // ✅ ya emitimos una orden, no seguimos con el suelo
            }
        }

        // 🔹 Si no, click en el terreno → mover
        if (Physics.Raycast(ray, out RaycastHit groundHit, Mathf.Infinity, groundMask))
        {
            Vector3 destination = groundHit.point;
            foreach (var obj in selectionHandler.SelectedObjects)
            {
                if (obj.IsOwnedBy(localPlayerId)) // ✅ solo mis unidades
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
