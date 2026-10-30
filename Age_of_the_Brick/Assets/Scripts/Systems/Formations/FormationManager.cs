using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Tracks formation groups for local-player units and issues slot-based move / attack-approach orders.
/// </summary>
public class FormationManager : MonoBehaviour
{
    public static FormationManager Instance { get; private set; }

    private readonly Dictionary<int, FormationGroup> groupsById = new();
    private readonly Dictionary<Unit, int> unitToGroupId = new();
    private int nextId = 1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void Update()
    {
        TickOrders();
    }

    public static bool IsFormationEligible(Unit unit)
    {
        if (unit == null || unit.stats == null) return false;
        return unit.stats.role != UnitRole.Worker;
    }

    public FormationGroup GetGroupForUnit(Unit unit)
    {
        if (unit == null) return null;
        if (!unitToGroupId.TryGetValue(unit, out int id)) return null;
        return groupsById.TryGetValue(id, out FormationGroup g) ? g : null;
    }

    public FormationGroup ApplyFormation(IReadOnlyList<Selectable> selection, FormationType type)
    {
        PruneDeadMembers();
        List<Unit> eligible = CollectEligible(selection);
        if (eligible.Count == 0) return null;

        for (int i = 0; i < eligible.Count; i++)
            LeaveGroup(eligible[i]);

        FormationGroup group = new FormationGroup(nextId++);
        group.Type = type;
        group.SetMembers(eligible);
        group.RefreshCenterFromMembers();
        group.Facing = ResolveApplyFacing(eligible);

        groupsById[group.Id] = group;
        for (int i = 0; i < eligible.Count; i++)
            unitToGroupId[eligible[i]] = group.Id;

        Dictionary<Unit, Vector3> slots = FormationLayout.ComputeSlots(
            group.Members, group.Type, group.Center, group.Facing);
        group.UpdateMemberSlots(slots);
        group.CaptureLocalOffsets(slots, group.Center, group.Facing);
        group.SetActiveMovers(eligible);
        group.OrderState = FormationOrderState.Idle;

        foreach (KeyValuePair<Unit, Vector3> kv in slots)
            kv.Key.MoveTo(kv.Value);

        return group;
    }

    /// <summary>
    /// Prefer camera look-on-XZ so formation "front" matches the player's view into the map
    /// (unit transform.forward often faces the camera and flipped the wedge tip).
    /// </summary>
    private static Vector3 ResolveApplyFacing(List<Unit> eligible)
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            Vector3 camFwd = cam.transform.forward;
            camFwd.y = 0f;
            if (camFwd.sqrMagnitude > 0.0001f)
                return camFwd.normalized;
        }

        Vector3 facing = Vector3.zero;
        if (eligible != null)
        {
            for (int i = 0; i < eligible.Count; i++)
            {
                if (eligible[i] == null) continue;
                Vector3 f = eligible[i].transform.forward;
                f.y = 0f;
                facing += f;
            }
        }

        if (facing.sqrMagnitude > 0.0001f)
            return facing.normalized;
        return Vector3.forward;
    }

    public bool TryGetMemberSlot(Unit unit, out Vector3 slot)
    {
        slot = default;
        FormationGroup group = GetGroupForUnit(unit);
        if (group == null) return false;
        return group.TryGetMemberSlot(unit, out slot);
    }

    public bool TryGetSharedGroup(IReadOnlyList<Selectable> selection, out FormationGroup group)
    {
        group = null;
        PruneDeadMembers();
        FormationGroup found = null;
        bool anyEligible = false;

        for (int i = 0; i < selection.Count; i++)
        {
            if (selection[i] is not Unit unit) continue;
            if (!IsFormationEligible(unit)) continue;
            anyEligible = true;
            FormationGroup g = GetGroupForUnit(unit);
            if (g == null) return false;
            if (found == null) found = g;
            else if (found.Id != g.Id) return false;
        }

        if (!anyEligible || found == null) return false;
        group = found;
        return true;
    }

    /// <summary>
    /// Hybrid formation ground move: reform facing in place when turn &gt; 20°, else advance.
    /// Large turns (&gt; 90°) rematch seats by nearest after rotating to avoid cross-centroid collapse.
    /// </summary>
    public Dictionary<Unit, Vector3> IssueFormationMove(
        FormationGroup group,
        Vector3 destination,
        IReadOnlyList<Selectable> selection)
    {
        if (group == null) return new Dictionary<Unit, Vector3>();

        PruneDeadMembers();
        List<Unit> movers = CollectFormationMovers(group, selection);
        if (movers.Count == 0) return new Dictionary<Unit, Vector3>();

        group.ClearOrder();
        group.SetActiveMovers(movers);
        EnsureLocalOffsets(group);

        group.RefreshCenterFromMembers();

        Vector3 desiredFacing = destination - group.Center;
        desiredFacing.y = 0f;
        if (desiredFacing.sqrMagnitude < 0.0001f)
            desiredFacing = group.Facing;
        else
            desiredFacing.Normalize();

        float turn = FormationGroup.FacingAngleDegrees(group.Facing, desiredFacing);
        bool needsFormUp = !AreMoversFormedUp(group, movers);
        if (turn > FormationGroup.FacingTurnThresholdDegrees || needsFormUp)
        {
            group.PendingDestination = destination;
            group.HasPendingDestination = true;
            group.OrderState = FormationOrderState.ReformFacing;
            group.ReformStartedAt = Time.time;
            bool largeTurn = turn > FormationGroup.LargeTurnRelayoutDegrees;
            // Form-up without facing change: keep current facing, only gather to seats.
            Vector3 reformFacing = turn > FormationGroup.FacingTurnThresholdDegrees
                ? desiredFacing
                : group.Facing;
            return IssueReformSlotsAt(group, movers, group.Center, reformFacing, largeTurn);
        }

        return BeginAdvance(group, movers, destination, desiredFacing);
    }

    /// <summary>
    /// Formation attack approach: hold rigid seats toward target until units enter combat range.
    /// </summary>
    public Dictionary<Unit, Vector3> IssueFormationAttackApproach(
        FormationGroup group,
        Selectable target,
        IReadOnlyList<Selectable> selection)
    {
        if (group == null || target == null) return new Dictionary<Unit, Vector3>();

        PruneDeadMembers();
        List<Unit> movers = CollectFormationMovers(group, selection);
        if (movers.Count == 0) return new Dictionary<Unit, Vector3>();

        group.ClearOrder();
        group.SetActiveMovers(movers);
        group.PendingAttackTarget = target;
        EnsureLocalOffsets(group);

        group.RefreshCenterFromMembers();

        Vector3 desiredFacing = target.transform.position - group.Center;
        desiredFacing.y = 0f;
        if (desiredFacing.sqrMagnitude < 0.0001f)
            desiredFacing = group.Facing;
        else
            desiredFacing.Normalize();

        float turn = FormationGroup.FacingAngleDegrees(group.Facing, desiredFacing);
        bool needsFormUp = !AreMoversFormedUp(group, movers);
        if (turn > FormationGroup.FacingTurnThresholdDegrees || needsFormUp)
        {
            group.OrderState = FormationOrderState.ReformFacing;
            group.ReformStartedAt = Time.time;
            bool largeTurn = turn > FormationGroup.LargeTurnRelayoutDegrees;
            Vector3 reformFacing = turn > FormationGroup.FacingTurnThresholdDegrees
                ? desiredFacing
                : group.Facing;
            return IssueReformSlotsAt(group, movers, group.Center, reformFacing, largeTurn);
        }

        return BeginAttackApproach(group, movers, target, desiredFacing);
    }

    private void TickOrders()
    {
        if (groupsById.Count == 0) return;

        PruneDeadMembers();
        var groups = new List<FormationGroup>(groupsById.Values);
        for (int i = 0; i < groups.Count; i++)
        {
            FormationGroup group = groups[i];
            if (group == null) continue;

            switch (group.OrderState)
            {
                case FormationOrderState.ReformFacing:
                    TickReformFacing(group);
                    break;
                case FormationOrderState.AttackApproach:
                    TickAttackApproach(group);
                    break;
                case FormationOrderState.Idle:
                case FormationOrderState.Advance:
                    TickHoldSlots(group);
                    break;
            }
        }
    }

    /// <summary>
    /// After assign / advance, re-issue MoveTo if a member is pushed off its seat.
    /// Skips units that are mid-player order elsewhere (Attacking / Gathering).
    /// </summary>
    private void TickHoldSlots(FormationGroup group)
    {
        if (group == null || !group.HasLocalOffsets) return;

        // Keep world slots aligned with current center/facing for hold checks.
        Dictionary<Unit, Vector3> worldSlots = group.BuildWorldSlotsFromLocalOffsets(
            group.Center, group.Facing);
        group.UpdateMemberSlots(worldSlots);

        float epsSq = FormationGroup.HoldSlotEpsilon * FormationGroup.HoldSlotEpsilon;
        for (int i = 0; i < group.Members.Count; i++)
        {
            Unit unit = group.Members[i];
            if (unit == null || unit.state != SelectableState.Alive) continue;
            if (group.IsEngaged(unit)) continue;

            UnitBehaviourState behaviour = unit.BehaviourState;
            if (behaviour == UnitBehaviourState.Attacking
                || behaviour == UnitBehaviourState.Working)
                continue;

            if (!worldSlots.TryGetValue(unit, out Vector3 slot)) continue;

            float dx = unit.transform.position.x - slot.x;
            float dz = unit.transform.position.z - slot.z;
            if (dx * dx + dz * dz <= epsSq)
                continue;

            // Already pathing toward this seat — avoid MoveTo spam.
            if (unit.IsMovingToward(slot, FormationGroup.HoldSlotEpsilon))
                continue;

            unit.MoveTo(slot);
        }
    }

    private void TickReformFacing(FormationGroup group)
    {
        if (IsReformReady(group))
        {
            List<Unit> movers = SnapshotMovers(group);
            if (group.PendingAttackTarget != null
                && group.PendingAttackTarget.state == SelectableState.Alive)
            {
                Vector3 facing = group.PendingAttackTarget.transform.position - group.Center;
                facing.y = 0f;
                if (facing.sqrMagnitude > 0.0001f)
                    facing.Normalize();
                else
                    facing = group.Facing;
                BeginAttackApproach(group, movers, group.PendingAttackTarget, facing);
            }
            else if (group.HasPendingDestination)
            {
                Vector3 facing = group.PendingDestination - group.Center;
                facing.y = 0f;
                if (facing.sqrMagnitude > 0.0001f)
                    facing.Normalize();
                else
                    facing = group.Facing;
                BeginAdvance(group, movers, group.PendingDestination, facing);
            }
            else
            {
                group.ClearOrder();
            }
        }
    }

    private void TickAttackApproach(FormationGroup group)
    {
        Selectable target = group.PendingAttackTarget;
        if (target == null || target.state != SelectableState.Alive)
        {
            group.ClearOrder();
            return;
        }

        List<Unit> movers = SnapshotMovers(group);
        if (movers.Count == 0)
        {
            group.OrderState = FormationOrderState.Engaged;
            return;
        }

        group.RefreshCenterFromMembers();
        Vector3 memberCentroid = group.Center;

        Vector3 toTarget = target.transform.position - memberCentroid;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude > 0.0001f)
            group.Facing = toTarget.normalized;

        float standoff = EstimateGroupStandoff(movers);
        Vector3 approachCenter = memberCentroid;
        if (toTarget.sqrMagnitude > 0.0001f)
        {
            float dist = toTarget.magnitude;
            float centerDist = Mathf.Max(0.5f, dist - standoff);
            approachCenter = target.transform.position - group.Facing * centerDist;
            approachCenter.y = memberCentroid.y;
        }

        var stillApproaching = new List<Unit>();
        for (int i = 0; i < movers.Count; i++)
        {
            Unit unit = movers[i];
            if (unit == null || group.IsEngaged(unit)) continue;

            float range = unit.GetInteractionRange();
            float dist = Vector3.Distance(unit.transform.position, target.transform.position);
            if (dist <= range)
            {
                if (unit.TryGetComponent(out IAttacker attacker))
                    attacker.Attack(target);
                else
                    unit.ExecuteContextCommand(target);
                group.MarkEngaged(unit);
                continue;
            }

            stillApproaching.Add(unit);
        }

        if (stillApproaching.Count == 0)
        {
            group.Center = memberCentroid;
            group.OrderState = FormationOrderState.Engaged;
            return;
        }

        // Re-issue only when the standoff center moved enough (keeps shape without MoveTo spam).
        bool needReissue = !group.TryGetMemberSlot(stillApproaching[0], out Vector3 prevSlot);
        if (!needReissue)
        {
            Vector3 expected = SlotAt(group, stillApproaching[0], approachCenter, group.Facing);
            float dx = prevSlot.x - expected.x;
            float dz = prevSlot.z - expected.z;
            needReissue = dx * dx + dz * dz > 0.75f * 0.75f;
        }

        if (needReissue)
            IssueRigidSlotsAt(group, stillApproaching, approachCenter, group.Facing);
        else
            group.Center = memberCentroid;
    }

    private static Vector3 SlotAt(FormationGroup group, Unit unit, Vector3 center, Vector3 facing)
    {
        if (unit == null || !group.TryGetLocalOffset(unit, out Vector3 local))
            return center;
        FormationLayout.GetFacingBasis(facing, out Vector3 forward, out Vector3 right);
        return center + right * local.x + forward * local.z;
    }

    private static float EstimateGroupStandoff(List<Unit> movers)
    {
        float maxRange = 2f;
        for (int i = 0; i < movers.Count; i++)
        {
            if (movers[i] == null) continue;
            maxRange = Mathf.Max(maxRange, movers[i].GetInteractionRange());
        }
        return maxRange;
    }

    private static bool IsReformReady(FormationGroup group)
    {
        CountMoversNearSlots(group, out int total, out int near);
        if (total == 0) return true;

        if (near / (float)total >= FormationGroup.ReformReadyRatio)
            return true;

        // Timeout is a stuck-unit fallback only — still require a solid majority.
        if (Time.time - group.ReformStartedAt >= FormationGroup.ReformTimeoutSeconds
            && near / (float)total >= 0.75f)
            return true;

        return false;
    }

    private static bool AreMoversFormedUp(FormationGroup group, List<Unit> movers)
    {
        if (group == null || movers == null || movers.Count == 0) return true;
        if (!group.HasLocalOffsets) return false;

        Dictionary<Unit, Vector3> seats = group.BuildWorldSlotsFromLocalOffsets(
            group.Center, group.Facing);
        float eps = FormationGroup.ReformSlotEpsilon;
        float epsSq = eps * eps;

        for (int i = 0; i < movers.Count; i++)
        {
            Unit unit = movers[i];
            if (unit == null) continue;
            if (!seats.TryGetValue(unit, out Vector3 slot)) return false;
            float dx = unit.transform.position.x - slot.x;
            float dz = unit.transform.position.z - slot.z;
            if (dx * dx + dz * dz > epsSq)
                return false;
        }

        return true;
    }

    private static void CountMoversNearSlots(FormationGroup group, out int total, out int near)
    {
        total = 0;
        near = 0;
        foreach (Unit unit in group.ActiveMovers)
        {
            if (unit == null) continue;
            total++;
            if (!group.TryGetMemberSlot(unit, out Vector3 slot)) continue;
            Vector3 delta = unit.transform.position - slot;
            delta.y = 0f;
            if (delta.magnitude <= FormationGroup.ReformSlotEpsilon)
                near++;
        }
    }

    private Dictionary<Unit, Vector3> BeginAdvance(
        FormationGroup group,
        List<Unit> movers,
        Vector3 destination,
        Vector3 facing)
    {
        group.Facing = facing;
        group.Center = destination;
        group.HasPendingDestination = false;
        group.PendingAttackTarget = null;
        group.OrderState = FormationOrderState.Advance;
        group.SetActiveMovers(movers);
        return IssueRigidSlotsAt(group, movers, destination, facing);
    }

    private Dictionary<Unit, Vector3> BeginAttackApproach(
        FormationGroup group,
        List<Unit> movers,
        Selectable target,
        Vector3 facing)
    {
        group.Facing = facing;
        group.PendingAttackTarget = target;
        group.HasPendingDestination = false;
        group.OrderState = FormationOrderState.AttackApproach;
        // Immediate first approach tick positions
        TickAttackApproach(group);
        return new Dictionary<Unit, Vector3>(group.Members.Count);
    }

    private void EnsureLocalOffsets(FormationGroup group)
    {
        if (group.HasLocalOffsets) return;

        Dictionary<Unit, Vector3> seeded = FormationLayout.ComputeSlots(
            group.Members, group.Type, group.Center, group.Facing);
        group.UpdateMemberSlots(seeded);
        group.CaptureLocalOffsets(seeded, group.Center, group.Facing);
    }

    /// <summary>
    /// In-place facing reform. Small/medium turns: rigid rotate of current seats.
    /// Large turns (&gt;90°): recompute layout facing the new direction and assign by nearest
    /// within role — avoids units crossing the centroid (visual collapse).
    /// </summary>
    private Dictionary<Unit, Vector3> IssueReformSlotsAt(
        FormationGroup group,
        List<Unit> movers,
        Vector3 center,
        Vector3 facing,
        bool largeTurnRelayout)
    {
        Dictionary<Unit, Vector3> assigned;
        if (largeTurnRelayout)
        {
            assigned = FormationLayout.ComputeSlots(
                group.Members, group.Type, center, facing);
        }
        else
        {
            assigned = group.BuildWorldSlotsFromLocalOffsets(center, facing);
        }

        return IssueSlotsAt(group, movers, center, facing, assigned);
    }

    private Dictionary<Unit, Vector3> IssueRigidSlotsAt(
        FormationGroup group,
        List<Unit> movers,
        Vector3 center,
        Vector3 facing)
    {
        Dictionary<Unit, Vector3> slots = group.BuildWorldSlotsFromLocalOffsets(center, facing);
        return IssueSlotsAt(group, movers, center, facing, slots);
    }

    private Dictionary<Unit, Vector3> IssueSlotsAt(
        FormationGroup group,
        List<Unit> movers,
        Vector3 center,
        Vector3 facing,
        Dictionary<Unit, Vector3> slots)
    {
        var snapped = new Dictionary<Unit, Vector3>(slots.Count);
        foreach (KeyValuePair<Unit, Vector3> kv in slots)
        {
            Vector3 p = kv.Value;
            if (NavMesh.SamplePosition(p, out NavMeshHit hit, 3f, NavMesh.AllAreas))
                p = hit.position;
            snapped[kv.Key] = p;
        }

        group.UpdateMemberSlots(snapped);
        group.Center = center;
        group.Facing = facing;

        // Reform bakes seats (rotated ± rematched) so Advance / Hold keep the new layout.
        if (group.OrderState == FormationOrderState.ReformFacing)
            group.CaptureLocalOffsets(snapped, center, facing);

        var moved = new Dictionary<Unit, Vector3>();
        for (int i = 0; i < movers.Count; i++)
        {
            Unit unit = movers[i];
            if (unit == null || group.IsEngaged(unit)) continue;
            if (!snapped.TryGetValue(unit, out Vector3 slot)) continue;
            unit.MoveTo(slot);
            moved[unit] = slot;
        }

        return moved;
    }

    private static List<Unit> SnapshotMovers(FormationGroup group)
    {
        var list = new List<Unit>();
        foreach (Unit u in group.ActiveMovers)
        {
            if (u != null && !group.IsEngaged(u))
                list.Add(u);
        }
        return list;
    }

    private static List<Unit> CollectFormationMovers(
        FormationGroup group,
        IReadOnlyList<Selectable> selection)
    {
        var list = new List<Unit>();
        if (group == null || selection == null) return list;

        int localId = PlayerManager.Instance != null
            ? PlayerManager.Instance.GetLocalPlayerId()
            : 0;

        for (int i = 0; i < selection.Count; i++)
        {
            Selectable sel = selection[i];
            if (sel == null || !sel.IsOwnedBy(localId)) continue;
            if (sel is not Unit unit) continue;
            if (!IsFormationEligible(unit) || !group.Contains(unit)) continue;
            if (!list.Contains(unit))
                list.Add(unit);
        }

        return list;
    }

    public void LeaveGroup(Unit unit)
    {
        if (unit == null) return;
        if (!unitToGroupId.TryGetValue(unit, out int id)) return;
        unitToGroupId.Remove(unit);
        if (!groupsById.TryGetValue(id, out FormationGroup g)) return;
        g.Remove(unit);
        if (g.Members.Count == 0)
            groupsById.Remove(id);
    }

    public void PruneDeadMembers()
    {
        var units = new List<Unit>(unitToGroupId.Keys);
        for (int i = 0; i < units.Count; i++)
        {
            Unit u = units[i];
            if (u == null || u.state != SelectableState.Alive)
                LeaveGroup(u);
        }
    }

    private static List<Unit> CollectEligible(IReadOnlyList<Selectable> selection)
    {
        var list = new List<Unit>();
        if (selection == null) return list;

        int localId = PlayerManager.Instance != null
            ? PlayerManager.Instance.GetLocalPlayerId()
            : 0;

        for (int i = 0; i < selection.Count; i++)
        {
            Selectable sel = selection[i];
            if (sel == null || !sel.IsOwnedBy(localId)) continue;
            if (sel is not Unit unit) continue;
            if (!IsFormationEligible(unit)) continue;
            if (!list.Contains(unit))
                list.Add(unit);
        }

        return list;
    }
}
