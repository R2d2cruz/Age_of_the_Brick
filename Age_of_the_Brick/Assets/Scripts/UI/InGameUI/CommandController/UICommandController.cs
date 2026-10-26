using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the command panel UI and manages the command grid.
/// Automatically reacts to selection changes, player resource updates,
/// and command availability conditions.
/// </summary>
public class UICommandController : MonoBehaviour
{
    /// <summary>
    /// Singleton instance of the command controller.
    /// </summary>
    public static UICommandController Instance { get; private set; }

    [Header("UI Grid Settings")]

    /// <summary>
    /// Root container of the command panel.
    /// Used to show or hide the entire interface.
    /// </summary>
    [SerializeField] private GameObject commandPanelContainer;

    /// <summary>
    /// Collection of command buttons representing the command grid.
    /// Each button occupies a predefined slot in the interface.
    /// </summary>
    [SerializeField] private List<UIActionButton> gridButtons;

    [Header("Task Queue")]
    [SerializeField] private GameObject taskContainer;
    [SerializeField] private GameObject QueuetaskContainer;
    [SerializeField] private GridLayoutGroup taskGrid;
    [SerializeField] private TaskButton bigTaskButtonPrefab;
    [SerializeField] private TaskButton smallTaskButtonPrefab;
    [SerializeField] private int maxBigButtons = 8;
    private ProductionQueueComponent activeQueue;
    private List<TaskButton> spawnedTaskButtons = new List<TaskButton>();


    /// <summary>
    /// Current list of selected entities.
    /// </summary>
    private List<Selectable> currentSelection = new List<Selectable>();

    /// <summary>
    /// Reference to the local player.
    /// </summary>
    private Player localPlayer;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Subscribe to selection change events.
        if (SelectionHandler.Instance != null)
        {
            SelectionHandler.Instance.OnSelectionChanged += HandleSelectionChanged;
        }

        // Subscribe to local player changes and resource updates.
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnLocalPlayerChanged += HandleLocalPlayerChanged;

            Player currentLocal = PlayerManager.Instance.GetLocalPlayer();
            if (currentLocal != null)
            {
                HandleLocalPlayerChanged(currentLocal);
            }
        }

        HidePanel();
    }

    private void Update()
    {
        // Actualiza el fillAmount del primer botón (el único que progresa)
        if (activeQueue != null && spawnedTaskButtons.Count > 0)
        {
            spawnedTaskButtons[0].UpdateProgress(activeQueue.CurrentTaskProgress);
        }
    }

    private void OnDestroy()
    {
        if (SelectionHandler.Instance != null)
        {
            SelectionHandler.Instance.OnSelectionChanged -= HandleSelectionChanged;
        }

        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnLocalPlayerChanged -= HandleLocalPlayerChanged;
        }

        if (localPlayer != null)
        {
            localPlayer.OnResourceChanged -= HandleResourceChanged;
        }

        if (activeQueue != null)
        {
            activeQueue.OnQueueChanged -= RefreshTaskQueueUI;
        }
    }

    /// <summary>
    /// Updates the local player subscriptions when control changes
    /// to a different player instance.
    /// </summary>
    /// <param name="newLocalPlayer">
    /// The new local player.
    /// </param>
    private void HandleLocalPlayerChanged(Player newLocalPlayer)
    {
        if (localPlayer != null)
        {
            localPlayer.OnResourceChanged -= HandleResourceChanged;
        }

        localPlayer = newLocalPlayer;

        if (localPlayer != null)
        {
            localPlayer.OnResourceChanged += HandleResourceChanged;
        }
    }

    /// <summary>
    /// Triggered when the player's resources change.
    /// Refreshes button interactability in real time.
    /// </summary>
    private void HandleResourceChanged(ResourceType type, int amount)
    {
        if (commandPanelContainer.activeSelf)
        {
            RefreshGrid();
        }
    }

    /// <summary>
    /// Triggered whenever the current selection changes.
    /// Updates the command grid to match the selected entities.
    /// </summary>
    private void HandleSelectionChanged(List<Selectable> selectedList)
    {
        currentSelection = selectedList;

        if (currentSelection == null || currentSelection.Count == 0)
        {
            HidePanel();
            BindQueue(null); // Limpia la cola
            return;
        }

        RefreshGrid();

        // 1. Buscamos si el elemento principal tiene una cola de producción
        Selectable primary = currentSelection[0];
        ProductionQueueComponent queue = primary.GetComponent<ProductionQueueComponent>();
        BindQueue(queue);
    }

    #region Task Queue Logic

    /// <summary>
    /// Vincula o desvincula un edificio productor al panel de la interfaz.
    /// </summary>
    private void BindQueue(ProductionQueueComponent newQueue)
    {
        if (activeQueue != null)
        {
            activeQueue.OnQueueChanged -= RefreshTaskQueueUI;
        }

        activeQueue = newQueue;

        if (activeQueue != null)
        {
            activeQueue.OnQueueChanged += RefreshTaskQueueUI;
            RefreshTaskQueueUI();
            taskContainer.SetActive(true);
        }
        else
        {
            ClearTaskButtons();
            taskContainer.SetActive(false);
        }
    }

    /// <summary>
    /// Redibuja los botones de la cola basándose en el tamaño de la misma.
    /// </summary>
    private void RefreshTaskQueueUI()
    {
        ClearTaskButtons();

        if (activeQueue == null) return;

        var currentTasks = activeQueue.CurrentQueue;
        if (currentTasks.Count == 0) return;

        bool useCompactMode = currentTasks.Count > maxBigButtons;

        taskGrid.cellSize = new Vector2(taskGrid.cellSize.x, useCompactMode ? 30f : 60f);

        for (int i = 0; i < currentTasks.Count; i++)
        {
            // Determina qué prefab instanciar:
            // Si hay muchas tareas, solo el índice 0 (el primero) usa el botón grande.
            TaskButton prefabToUse = (useCompactMode && i > 0) ? smallTaskButtonPrefab : bigTaskButtonPrefab;

            TaskButton newButton;

            if (i == 0)
            {
                newButton = Instantiate(prefabToUse, taskContainer.transform);
            }
            else
            {
                newButton = Instantiate(prefabToUse, QueuetaskContainer.transform);
            }

            newButton.Setup(currentTasks[i], i, activeQueue);

            spawnedTaskButtons.Add(newButton);
        }
    }

    private void ClearTaskButtons()
    {
        foreach (var btn in spawnedTaskButtons)
        {
            if (btn != null) Destroy(btn.gameObject);
        }
        spawnedTaskButtons.Clear();
    }

    #endregion

    /// <summary>
    /// Rebuilds the command grid based on the current selection.
    /// </summary>
    public void RefreshGrid()
    {
        // Clear all command slots.
        ClearGrid();

        if (currentSelection == null || currentSelection.Count == 0)
        {
            HidePanel();
            return;
        }

        int activeLocalPlayerId = PlayerManager.Instance != null
            ? PlayerManager.Instance.GetLocalPlayerId()
            : localPlayer.playerId;

        // Obtain the primary selected entity.
        Selectable primary = currentSelection[0];

        // Verify ownership before displaying commands.
        if (!primary.IsOwnedBy(activeLocalPlayerId))
        {
            HidePanel();
            return;
        }

        ShowPanel();

        IActionProvider primaryProvider = primary.GetComponent<IActionProvider>();

        if (primaryProvider == null)
            return;

        List<CommandAction> availableActions = primaryProvider.GetAvailableActions();

        // Inject formation commands for military group selections.
        if (currentSelection.Count > 1 && IsMilitarySelection())
        {
            InjectFormationActions(availableActions);
        }

        // Assign actions to their designated grid slots.
        foreach (var action in availableActions)
        {
            if (action.gridIndex >= 0 && action.gridIndex < gridButtons.Count)
            {
                gridButtons[action.gridIndex].Setup(action);
                gridButtons[action.gridIndex].gameObject.SetActive(true);
            }
        }
    }

    /// <summary>
    /// Determines whether the current selection contains
    /// military units eligible for formation commands.
    /// </summary>
    /// <returns>
    /// True if at least one military unit is selected;
    /// otherwise, false.
    /// </returns>
    private bool IsMilitarySelection()
    {
        /* foreach (var sel in currentSelection)
        {
            if (sel is MilitaryUnit)
                return true;
        } */

        return false;
    }

    /// <summary>
    /// Adds formation-related commands to the available action list.
    /// Used for multi-selection control of military units.
    /// </summary>
    /// <param name="actions">
    /// The list receiving the formation commands.
    /// </param>
    private void InjectFormationActions(List<CommandAction> actions)
    {
        actions.Add(new CommandAction
        {
            id = "fmt_line",
            displayName = "Line Formation",
            description = "Positions units in a front line formation.",
            gridIndex = 8,
            isInteractable = () => true,
            actionCallback = () => SetGroupFormation(FormationType.Line)
        });

        actions.Add(new CommandAction
        {
            id = "fmt_box",
            displayName = "Box Formation",
            description = "Positions units in a compact defensive formation.",
            gridIndex = 9,
            isInteractable = () => true,
            actionCallback = () => SetGroupFormation(FormationType.Box)
        });
    }

    /// <summary>
    /// Applies a formation order to all selected military units.
    /// </summary>
    /// <param name="formation">
    /// The formation to apply.
    /// </param>
    private void SetGroupFormation(FormationType formation)
    {
        int localPlayerId = PlayerManager.Instance != null
            ? PlayerManager.Instance.GetLocalPlayerId()
            : 0;

        foreach (var sel in currentSelection)
        {
            /*
            if (sel.IsOwnedBy(localPlayerId) && sel is MilitaryUnit military)
            {
                military.SetFormation(formation);
            }
            */
        }
    }

    /// <summary>
    /// Clears all command slots in the grid.
    /// </summary>
    private void ClearGrid()
    {
        foreach (var btn in gridButtons)
        {
            btn.Clear();
        }
    }

    /// <summary>
    /// Displays the command panel.
    /// </summary>
    private void ShowPanel() => commandPanelContainer.SetActive(true);

    /// <summary>
    /// Hides the command panel.
    /// </summary>
    private void HidePanel() => commandPanelContainer.SetActive(false);
}

/// <summary>
/// Defines the available military formations that can be applied
/// to a group of selected units.
/// </summary>
public enum FormationType
{
    /// <summary>
    /// Units spread horizontally to maximize frontage.
    /// </summary>
    Line,

    /// <summary>
    /// Compact defensive formation.
    /// </summary>
    Box,

    /// <summary>
    /// Offensive wedge-shaped formation designed to break enemy lines.
    /// </summary>
    Wedge,

    /// <summary>
    /// Loose formation intended to reduce area damage and increase spacing.
    /// </summary>
    Scattered
}