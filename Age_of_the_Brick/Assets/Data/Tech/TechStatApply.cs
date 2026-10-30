using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pure helpers for tech target matching and flat modifier application.
/// </summary>
public static class TechStatApply
{
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
}
