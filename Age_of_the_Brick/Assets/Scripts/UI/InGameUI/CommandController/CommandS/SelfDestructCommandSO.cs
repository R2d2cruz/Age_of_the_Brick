using UnityEngine;

[CreateAssetMenu(fileName = "NewSelfDestructCommand", menuName = "AgeOfTheBrick/Commands/Actions/Self Destruct")]
public class SelfDestructCommandSO : CommandSO
{
    [Header("UI Details")]
    public string commandName = "Delete Unit";
    public Sprite icon;
    [TextArea] public string description = "Destroys this unit.";

    public override CommandAction CreateRuntimeAction(Selectable context)
    {
        return new CommandAction
        {
            id = "cmd_self_destruct",
            displayName = commandName,
            description = description,
            icon = icon,
            buttonType = EActionButtonType.Warning, // Red/Warning style tile
            gridIndex = defaultGridIndex,
            hotkey = defaultHotkey,
            isInteractable = () => context != null && context.state == SelectableState.Alive,
            actionCallback = () =>
            {
                if (context is Unit unit)
                {
                    unit.ExecuteSelfDestruct();
                }
            }
        };
    }
}