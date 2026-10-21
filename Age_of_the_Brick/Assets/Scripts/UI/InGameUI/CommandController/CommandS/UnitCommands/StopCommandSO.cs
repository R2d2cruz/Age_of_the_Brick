using UnityEngine;

[CreateAssetMenu(fileName = "NewStopCommand", menuName = "AgeOfTheBrick/Commands/Actions/Stop")]
public class StopCommandSO : CommandSO
{
    [Header("UI Details")]
    public string commandName = "Stop";
    public Sprite icon;
    [TextArea] public string description = "Cancels the unit's current order.";

    /// <summary>
    /// Displays the Stop command only if the unit is actively performing an action (not Idle).
    /// </summary>
    public override bool ShouldDisplayAction(Selectable context)
    {
        if (context is Unit unit)
        {
            // Only show button if the unit is NOT in Idle state
            return unit.BehaviourState != UnitBehaviourState.Idle;
        }

        return base.ShouldDisplayAction(context);
    }

    public override CommandAction CreateRuntimeAction(Selectable context)
    {
        return new CommandAction
        {
            id = "cmd_stop",
            displayName = commandName,
            description = description,
            icon = icon,
            buttonType = defaultButtonType,
            gridIndex = defaultGridIndex,
            hotkey = defaultHotkey,
            isInteractable = () => context != null && context.state == SelectableState.Alive,
            actionCallback = () =>
            {
                if (context is Unit unit)
                {
                    unit.Stop();
                }
            }
        };
    }
}