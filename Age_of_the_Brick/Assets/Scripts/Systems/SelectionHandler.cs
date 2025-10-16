using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class SelectionHandler : MonoBehaviour
{
    [Header("Selección con Click")]
    [SerializeField] private LayerMask selectableMask;

    [Header("Selección con Drag Box")]
    [SerializeField] private RectTransform selectionBoxUI;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private RTSInputActions inputActions;
    private Vector2 startPos;
    private Vector2 endPos;

    public List<Selectable> SelectedObjects { get; private set; } = new List<Selectable>();

    private Camera mainCamera;
    private Canvas parentCanvas;
    [SerializeField] private int localPlayerId = 1; // 🔹 el jugador actual

    private Player localPlayer;

    private void Awake()
    {
        mainCamera = Camera.main;
        inputActions = new RTSInputActions();

        if (selectionBoxUI != null)
        {
            parentCanvas = selectionBoxUI.GetComponentInParent<Canvas>();
            selectionBoxUI.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        localPlayer = PlayerManager.Instance.GetPlayer(localPlayerId);
    }

    public void SetLocalPlayer(int newPlayerId)
    {
        localPlayerId = newPlayerId;
        localPlayer = PlayerManager.Instance.GetPlayer(localPlayerId);
    }


    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.Gameplay.LeftClick.started += ctx => OnLeftClickDown();
        inputActions.Gameplay.LeftClick.canceled += ctx => OnLeftClickUp();
    }

    private void OnDisable()
    {
        inputActions.Gameplay.LeftClick.started -= ctx => OnLeftClickDown();
        inputActions.Gameplay.LeftClick.canceled -= ctx => OnLeftClickUp();
        inputActions.Disable();
    }

    private void OnLeftClickDown()
    {
        startPos = inputActions.Gameplay.MousePos.ReadValue<Vector2>();
        if (selectionBoxUI != null) selectionBoxUI.gameObject.SetActive(true);
    }

    private void OnLeftClickUp()
    {
        endPos = inputActions.Gameplay.MousePos.ReadValue<Vector2>();
        if (selectionBoxUI != null) selectionBoxUI.gameObject.SetActive(false);

        if (Vector2.Distance(endPos, startPos) < 10f)
        {
            HandleSingleClick(endPos);
        }
        else
        {
            HandleDragSelection();
        }
    }

    private void HandleSingleClick(Vector2 clickPos)
    {
        // Si el cursor está sobre UI, ignoramos la selección del mundo.
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            if (debugLogs) Debug.Log("Click sobre UI, ignorado.");
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(clickPos);

        // 1) Intentamos raycast usando la selectableMask (lo correcto si configuraste capas)
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, selectableMask.value, QueryTriggerInteraction.Collide))
        {
            Selectable selectable = hit.collider.GetComponentInParent<Selectable>();
            if (selectable != null)
            {
                if (debugLogs) Debug.Log($"Raycast (mask) hit: {hit.collider.name} -> seleccionando {selectable.name}");
                ClearSelection();
                AddToSelection(selectable);
                return;
            }
        }

        // 2) Fallback robusto: raycast all en todas las capas y buscar el Selectable más cercano
        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, ~0, QueryTriggerInteraction.Collide);
        float closestDist = Mathf.Infinity;
        Selectable closestSelectable = null;

        foreach (var h in hits)
        {
            var s = h.collider.GetComponentInParent<Selectable>();
            if (s != null && h.distance < closestDist)
            {
                closestDist = h.distance;
                closestSelectable = s;
            }
        }

        if (closestSelectable != null)
        {
            if (debugLogs) Debug.Log($"RaycastAll found selectable: {closestSelectable.name} (dist {closestDist})");
            ClearSelection();
            AddToSelection(closestSelectable);
            return;
        }

        // 3) Si no hay nada: deseleccionar (click en terreno vacío)
        if (debugLogs) Debug.Log("Click en vacío -> ClearSelection()");
        ClearSelection();
    }

    private void HandleDragSelection()
    {
        if (localPlayer == null)
            return;

        Vector2 min = Vector2.Min(startPos, endPos);
        Vector2 max = Vector2.Max(startPos, endPos);

        ClearSelection();

        List<Unit> units = localPlayer.Registry.GetUnitsInScreenRect(mainCamera, min, max);

        foreach (var unit in units)
            AddToSelection(unit);
    }


    private void Update()
    {
        if (localPlayer != null)
            localPlayer.Update(Time.deltaTime);

        if (inputActions.Gameplay.LeftClick.IsPressed())
        {
            endPos = inputActions.Gameplay.MousePos.ReadValue<Vector2>();
            UpdateSelectionBox();
        }
    }

    private void UpdateSelectionBox()
    {
        if (selectionBoxUI == null || parentCanvas == null) return;

        Vector2 localStart, localEnd;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas.transform as RectTransform, startPos,
            parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera,
            out localStart);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas.transform as RectTransform, endPos,
            parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera,
            out localEnd);

        Vector2 min = Vector2.Min(localStart, localEnd);
        Vector2 max = Vector2.Max(localStart, localEnd);
        Vector2 size = max - min;

        // suponiendo pivot (0,0) -> esquina inferior izquierda
        selectionBoxUI.anchoredPosition = min;
        selectionBoxUI.sizeDelta = size;
    }

    private void AddToSelection(Selectable selectable)
    {
        if (!SelectedObjects.Contains(selectable))
        {
            SelectedObjects.Add(selectable);
            selectable.Select();
        }
    }

    private void ClearSelection()
    {
        foreach (var obj in SelectedObjects)
        {
            obj.Deselect();
        }
        SelectedObjects.Clear();
    }
}
