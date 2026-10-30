using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// EditMode tests for pure formation slot math (no scene / NavMesh required).
/// </summary>
public class FormationLayoutTests
{
    const float Spacing = 2f;
    static readonly Vector3 Center = Vector3.zero;
    static readonly Vector3 Facing = Vector3.forward;

    static List<FormationMemberRequest> SameRole(int count, UnitRole role = UnitRole.Infantry)
    {
        var list = new List<FormationMemberRequest>(count);
        for (int i = 0; i < count; i++)
            list.Add(new FormationMemberRequest(i, role));
        return list;
    }

    static Dictionary<int, Vector3> Slots(FormationType type, int count, UnitRole role = UnitRole.Infantry)
    {
        return FormationLayout.ComputeSlotsById(
            SameRole(count, role), type, Center, Facing, Spacing, snapToNavMesh: false);
    }

    static float Lateral(Vector3 p) => Vector3.Dot(p - Center, Vector3.right);
    static float Depth(Vector3 p) => Vector3.Dot(p - Center, Vector3.forward);

    // -------------------------------------------------------------------------
    // Line — wide ranks (former Loose / PlaceLine behavior)
    // -------------------------------------------------------------------------

    [Test]
    public void Line_PlacesUnitsInSingleWideRank_WhenSameRole()
    {
        var slots = Slots(FormationType.Line, 5);
        float step = Spacing * FormationLayout.LineSpacingMultiplier;

        Assert.AreEqual(5, slots.Count);
        float[] laterals = slots.Values.Select(Lateral).OrderBy(x => x).ToArray();
        for (int i = 1; i < laterals.Length; i++)
            Assert.AreEqual(step, laterals[i] - laterals[i - 1], 0.01f);

        float depthSpan = slots.Values.Max(Depth) - slots.Values.Min(Depth);
        Assert.Less(depthSpan, step * 0.5f, "Same-role Line should be one rank (shallow depth).");
    }

    [Test]
    public void Line_PutsInfantryAheadOfRanged()
    {
        var members = new List<FormationMemberRequest>
        {
            new(0, UnitRole.Ranged),
            new(1, UnitRole.Infantry),
            new(2, UnitRole.Ranged),
        };
        var slots = FormationLayout.ComputeSlotsById(
            members, FormationType.Line, Center, Facing, Spacing, false);

        Assert.Greater(Depth(slots[1]), Depth(slots[0]) - 0.01f);
        Assert.Greater(Depth(slots[1]), Depth(slots[2]) - 0.01f);
    }

    [Test]
    public void Line_PutsInfantryAheadOfCavalry()
    {
        var members = new List<FormationMemberRequest>
        {
            new(0, UnitRole.Cavalry),
            new(1, UnitRole.Infantry),
            new(2, UnitRole.Cavalry),
        };
        var slots = FormationLayout.ComputeSlotsById(
            members, FormationType.Line, Center, Facing, Spacing, false);

        Assert.Greater(Depth(slots[1]), Depth(slots[0]) - 0.01f);
        Assert.Greater(Depth(slots[1]), Depth(slots[2]) - 0.01f);
    }

    // -------------------------------------------------------------------------
    // Loose — scatter inside the Box footprint
    // -------------------------------------------------------------------------

    [Test]
    public void Loose_KeepsAllPointsInsideBoxFootprint()
    {
        const int n = 9;
        var box = Slots(FormationType.Box, n);
        var loose = Slots(FormationType.Loose, n);

        Bounds boxBounds = BoundsOf(box.Values);
        // Loose uses in-cell jitter up to ~0.35 * spacing on each axis
        boxBounds.Expand(Spacing * 0.75f);

        foreach (Vector3 p in loose.Values)
            Assert.IsTrue(boxBounds.Contains(p), $"Loose point {p} outside Box footprint {boxBounds}");
    }

    [Test]
    public void Loose_IsNotACollinearLine()
    {
        var loose = Slots(FormationType.Loose, 8);
        float depthSpan = loose.Values.Max(Depth) - loose.Values.Min(Depth);
        float latSpan = loose.Values.Max(Lateral) - loose.Values.Min(Lateral);

        Assert.Greater(depthSpan, Spacing * 0.75f, "Loose should spread in depth, not a single line.");
        Assert.Greater(latSpan, Spacing * 1.5f, "Loose should spread laterally.");
    }

    // -------------------------------------------------------------------------
    // Box — wider grid; corners → edges → center fill order
    // -------------------------------------------------------------------------

    [Test]
    public void Box_IsSquareFootprint_UnlikeFlatLine()
    {
        const int n = 9;
        var line = Slots(FormationType.Line, n);
        var box = Slots(FormationType.Box, n);

        float lineDepth = line.Values.Max(Depth) - line.Values.Min(Depth);
        float boxDepth = box.Values.Max(Depth) - box.Values.Min(Depth);
        float boxLat = box.Values.Max(Lateral) - box.Values.Min(Lateral);
        float boxCell = Spacing * FormationLayout.BoxSpacingMultiplier;

        Assert.Less(lineDepth, Spacing * FormationLayout.LineSpacingMultiplier * 0.5f);
        Assert.Greater(boxDepth, boxCell * 0.9f, "Box must have depth (not a flat line).");
        Assert.AreEqual(boxDepth, boxLat, boxCell * 0.05f, "Box footprint should be square.");
    }

    [Test]
    public void Box_AndLoose_AreMuchLargerThanDefaultSpacing()
    {
        var box = Slots(FormationType.Box, 5);
        float boxLat = box.Values.Max(Lateral) - box.Values.Min(Lateral);
        // 3×3 sparse square → lateral span = 2 * cell
        Assert.Greater(boxLat, Spacing * 4f, "Box with 5 units should feel spread out.");
    }

    [Test]
    public void Box_GridSize_IsSquare()
    {
        FormationLayout.GetSparseBoxGridSize(9, out int cols9, out int rows9);
        Assert.AreEqual(3, cols9);
        Assert.AreEqual(3, rows9);

        FormationLayout.GetSparseBoxGridSize(5, out int cols5, out int rows5);
        Assert.AreEqual(cols5, rows5);
        Assert.AreEqual(3, cols5);
    }

    [Test]
    public void Box_FillOrder_OuterCornersThenWallsThenInnerRing()
    {
        FormationLayout.GetBoxGridSize(9, out int cols, out int rows);
        Assert.AreEqual(3, cols);
        Assert.AreEqual(3, rows);
        List<(int row, int col)> order = FormationLayout.BuildBoxFillOrder(rows, cols);

        Assert.GreaterOrEqual(order.Count, 9);

        // First four cells must be the four outer corners.
        var firstFour = order.Take(4).ToHashSet();
        var corners = new HashSet<(int row, int col)>
        {
            (0, 0),
            (0, cols - 1),
            (rows - 1, 0),
            (rows - 1, cols - 1)
        };

        Assert.IsTrue(corners.SetEquals(firstFour), "Outer ring corners must fill first.");

        // Center of 3×3 is the inner ring and must come after outer perimeter (4 corners + 4 edges = 8).
        int interiorIndex = order.FindIndex(c => c.row == 1 && c.col == 1);
        Assert.GreaterOrEqual(interiorIndex, 8, "Inner cell should fill after outer ring.");
    }

    [Test]
    public void Box_Sparse_ThreeInfantry_HasDepthAndLateralSpan_NotPackedLine()
    {
        var box = Slots(FormationType.Box, 3);
        float depthSpan = box.Values.Max(Depth) - box.Values.Min(Depth);
        float latSpan = box.Values.Max(Lateral) - box.Values.Min(Lateral);
        float cell = Spacing * FormationLayout.BoxSpacingMultiplier;

        Assert.Greater(depthSpan, cell * 0.9f, "Sparse Box with 3 units must have depth (not a single rank).");
        Assert.Greater(latSpan, cell * 0.9f, "Sparse Box with 3 units must have lateral span.");
    }

    [Test]
    public void Box_PrefersCavalryAtCorners_OverInfantry()
    {
        // 1 C + 3 I on a sparse grid: first fill seats are corners → C must occupy a corner.
        var members = new List<FormationMemberRequest>
        {
            new(0, UnitRole.Infantry),
            new(1, UnitRole.Infantry),
            new(2, UnitRole.Cavalry),
            new(3, UnitRole.Infantry),
        };
        var slots = FormationLayout.ComputeSlotsById(
            members, FormationType.Box, Center, Facing, Spacing, false);

        FormationLayout.GetSparseBoxGridSize(members.Count, out int cols, out int rows);
        float cell = Spacing * FormationLayout.BoxSpacingMultiplier;
        float depthOrigin = 0.5f * (rows - 1) * cell;
        float latOrigin = -0.5f * (cols - 1) * cell;

        bool IsCorner(Vector3 p)
        {
            float d = Depth(p);
            float l = Lateral(p);
            bool frontOrBack =
                Mathf.Abs(d - depthOrigin) < 0.05f
                || Mathf.Abs(d - (depthOrigin - (rows - 1) * cell)) < 0.05f;
            bool leftOrRight =
                Mathf.Abs(l - latOrigin) < 0.05f
                || Mathf.Abs(l - (latOrigin + (cols - 1) * cell)) < 0.05f;
            return frontOrBack && leftOrRight;
        }

        Assert.IsTrue(IsCorner(slots[2]), "Cavalry should take a corner seat before Infantry.");
    }

    // -------------------------------------------------------------------------
    // Wedge — V tip forward, arms opening rearward (not a filled diamond)
    // -------------------------------------------------------------------------

    [Test]
    public void Wedge_HasSingleForwardTip()
    {
        var slots = Slots(FormationType.Wedge, 7);
        float maxDepth = slots.Values.Max(Depth);
        int atTip = slots.Values.Count(p => Mathf.Abs(Depth(p) - maxDepth) < 0.05f);

        Assert.AreEqual(1, atTip, "Wedge should have exactly one tip unit at the front.");
    }

    [Test]
    public void Wedge_ArmsWidenRearward()
    {
        var slots = Slots(FormationType.Wedge, 7);
        float tipDepth = slots.Values.Max(Depth);

        float WidthAt(float depthBand)
        {
            var band = slots.Values.Where(p => Mathf.Abs(Depth(p) - depthBand) < Spacing * 0.35f).ToList();
            if (band.Count < 2) return 0f;
            return band.Max(Lateral) - band.Min(Lateral);
        }

        float rearDepth = slots.Values.Min(Depth);
        float tipWidth = WidthAt(tipDepth);
        float rearWidth = WidthAt(rearDepth);

        Assert.Less(tipWidth, Spacing * 0.5f, "Tip rank should be narrow.");
        Assert.Greater(rearWidth, tipWidth + Spacing * 0.5f, "Rear of V should be wider than tip.");
    }

    [Test]
    public void Wedge_IsHollowNotFilledDiamond()
    {
        var slots = Slots(FormationType.Wedge, 9);
        // Interior near centroid should be empty for a hollow V
        int nearCenter = slots.Values.Count(p => p.magnitude < Spacing * 0.6f);
        Assert.AreEqual(0, nearCenter, "Hollow V should not pack units into the geometric center.");
    }

    [Test]
    public void Wedge_PrefersCavalryAtTip_OverInfantry()
    {
        var members = new List<FormationMemberRequest>
        {
            new(0, UnitRole.Infantry),
            new(1, UnitRole.Cavalry),
            new(2, UnitRole.Infantry),
            new(3, UnitRole.Ranged),
        };
        var slots = FormationLayout.ComputeSlotsById(
            members, FormationType.Wedge, Center, Facing, Spacing, false);

        float tipDepth = slots.Values.Max(Depth);
        Assert.AreEqual(tipDepth, Depth(slots[1]), 0.05f, "Cavalry should occupy the wedge tip.");
    }

    // -------------------------------------------------------------------------
    // Move preservation helper — offsets stable under re-layout of full group
    // -------------------------------------------------------------------------

    [Test]
    public void RigidLocalOffsets_PreserveSeats_WhenMovingAndRotating()
    {
        var atOrigin = Slots(FormationType.Wedge, 5);
        var local = FormationLayout.WorldToLocalOffsets(atOrigin, Center, Facing);

        Vector3 newCenter = new Vector3(10f, 0f, 20f);
        Vector3 newFacing = Vector3.right;
        var moved = FormationLayout.ApplyLocalOffsets(local, newCenter, newFacing);

        // Same unit keeps the same relative neighbor distances (rigid body)
        for (int a = 0; a < 5; a++)
        {
            for (int b = a + 1; b < 5; b++)
            {
                float d0 = Vector3.Distance(atOrigin[a], atOrigin[b]);
                float d1 = Vector3.Distance(moved[a], moved[b]);
                Assert.AreEqual(d0, d1, 0.05f);
            }
        }

        // Tip (id 0) must still lead along the new facing
        float tipDepth = Vector3.Dot(moved[0] - newCenter, newFacing.normalized);
        for (int i = 1; i < 5; i++)
        {
            float d = Vector3.Dot(moved[i] - newCenter, newFacing.normalized);
            Assert.Greater(tipDepth, d - 0.01f, "Tip seat must remain the forward-most after rigid move.");
        }
    }

    [Test]
    public void RoleToLayer_ExcludesWorker()
    {
        Assert.AreEqual(-1, FormationLayout.RoleToLayer(UnitRole.Worker));
        Assert.AreEqual(0, FormationLayout.RoleToLayer(UnitRole.Infantry));
    }

    // ---- helpers -------------------------------------------------------------

    static Bounds BoundsOf(IEnumerable<Vector3> points)
    {
        var arr = points.ToArray();
        var b = new Bounds(arr[0], Vector3.zero);
        for (int i = 1; i < arr.Length; i++)
            b.Encapsulate(arr[i]);
        return b;
    }
}
