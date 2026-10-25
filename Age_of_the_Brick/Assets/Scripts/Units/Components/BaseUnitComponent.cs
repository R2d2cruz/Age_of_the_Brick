using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base implementation for unit components.
/// Provides common functionality shared by specialized components
/// such as gathering, combat, construction, healing, or research.
/// </summary>
public abstract class BaseUnitComponent : MonoBehaviour, IUnitComponent
{
    /// <summary>
    /// Reference to the unit that owns this component.
    /// Accessible by derived classes.
    /// </summary>
    protected Unit unit;

    /// <summary>
    /// Identifier of the player that owns this component.
    /// </summary>
    public int OwnerPlayerId => unit.ownerPlayerId;

    /// <summary>
    /// Initializes the component and assigns its owning unit.
    /// </summary>
    /// <param name="coreUnit">
    /// Unit that owns this component.
    /// </param>
    public virtual void Initialize(Unit coreUnit)
    {
        unit = coreUnit;
    }

    /// <summary>
    /// Returns the command actions provided by this component.
    /// Override in derived classes to expose gameplay actions
    /// to the command panel.
    /// </summary>
    /// <returns>
    /// List of available command actions.
    /// </returns>
    public virtual List<CommandAction> GetAvailableActions()
    {
        return new List<CommandAction>();
    }

    /// <summary>
    /// Called when the owning player advances to a new era.
    /// Allows derived components to update statistics,
    /// abilities, or visual behavior.
    /// </summary>
    /// <param name="newEra">
    /// New era index.
    /// </param>
    public virtual void OnEraChanged(int newEra)
    {
    }

    /// <summary>
    /// Called when another unit component changes its state.
    /// Override this method to react to changes in related components.
    /// </summary>
    /// <param name="unitComponent">
    /// Component that triggered the notification.
    /// </param>
    public virtual void OnUnitChange(IUnitComponent unitComponent)
    {
    }
}