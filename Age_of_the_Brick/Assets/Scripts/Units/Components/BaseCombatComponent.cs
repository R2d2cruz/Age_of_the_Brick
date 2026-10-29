using GLTFast;
using Mono.Cecil.Cil;
using UnityEngine;
using UnityEngine.AI;

public class BaseCombatComponent : BaseUnitComponent, IAttacker
{

    /// <summary>
    /// Visual weapon object enabled while attacking.
    /// </summary>
    [SerializeField] protected GameObject weapon;

    /// <summary>
    /// Current target assigned to this combat component.
    /// </summary>
    protected Selectable currentTarget;   // Generic target (resource/building/unit)

    /// <summary>
    /// State responsible for combat behavior.
    /// </summary>
    protected UnitState attackState;

    /// <summary>
    /// Initializes the combat component and creates
    /// the attack state instance.
    /// </summary>
    /// <param name="coreUnit">
    /// Unit that owns this component.
    /// </param>
    public override void Initialize(Unit coreUnit)
    {
        base.Initialize(coreUnit);
        attackState = CreateAttackState();
    }
    public override void OnEraChanged(int newEra)
    {
        base.OnEraChanged(newEra);
        unit.ChangeAnimatorMultiplier("attackSpeedMultiplier", unit.currentStats.attackSpeed);
    }

    /// <summary>
    /// Assigns a target and switches the unit to the attack state.
    /// </summary>
    /// <param name="target">
    /// Target to attack.
    /// </param>
    public void Attack(Selectable target)
    {
        if (target == null) return;

        if (target.CompareTag("Resource"))
        {
            unit.Wait();
            return;
        }
        currentTarget = target;
        unit.ChangeState(attackState);
    }

    /// <summary>
    /// Creates the attack state used by this component.
    /// Derived classes may override this method to provide
    /// ranged or specialized combat behaviors.
    /// </summary>
    /// <returns>
    /// Attack state instance.
    /// </returns>
    protected virtual UnitState CreateAttackState()
    {
        return new AttackingState(unit, this);
    }

    /// <summary>
    /// Stops combat and clears the current target.
    /// </summary>
    public void StopAttacking()
    {
        currentTarget = null;

        unit.Wait();
    }

    /// <summary>
    /// Enables or disables the visual weapon object.
    /// </summary>
    /// <param name="active">
    /// Whether the weapon should be visible.
    /// </param>
    public virtual void SetActiveWeapon(bool active)
    {
        if (weapon == null) return;
        weapon.SetActive(active);
    }

    /// <summary>
    /// Plays the default attack animation.
    /// </summary>
    public virtual void PlayAttackAnimation()

    {
        unit.ChangeAnimation("attack");
    }

    // ============================================================
    // COMBAT AND ROUTINES
    // ============================================================

    /// <summary>
    /// Finds the nearest enemy unit within the visionRange radius.
    /// </summary>
    protected Unit FindNearestEnemy()
    {
        float visionRangeRadius = (unit.currentStats != null && unit.currentStats.visionRange * 2 > 0) ? unit.currentStats.visionRange * 2 : 12f;
        Collider[] hits = Physics.OverlapSphere(transform.position, visionRangeRadius);

        Unit nearestEnemy = null;
        float minDist = Mathf.Infinity;

        foreach (var hit in hits)
        {
            Unit candidate = hit.GetComponent<Unit>();
            if (candidate == null || candidate == unit) continue;
            if (candidate.state != SelectableState.Alive) continue;
            if (!candidate.IsEnemyTo(unit.ownerPlayerId)) continue;

            float dist = Vector3.Distance(transform.position, candidate.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearestEnemy = candidate;
            }
        }
        return nearestEnemy;
    }

    /// <summary>
    /// Called by attack animation event when the hit connects.
    /// </summary>
    public virtual void OnAttackHit()
    {
        if (currentTarget == null || currentTarget.state == SelectableState.Dead) return;
    }

    /// <summary>
    /// Executes the actual combat logic.
    /// </summary>
    protected virtual void ExecuteAttackLogic()
    {
    }


    #region Melee Attack State Machine

    protected class AttackingState : UnitState
    {
        /// <summary>
        /// Internal combat workflow states.
        /// </summary>
        private enum CombatSubState
        {
            /// <summary>
            /// Moving toward the current target.
            /// </summary>
            MovingToTarget,

            /// <summary>
            /// Attacking the current target.
            /// </summary>
            AttackingTarget
        }

        private readonly BaseCombatComponent baseCombatComponent;
        private readonly NavMeshAgent agent;
        private CombatSubState currentSubState;

        private float attackTimer = 0f;
        private const float BaseAnimationDuration = 1.05f;

        /// <summary>
        /// Creates a new combat state for the specified unit.
        /// </summary>
        /// <param name="u">
        /// Unit that owns the state.
        /// </param>
        /// <param name="component">
        /// Combat component controlling the attack behavior.
        /// </param>
        public AttackingState(Unit u, BaseCombatComponent component) : base(u)
        {
            this.baseCombatComponent = component;
            agent = unit.GetComponent<NavMeshAgent>();
        }

        public override void Enter()
        {
            attackTimer = 0f;
            baseCombatComponent.SetActiveWeapon(true);
            // Evaluate whether the unit should move or attack immediately.
            SetSubState(CombatSubState.MovingToTarget);
        }

        public override void Tick()
        {
            if (baseCombatComponent.currentTarget == null || baseCombatComponent.currentTarget.state != SelectableState.Alive)
            {
                // Target lost or dead: stop combat. StanceComponent re-acquires targets on its own timer.
                baseCombatComponent.StopAttacking();
                return; // StopAttacking already switched the unit to Idle.
            }

            // Execute the current sub-state logic.
            switch (currentSubState)
            {
                case CombatSubState.MovingToTarget:
                    HandleMovingToTarget();
                    break;

                case CombatSubState.AttackingTarget:
                    HandleAttackingTarget();
                    break;
            }
        }

        /// <summary>
        /// Switches the active combat sub-state
        /// and updates movement and animations.
        /// </summary>
        /// <param name="newSubState">
        /// New combat sub-state.
        /// </param>
        private void SetSubState(CombatSubState newSubState)
        {
            currentSubState = newSubState;

            switch (currentSubState)
            {
                case CombatSubState.MovingToTarget:
                    if (agent != null) agent.isStopped = false;
                    unit.SetBehaviourState(UnitBehaviourState.Moving);
                    break;

                case CombatSubState.AttackingTarget:
                    if (agent != null) agent.isStopped = true;
                    attackTimer = 0f; // Reset the timer to start a new attack cycle.
                    unit.SetBehaviourState(UnitBehaviourState.Attacking);
                    break;
            }
        }

        private void HandleMovingToTarget()
        {
            // Continuously update the destination because
            // the target may move during combat.
            if (agent != null && baseCombatComponent.currentTarget != null)
            {
                agent.SetDestination(baseCombatComponent.currentTarget.transform.position);
            }

            float distanceToTarget = unit.GetDistanceToTargetSurface(baseCombatComponent.currentTarget);
            float attackRange = unit.GetInteractionRange();

            if (distanceToTarget <= attackRange)
            {
                baseCombatComponent.PlayAttackAnimation();
                SetSubState(CombatSubState.AttackingTarget);
            }
        }

        private void HandleAttackingTarget()
        {
            float distanceToTarget = unit.GetDistanceToTargetSurface(baseCombatComponent.currentTarget);
            float attackRange = unit.GetInteractionRange();

            // The target left attack range, resume pursuit.
            if (distanceToTarget > attackRange)
            {
                SetSubState(CombatSubState.MovingToTarget);
                return;
            }

            // Smoothly rotate toward the target.
            Vector3 lookDir = baseCombatComponent.currentTarget.transform.position - unit.transform.position;
            lookDir.y = 0;
            if (lookDir.sqrMagnitude > 0.001f)
            {
                unit.transform.rotation = Quaternion.Slerp(
                    unit.transform.rotation,
                    Quaternion.LookRotation(lookDir),
                    Time.deltaTime * 10f
                );
            }

            // Combat timing logic.
            float speedMultiplier = Mathf.Max(0.0001f, unit.currentStats.attackSpeed);
            attackTimer += Time.deltaTime * speedMultiplier;

            if (attackTimer >= BaseAnimationDuration)
            {
                // Preserve remaining milliseconds to avoid
                // losing accumulated time.
                attackTimer -= BaseAnimationDuration;

                // Logical attack execution.
                baseCombatComponent.ExecuteAttackLogic();
            }
        }

        public override void Exit()
        {
            attackTimer = 0f;
            baseCombatComponent.SetActiveWeapon(false);
            if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
        }

        public override UnitBehaviourState GetBehaviourState()
        {
            return currentSubState switch
            {
                CombatSubState.MovingToTarget => UnitBehaviourState.Moving,
                CombatSubState.AttackingTarget => UnitBehaviourState.Attacking,
                _ => UnitBehaviourState.Idle
            };
        }
    }

    #endregion
}
