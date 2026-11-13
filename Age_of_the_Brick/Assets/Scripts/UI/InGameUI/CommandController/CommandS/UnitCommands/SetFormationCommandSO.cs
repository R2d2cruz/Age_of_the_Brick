using UnityEngine;

/// <summary>
/// Command that applies a formation layout to all formation-eligible units in the current selection.
/// </summary>
[CreateAssetMenu(fileName = "NewSetFormationCommand", menuName = "AgeOfTheBrick/Commands/Actions/Set Formation")]
public class SetFormationCommandSO : CommandSO
{
    [Header("UI Details")]
    public string commandName = "Formation";
    public Sprite icon;
    [TextArea] public string description;
    public FormationType formationToApply;

    /// <summary>Shown only for units that can join formations (non-workers).</summary>
    public override bool ShouldDisplayAction(Selectable context)
    {
        return context is Unit u && FormationManager.IsFormationEligible(u);
    }

    public override CommandAction CreateRuntimeAction(Selectable context)
    {
        return new CommandAction
        {
            id = $"cmd_formation_{formationToApply}",
            displayName = commandName,
            description = description,
            icon = icon,
            buttonType = defaultButtonType,
            gridIndex = defaultGridIndex,
            hotkey = defaultHotkey,
            isInteractable = () => context != null && context.state == SelectableState.Alive,
            actionCallback = () =>
            {
                if (FormationManager.Instance == null) return;
                if (SelectionHandler.Instance != null)
                {
                    FormationManager.Instance.ApplyFormation(
                        SelectionHandler.Instance.SelectedObjects,
                        formationToApply);
                    return;
                }

                if (context is Unit)
                {
                    var list = new System.Collections.Generic.List<Selectable> { context };
                    FormationManager.Instance.ApplyFormation(list, formationToApply);
                }
            }
        };
    }
}
