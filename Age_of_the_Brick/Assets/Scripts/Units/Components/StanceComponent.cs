using UnityEngine;

/// <summary>
/// Tracks unit combat stance (Aggressive, Defensive, Hold Position, Passive), the anchor position,
/// and periodically evaluates auto-engagement, leashing, and return-to-anchor behavior.
/// Player orders (move/attack) override stance-driven actions.
/// </summary>
public class StanceComponent : BaseUnitComponent
{
    [SerializeField] private float evaluateInterval = 0.35f;

    /// <summary>Active stance for this unit.</summary>
    public UnitStance CurrentStance { get; private set; } = UnitStance.Aggressive;

    /// <summary>World position guarded by Defensive/Hold stances.</summary>
    public Vector3 Anchor { get; private set; }

    private IAttacker attacker;
    private float evaluateTimer;
    private bool stanceInitiatedCombat;
    private bool returningToAnchor;
    private bool issuingStanceMove;
    private bool pendingReturnAfterPlayerCombat;
    private Selectable stanceTarget;

    /// <summary>Caches the attacker, sets the initial anchor and defaults to Aggressive.</summary>
    /// <param name="coreUnit">Owning unit.</param>
    public override void Initialize(Unit coreUnit)
    {
        base.Initialize(coreUnit);
        attacker = coreUnit.GetComponent<IAttacker>();
        Anchor = coreUnit.transform.position;
        CurrentStance = UnitStance.Aggressive;
    }

    /// <summary>True while the stance itself is issuing a move (return to anchor).</summary>
    public bool IsIssuingStanceMove => issuingStanceMove;

    /// <summary>True while the unit walks back to its anchor under stance control.</summary>
    public bool IsReturningToAnchor => returningToAnchor;

    /// <summary>
    /// Called when a player/system order (move, etc.) overrides stance-driven behavior.
    /// Clears stance combat ownership and return-to-anchor state.
    /// </summary>
    public void NotifyPlayerOrder()
    {
        ClearStanceCombat();
        returningToAnchor = false;
        pendingReturnAfterPlayerCombat = false;
    }

    /// <summary>
    /// Called by the combat component whenever an attack is assigned. Keeps stance ownership only
    /// when the target is the one the stance chose; otherwise the player took over.
    /// </summary>
    public void AcknowledgeAttack(Selectable target)
    {
        if (stanceInitiatedCombat && target == stanceTarget) return;
        NotifyPlayerOrder();

        // Defensive units walk back to their anchor once the player-ordered fight ends.
        if (CurrentStance == UnitStance.Defensive)
            pendingReturnAfterPlayerCombat = true;
    }

    /// <summary>
    /// Changes the stance. Cancels stance-owned combat and, for Defensive/Hold, anchors at the current position.
    /// </summary>
    /// <param name="stance">New stance to apply.</param>
    public void SetStance(UnitStance stance)
    {
        if (stanceInitiatedCombat && attacker != null)
            attacker.StopAttacking();

        CurrentStance = stance;
        ClearStanceCombat();
        returningToAnchor = false;
        pendingReturnAfterPlayerCombat = false;
        if (stance == UnitStance.Defensive || stance == UnitStance.HoldPosition)
            SetAnchor(unit.transform.position);
    }

    /// <summary>Sets the world position Defensive/Hold units guard and return to.</summary>
    /// <param name="worldPosition">New anchor position.</param>
    public void SetAnchor(Vector3 worldPosition) => Anchor = worldPosition;

    /// <summary>
    /// Called when a move order finishes. Defensive/Hold units re-anchor at the destination.
    /// </summary>
    /// <param name="destination">Position the unit was moving to.</param>
    public void OnMoveCompleted(Vector3 destination)
    {
        if (CurrentStance == UnitStance.Defensive || CurrentStance == UnitStance.HoldPosition)
            SetAnchor(destination);
        returningToAnchor = false;
    }

    /// <summary>
    /// Called when a nearby ally takes damage from an enemy. Defensive units assist if the
    /// aggressor is within vision of both the ally and this unit's anchor (leash).
    /// </summary>
    public void NotifyAllyDamaged(Selectable damagedAlly, Selectable aggressor)
    {
        if (CurrentStance != UnitStance.Defensive) return;
        if (damagedAlly == null) return;
        if (aggressor == null || aggressor.state != SelectableState.Alive) return;
        if (!aggressor.IsEnemyTo(unit.ownerPlayerId)) return;

        // Already fighting under stance control: re-issuing Attack would reset the Attack state on every hit.
        if (stanceInitiatedCombat
            && (aggressor == stanceTarget || unit.BehaviourState == UnitBehaviourState.Attacking))
            return;

        float vision = GetVisionRadius();
        if (Vector3.Distance(unit.transform.position, damagedAlly.transform.position) > vision)
            return;
        if (Vector3.Distance(Anchor, aggressor.transform.position) > vision)
            return; // still respect Defensive leash
        if (!IsIdleEligible() && !stanceInitiatedCombat) return;
        BeginStanceAttack(aggressor);
    }

    private void Update()
    {
        if (unit == null || unit.state != SelectableState.Alive) return;

        if (returningToAnchor && unit.BehaviourState == UnitBehaviourState.Idle)
            returningToAnchor = false;

        // Hold must never chase: check every frame, not only on the evaluate interval.
        if (stanceInitiatedCombat && CurrentStance == UnitStance.HoldPosition && HoldGuard())
            return;

        evaluateTimer += Time.deltaTime;
        if (evaluateTimer < evaluateInterval) return;
        evaluateTimer = 0f;
        Evaluate();
    }

    /// <summary>
    /// Periodic stance policy: auto-engage, leash, hold and return-to-anchor decisions.
    /// </summary>
    public void Evaluate()
    {
        if (attacker == null) return;
        float vision = GetVisionRadius();
        float attackRange = unit.GetInteractionRange();

        if (stanceInitiatedCombat)
        {
            Selectable liveTarget = FindActiveStanceTarget();

            if (CurrentStance == UnitStance.HoldPosition && HoldGuard())
                return;

            if (CurrentStance == UnitStance.Defensive)
            {
                if (liveTarget == null
                    || Vector3.Distance(Anchor, liveTarget.transform.position) > vision)
                {
                    ReturnToAnchor();
                    return;
                }
            }

            if (CurrentStance == UnitStance.Passive)
            {
                attacker.StopAttacking();
                ClearStanceCombat();
                return;
            }

            // Aggressive: keep fighting until combat stops itself (target dead -> Idle).
            if (unit.BehaviourState == UnitBehaviourState.Idle)
            {
                ClearStanceCombat();
                if (CurrentStance == UnitStance.Defensive)
                    ReturnToAnchor();
            }
            return;
        }

        // Player-ordered combat finished while Defensive: walk back to the anchor.
        if (pendingReturnAfterPlayerCombat && unit.BehaviourState == UnitBehaviourState.Idle)
        {
            pendingReturnAfterPlayerCombat = false;
            if (CurrentStance == UnitStance.Defensive)
            {
                ReturnToAnchor();
                return;
            }
        }

        if (!IsIdleEligible()) return;
        if (CurrentStance == UnitStance.Passive) return;

        if (CurrentStance == UnitStance.Aggressive)
        {
            Selectable target = FindNearestEnemyFrom(unit.transform.position, vision);
            if (target != null) BeginStanceAttack(target);
            return;
        }

        if (CurrentStance == UnitStance.Defensive)
        {
            Selectable target = FindNearestEnemyFrom(Anchor, vision);
            if (target != null) BeginStanceAttack(target);
            return;
        }

        if (CurrentStance == UnitStance.HoldPosition)
        {
            Selectable target = FindNearestEnemyFrom(unit.transform.position, attackRange + 0.5f, requirePivotWithinRadius: false);
            if (target != null && unit.GetDistanceToTargetSurface(target) <= attackRange)
                BeginStanceAttack(target);
        }
    }

    private float GetVisionRadius()
    {
        return (unit.currentStats != null && unit.currentStats.visionRange > 0)
            ? unit.currentStats.visionRange
            : 12f;
    }

    private Selectable FindNearestEnemyFrom(Vector3 origin, float radius, bool requirePivotWithinRadius = true)
    {
        Collider[] hits = Physics.OverlapSphere(origin, radius);
        Selectable best = null;
        float bestDist = float.PositiveInfinity;

        foreach (Collider hit in hits)
        {
            Selectable candidate = hit.GetComponentInParent<Selectable>();
            if (candidate == null || candidate == unit) continue;
            if (candidate.state != SelectableState.Alive) continue;
            if (!candidate.IsEnemyTo(unit.ownerPlayerId)) continue;
            if (candidate is not Unit && candidate is not Building) continue;

            float dist = Vector3.Distance(origin, candidate.transform.position);
            if (requirePivotWithinRadius && dist > radius) continue;
            if (dist < bestDist)
            {
                bestDist = dist;
                best = candidate;
            }
        }
        return best;
    }

    private bool IsIdleEligible()
    {
        if (returningToAnchor) return false;
        // Player-ordered combat or movement: do not steal control.
        if (!stanceInitiatedCombat && unit.BehaviourState == UnitBehaviourState.Attacking)
            return false;
        if (!stanceInitiatedCombat && unit.BehaviourState == UnitBehaviourState.Moving)
            return false;
        return unit.BehaviourState == UnitBehaviourState.Idle
            || stanceInitiatedCombat;
    }

    private void BeginStanceAttack(Selectable target)
    {
        if (attacker == null || target == null) return;
        stanceInitiatedCombat = true;
        pendingReturnAfterPlayerCombat = false;
        stanceTarget = target;
        attacker.Attack(target);
    }

    private Selectable FindActiveStanceTarget()
    {
        if (stanceTarget == null || stanceTarget.state != SelectableState.Alive)
            return null;
        return stanceTarget;
    }

    private void ClearStanceCombat()
    {
        stanceInitiatedCombat = false;
        stanceTarget = null;
    }

    /// <summary>
    /// Hold-position guard: cancels stance combat if the target is gone, out of range,
    /// or the unit started moving (chasing). Returns true when combat was cancelled.
    /// </summary>
    private bool HoldGuard()
    {
        if (attacker == null) return false;
        Selectable liveTarget = FindActiveStanceTarget();
        if (liveTarget == null
            || unit.GetDistanceToTargetSurface(liveTarget) > unit.GetInteractionRange()
            || unit.BehaviourState == UnitBehaviourState.Moving)
        {
            attacker.StopAttacking();
            ClearStanceCombat();
            return true;
        }
        return false;
    }

    private void ReturnToAnchor()
    {
        if (attacker != null) attacker.StopAttacking();
        ClearStanceCombat();
        issuingStanceMove = true;
        unit.MoveTo(Anchor);
        issuingStanceMove = false;
        returningToAnchor = true;
    }
}
