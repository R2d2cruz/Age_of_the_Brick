using UnityEngine;

/// <summary>
/// Command that sets the stance (Aggressive/Defensive/HoldPosition/Passive) on all selected owned units.
/// </summary>
[CreateAssetMenu(fileName = "NewSetStanceCommand", menuName = "AgeOfTheBrick/Commands/Actions/Set Stance")]
public class SetStanceCommandSO : CommandSO
{
    [Header("UI Details")]
    public string commandName = "Stance";
    public Sprite icon;
    [TextArea] public string description;
    public UnitStance stanceToApply;

    /// <summary>Shown only for units that have a <see cref="StanceComponent"/>.</summary>
    public override bool ShouldDisplayAction(Selectable context)
    {
        return context is Unit u && u.GetComponent<StanceComponent>() != null;
    }

    public override CommandAction CreateRuntimeAction(Selectable context)
    {
        return new CommandAction
        {
            id = $"cmd_stance_{stanceToApply}",
            displayName = commandName,
            description = description,
            icon = icon,
            buttonType = defaultButtonType,
            gridIndex = defaultGridIndex,
            hotkey = defaultHotkey,
            isInteractable = () => context != null && context.state == SelectableState.Alive,
            actionCallback = () =>
            {
                int localId = PlayerManager.Instance != null
                    ? PlayerManager.Instance.GetLocalPlayerId()
                    : 0;
                bool applied = false;

                if (SelectionHandler.Instance != null)
                {
                    foreach (Selectable sel in SelectionHandler.Instance.SelectedObjects)
                    {
                        if (!sel.IsOwnedBy(localId)) continue;
                        if (sel is not Unit unit) continue;
                        StanceComponent stance = unit.GetComponent<StanceComponent>();
                        if (stance == null) continue;
                        stance.SetStance(stanceToApply);
                        applied = true;
                    }
                }

                if (!applied && context is Unit ctxUnit)
                    ctxUnit.GetComponent<StanceComponent>()?.SetStance(stanceToApply);
            }
        };
    }
}