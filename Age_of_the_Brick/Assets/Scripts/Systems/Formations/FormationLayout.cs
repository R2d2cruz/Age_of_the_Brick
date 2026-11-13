using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Pure formation slot math. Prefer <see cref="ComputeSlotsById"/> for tests;
/// <see cref="ComputeSlots"/> wraps unit refs and optional NavMesh snapping.
/// </summary>
public static class FormationLayout
{
    public const float DefaultSpacing = 2.0f;
    /// <summary>Line uses slightly wider rank spacing (former Loose look).</summary>
    public const float LineSpacingMultiplier = 1.75f;
    /// <summary>Box/Loose cell spacing multiplier (keeps formations spread out).</summary>
    public const float BoxSpacingMultiplier = 3.5f;

    /// <summary>Builds a flat XZ facing basis (forward + right).</summary>
    public static void GetFacingBasis(Vector3 facing, out Vector3 forward, out Vector3 right)
    {
        forward = facing;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;
        forward.Normalize();
        right = Vector3.Cross(Vector3.up, forward).normalized;
    }

    /// <summary>
    /// Rigidly maps local offsets (x=right, z=forward) into world positions.
    /// </summary>
    public static Dictionary<int, Vector3> ApplyLocalOffsets(
        IReadOnlyDictionary<int, Vector3> localOffsets,
        Vector3 center,
        Vector3 facing)
    {
        var result = new Dictionary<int, Vector3>();
        if (localOffsets == null) return result;

        GetFacingBasis(facing, out Vector3 forward, out Vector3 right);
        foreach (KeyValuePair<int, Vector3> kv in localOffsets)
        {
            Vector3 local = kv.Value;
            result[kv.Key] = center + right * local.x + forward * local.z;
        }

        return result;
    }

    /// <summary>
    /// Converts world slots to local offsets in the given facing basis.
    /// </summary>
    public static Dictionary<int, Vector3> WorldToLocalOffsets(
        IReadOnlyDictionary<int, Vector3> worldSlots,
        Vector3 center,
        Vector3 facing)
    {
        var result = new Dictionary<int, Vector3>();
        if (worldSlots == null) return result;

        GetFacingBasis(facing, out Vector3 forward, out Vector3 right);
        foreach (KeyValuePair<int, Vector3> kv in worldSlots)
        {
            Vector3 delta = kv.Value - center;
            result[kv.Key] = new Vector3(
                Vector3.Dot(delta, right),
                0f,
                Vector3.Dot(delta, forward));
        }

        return result;
    }

    /// <summary>
    /// Computes world slots keyed by member request id. Does not require Unit MonoBehaviours.
    /// </summary>
    public static Dictionary<int, Vector3> ComputeSlotsById(
        IReadOnlyList<FormationMemberRequest> members,
        FormationType type,
        Vector3 center,
        Vector3 facing,
        float spacing = DefaultSpacing,
        bool snapToNavMesh = false)
    {
        var result = new Dictionary<int, Vector3>();
        if (members == null || members.Count == 0)
            return result;

        GetFacingBasis(facing, out Vector3 forward, out Vector3 right);

        // Bucket by layer 0..4 (Worker excluded)
        var layers = new List<FormationMemberRequest>[5];
        for (int i = 0; i < 5; i++)
            layers[i] = new List<FormationMemberRequest>();

        for (int i = 0; i < members.Count; i++)
        {
            FormationMemberRequest m = members[i];
            int layer = RoleToLayer(m.Role);
            if (layer < 0) continue;
            layers[layer].Add(m);
        }

        var orderedLayers = new List<List<FormationMemberRequest>>();
        for (int i = 0; i < 5; i++)
        {
            if (layers[i].Count > 0)
                orderedLayers.Add(layers[i]);
        }

        if (orderedLayers.Count == 0)
            return result;

        switch (type)
        {
            case FormationType.Box:
                PlaceBox(result, OrderForBoxRoles(members), center, forward, right, spacing * BoxSpacingMultiplier);
                break;
            case FormationType.Loose:
                PlaceLoose(result, Flatten(orderedLayers), center, forward, right, spacing * BoxSpacingMultiplier);
                break;
            case FormationType.Wedge:
                PlaceWedge(result, FlattenForWedge(orderedLayers), center, forward, right, spacing);
                break;
            case FormationType.Line:
            default:
                PlaceLine(result, orderedLayers, center, forward, right, spacing * LineSpacingMultiplier);
                break;
        }

        if (snapToNavMesh)
            SnapToNavMesh(result);

        return result;
    }

    /// <summary>
    /// Unit-facing wrapper used by <see cref="FormationManager"/>.
    /// Role rules place provisional seats, then each role rematches units to those seats by nearest world distance.
    /// </summary>
    public static Dictionary<Unit, Vector3> ComputeSlots(
        IReadOnlyList<Unit> members,
        FormationType type,
        Vector3 center,
        Vector3 facing,
        float spacing = DefaultSpacing)
    {
        var provisional = new Dictionary<Unit, Vector3>();
        if (members == null || members.Count == 0)
            return provisional;

        var requests = new List<FormationMemberRequest>(members.Count);
        var idToUnit = new Dictionary<int, Unit>();
        for (int i = 0; i < members.Count; i++)
        {
            Unit u = members[i];
            if (u == null || u.stats == null) continue;
            requests.Add(new FormationMemberRequest(i, u.stats.role));
            idToUnit[i] = u;
        }

        Dictionary<int, Vector3> byId = ComputeSlotsById(
            requests, type, center, facing, spacing, snapToNavMesh: true);

        foreach (KeyValuePair<int, Vector3> kv in byId)
        {
            if (idToUnit.TryGetValue(kv.Key, out Unit unit))
                provisional[unit] = kv.Value;
        }

        return AssignNearestWithinRole(provisional);
    }

    /// <summary>
    /// Within each <see cref="UnitRole"/>, greedily assigns units to provisional seats by nearest XZ distance.
    /// Preserves role banding while reducing path crossings on assign.
    /// </summary>
    public static Dictionary<Unit, Vector3> AssignNearestWithinRole(
        Dictionary<Unit, Vector3> provisional)
    {
        var result = new Dictionary<Unit, Vector3>();
        if (provisional == null || provisional.Count == 0)
            return result;

        var byRole = new Dictionary<UnitRole, List<Unit>>();
        var seatsByRole = new Dictionary<UnitRole, List<Vector3>>();

        foreach (KeyValuePair<Unit, Vector3> kv in provisional)
        {
            Unit unit = kv.Key;
            if (unit == null || unit.stats == null) continue;
            UnitRole role = unit.stats.role;
            if (RoleToLayer(role) < 0) continue;

            if (!byRole.TryGetValue(role, out List<Unit> units))
            {
                units = new List<Unit>();
                byRole[role] = units;
            }
            units.Add(unit);

            if (!seatsByRole.TryGetValue(role, out List<Vector3> seats))
            {
                seats = new List<Vector3>();
                seatsByRole[role] = seats;
            }
            seats.Add(kv.Value);
        }

        foreach (KeyValuePair<UnitRole, List<Unit>> rolePair in byRole)
        {
            List<Unit> units = rolePair.Value;
            List<Vector3> seats = seatsByRole[rolePair.Key];
            GreedyNearestAssign(units, seats, result);
        }

        return result;
    }

    /// <summary>
    /// Repeatedly pairs the globally closest remaining unit–seat (XZ).
    /// </summary>
    public static void GreedyNearestAssign(
        List<Unit> units,
        List<Vector3> seats,
        Dictionary<Unit, Vector3> result)
    {
        if (units == null || seats == null || result == null) return;

        var freeUnits = new List<Unit>(units);
        var freeSeats = new List<Vector3>(seats);

        while (freeUnits.Count > 0 && freeSeats.Count > 0)
        {
            int bestU = 0;
            int bestS = 0;
            float bestDist = float.MaxValue;

            for (int u = 0; u < freeUnits.Count; u++)
            {
                Unit unit = freeUnits[u];
                if (unit == null) continue;
                Vector3 up = unit.transform.position;
                for (int s = 0; s < freeSeats.Count; s++)
                {
                    float dx = up.x - freeSeats[s].x;
                    float dz = up.z - freeSeats[s].z;
                    float d = dx * dx + dz * dz;
                    if (d < bestDist)
                    {
                        bestDist = d;
                        bestU = u;
                        bestS = s;
                    }
                }
            }

            Unit chosen = freeUnits[bestU];
            if (chosen != null)
                result[chosen] = freeSeats[bestS];
            freeUnits.RemoveAt(bestU);
            freeSeats.RemoveAt(bestS);
        }
    }

    public static int RoleToLayer(UnitRole role)
    {
        switch (role)
        {
            case UnitRole.Infantry: return 0;
            case UnitRole.Cavalry: return 1;
            case UnitRole.Ranged: return 2;
            case UnitRole.Siege: return 3;
            case UnitRole.Support: return 4;
            default: return -1;
        }
    }

    /// <summary>
    /// Square grid footprint for Box / Loose: side = ceil(sqrt(count)).
    /// </summary>
    public static void GetBoxGridSize(int count, out int cols, out int rows)
    {
        count = Math.Max(1, count);
        int side = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(count)));
        while (side * side < count)
            side++;
        cols = side;
        rows = side;
    }

    /// <summary>
    /// Sparse square Box: same as square grid (empty cells when count is not a perfect square).
    /// Minimum 2×2 when count ≥ 3 so small groups keep a box silhouette.
    /// </summary>
    public static void GetSparseBoxGridSize(int count, out int cols, out int rows)
    {
        count = Math.Max(1, count);
        GetBoxGridSize(count, out cols, out rows);

        if (count >= 3)
        {
            cols = Math.Max(cols, 2);
            rows = Math.Max(rows, 2);
        }
    }

    // -------------------------------------------------------------------------

    private static void PlaceLine(
        Dictionary<int, Vector3> result,
        List<List<FormationMemberRequest>> orderedLayers,
        Vector3 center,
        Vector3 forward,
        Vector3 right,
        float spacing)
    {
        int rankCount = orderedLayers.Count;
        float depthOrigin = -0.5f * (rankCount - 1) * spacing;

        for (int r = 0; r < rankCount; r++)
        {
            List<FormationMemberRequest> rank = orderedLayers[r];
            float depth = depthOrigin + (rankCount - 1 - r) * spacing;
            int n = rank.Count;
            float lateralOrigin = -0.5f * (n - 1) * spacing;
            for (int i = 0; i < n; i++)
            {
                float lat = lateralOrigin + i * spacing;
                result[rank[i].Id] = center + forward * depth + right * lat;
            }
        }
    }

    private static void PlaceBox(
        Dictionary<int, Vector3> result,
        List<FormationMemberRequest> units,
        Vector3 center,
        Vector3 forward,
        Vector3 right,
        float spacing)
    {
        if (units.Count == 0) return;

        GetSparseBoxGridSize(units.Count, out int cols, out int rows);
        List<(int row, int col)> cells = BuildBoxFillOrder(rows, cols);

        float depthOrigin = 0.5f * (rows - 1) * spacing;
        float latOrigin = -0.5f * (cols - 1) * spacing;

        int assign = Math.Min(units.Count, cells.Count);
        for (int i = 0; i < assign; i++)
        {
            (int row, int col) = cells[i];
            float depth = depthOrigin - row * spacing;
            float lat = latOrigin + col * spacing;
            result[units[i].Id] = center + forward * depth + right * lat;
        }
    }

    /// <summary>
    /// Box role priority: Cavalry → Infantry → Ranged → Siege → Support (stable within role).
    /// </summary>
    private static List<FormationMemberRequest> OrderForBoxRoles(IReadOnlyList<FormationMemberRequest> members)
    {
        var ordered = new List<FormationMemberRequest>();
        if (members == null) return ordered;

        UnitRole[] priority =
        {
            UnitRole.Cavalry,
            UnitRole.Infantry,
            UnitRole.Ranged,
            UnitRole.Siege,
            UnitRole.Support
        };

        for (int p = 0; p < priority.Length; p++)
        {
            UnitRole role = priority[p];
            for (int i = 0; i < members.Count; i++)
            {
                if (RoleToLayer(members[i].Role) < 0) continue;
                if (members[i].Role == role)
                    ordered.Add(members[i]);
            }
        }

        return ordered;
    }

    /// <summary>
    /// Concentric square rings: for each ring, corners first, then walls (edges), then next inner ring.
    /// </summary>
    public static List<(int row, int col)> BuildBoxFillOrder(int rows, int cols)
    {
        var order = new List<(int row, int col)>(rows * cols);
        var used = new bool[rows, cols];

        void TryAdd(int r, int c)
        {
            if (r < 0 || c < 0 || r >= rows || c >= cols) return;
            if (used[r, c]) return;
            used[r, c] = true;
            order.Add((r, c));
        }

        int ringMax = Math.Max(0, (Math.Min(rows, cols) + 1) / 2);
        for (int ring = 0; ring < ringMax; ring++)
        {
            int r0 = ring;
            int r1 = rows - 1 - ring;
            int c0 = ring;
            int c1 = cols - 1 - ring;
            if (r0 > r1 || c0 > c1)
                break;

            // Corners of this ring
            TryAdd(r0, c0);
            if (c1 != c0) TryAdd(r0, c1);
            if (r1 != r0)
            {
                TryAdd(r1, c0);
                if (c1 != c0) TryAdd(r1, c1);
            }

            // Walls (edges excluding corners)
            for (int c = c0 + 1; c < c1; c++)
            {
                TryAdd(r0, c);
                if (r1 != r0) TryAdd(r1, c);
            }
            for (int r = r0 + 1; r < r1; r++)
            {
                TryAdd(r, c0);
                if (c1 != c0) TryAdd(r, c1);
            }

            // Degenerate 1×N / N×1 leftovers inside ring already covered by corners/walls.
            if (r0 == r1 && c0 == c1)
                TryAdd(r0, c0);
        }

        // Safety: any cell not covered (non-square grids)
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
                TryAdd(r, c);
        }

        return order;
    }

    private static void PlaceLoose(
        Dictionary<int, Vector3> result,
        List<FormationMemberRequest> units,
        Vector3 center,
        Vector3 forward,
        Vector3 right,
        float spacing)
    {
        if (units.Count == 0) return;

        GetBoxGridSize(units.Count, out int cols, out int rows);
        float depthOrigin = 0.5f * (rows - 1) * spacing;
        float latOrigin = -0.5f * (cols - 1) * spacing;
        float jitter = spacing * 0.35f;

        for (int i = 0; i < units.Count; i++)
        {
            int row = i / cols;
            int col = i % cols;
            if (row >= rows)
                row = rows - 1;

            float depth = depthOrigin - row * spacing;
            float lat = latOrigin + col * spacing;

            // Deterministic scatter inside the cell
            float jx = (Hash01(units[i].Id, 11) - 0.5f) * 2f * jitter;
            float jz = (Hash01(units[i].Id, 29) - 0.5f) * 2f * jitter;

            result[units[i].Id] = center + forward * (depth + jz) + right * (lat + jx);
        }
    }

    /// <summary>
    /// Hollow V pointing along <paramref name="forward"/> (tip leads, arms open rearward).
    /// Depth uses +forward as the front of the formation (same convention as Line ranks).
    /// </summary>
    private static void PlaceWedge(
        Dictionary<int, Vector3> result,
        List<FormationMemberRequest> units,
        Vector3 center,
        Vector3 forward,
        Vector3 right,
        float spacing)
    {
        if (units.Count == 0) return;

        // Tip leads in the facing direction; arms step backward and outward (open V).
        int armCount = (units.Count - 1 + 1) / 2; // pairs behind tip
        float tipDepth = armCount * spacing * 0.5f;

        result[units[0].Id] = center + forward * tipDepth;

        for (int i = 1; i < units.Count; i++)
        {
            int arm = (i + 1) / 2; // 1,1,2,2,3,3...
            int side = (i % 2 == 1) ? -1 : 1;
            float depth = tipDepth - arm * spacing;
            float lat = side * arm * spacing;
            result[units[i].Id] = center + forward * depth + right * lat;
        }
    }

    private static List<FormationMemberRequest> Flatten(List<List<FormationMemberRequest>> orderedLayers)
    {
        var flat = new List<FormationMemberRequest>();
        for (int i = 0; i < orderedLayers.Count; i++)
            flat.AddRange(orderedLayers[i]);
        return flat;
    }

    private static List<FormationMemberRequest> FlattenForWedge(List<List<FormationMemberRequest>> orderedLayers)
    {
        var flat = new List<FormationMemberRequest>();
        int cavalryIdx = -1;
        int infantryIdx = -1;
        for (int i = 0; i < orderedLayers.Count; i++)
        {
            List<FormationMemberRequest> layer = orderedLayers[i];
            if (layer.Count == 0) continue;
            if (layer[0].Role == UnitRole.Cavalry && cavalryIdx < 0)
                cavalryIdx = i;
            else if (layer[0].Role == UnitRole.Infantry && infantryIdx < 0)
                infantryIdx = i;
        }

        if (cavalryIdx >= 0)
            flat.AddRange(orderedLayers[cavalryIdx]);
        if (infantryIdx >= 0 && infantryIdx != cavalryIdx)
            flat.AddRange(orderedLayers[infantryIdx]);

        for (int i = 0; i < orderedLayers.Count; i++)
        {
            if (i == cavalryIdx || i == infantryIdx) continue;
            flat.AddRange(orderedLayers[i]);
        }

        return flat;
    }

    private static void SnapToNavMesh(Dictionary<int, Vector3> slots)
    {
        var keys = new List<int>(slots.Keys);
        for (int i = 0; i < keys.Count; i++)
        {
            int id = keys[i];
            Vector3 p = slots[id];
            if (NavMesh.SamplePosition(p, out NavMeshHit hit, 3f, NavMesh.AllAreas))
                slots[id] = hit.position;
        }
    }

    private static float Hash01(int a, int salt)
    {
        unchecked
        {
            uint x = (uint)(a * 73856093) ^ (uint)(salt * 19349663);
            x ^= x >> 13;
            x *= 1274126177u;
            return (x & 0xFFFFFF) / (float)0xFFFFFF;
        }
    }
}
