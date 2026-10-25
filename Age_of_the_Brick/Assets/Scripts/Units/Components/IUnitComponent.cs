/// <summary>
/// Base contract for all unit components.
/// Unit components provide specialized functionality such as gathering,
/// attacking, building, healing, or researching while exposing actions
/// to the command system.
/// </summary>
public interface IUnitComponent : IActionProvider
{
    /// <summary>
    /// Initializes the component and assigns its owning unit.
    /// Called when the component is attached or created.
    /// </summary>
    /// <param name="coreUnit">
    /// Unit that owns this component.
    /// </param>
    void Initialize(Unit coreUnit);

    /// <summary>
    /// Called when the owning player advances to a new era.
    /// Allows the component to update statistics, abilities,
    /// or visuals related to era progression.
    /// </summary>
    /// <param name="newEra">
    /// New era index.
    /// </param>
    void OnEraChanged(int newEra);

    /// <summary>
    /// Called when new unit needs old component information
    /// </summary>
    /// <param name="unitComponent">
    /// Component that triggered the change.
    /// </param>
    void OnUnitChange(IUnitComponent unitComponent);
}