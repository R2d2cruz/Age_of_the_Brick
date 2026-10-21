using UnityEngine;

/// <summary>
/// Base class for all command definitions.
/// Command assets define how actions are displayed and executed
/// for a given selectable entity.
/// </summary>
public abstract class CommandSO : ScriptableObject
{
    [Header("Base Grid Placement")]

    /// <summary>
    /// Default position of the command within the command panel grid.
    /// </summary>
    [Tooltip("Default position of the command in the command panel grid.")]
    public int defaultGridIndex;

    /// <summary>
    /// Default keyboard shortcut assigned to this command.
    /// </summary>
    [Tooltip("Default keyboard shortcut assigned to this command.")]
    public KeyCode defaultHotkey;

    /// <summary>
    /// Default visual category of the command button.
    /// Determines the button style displayed in the UI.
    /// </summary>
    [Tooltip("Default visual style used by the command button.")]
    public EActionButtonType defaultButtonType = EActionButtonType.Standard;

    /// <summary>
    /// Determines whether this command should be displayed
    /// in the command panel for the specified context.
    /// </summary>
    /// <param name="context">
    /// The currently selected entity requesting available commands.
    /// </param>
    /// <returns>
    /// True if the command should be visible; otherwise, false.
    /// </returns>
    public virtual bool ShouldDisplayAction(Selectable context)
    {
        return true; // Visible by default for most commands.
    }

    /// <summary>
    /// Creates a runtime command instance bound to the specified entity.
    /// Runtime commands contain the dynamic state and callbacks required
    /// for execution during gameplay.
    /// </summary>
    /// <param name="context">
    /// The entity for which the command is being generated.
    /// </param>
    /// <returns>
    /// A fully configured runtime command action.
    /// </returns>
    public abstract CommandAction CreateRuntimeAction(Selectable context);
}