using System.Collections.Generic;
using UnityEngine;

public sealed class FormationGroup
{
    public const float FacingTurnThresholdDegrees = 20f;
    /// <summary>
    /// Above this turn, reform recomputes layout + nearest-within-role seats
    /// so a ~180° flip does not send everyone across the centroid (collapse).
    /// </summary>
    public const float LargeTurnRelayoutDegrees = 90f;
    public const float ReformSlotEpsilon = 2.25f;
    public const float HoldSlotEpsilon = 1.25f;
    /// <summary>Safety only — reform prefers waiting until all movers are on seat.</summary>
    public const float ReformTimeoutSeconds = 10f;
    /// <summary>Fraction of movers that must be on seat before Advance (1 = everyone).</summary>
    public const float ReformReadyRatio = 1f;

    public int Id { get; }
    public FormationType Type { get; set; }
    public Vector3 Center { get; set; }
    public Vector3 Facing { get; set; } // XZ-normalized; Y = 0

    public FormationOrderState OrderState { get; set; } = FormationOrderState.Idle;
    public Vector3 PendingDestination { get; set; }
    public Selectable PendingAttackTarget { get; set; }
    public float ReformStartedAt { get; set; }
    public bool HasPendingDestination { get; set; }

    private readonly List<Unit> members = new();
    private readonly Dictionary<Unit, Vector3> memberSlots = new();
    /// <summary>Local slot: x = right, z = forward, relative to Center/Facing when captured.</summary>
    private readonly Dictionary<Unit, Vector3> localOffsets = new();
    private readonly HashSet<Unit> activeMovers = new();
    private readonly HashSet<Unit> engagedUnits = new();

    public FormationGroup(int id)
    {
        Id = id;
        Facing = Vector3.forward;
    }

    public IReadOnlyList<Unit> Members => members;

    public bool HasLocalOffsets => localOffsets.Count > 0;

    public IReadOnlyCollection<Unit> ActiveMovers => activeMovers;

    public void SetMembers(List<Unit> units)
    {
        members.Clear();
        memberSlots.Clear();
        localOffsets.Clear();
        ClearOrder();
        if (units == null) return;
        for (int i = 0; i < units.Count; i++)
        {
            if (units[i] != null)
                members.Add(units[i]);
        }
    }

    public bool Remove(Unit unit)
    {
        memberSlots.Remove(unit);
        localOffsets.Remove(unit);
        activeMovers.Remove(unit);
        engagedUnits.Remove(unit);
        return members.Remove(unit);
    }

    public bool Contains(Unit unit) => members.Contains(unit);

    public void UpdateMemberSlots(Dictionary<Unit, Vector3> slots)
    {
        if (slots == null) return;
        foreach (KeyValuePair<Unit, Vector3> kv in slots)
        {
            if (kv.Key != null)
                memberSlots[kv.Key] = kv.Value;
        }
    }

    public bool TryGetMemberSlot(Unit unit, out Vector3 slot) =>
        memberSlots.TryGetValue(unit, out slot);

    public bool TryGetLocalOffset(Unit unit, out Vector3 local) =>
        localOffsets.TryGetValue(unit, out local);

    public void SetActiveMovers(IReadOnlyList<Unit> movers)
    {
        activeMovers.Clear();
        engagedUnits.Clear();
        if (movers == null) return;
        for (int i = 0; i < movers.Count; i++)
        {
            if (movers[i] != null)
                activeMovers.Add(movers[i]);
        }
    }

    public bool IsEngaged(Unit unit) => engagedUnits.Contains(unit);

    public void MarkEngaged(Unit unit)
    {
        if (unit == null) return;
        engagedUnits.Add(unit);
        activeMovers.Remove(unit);
    }

    public int EngagedCount => engagedUnits.Count;

    public void ClearOrder()
    {
        OrderState = FormationOrderState.Idle;
        PendingDestination = Vector3.zero;
        PendingAttackTarget = null;
        HasPendingDestination = false;
        ReformStartedAt = 0f;
        activeMovers.Clear();
        engagedUnits.Clear();
    }

    /// <summary>
    /// Stores each unit's slot in the current facing basis so later moves can
    /// translate/rotate without reassignment.
    /// </summary>
    public void CaptureLocalOffsets(Dictionary<Unit, Vector3> worldSlots, Vector3 center, Vector3 facing)
    {
        localOffsets.Clear();
        if (worldSlots == null) return;

        FormationLayout.GetFacingBasis(facing, out Vector3 forward, out Vector3 right);

        foreach (KeyValuePair<Unit, Vector3> kv in worldSlots)
        {
            if (kv.Key == null) continue;
            Vector3 delta = kv.Value - center;
            localOffsets[kv.Key] = new Vector3(
                Vector3.Dot(delta, right),
                0f,
                Vector3.Dot(delta, forward));
        }
    }

    /// <summary>
    /// Rebuilds world slots from stored local offsets at a new center/facing (rigid formation).
    /// </summary>
    public Dictionary<Unit, Vector3> BuildWorldSlotsFromLocalOffsets(Vector3 center, Vector3 facing)
    {
        var result = new Dictionary<Unit, Vector3>();
        FormationLayout.GetFacingBasis(facing, out Vector3 forward, out Vector3 right);

        foreach (KeyValuePair<Unit, Vector3> kv in localOffsets)
        {
            if (kv.Key == null) continue;
            Vector3 local = kv.Value;
            result[kv.Key] = center + right * local.x + forward * local.z;
        }

        return result;
    }

    public void RefreshCenterFromMembers()
    {
        if (members.Count == 0)
        {
            Center = Vector3.zero;
            return;
        }

        Vector3 sum = Vector3.zero;
        int count = 0;
        for (int i = 0; i < members.Count; i++)
        {
            Unit u = members[i];
            if (u == null) continue;
            sum += u.transform.position;
            count++;
        }
        Center = count > 0 ? sum / count : Vector3.zero;
    }

    public static float FacingAngleDegrees(Vector3 fromFacing, Vector3 toFacing)
    {
        Vector3 a = fromFacing;
        a.y = 0f;
        Vector3 b = toFacing;
        b.y = 0f;
        if (a.sqrMagnitude < 0.0001f || b.sqrMagnitude < 0.0001f)
            return 0f;
        return Vector3.Angle(a.normalized, b.normalized);
    }
}
