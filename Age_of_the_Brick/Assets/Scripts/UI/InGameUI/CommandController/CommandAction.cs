using System;
using UnityEngine;

/// <summary>
/// Represents an action that can be displayed and executed from the command panel.
/// Each command defines its visual representation, input bindings,
/// availability conditions, and execution behavior.
/// </summary>
[System.Serializable]
public struct CommandAction
{
    /// <summary>
    /// Unique identifier of the command.
    /// Examples: "cmd_attack", "build_house", "train_villager".
    /// </summary>
    [Tooltip("Unique identifier of the command.")]
    public string id;

    /// <summary>
    /// Display name shown in the user interface and tooltips.
    /// </summary>
    [Tooltip("Display name shown in the user interface.")]
    public string displayName;

    /// <summary>
    /// Short description displayed in the tooltip.
    /// Can include costs, requirements, or gameplay information.
    /// </summary>
    [Tooltip("Description displayed in the command tooltip.")]
    public string description;

    /// <summary>
    /// Visual icon displayed on the action button.
    /// </summary>
    [Tooltip("Visual icon displayed on the command button.")]
    public Sprite icon;

    /// <summary>
    /// Defines the visual behavior and category of the button.
    /// </summary>
    [Tooltip("Type of button associated with this command.")]
    public EActionButtonType buttonType;

    /// <summary>
    /// Position of the command inside the HUD command grid.
    /// For example, a 3x4 grid uses indexes from 0 to 11.
    /// </summary>
    [Tooltip("Position of the command in the HUD grid.")]
    public int gridIndex;

    /// <summary>
    /// Keyboard shortcut assigned to this command.
    /// </summary>
    [Tooltip("Keyboard shortcut used to activate the command.")]
    public KeyCode hotkey;

    /// <summary>
    /// Determines whether the command can currently be executed.
    /// Returning false will usually disable or gray out the button.
    /// </summary>
    [Tooltip("Returns whether the command is currently available.")]
    public Func<bool> isInteractable;

    /// <summary>
    /// Callback executed when the player activates the command.
    /// Contains the gameplay logic associated with the action.
    /// </summary>
    [Tooltip("Action executed when the command is activated.")]
    public Action actionCallback;
}