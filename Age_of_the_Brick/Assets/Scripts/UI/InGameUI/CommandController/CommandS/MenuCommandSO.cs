using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewMenuCommand", menuName = "AgeOfTheBrick/Commands/Menu Command")]
public class MenuCommandSO : CommandSO
{
    [Header("Menu Setup")]
    public string menuName;
    public Sprite menuIcon;
    public List<CommandSO> subCommands = new List<CommandSO>();

    public override CommandAction CreateRuntimeAction(Selectable context)
    {
        return new CommandAction
        {
            id = $"menu_{menuName.ToLower()}",
            displayName = menuName,
            description = $"Opens {menuName} menu.",
            icon = menuIcon,
            buttonType = EActionButtonType.Secondary,
            gridIndex = defaultGridIndex,
            hotkey = defaultHotkey,
            isInteractable = () => context != null && context.state == SelectableState.Alive,
            actionCallback = () =>
            {
                // Tells the entity or UI to open this specific sub-list of commands
                if (context is ISubMenuHandler menuHandler)
                {
                    menuHandler.OpenSubMenu(subCommands);
                }
            }
        };
    }
}

public interface ISubMenuHandler
{
    void OpenSubMenu(List<CommandSO> subMenuCommands);
    void ReturnToMainMenu();
}