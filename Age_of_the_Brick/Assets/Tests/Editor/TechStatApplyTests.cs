using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class TechStatApplyTests
{
    [Test]
    public void MatchesUnit_WhenBothFiltersEmpty_ReturnsTrue()
    {
        var tech = ScriptableObject.CreateInstance<TechSO>();
        var unitStats = ScriptableObject.CreateInstance<UnitStats>();
        unitStats.role = UnitRole.Frontline;

        Assert.IsTrue(TechStatApply.MatchesUnit(tech, unitStats));
    }

    [Test]
    public void MatchesUnit_WhenRoleFilterExcludes_ReturnsFalse()
    {
        var tech = ScriptableObject.CreateInstance<TechSO>();
        tech.targetUnitRoles = new List<UnitRole> { UnitRole.Skirmisher };
        var unitStats = ScriptableObject.CreateInstance<UnitStats>();
        unitStats.role = UnitRole.Frontline;

        Assert.IsFalse(TechStatApply.MatchesUnit(tech, unitStats));
    }

    [Test]
    public void ApplyUnitModifiers_AddsFlatLightAttack()
    {
        var baseStats = new EraStats { lightAttack = 5, health = 100 };
        var mods = new UnitStatModifiers { lightAttack = 2 };
        TechStatApply.ApplyUnitModifiers(baseStats, mods);
        Assert.AreEqual(7, baseStats.lightAttack);
        Assert.AreEqual(100, baseStats.health);
    }

    [Test]
    public void CloneEraStats_DoesNotAliasSource()
    {
        var source = new EraStats { lightAttack = 3, moveSpeed = 4f };
        var copy = TechStatApply.CloneEraStats(source);
        copy.lightAttack = 99;
        Assert.AreEqual(3, source.lightAttack);
    }

    [Test]
    public void ApplyUnitMultipliers_MultipliesAfterFlatSemantics_FloorsInts()
    {
        var stats = new EraStats { lightAttack = 12, gatherSpeed = 10f };
        var mults = new UnitStatMultipliers { lightAttack = 1.1f, gatherSpeed = 1.1f };
        TechStatApply.ApplyUnitMultipliers(stats, mults);
        Assert.AreEqual(13, stats.lightAttack);
        Assert.AreEqual(11f, stats.gatherSpeed, 0.0001f);
    }

    [Test]
    public void ApplyUnitMultipliers_StacksMultiplicatively()
    {
        var stats = new EraStats { gatherSpeed = 10f };
        TechStatApply.ApplyUnitMultipliers(stats, new UnitStatMultipliers { gatherSpeed = 1.1f });
        TechStatApply.ApplyUnitMultipliers(stats, new UnitStatMultipliers { gatherSpeed = 1.2f });
        Assert.AreEqual(13.2f, stats.gatherSpeed, 0.0001f);
    }

    [Test]
    public void ApplyUnitMultipliers_IdentityDoesNotChange()
    {
        var stats = new EraStats { lightAttack = 7, gatherSpeed = 3f };
        TechStatApply.ApplyUnitMultipliers(stats, new UnitStatMultipliers());
        Assert.AreEqual(7, stats.lightAttack);
        Assert.AreEqual(3f, stats.gatherSpeed, 0.0001f);
    }

    [Test]
    public void ApplyUnitMultipliers_ClampsNegativeResultToZero()
    {
        var stats = new EraStats { lightAttack = 5 };
        TechStatApply.ApplyUnitMultipliers(stats, new UnitStatMultipliers { lightAttack = -2f });
        Assert.AreEqual(0, stats.lightAttack);
    }

    [Test]
    public void ApplyBuildingMultipliers_FloorsHealth()
    {
        var stats = new EraStatsBuilding { health = 100 };
        TechStatApply.ApplyBuildingMultipliers(stats, new BuildingStatMultipliers { health = 1.15f });
        Assert.AreEqual(115, stats.health);
    }
}
