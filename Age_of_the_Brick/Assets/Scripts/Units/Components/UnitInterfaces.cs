/// <summary>
/// Defines the contract for units capable of gathering resources.
/// </summary>
public interface IGatherer : IUnitComponent
{
    /// <summary>
    /// Starts gathering the specified resource.
    /// </summary>
    /// <param name="resource">
    /// Resource node to gather from.
    /// </param>
    void StartGathering(Resource resource);
}

/// <summary>
/// Defines the contract for units capable of constructing buildings.
/// </summary>
public interface IBuilder : IUnitComponent
{
    /// <summary>
    /// Begins the placement process for a new building.
    /// </summary>
    /// <param name="buildingStats">
    /// Building type to be placed.
    /// </param>
    void StartPlacingBuilding(BuildingStats buildingStats);

    /// <summary>
    /// Starts constructing the specified building.
    /// </summary>
    /// <param name="building">
    /// Building to construct or continue constructing.
    /// </param>
    void StartBuilding(Building building);
}

/// <summary>
/// Defines the contract for units capable of attacking targets.
/// </summary>
public interface IAttacker : IUnitComponent
{
    /// <summary>
    /// Starts attacking the specified target.
    /// </summary>
    /// <param name="target">
    /// Target to attack.
    /// </param>
    void Attack(Selectable target);

    /// <summary>
    /// Stops the current attack action.
    /// </summary>
    void StopAttacking();
}