/// <summary>
/// Represents the current high-level behavior of a unit.
/// Used for AI decision-making, animation control,
/// debugging, and user interface feedback.
/// </summary>
public enum UnitBehaviourState
{
    /// <summary>
    /// The unit is not performing any action.
    /// </summary>
    Idle,

    /// <summary>
    /// The unit is moving toward a destination.
    /// </summary>
    Moving,

    /// <summary>
    /// The unit is actively attacking a target.
    /// </summary>
    Attacking,

    /// <summary>
    /// The unit is performing a productive task such as
    /// gathering resources, constructing buildings,
    /// repairing structures, or other work-related actions.
    /// </summary>
    Working
}



/// <summary>
/// Base class for all unit states.
/// Defines the common lifecycle methods used by the unit state machine.
/// </summary>
public abstract class UnitState
{
    /// <summary>
    /// Unit that owns this state.
    /// </summary>
    protected Unit unit;

    /// <summary>
    /// Creates a new state for the specified unit.
    /// </summary>
    /// <param name="u">
    /// Unit that will use this state.
    /// </param>
    public UnitState(Unit u)
    {
        unit = u;
    }

    /// <summary>
    /// Called when the state becomes active.
    /// Used to initialize state-specific behavior.
    /// </summary>
    public virtual void Enter() { }

    /// <summary>
    /// Called when the state is exited.
    /// Used to clean up state-specific data.
    /// </summary>
    public virtual void Exit() { }

    /// <summary>
    /// Called every frame while the state is active.
    /// Contains the main state logic.
    /// </summary>
    public virtual void Tick() { }

    /// <summary>
    /// Returns the high-level behavior category represented by this state.
    /// Used by UI, AI, and debugging systems.
    /// </summary>
    /// <returns>
    /// The behavior type associated with this state.
    /// </returns>
    public virtual UnitBehaviourState GetBehaviourState()
    {
        return UnitBehaviourState.Idle;
    }
}