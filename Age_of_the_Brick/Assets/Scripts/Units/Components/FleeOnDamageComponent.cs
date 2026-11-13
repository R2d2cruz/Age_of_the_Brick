using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Makes the owning unit flee away from an enemy damage source, then idle under a short
/// re-flee cooldown before resuming the prior gather/build/move task when still valid.
/// Presence of this component on the prefab is the content gate (no <see cref="UnitRole"/> check).
/// </summary>
public class FleeOnDamageComponent : BaseUnitComponent
{
    private enum SnapshotKind
    {
        None,
        Gather,
        Build,
        Move
    }

    [SerializeField]
    [Tooltip("Flee distance when visionRange is missing or zero.")]
    private float defaultFleeDistance = 10f;

    [SerializeField]
    [Tooltip("Seconds after arrive/timeout before resume and before a new flee may start.")]
    private float reFleeCooldownSeconds = 2.5f;

    [SerializeField]
    [Tooltip("Extra seconds added to distance/speed for the flee move timeout.")]
    private float fleeTimeoutMarginSeconds = 2f;

    private bool issuingFleeMove;
    private bool fleeing;
    private bool pendingResume;
    private bool ignorePlayerOrderNotify;
    private float cooldownRemaining;
    private float fleeTimeoutRemaining;

    private SnapshotKind snapshotKind;
    private Resource snapshotResource;
    private Building snapshotBuilding;
    private Vector3 snapshotMoveDestination;

    /// <summary>True while this component is issuing its own flee <see cref="Unit.MoveTo"/>.</summary>
    public bool IsIssuingFleeMove => issuingFleeMove;

    private void Update()
    {
        if (unit == null || unit.state != SelectableState.Alive)
            return;

        if (fleeing)
        {
            fleeTimeoutRemaining -= Time.deltaTime;
            if (fleeTimeoutRemaining <= 0f
                || unit.BehaviourState == UnitBehaviourState.Idle)
            {
                BeginCooldownPhase();
            }
        }

        if (cooldownRemaining > 0f)
        {
            cooldownRemaining -= Time.deltaTime;
            if (cooldownRemaining <= 0f)
            {
                cooldownRemaining = 0f;
                if (pendingResume)
                {
                    pendingResume = false;
                    ResumePriorTask();
                }
            }
        }
    }

    /// <summary>
    /// Called from <see cref="Unit.TakeDamage"/> when an enemy source damaged this unit.
    /// </summary>
    /// <param name="source">Enemy that dealt the damage.</param>
    public void OnDamaged(Selectable source)
    {
        if (unit == null || unit.state != SelectableState.Alive)
            return;
        if (source == null || source.state != SelectableState.Alive)
            return;
        if (!source.IsEnemyTo(unit.ownerPlayerId))
            return;
        if (fleeing || cooldownRemaining > 0f || pendingResume)
            return;
        if (unit.TryGetComponent(out IAttacker attacker) && attacker.IsInCombat)
            return;

        SnapshotPriorTask();
        StartFleeMove(source.transform.position);
    }

    /// <summary>
    /// Cancels flee and clears any resume snapshot. Called when a player order takes over.
    /// </summary>
    public void NotifyPlayerOrder()
    {
        if (ignorePlayerOrderNotify)
            return;

        fleeing = false;
        pendingResume = false;
        cooldownRemaining = 0f;
        fleeTimeoutRemaining = 0f;
        ClearSnapshot();
    }

    /// <summary>
    /// Called when a move order finishes. Completes an in-progress flee move.
    /// </summary>
    /// <param name="destination">Position the unit was moving to.</param>
    public void OnMoveCompleted(Vector3 destination)
    {
        if (!fleeing)
            return;
        BeginCooldownPhase();
    }

    private void StartFleeMove(Vector3 threatPosition)
    {
        float distance = ResolveFleeDistance();
        Vector3 fleePoint = FleeDestination.ComputeAwayPoint(
            unit.transform.position,
            threatPosition,
            distance,
            unit.transform.forward);

        fleeing = true;
        pendingResume = false;
        cooldownRemaining = 0f;

        float speed = unit.currentStats != null ? Mathf.Max(0.01f, unit.currentStats.moveSpeed) : 1f;
        fleeTimeoutRemaining = distance / speed + Mathf.Max(0f, fleeTimeoutMarginSeconds);

        // Gather Extracting leaves a completed path (remainingDistance ≈ 0). Without clearing it,
        // MovingState treats the flee MoveTo as already arrived. Build Constructing usually still
        // has remainingDistance toward the building center, so flee worked there without this.
        if (unit.TryGetComponent(out NavMeshAgent agent))
        {
            agent.ResetPath();
            agent.isStopped = false;
        }

        issuingFleeMove = true;
        unit.MoveTo(fleePoint);
        issuingFleeMove = false;
    }

    private float ResolveFleeDistance()
    {
        if (unit.currentStats != null && unit.currentStats.visionRange > 0f)
            return unit.currentStats.visionRange;
        return Mathf.Max(0f, defaultFleeDistance);
    }

    private void BeginCooldownPhase()
    {
        if (!fleeing)
            return;

        fleeing = false;
        fleeTimeoutRemaining = 0f;
        pendingResume = true;
        cooldownRemaining = Mathf.Max(0f, reFleeCooldownSeconds);
        unit.Wait();
    }

    private void SnapshotPriorTask()
    {
        ClearSnapshot();

        if (unit.TryGetComponent(out GathererComponent gatherer)
            && gatherer.TargetResource != null
            && gatherer.TargetResource.GetQuantity() > 0)
        {
            snapshotKind = SnapshotKind.Gather;
            snapshotResource = gatherer.TargetResource;
            return;
        }

        if (unit.TryGetComponent(out BuilderComponent builder)
            && builder.TargetBuilding != null
            && !builder.TargetBuilding.IsFullyBuilt)
        {
            snapshotKind = SnapshotKind.Build;
            snapshotBuilding = builder.TargetBuilding;
            return;
        }

        if (unit.BehaviourState == UnitBehaviourState.Moving)
        {
            snapshotKind = SnapshotKind.Move;
            snapshotMoveDestination = unit.LastMoveDestination;
        }
    }

    private void ResumePriorTask()
    {
        SnapshotKind kind = snapshotKind;
        Resource resource = snapshotResource;
        Building building = snapshotBuilding;
        Vector3 moveDestination = snapshotMoveDestination;
        ClearSnapshot();

        ignorePlayerOrderNotify = true;
        try
        {
            switch (kind)
            {
                case SnapshotKind.Gather:
                    if (resource != null && resource.GetQuantity() > 0
                        && unit.TryGetComponent(out GathererComponent gatherer))
                    {
                        gatherer.StartGathering(resource);
                    }
                    break;

                case SnapshotKind.Build:
                    if (building != null && !building.IsFullyBuilt
                        && unit.TryGetComponent(out BuilderComponent builder))
                    {
                        builder.StartBuilding(building);
                    }
                    break;

                case SnapshotKind.Move:
                    unit.MoveTo(moveDestination);
                    break;
            }
        }
        finally
        {
            ignorePlayerOrderNotify = false;
        }
    }

    private void ClearSnapshot()
    {
        snapshotKind = SnapshotKind.None;
        snapshotResource = null;
        snapshotBuilding = null;
        snapshotMoveDestination = Vector3.zero;
    }
}
