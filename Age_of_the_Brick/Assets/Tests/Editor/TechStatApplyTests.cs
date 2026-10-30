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
}
