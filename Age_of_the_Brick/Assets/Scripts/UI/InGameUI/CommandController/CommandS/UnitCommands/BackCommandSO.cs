using UnityEngine;

[CreateAssetMenu(fileName = "NewBackCommand", menuName = "AgeOfTheBrick/Commands/Navigation/Back")]
public class BackCommandSO : CommandSO
{
    [Header("Navigation Details")]
    public string commandName = "Back";
    public Sprite backArrowIcon;
    [TextArea] public string commandDescription = "Returns to the main command panel.";

    public override CommandAction CreateRuntimeAction(Selectable context)
    {
        return new CommandAction
        {
            id = "cmd_back",
            displayName = commandName,
            description = commandDescription,
            icon = backArrowIcon,
            buttonType = EActionButtonType.Navigation, // Uses rectangular/arrow texture style
            gridIndex = defaultGridIndex,            // e.g., slot 11 or 15
            hotkey = defaultHotkey,                  // e.g., KeyCode.Escape
            isInteractable = () => context != null && context.state == SelectableState.Alive,
            actionCallback = () =>
            {
                if (context is ISubMenuHandler menuHandler)
                {
                    menuHandler.ReturnToMainMenu();
                }
            }
        };
    }
}