using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using UnityEngine.UI;

public enum UnitBehaviourState
{
    Idle,
    Moving,
    Attacking,
    Waiting
}

/// <summary>
/// Base class for all in-game units.
/// Inherits from <see cref="Selectable"/>, allowing player interaction (selection, commands).
/// Controls movement, combat, animations, and health UI.
/// </summary>
public class Unit : Selectable
{
    // ============================================================
    // GENERAL CONFIGURATION
    // ============================================================

    [Header("General Configuration")]
    public UnitStats stats;                 // ScriptableObject containing per-era stats.
    public EraStats currentStats;           // Active stats depending on the current era.
    private Animator animator;              // Animation controller.
    private NavMeshAgent agent;             // Navigation and pathfinding agent.
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
    private Player Owner;

    // ============================================================
    // HEALTH UI (WORLD SPACE)
    // ============================================================

    [Header("Health UI (World)")]
    [SerializeField] private Vector3 healthCanvasOffset = new Vector3(0f, 5.4f, 0f);
    private static Camera mainCamCached; // Cached main camera reference for efficiency.

    // ============================================================
    // COMBAT AND STATE
    // ============================================================

    private bool isAttacking = false;
    private Selectable currentTarget;   // Generic target (resource/building/unit)
    private Unit attackTarget;          // Combat target (enemy unit)

    [Header("Death Behavior")]
    public bool fightsAfterDeath = false;       // Allows post-death fighting for a short time.
    public float postDeathFightDuration = 2f;   // Duration of post-death state.

    // ============================================================
    // STATE MACHINE
    // ============================================================

    [Header("Unit States")]
    private UnitBehaviourState behaviourState = UnitBehaviourState.Idle;
    public UnitBehaviourState BehaviourState => behaviourState;

    private UnitState currentState; // Current state instance.
    // Cached state instances to avoid GC allocations during transitions
    protected UnitState idleState;
    protected UnitState moveState;
    protected UnitState attackState;

    protected virtual void Awake()
    {
        // Initialize state cache via Virtual Factory Methods
        idleState = CreateIdleState();
        moveState = CreateMoveState();
        attackState = CreateAttackState();
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
        Owner?.Registry.UnregisterUnit(this);
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
        UpdateStats(owner.CurrentEra);

        if (currentHealth <= 0)
            currentHealth = currentStats.health;

        Owner.Registry.RegisterUnit(this);
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
    public void UpdateStats(int era)
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

        if (animator != null)
        {
            animator.SetFloat("moveSpeedMultiplier", currentStats.moveSpeed);
            animator.SetFloat("attackSpeedMultiplier", currentStats.attackSpeed);
        }
    }

    /// <summary>
    /// Transfers relevant data when evolving visually between eras.
    /// </summary>
    public void InheritFrom(Unit oldUnit, int era)
    {
        currentHealth = Mathf.Min(oldUnit.currentHealth, stats.statsPerEra[era].health);
        UpdateStats(era);

        if (oldUnit.attackTarget != null)
        {
            attackTarget = oldUnit.attackTarget;
            if (oldUnit.isAttacking)
                ChangeState(attackState);
        }

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

    protected virtual UnitState CreateAttackState()
    {
        return new AttackingState(this);
    }

    #endregion

    #region State Change Functions

    protected virtual void ChangeToIdleState()
    {
        ChangeState(idleState);
    }

    protected virtual void ChangeToMoveState(Vector3 des)
    {
        moveState = new MovingState(this, des);
        ChangeState(moveState);
    }

    protected virtual void ChangeToAttackState()
    {
        ChangeState(attackState);
    }

    #endregion


    protected void SetBehaviourState(UnitBehaviourState newState)
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
            case UnitBehaviourState.Attacking:
                PlayAttackAnimation();
                break;
        }
    }

    protected void ChangeState(UnitState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState?.Enter();
        SetBehaviourState(currentState.GetBehaviourState());
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

    public virtual void PlayIdleAnimation()
    {
        animator?.SetBool("isMoving", false);
    }

    public void PlayWalkAnimation()
    {
        if (animator != null)
        {
            animator.SetBool("attack", false);
            animator.SetBool("isMoving", true);
        }
    }

    protected virtual void PlayAttackAnimation()
    {
        if (animator != null)
        {
            animator.SetBool("attack", true);
            animator.SetBool("isMoving", false);
        }
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
        attackTarget = null;
        isAttacking = false;
        if (agent != null) agent.isStopped = true;
        ChangeToIdleState();
    }

    public void MoveTo(Vector3 destination)
    {
        currentTarget = null;
        attackTarget = null;
        isAttacking = false;

        if (agent != null)
        {
            agent.isStopped = false;
            agent.SetDestination(destination);
        }

        // If MoveState needs parameters, configure them before changing state
        ChangeToMoveState(destination);
    }

    public void SetTarget(Selectable target)
    {
        if (target == null) return;

        currentTarget = target;
        attackTarget = target.GetComponent<Unit>();
        isAttacking = false;

        if (attackTarget != null && attackTarget.Owner != Owner)
        {
            isAttacking = true;
            ChangeToAttackState();
        }
        else if (target.CompareTag("Resource"))
        {
            MoveTo(target.transform.position);
        }
        else if (target.CompareTag("Building"))
        {
            // TODO: handle building interaction
        }
    }

    public void Attack(Unit target)
    {
        if (target == null) return;

        currentTarget = target;
        attackTarget = target;
        isAttacking = true;
        ChangeToAttackState();
    }

    // ============================================================
    // COMBAT AND ROUTINES
    // ============================================================

    /// <summary>
    /// Finds the nearest enemy unit within the visionRange radius.
    /// </summary>
    protected Unit FindNearestEnemy()
    {
        float visionRangeRadius = (currentStats != null && currentStats.visionRange * 2 > 0) ? currentStats.visionRange * 2 : 12f;
        Collider[] hits = Physics.OverlapSphere(transform.position, visionRangeRadius);

        Unit nearestEnemy = null;
        float minDist = Mathf.Infinity;

        foreach (var hit in hits)
        {
            Unit candidate = hit.GetComponent<Unit>();
            if (candidate == null || candidate == this) continue;
            if (candidate.state != SelectableState.Alive) continue;
            if (!candidate.IsEnemyTo(ownerPlayerId)) continue;

            float dist = Vector3.Distance(transform.position, candidate.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearestEnemy = candidate;
            }
        }
        return nearestEnemy;
    }

    private void StopAttacking()
    {
        isAttacking = false;
        attackTarget = null;

        if (agent != null)
            agent.isStopped = true;

        if (animator != null)
        {
            animator.SetBool("attack", false);
            animator.SetBool("isMoving", false);
        }
    }

    /// <summary>
    /// Called by attack animation event when the hit connects.
    /// </summary>
    public virtual void OnAttackHit()
    {
        if (attackTarget == null) return;
        if (attackTarget.state == SelectableState.Dead) return;

        float dist = Vector3.Distance(transform.position, attackTarget.transform.position);
        if (dist > currentStats.range && dist > 2) return;
        attackTarget.TakeDamage(currentStats.lightAttack, currentStats.heavyAttack);
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
        isAttacking = false;

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

    protected abstract class UnitState
    {
        protected Unit unit;
        public UnitState(Unit u) { unit = u; }
        public virtual void Enter() { }
        public virtual void Exit() { }
        public virtual void Tick() { }
        public virtual UnitBehaviourState GetBehaviourState() { return UnitBehaviourState.Idle; }
    }

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
            if (unit.attackTarget != null && unit.attackTarget != unit && unit.attackTarget.state == SelectableState.Alive)
            {
                unit.ChangeToAttackState();
                return;
            }

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
                unit.ChangeToIdleState();
            }
        }

        public override UnitBehaviourState GetBehaviourState() => UnitBehaviourState.Moving;
    }

    protected class AttackingState : UnitState
    {
        private float attackCooldown = 0;
        public AttackingState(Unit u) : base(u) { }

        public override void Enter()
        {
            unit.isAttacking = true;
            unit.SetBehaviourState(UnitBehaviourState.Attacking);
            attackCooldown = 1f / Mathf.Max(0.0001f, unit.currentStats.attackSpeed);
        }

        public override void Tick()
        {
            if (unit.attackTarget == null || unit.attackTarget.state != SelectableState.Alive)
            {
                Unit newTarget = unit.FindNearestEnemy();
                if (newTarget != null)
                {
                    unit.attackTarget = newTarget;
                }
                else
                {
                    unit.StopAttacking();
                    unit.ChangeToIdleState();
                    return;
                }
            }

            if (unit.attackTarget != null)
            {
                float dist = Vector3.Distance(unit.transform.position, unit.attackTarget.transform.position);

                if (dist > unit.currentStats.range && dist > 2f)
                {
                    if (unit.agent != null)
                    {
                        unit.agent.isStopped = false;
                        unit.agent.SetDestination(unit.attackTarget.transform.position);
                    }
                    unit.SetBehaviourState(UnitBehaviourState.Moving);
                }
                else
                {
                    // En rango: detenerse, rotar y atacar según cooldown
                    if (unit.agent != null) unit.agent.isStopped = true;

                    Vector3 lookDir = unit.attackTarget.transform.position - unit.transform.position;
                    lookDir.y = 0;
                    if (lookDir.sqrMagnitude > 0.001f)
                        unit.transform.rotation = Quaternion.LookRotation(lookDir);

                    unit.SetBehaviourState(UnitBehaviourState.Attacking);

                    attackCooldown -= Time.deltaTime;
                    if (attackCooldown <= 0f)
                    {
                        // Reproduce la animación, y el evento dentro de ella llamará a OnAttackHit()
                        unit.PlayAttackAnimation();

                        attackCooldown = 1f / Mathf.Max(0.0001f, unit.currentStats.attackSpeed);
                    }
                }
            }
        }

        public override void Exit()
        {
            unit.isAttacking = false;
            unit.animator?.SetBool("attack", false);
        }

        public override UnitBehaviourState GetBehaviourState() => UnitBehaviourState.Attacking;
    }
}
