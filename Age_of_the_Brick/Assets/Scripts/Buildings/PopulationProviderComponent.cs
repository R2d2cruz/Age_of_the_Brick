using UnityEngine;

[RequireComponent(typeof(Building))]
public class PopulationProviderComponent : MonoBehaviour
{
    private Building building;
    private bool isApplied;
    private int appliedAmount;
    private Player appliedTo;

    private void Awake()
    {
        building = GetComponent<Building>();
    }

    private void OnEnable()
    {
        building ??= GetComponent<Building>();

        if (building == null)
            return;

        building.OnConstructionFinished += OnActivated;
        if (building.IsFullyBuilt)
            TryApplyCapacity();
    }

    private void OnDisable()
    {
        if (building == null)
            building.OnConstructionFinished -= OnActivated;
    }

    private void OnDestroy()
    {
        if (building != null)
            building.OnConstructionFinished -= OnActivated;
        TryRemoveCapacity();
    }

    private void OnActivated()
    {
        TryApplyCapacity();
    }

    private void TryApplyCapacity()
    {
        TryApplyCapacityTo(building != null ? building.Owner : null);
    }

    private void TryApplyCapacityTo(Player target)
    {
        if (isApplied)
            return;
        if (building == null || !building.IsFullyBuilt)
            return;
        if (target == null)
            return;
        if (building.stats == null || building.stats.populationProvided <= 0)
            return;

        int amount = building.stats.populationProvided;
        target.AddPopulationCapacity(amount);
        isApplied = true;
        appliedAmount = amount;
        appliedTo = target;
    }

    private void TryRemoveCapacity()
    {
        if (!isApplied)
            return;

        appliedTo?.RemovePopulationCapacity(appliedAmount);

        isApplied = false;
        appliedAmount = 0;
        appliedTo = null;
    }

    /// <summary>
    /// Moves applied population capacity between owners. Not called automatically yet;
    /// intended for a future Building ownership-transfer path.
    /// </summary>
    public void OnOwnerChanged(Player previousOwner, Player newOwner)
    {
        if (previousOwner == newOwner && isApplied && appliedTo == newOwner)
            return;

        TryRemoveCapacity();

        if (newOwner == null)
            return;
        if (building == null || !building.IsFullyBuilt)
            return;
        if (building.stats == null || building.stats.populationProvided <= 0)
            return;

        TryApplyCapacityTo(newOwner);
    }
}
