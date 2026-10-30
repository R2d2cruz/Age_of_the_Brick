using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Queues tech research on a building's production queue and exposes research commands in the UI.
/// </summary>
[RequireComponent(typeof(ProductionQueueComponent), typeof(Selectable))]
public class TechResearcher : MonoBehaviour, IActionProvider
{
    [Header("Command Asset Lists")]
    [SerializeField] private List<CommandSO> researchCommands;

    private ProductionQueueComponent queueComponent;
    private Selectable buildingSelectable;

    private void Awake()
    {
        queueComponent = GetComponent<ProductionQueueComponent>();
        buildingSelectable = GetComponent<Selectable>();
    }

    /// <summary>
    /// Starts research for the given tech if the owner can pay and the building is complete.
    /// </summary>
    public void QueueResearch(TechSO tech)
    {
        if (tech == null || buildingSelectable == null) return;

        Building building = buildingSelectable as Building;
        if (building != null && !building.IsFullyBuilt) return;

        Player owner = PlayerManager.Instance?.GetPlayer(buildingSelectable.ownerPlayerId);
        if (owner == null) return;
        if (!owner.TryBeginResearch(tech)) return;

        queueComponent.AddTask(new ResearchTask(tech, owner));
    }

    public List<CommandAction> GetAvailableActions()
    {
        List<CommandAction> runtimeActions = new List<CommandAction>();
        if (researchCommands == null) return runtimeActions;

        foreach (CommandSO cmdSO in researchCommands)
        {
            if (cmdSO != null && cmdSO.ShouldDisplayAction(buildingSelectable))
                runtimeActions.Add(cmdSO.CreateRuntimeAction(buildingSelectable));
        }
        return runtimeActions;
    }

    private class ResearchTask : IProductionTask
    {
        private readonly TechSO tech;
        private readonly Player owner;
        private bool resolved;

        public ResearchTask(TechSO tech, Player owner)
        {
            this.tech = tech;
            this.owner = owner;
        }

        public string TaskName => tech != null ? tech.techName : "Research";
        public float ProductionTime => tech != null ? tech.researchTime : 0f;
        public Sprite Icon => tech != null ? tech.icon : null;

        public void Execute()
        {
            if (resolved) return;
            resolved = true;
            owner?.CompleteResearch(tech);
        }

        public void Cancel()
        {
            if (resolved) return;
            resolved = true;
            if (owner == null || tech == null) return;
            owner.ClearResearchPending(tech);
            owner.RefundTechCosts(tech);
        }
    }
}
