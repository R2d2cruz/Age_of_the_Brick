using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pure helpers for tech target matching and flat modifier application.
/// </summary>
public static class TechStatApply
{
    private static bool s_loggedZeroMultiplierFactor;

    public static bool MatchesUnit(TechSO tech, UnitStats unitStats)
    {
        if (tech == null || unitStats == null) return false;

        bool roleOk = tech.targetUnitRoles == null || tech.targetUnitRoles.Count == 0
            || tech.targetUnitRoles.Contains(unitStats.role);
        bool typeOk = tech.targetUnitStats == null || tech.targetUnitStats.Count == 0
            || tech.targetUnitStats.Contains(unitStats);
        return roleOk && typeOk;
    }

    public static bool MatchesBuilding(TechSO tech, BuildingStats buildingStats)
    {
        if (tech == null || buildingStats == null) return false;
        return tech.targetBuildingStats == null || tech.targetBuildingStats.Count == 0
            || tech.targetBuildingStats.Contains(buildingStats);
    }

    public static EraStats CloneEraStats(EraStats source)
    {
        if (source == null) return null;
        return new EraStats
        {
            image = source.image,
            health = source.health,
            lightDefense = source.lightDefense,
            heavyDefense = source.heavyDefense,
            visionRange = source.visionRange,
            moveSpeed = source.moveSpeed,
            range = source.range,
            attackSpeed = source.attackSpeed,
            lightAttack = source.lightAttack,
            heavyAttack = source.heavyAttack,
            buildingMultiplier = source.buildingMultiplier,
            projectileTrackTime = source.projectileTrackTime,
            projectileSpeed = source.projectileSpeed,
            healSpeed = source.healSpeed,
            buildSpeed = source.buildSpeed,
            buildHealth = source.buildHealth,
            gatherSpeed = source.gatherSpeed
        };
    }

    public static EraStatsBuilding CloneEraStatsBuilding(EraStatsBuilding source)
    {
        if (source == null) return null;
        return new EraStatsBuilding
        {
            image = source.image,
            health = source.health,
            lightDefense = source.lightDefense,
            heavyDefense = source.heavyDefense,
            visionRange = source.visionRange,
            gridSize = source.gridSize,
            garrisonCapacity = source.garrisonCapacity,
            attackRange = source.attackRange,
            lightAttack = source.lightAttack,
            heavyAttack = source.heavyAttack
        };
    }

    public static void ApplyUnitModifiers(EraStats target, UnitStatModifiers mods)
    {
        if (target == null || mods == null) return;
        target.health += mods.health;
        target.lightDefense += mods.lightDefense;
        target.heavyDefense += mods.heavyDefense;
        target.visionRange += mods.visionRange;
        target.moveSpeed += mods.moveSpeed;
        target.range += mods.range;
        target.attackSpeed += mods.attackSpeed;
        target.lightAttack += mods.lightAttack;
        target.heavyAttack += mods.heavyAttack;
        target.buildingMultiplier += mods.buildingMultiplier;
        target.projectileTrackTime += mods.projectileTrackTime;
        target.projectileSpeed += mods.projectileSpeed;
        target.healSpeed += mods.healSpeed;
        target.buildSpeed += mods.buildSpeed;
        target.buildHealth += mods.buildHealth;
        target.gatherSpeed += mods.gatherSpeed;
    }

    public static void ApplyBuildingModifiers(EraStatsBuilding target, BuildingStatModifiers mods)
    {
        if (target == null || mods == null) return;
        target.health += mods.health;
        target.lightDefense += mods.lightDefense;
        target.heavyDefense += mods.heavyDefense;
        target.visionRange += mods.visionRange;
        target.garrisonCapacity += mods.garrisonCapacity;
        target.attackRange += mods.attackRange;
        target.lightAttack += mods.lightAttack;
        target.heavyAttack += mods.heavyAttack;
    }

    /// <summary>
    /// Multiplies numeric fields on <paramref name="target"/> by per-field factors.
    /// Identity factors (~1) are skipped. Legacy unset factors (~0) are treated as identity.
    /// Int fields are floored; results are clamped to non-negative values.
    /// </summary>
    public static void ApplyUnitMultipliers(EraStats target, UnitStatMultipliers mults)
    {
        if (target == null || mults == null) return;
        target.health = ScaleInt(target.health, mults.health);
        target.lightDefense = ScaleInt(target.lightDefense, mults.lightDefense);
        target.heavyDefense = ScaleInt(target.heavyDefense, mults.heavyDefense);
        target.visionRange = ScaleInt(target.visionRange, mults.visionRange);
        target.moveSpeed = ScaleFloat(target.moveSpeed, mults.moveSpeed);
        target.range = ScaleFloat(target.range, mults.range);
        target.attackSpeed = ScaleFloat(target.attackSpeed, mults.attackSpeed);
        target.lightAttack = ScaleInt(target.lightAttack, mults.lightAttack);
        target.heavyAttack = ScaleInt(target.heavyAttack, mults.heavyAttack);
        target.buildingMultiplier = ScaleInt(target.buildingMultiplier, mults.buildingMultiplier);
        target.projectileTrackTime = ScaleFloat(target.projectileTrackTime, mults.projectileTrackTime);
        target.projectileSpeed = ScaleFloat(target.projectileSpeed, mults.projectileSpeed);
        target.healSpeed = ScaleFloat(target.healSpeed, mults.healSpeed);
        target.buildSpeed = ScaleFloat(target.buildSpeed, mults.buildSpeed);
        target.buildHealth = ScaleFloat(target.buildHealth, mults.buildHealth);
        target.gatherSpeed = ScaleFloat(target.gatherSpeed, mults.gatherSpeed);
    }

    /// <summary>
    /// Multiplies numeric fields on <paramref name="target"/> by per-field factors.
    /// Identity factors (~1) are skipped. Legacy unset factors (~0) are treated as identity.
    /// Int fields are floored; results are clamped to non-negative values.
    /// </summary>
    public static void ApplyBuildingMultipliers(EraStatsBuilding target, BuildingStatMultipliers mults)
    {
        if (target == null || mults == null) return;
        target.health = ScaleInt(target.health, mults.health);
        target.lightDefense = ScaleInt(target.lightDefense, mults.lightDefense);
        target.heavyDefense = ScaleInt(target.heavyDefense, mults.heavyDefense);
        target.visionRange = ScaleInt(target.visionRange, mults.visionRange);
        target.garrisonCapacity = ScaleInt(target.garrisonCapacity, mults.garrisonCapacity);
        target.attackRange = ScaleFloat(target.attackRange, mults.attackRange);
        target.lightAttack = ScaleInt(target.lightAttack, mults.lightAttack);
        target.heavyAttack = ScaleInt(target.heavyAttack, mults.heavyAttack);
    }

    private static int ScaleInt(int value, float factor)
    {
        if (Mathf.Approximately(factor, 1f)) return value;
        // 0 means unset/legacy YAML; intentional zero-scale should use a tiny epsilon.
        if (Mathf.Approximately(factor, 0f))
        {
            LogZeroMultiplierFactorOnce();
            return value;
        }

        return Mathf.Max(0, Mathf.FloorToInt(value * factor));
    }

    private static float ScaleFloat(float value, float factor)
    {
        if (Mathf.Approximately(factor, 1f)) return value;
        // 0 means unset/legacy YAML; intentional zero-scale should use a tiny epsilon.
        if (Mathf.Approximately(factor, 0f))
        {
            LogZeroMultiplierFactorOnce();
            return value;
        }

        return Mathf.Max(0f, value * factor);
    }

    private static void LogZeroMultiplierFactorOnce()
    {
        if (s_loggedZeroMultiplierFactor) return;
        s_loggedZeroMultiplierFactor = true;
        Debug.LogWarning(
            "TechStatApply: multiplier factor ~0 treated as identity (unset/legacy asset). " +
            "Use a tiny epsilon for intentional zero-scale; prefer setting factors to 1 on TechSO assets.");
    }
}
