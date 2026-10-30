using UnityEngine;

[CreateAssetMenu(fileName = "NewResearchCommand", menuName = "AgeOfTheBrick/Commands/Research Command")]
public class ResearchCommandSO : CommandSO
{
    [Header("Tech Data Reference")]
    public TechSO techToResearch;

    public override bool ShouldDisplayAction(Selectable context)
    {
        if (context == null || techToResearch == null) return false;
        if (context.state != SelectableState.Alive) return false;
        if (context is Building building && !building.IsFullyBuilt) return false;
        if (!context.TryGetComponent(out TechResearcher _)) return false;

        Player owner = PlayerManager.Instance?.GetPlayer(context.ownerPlayerId);
        if (owner == null) return false;
        if (owner.HasResearched(techToResearch)) return false;
        return true;
    }

    public override CommandAction CreateRuntimeAction(Selectable context)
    {
        Player owner = PlayerManager.Instance?.GetPlayer(context.ownerPlayerId);
        string name = techToResearch != null ? techToResearch.techName : "Tech";
        Sprite icon = techToResearch != null ? techToResearch.icon : null;

        return new CommandAction
        {
            id = $"research_{name.ToLower()}",
            displayName = $"Research {name}",
            description = techToResearch != null ? techToResearch.description : string.Empty,
            icon = icon,
            buttonType = EActionButtonType.Standard,
            gridIndex = defaultGridIndex,
            hotkey = defaultHotkey,
            isInteractable = () =>
            {
                if (context == null || context.state != SelectableState.Alive) return false;
                if (owner == null || techToResearch == null) return false;
                if (owner.HasResearched(techToResearch) || owner.IsResearchPending(techToResearch))
                    return false;
                if (owner.CurrentEra < techToResearch.requiredEra) return false;
                return owner.HasResourcesForTech(techToResearch);
            },
            actionCallback = () =>
            {
                if (context.TryGetComponent(out TechResearcher researcher))
                    researcher.QueueResearch(techToResearch);
            }
        };
    }
}
