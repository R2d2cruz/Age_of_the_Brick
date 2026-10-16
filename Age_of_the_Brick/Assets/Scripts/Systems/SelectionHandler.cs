using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class SelectionHandler : MonoBehaviour
{
    public static SelectionHandler Instance { get; private set; }

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

    // 🔔 Event for UI update notification
    public event Action<List<Selectable>> OnSelectionChanged;

    private Camera mainCamera;
    private Canvas parentCanvas;
    [SerializeField] private int localPlayerId = 1; // 🔹 el jugador actual

    private Player localPlayer;

    private void Awake()
    {
        // Singleton pattern setup
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

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
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnLocalPlayerChanged += HandleLocalPlayerChanged;

            Player currentLocal = PlayerManager.Instance.GetLocalPlayer();
            if (currentLocal != null)
            {
                HandleLocalPlayerChanged(currentLocal);
            }
        }
    }

    private void OnDestroy()
    {
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnLocalPlayerChanged -= HandleLocalPlayerChanged;
        }
    }

    private void HandleLocalPlayerChanged(Player newLocalPlayer)
    {
        localPlayer = newLocalPlayer;
        localPlayerId = newLocalPlayer.playerId;
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
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            if (debugLogs) Debug.Log("Click sobre UI, ignorado.");
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(clickPos);

        // 1) Raycast using selectableMask
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, selectableMask.value, QueryTriggerInteraction.Collide))
        {
            Selectable selectable = hit.collider.GetComponentInParent<Selectable>();
            if (selectable != null)
            {
                if (debugLogs) Debug.Log($"Raycast (mask) hit: {hit.collider.name} -> seleccionando {selectable.name}");
                ClearSelection();
                AddToSelection(selectable);
                NotifySelectionChanged();
                return;
            }
        }

        // 2) Fallback RaycastAll
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
            NotifySelectionChanged();
            return;
        }

        // 3) Ground click: Clear selection
        if (debugLogs) Debug.Log("Click en vacío -> ClearSelection()");
        ClearSelection();
        NotifySelectionChanged();
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
        {
            AddToSelection(unit);
        }

        NotifySelectionChanged();
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

    public void NotifySelectionChanged()
    {
        OnSelectionChanged?.Invoke(SelectedObjects);
    }
}