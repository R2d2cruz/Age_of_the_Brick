using UnityEngine;
using UnityEngine.InputSystem; // 👈 Necesario para el nuevo Input System

public class CommandHandler : MonoBehaviour
{
    [SerializeField] private SelectionHandler selectionHandler;
    [SerializeField] private LayerMask groundMask;  // capa del suelo
    [SerializeField] private LayerMask targetMask;  // capa de enemigos, recursos, etc.
    [SerializeField] private int localPlayerId = 1; // 🔹 el jugador actual

    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        // 👇 Nueva forma de detectar click derecho
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            HandleCommand();
        }
    }

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
                            if (target.IsEnemyTo(localPlayerId))
                            {
                                unit.Attack(target.GetComponent<Unit>()); // 🔹 orden de ataque
                            }
                            else
                            {
                                // 🔹 Aquí luego se puede expandir a recolectar, reparar, etc.
                                //unit.InteractWith(target);
                            }
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
