using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

/// <summary>
/// Handles unit and building selection using mouse clicks and drag-box selection.
/// Maintains the current selection state and notifies interested systems when
/// the selection changes.
/// </summary>
public class SelectionHandler : MonoBehaviour
{
    /// <summary>
    /// Singleton instance of the selection handler.
    /// </summary>
    public static SelectionHandler Instance { get; private set; }

    [Header("Click Selection")]

    /// <summary>
    /// Layer mask used to detect selectable entities.
    /// </summary>
    [SerializeField] private LayerMask selectableMask;

    [Header("Drag Selection")]

    /// <summary>
    /// UI element used to display the drag selection rectangle.
    /// </summary>
    [SerializeField] private RectTransform selectionBoxUI;

    [Header("Debug")]

    /// <summary>
    /// Enables additional debug logging.
    /// </summary>
    [SerializeField] private bool debugLogs = false;

    /// <summary>
    /// Initial mouse position when starting a selection.
    /// </summary>
    private Vector2 startPos;

    /// <summary>
    /// Current or final mouse position during selection.
    /// </summary>
    private Vector2 endPos;

    /// <summary>
    /// Collection of currently selected entities.
    /// </summary>
    public List<Selectable> SelectedObjects { get; private set; } = new List<Selectable>();

    /// <summary>
    /// Triggered whenever the selection changes.
    /// Used by UI systems and gameplay controllers.
    /// </summary>
    public event Action<List<Selectable>> OnSelectionChanged;

    /// <summary>
    /// Cached reference to the main camera.
    /// </summary>
    private Camera mainCamera;

    /// <summary>
    /// Parent canvas used to convert screen coordinates
    /// into UI coordinates for the drag box.
    /// </summary>
    private Canvas parentCanvas;

    /// <summary>
    /// Identifier of the local player.
    /// </summary>
    [SerializeField] private int localPlayerId = 1;

    /// <summary>
    /// Cached reference to the local player.
    /// </summary>
    private Player localPlayer;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        mainCamera = Camera.main;

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

    /// <summary>
    /// Updates local player references when ownership changes.
    /// </summary>
    /// <param name="newLocalPlayer">
    /// Newly assigned local player.
    /// </param>
    private void HandleLocalPlayerChanged(Player newLocalPlayer)
    {
        localPlayer = newLocalPlayer;
        localPlayerId = newLocalPlayer.playerId;
    }

    /// <summary>
    /// Subscribes to input events used for unit and building selection.
    /// </summary>
    private void OnEnable()
    {
        if (InputManager.Instance == null)
            return;

        var gameplay = InputManager.Instance.Actions.Gameplay;

        gameplay.LeftClick.started += OnLeftClickStarted;
        gameplay.LeftClick.canceled += OnLeftClickCanceled;
    }

    /// <summary>
    /// Unsubscribes from selection input events.
    /// </summary>
    private void OnDisable()
    {
        if (InputManager.Instance == null)
            return;

        var gameplay = InputManager.Instance.Actions.Gameplay;

        gameplay.LeftClick.started -= OnLeftClickStarted;
        gameplay.LeftClick.canceled -= OnLeftClickCanceled;
    }

    /// <summary>
    /// Triggered when the left mouse button is pressed.
    /// Begins a click or drag selection operation.
    /// </summary>
    /// <param name="ctx">
    /// Input callback context provided by the Unity Input System.
    /// </param>
    private void OnLeftClickStarted(InputAction.CallbackContext ctx)
    {
        OnLeftClickDown();
    }

    /// <summary>
    /// Triggered when the left mouse button is released.
    /// Completes the current click or drag selection operation.
    /// </summary>
    /// <param name="ctx">
    /// Input callback context provided by the Unity Input System.
    /// </param>
    private void OnLeftClickCanceled(InputAction.CallbackContext ctx)
    {
        OnLeftClickUp();
    }

    /// <summary>
    /// Begins a click or drag selection operation.
    /// </summary>
    private void OnLeftClickDown()
    {
        // Ignore selection while placing a building.
        if (BuildingPlacementHandler.Instance != null &&
            BuildingPlacementHandler.Instance.IsPlacing)
            return;

        startPos = InputManager.Instance.GetMousePosition();

        if (selectionBoxUI != null)
            selectionBoxUI.gameObject.SetActive(true);
    }

    /// <summary>
    /// Completes a click or drag selection operation.
    /// </summary>
    private void OnLeftClickUp()
    {
        if (BuildingPlacementHandler.Instance != null &&
            BuildingPlacementHandler.Instance.IsPlacing)
        {
            if (selectionBoxUI != null)
                selectionBoxUI.gameObject.SetActive(false);

            return;
        }

        endPos = InputManager.Instance.GetMousePosition();

        if (selectionBoxUI != null)
            selectionBoxUI.gameObject.SetActive(false);

        if (Vector2.Distance(endPos, startPos) < 10f)
        {
            HandleSingleClick(endPos);
        }
        else
        {
            HandleDragSelection();
        }
    }

    /// <summary>
    /// Processes a single-click selection.
    /// Attempts to select the closest selectable object
    /// under the mouse cursor.
    /// </summary>
    /// <param name="clickPos">
    /// Mouse position in screen coordinates.
    /// </param>
    private void HandleSingleClick(Vector2 clickPos)
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Ray ray = mainCamera.ScreenPointToRay(clickPos);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, selectableMask.value, QueryTriggerInteraction.Collide))
        {
            Selectable selectable = hit.collider.GetComponentInParent<Selectable>();
            if (selectable != null)
            {
                ClearSelection();
                AddToSelection(selectable);
                NotifySelectionChanged();
                return;
            }
        }

        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, ~0, QueryTriggerInteraction.Collide);
        float closestDist = Mathf.Infinity;
        Selectable closestSelectable = null;

        foreach (var h in hits)
        {
            var selectable = h.collider.GetComponentInParent<Selectable>();

            if (selectable != null && h.distance < closestDist)
            {
                closestDist = h.distance;
                closestSelectable = selectable;
            }
        }

        if (closestSelectable != null)
        {
            ClearSelection();
            AddToSelection(closestSelectable);
            NotifySelectionChanged();
            return;
        }

        ClearSelection();
        NotifySelectionChanged();
    }

    /// <summary>
    /// Processes drag-box selection and selects all valid units
    /// inside the screen-space rectangle.
    /// </summary>
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
        if (InputManager.Instance != null &&
            InputManager.Instance.Actions.Gameplay.LeftClick.IsPressed())
        {
            // Ignore selection box updates while placing a building.
            if (BuildingPlacementHandler.Instance != null &&
                BuildingPlacementHandler.Instance.IsPlacing)
                return;

            endPos = InputManager.Instance.GetMousePosition();
            UpdateSelectionBox();
        }
    }

    /// <summary>
    /// Updates the visual drag-selection rectangle.
    /// </summary>
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

    /// <summary>
    /// Adds a selectable entity to the current selection.
    /// </summary>
    /// <param name="selectable">
    /// Entity to be selected.
    /// </param>
    private void AddToSelection(Selectable selectable)
    {
        if (!SelectedObjects.Contains(selectable))
        {
            SelectedObjects.Add(selectable);
            selectable.Select();
        }
    }

    /// <summary>
    /// Clears the current selection and deselects all entities.
    /// </summary>
    private void ClearSelection()
    {
        foreach (var obj in SelectedObjects)
        {
            obj.Deselect();
        }
        SelectedObjects.Clear();
    }

    /// <summary>
    /// Notifies all listeners that the current selection has changed.
    /// </summary>
    public void NotifySelectionChanged()
    {
        OnSelectionChanged?.Invoke(SelectedObjects);
    }
}