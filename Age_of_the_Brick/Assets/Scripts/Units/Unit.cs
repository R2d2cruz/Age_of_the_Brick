using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

/// <summary>
/// Base class for all in-game units.
/// Inherits from <see cref="Selectable"/>, allowing player interaction (selection, commands).
/// Controls movement, combat, animations, and health UI.
/// </summary>
public class Unit : Selectable, IActionProvider, ISubMenuHandler
{
    // ============================================================
    // GENERAL CONFIGURATION
    // ============================================================

    [Header("General Configuration")]
    public UnitStats stats;                 // ScriptableObject containing per-era stats.
    public EraStats currentStats;           // Active stats depending on the current era.
    protected Animator animator;              // Animation controller.
    protected String currentAnimation = "idle";
    private NavMeshAgent agent;             // Navigation and pathfinding agent.
    private Vector3 lastMoveDestination;
    private Renderer rend;                  // Main model renderer.

    /// <summary>
    /// Current health value of the unit at runtime.
    /// </summary>
    public float currentHealth;

    // ============================================================
    // OWNER / PLAYER
    // ============================================================

    /// <summary>
    /// Player who owns this unit. Handles registration, access, and era changes.
    /// </summary>
    public Player Owner { get; private set; }

    // ============================================================
    // HEALTH UI (WORLD SPACE)
    // ============================================================

    [Header("Health UI (World)")]
    [SerializeField] private Vector3 healthCanvasOffset = new Vector3(0f, 5.4f, 0f);
    private static Camera mainCamCached; // Cached main camera reference for efficiency.

    [Header("Death Behavior")]
    public bool fightsAfterDeath = false;       // Allows post-death fighting for a short time.
    public float postDeathFightDuration = 2f;   // Duration of post-death state.

    // ============================================================
    // STATE MACHINE
    // ============================================================

    private IUnitComponent[] unitComponents;

    [Header("Unit States")]
    private UnitBehaviourState behaviourState = UnitBehaviourState.Idle;
    public UnitBehaviourState BehaviourState => behaviourState;

    private UnitState currentState; // Current state instance.
    // Cached state instances to avoid GC allocations during transitions
    protected UnitState idleState;
    protected UnitState moveState;

    [Header("Command Asset Lists")]
    [SerializeField] private List<CommandSO> mainCommands;
    [SerializeField] private CommandSO backCommandAsset; // Asset for "Back" button
    private List<CommandSO> currentActiveCommands;

    /// <summary>
    /// Returns the list of actions available for this unit based on its current menu state.
    /// </summary>
    public virtual List<CommandAction> GetAvailableActions()
    {
        List<CommandAction> runtimeActions = new List<CommandAction>();

        // 1. Comandos base de la unidad
        foreach (CommandSO cmdSO in currentActiveCommands)
        {
            if (cmdSO != null && cmdSO.ShouldDisplayAction(this))
            {
                runtimeActions.Add(cmdSO.CreateRuntimeAction(this));
            }
        }

        // 2. NUEVO: Comandos inyectados por los componentes (Ej. Construir)
        if (unitComponents != null)
        {
            foreach (var component in unitComponents)
            {
                runtimeActions.AddRange(component.GetAvailableActions());
            }
        }

        // 3. Comandos de submenú (Back button)
        if (currentActiveCommands != mainCommands && backCommandAsset != null)
        {
            CommandAction backAction = backCommandAsset.CreateRuntimeAction(this);
            runtimeActions.Add(backAction);
        }

        return runtimeActions;
    }

    /// <summary>
    /// Returns the command panel to the main command set
    /// and refreshes the user interface.
    /// </summary>
    public void ReturnToMainMenu()
    {
        currentActiveCommands = mainCommands;
        UICommandController.Instance?.RefreshGrid();
    }

    /// <summary>
    /// Switches the current command panel to a submenu
    /// and refreshes the user interface.
    /// </summary>
    /// <param name="subMenuCommands">
    /// Collection of commands that will be displayed in the submenu.
    /// </param>
    public void OpenSubMenu(List<CommandSO> subMenuCommands)
    {
        currentActiveCommands = subMenuCommands;
        UICommandController.Instance?.RefreshGrid();
    }

    /// <summary>
    /// Stops the current action and returns the unit
    /// to its idle state.
    /// </summary>
    public virtual void Stop()
    {
        ChangeState(idleState);
    }

    /// <summary>
    /// Applies total current health as damage to kill the unit.
    /// </summary>
    public virtual void ExecuteSelfDestruct()
    {
        if (state == SelectableState.Dead) return;

        // Apply total max health in light/heavy damage to guarantee destruction
        TakeDamage(currentStats.health + currentStats.lightDefense, currentStats.health + currentStats.heavyDefense);
    }

    /// <summary>
    /// Gets the effective interaction range of the unit based on its current stats.
    /// Follows the same range pattern as combat attack distances.
    /// </summary>
    /// <returns>The unit's interaction range value, or a default fallback if stats are missing.</returns>
    public float GetInteractionRange()
    {
        return (currentStats != null && currentStats.range > 0f) ? currentStats.range : 1.5f;
    }

    /// <summary>
    /// Calculates the surface distance from the villager to a target Selectable's collider.
    /// Takes into account the villager's NavMeshAgent radius for accurate edge-to-edge interaction.
    /// </summary>
    /// <param name="target">The target entity with a Collider.</param>
    /// <returns>Distance from the villager's outer edge to the target's collider surface.</returns>
    public float GetDistanceToTargetSurface(Selectable target)
    {
        if (target == null) return float.MaxValue;

        Collider targetCollider = target.GetComponentInChildren<Collider>();
        Vector3 villagerPosition = transform.position;

        float agentRadius = (agent != null) ? agent.radius : 0.5f;

        if (targetCollider != null)
        {
            // Finds the closest point on the surface of the target's collider
            Vector3 closestPoint = targetCollider.ClosestPoint(villagerPosition);
            return Mathf.Max(0f, Vector3.Distance(villagerPosition, closestPoint) - agentRadius);
        }

        // Fallback if no collider is found on the target
        return Mathf.Max(0f, Vector3.Distance(villagerPosition, target.transform.position) - agentRadius);
    }

    /// <summary>
    /// Calculates the surface distance to a world position or optional building/dropoff collider.
    /// </summary>
    public float GetDistanceToPointSurface(Vector3 targetPosition, Collider targetCollider = null)
    {
        Vector3 villagerPosition = transform.position;
        float agentRadius = (agent != null) ? agent.radius : 0.5f;

        if (targetCollider != null)
        {
            Vector3 closestPoint = targetCollider.ClosestPoint(villagerPosition);
            return Mathf.Max(0f, Vector3.Distance(villagerPosition, closestPoint) - agentRadius);
        }

        return Mathf.Max(0f, Vector3.Distance(villagerPosition, targetPosition) - agentRadius);
    }

    protected virtual void Awake()
    {
        // Initialize state cache via Virtual Factory Methods
        idleState = CreateIdleState();
        moveState = CreateMoveState();
        currentActiveCommands = mainCommands;

        unitComponents = GetComponents<IUnitComponent>();
        foreach (var component in unitComponents)
        {
            component.Initialize(this);
        }
    }

    // ============================================================
    // LIFECYCLE METHODS
    // ============================================================

    protected override void Start()
    {
        base.Start();

        TryGetComponent(out animator);
        TryGetComponent(out agent);
        TryGetComponent(out rend);

        // Cache main camera
        if (mainCamCached == null)
            mainCamCached = Camera.main;

        // Try to auto-assign owner
        if (Owner == null && PlayerManager.Instance != null)
        {
            Player possibleOwner = PlayerManager.Instance.GetPlayer(ownerPlayerId);
            if (possibleOwner != null)
                Initialize(possibleOwner);
        }

        // Set default state
        if (currentState == null)
        {
            ChangeState(idleState);
        }
    }

    private void OnDestroy()
    {
        UnsubscribeFromOwner();
        Owner?.UnregisterUnit(this);
    }

    // ============================================================
    // INITIALIZATION AND PLAYER RELATIONSHIP
    // ============================================================

    /// <summary>
    /// Initializes the unit with its owner and registers it in the player's system.
    /// </summary>
    public void Initialize(Player owner)
    {
        Owner = owner;
        ownerPlayerId = owner.playerId;
        SubscribeToOwner();
        HandleEraChange(owner.CurrentEra);

        if (currentHealth <= 0)
            currentHealth = currentStats.health;

        Owner.RegisterUnit(this);
    }

    private void SubscribeToOwner()
    {
        if (Owner != null)
            Owner.OnEraChanged += HandleEraChange;
    }

    private void UnsubscribeFromOwner()
    {
        if (Owner != null)
            Owner.OnEraChanged -= HandleEraChange;
    }

    // ============================================================
    // ERA CHANGE AND EVOLUTION
    // ============================================================

    /// <summary>
    /// Handles era change: updates stats or model depending on unit type.
    /// </summary>
    private void HandleEraChange(int newEra)
    {
        if (stats.hasVisualEvolution)
        {
            GameObject newGO = Instantiate(stats.prefabsPerEra[newEra], transform.position, transform.rotation);
            Unit newUnit = newGO.GetComponent<Unit>();
            newUnit.Initialize(Owner);
            newUnit.InheritFrom(this, newEra);
            Destroy(gameObject);
        }
        else
        {
            UpdateStats(newEra);
        }
    }

    // ============================================================
    // STATS AND HEALTH
    // ============================================================

    /// <summary>
    /// Updates stats according to the era, maintaining health proportionally.
    /// </summary>
    public virtual void UpdateStats(int era)
    {
        EraStats oldStats = currentStats;
        currentStats = stats.statsPerEra[era];

        if (oldStats != null && oldStats.health > 0)
        {
            float healthPercent = currentHealth / oldStats.health;
            currentHealth = Mathf.Clamp(currentStats.health * healthPercent, 0.0f, currentStats.health);
        }
        else currentHealth = currentStats.health;

        if (agent != null)
            agent.speed = currentStats.moveSpeed;

        ChangeAnimatorMultiplier("moveSpeedMultiplier", currentStats.moveSpeed);
        if (TryGetComponent(out IGatherer gatherer)) gatherer.OnEraChanged(era);
        if (TryGetComponent(out IAttacker attacker)) attacker.OnEraChanged(era);
        if (TryGetComponent(out IBuilder builder)) builder.OnEraChanged(era);
    }

    /// <summary>
    /// Transfers relevant data when evolving visually between eras.
    /// </summary>
    public void InheritFrom(Unit oldUnit, int era)
    {
        currentHealth = Mathf.Min(oldUnit.currentHealth, stats.statsPerEra[era].health);
        UpdateStats(era);

        if (oldUnit.TryGetComponent(out IGatherer oldGatherer) && TryGetComponent(out IGatherer gatherer))
            gatherer.OnUnitChange(oldGatherer);
        if (oldUnit.TryGetComponent(out IAttacker oldAttacker) && TryGetComponent(out IAttacker attacker))
            attacker.OnUnitChange(oldAttacker);
        if (oldUnit.TryGetComponent(out IBuilder oldBuilder) && TryGetComponent(out IBuilder builder))
            builder.OnUnitChange(oldBuilder);


        if (oldUnit.agent != null && oldUnit.agent.hasPath)
            MoveTo(oldUnit.agent.destination);
    }

    // ============================================================
    // UPDATE LOOP
    // ============================================================

    private void Update()
    {
        if (state == SelectableState.Dead) return;

        currentState?.Tick();
    }

    /// <summary>
    /// Executes a contextual order on a targeted Selectable object (e.g., enemy unit, resource, or building).
    /// </summary>
    /// <param name="target">The target Selectable clicked by the player.</param>
    public virtual void ExecuteContextCommand(Selectable target)
    {
        if (target == null || target.state == SelectableState.Dead) return;

        // 1. ¿Es un recurso y puedo recolectar?
        if (target.CompareTag("Resource"))
        {
            if (TryGetComponent(out IGatherer gatherer) && target is Resource resource)
            {
                gatherer.StartGathering(resource);
                return;
            }
        }

        // 2. ¿Es un enemigo y puedo atacar?
        if (target.IsEnemyTo(ownerPlayerId))
        {
            if (TryGetComponent(out IAttacker attacker))
            {
                attacker.Attack(target);
                return;
            }
        }


        // 3. ¿Es un edificio aliado incompleto y puedo construir?
        if (target.CompareTag("Building") && !target.IsEnemyTo(ownerPlayerId))
        {
            Building building = target.GetComponent<Building>();
            if (building != null && !building.IsFullyBuilt)
            {
                if (TryGetComponent(out IBuilder builder))
                {
                    builder.StartBuilding(building);
                    return;
                }
            }
        }

        // 4. Si no puedo interactuar, simplemente me muevo hacia él
        MoveTo(target.transform.position);
    }

    // ============================================================
    // STATE MACHINE HELPERS
    // ============================================================

    #region Virtual State Factories (Subclasses override these as needed)

    protected virtual UnitState CreateIdleState()
    {
        return new IdleState(this);
    }

    protected virtual UnitState CreateMoveState()
    {
        return new MovingState(this, Vector3.zero);
    }

    #endregion

    #region State Change Functions

    public virtual void ChangeToIdleState()
    {
        ChangeState(idleState);
    }

    public virtual void ChangeToMoveState(Vector3 des)
    {
        moveState = new MovingState(this, des);
        ChangeState(moveState);
    }

    #endregion


    public void SetBehaviourState(UnitBehaviourState newState)
    {
        if (behaviourState == newState) return;

        behaviourState = newState;

        switch (newState)
        {
            case UnitBehaviourState.Idle:
                PlayIdleAnimation();
                break;
            case UnitBehaviourState.Moving:
                PlayWalkAnimation();
                break;
        }
    }

    public void ChangeState(UnitState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState?.Enter();
        SetBehaviourState(currentState.GetBehaviourState());
        // If this unit is currently selected, refresh command grid so Stop button appears/disappears live
        if (isSelected)
        {
            UICommandController.Instance?.RefreshGrid();
        }
    }

    // ============================================================
    // HEALTH UI
    // ============================================================

    public Vector3 get_healthCanvasOffset()
    {
        return healthCanvasOffset;
    }

    // ============================================================
    // ANIMATIONS
    // ============================================================

    public void ChangeAnimatorMultiplier(String key, float multiplier)
    {
        if (animator != null) animator.SetFloat(key, multiplier);
    }

    public void ChangeAnimation(String newAnim)
    {
        animator?.SetBool(currentAnimation, false);
        currentAnimation = newAnim;
        animator?.SetBool(currentAnimation, true);
    }

    public virtual void PlayIdleAnimation()
    {
        ChangeAnimation("idle");
    }

    public virtual void PlayWalkAnimation()
    {
        ChangeAnimation("isMoving");
    }

    // ============================================================
    // VISUAL SELECTION
    // ============================================================

    public void SetSelected(bool selected)
    {
        if (rend != null)
            rend.material.color = selected ? Color.yellow : Color.white;
    }

    // ============================================================
    // MOVEMENT AND COMBAT (PUBLIC API)
    // ============================================================

    public void Wait()
    {
        StopAllCoroutines();
        if (agent != null) agent.isStopped = true;
        ChangeToIdleState();
    }

    public void MoveTo(Vector3 destination)
    {
        lastMoveDestination = destination;
        if (agent != null)
        {
            agent.isStopped = false;
            agent.SetDestination(destination);
        }

        ChangeToMoveState(destination);
    }

    private void NotifyMoveCompleted()
    {
        StanceComponent stance = GetComponent<StanceComponent>();
        stance?.OnMoveCompleted(lastMoveDestination);
    }



    /// <summary>
    /// Applies incoming damage considering defenses.
    /// </summary>
    public void TakeDamage(int lightDamage, int heavyDamage)
    {
        if (state == SelectableState.Dead) return;

        int finalDamage = Mathf.Max(0, lightDamage - currentStats.lightDefense) +
                          Mathf.Max(0, heavyDamage - currentStats.heavyDefense);

        currentHealth -= finalDamage;

        if (currentHealth <= 0f && state == SelectableState.Alive)
        {
            state = SelectableState.Dying;
            StartCoroutine(DyingRoutine());
        }

        HealthBarsManager.Instance.UpdateUnitHealth(this, currentHealth, currentStats.health);
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(currentStats.health, currentHealth + amount);

        HealthBarsManager.Instance.UpdateUnitHealth(this, currentHealth, currentStats.health);
    }

    private IEnumerator DyingRoutine()
    {
        if (fightsAfterDeath)
        {
            yield return new WaitForSeconds(postDeathFightDuration);
        }
        Die();
    }

    private void Die()
    {
        if (state == SelectableState.Dead) return;

        state = SelectableState.Dead;

        StopAllCoroutines();
        animator?.SetTrigger("die");

        Destroy(gameObject, 3f);
    }

    private void OnDrawGizmosSelected()
    {
        if (currentStats != null)
        {
            Gizmos.color = Color.red;
            float visionRangeRadius = (currentStats.visionRange > 0) ? currentStats.visionRange : 12f;
            Gizmos.DrawWireSphere(transform.position, visionRangeRadius);
        }
    }

    // ============================================================
    // === Internal State Classes ===
    // ============================================================



    private class IdleState : UnitState
    {
        public IdleState(Unit u) : base(u) { }
        public override void Enter()
        {
            if (unit.agent != null)
            {
                unit.agent.ResetPath();
                unit.agent.isStopped = true;
            }
            unit.SetBehaviourState(UnitBehaviourState.Idle);
        }
        public override UnitBehaviourState GetBehaviourState() => UnitBehaviourState.Idle;
    }

    protected class MovingState : UnitState
    {
        private Vector3 destination;
        public MovingState(Unit u, Vector3 dest) : base(u) { destination = dest; }
        public override void Enter()
        {
            if (unit.agent != null)
            {
                unit.agent.isStopped = false;
                unit.agent.SetDestination(destination);
            }
            unit.SetBehaviourState(UnitBehaviourState.Moving);
        }

        public override void Tick()
        {
            if (unit.agent == null) return;

            if (unit.agent.pathPending)
            {
                unit.PlayWalkAnimation();
                return;
            }

            if (unit.agent.remainingDistance > unit.agent.stoppingDistance)
            {
                unit.PlayWalkAnimation();
            }
            else
            {
                unit.NotifyMoveCompleted();
                unit.ChangeToIdleState();
            }
        }

        public override UnitBehaviourState GetBehaviourState() => UnitBehaviourState.Moving;
    }
}
