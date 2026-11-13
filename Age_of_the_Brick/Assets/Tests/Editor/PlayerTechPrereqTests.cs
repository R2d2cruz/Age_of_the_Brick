using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class PlayerTechPrereqTests
{
    static TechSO MakeTech(string name)
    {
        var tech = ScriptableObject.CreateInstance<TechSO>();
        tech.techName = name;
        tech.requiredEra = 0;
        tech.researchCosts = new ResourceCostDictionary();
        tech.requiredTechs = new List<TechSO>();
        return tech;
    }

    [Test]
    public void HasTechPrerequisitesMet_EmptyList_ReturnsTrue()
    {
        var player = new Player(0, "Test");
        var tech = MakeTech("Root");
        Assert.IsTrue(player.HasTechPrerequisitesMet(tech));
    }

    [Test]
    public void HasTechPrerequisitesMet_RequiresAllDirectPrereqsResearched()
    {
        var player = new Player(0, "Test");
        var a = MakeTech("A");
        var b = MakeTech("B");
        var child = MakeTech("Child");
        child.requiredTechs = new List<TechSO> { a, b };

        Assert.IsFalse(player.HasTechPrerequisitesMet(child));

        player.AddResource(ResourceType.TreeBricks, 100);
        // Bypass cost by empty costs — CompleteResearch after pending path:
        Assert.IsTrue(player.TryBeginResearch(a));
        player.CompleteResearch(a);
        Assert.IsFalse(player.HasTechPrerequisitesMet(child));

        Assert.IsTrue(player.TryBeginResearch(b));
        player.CompleteResearch(b);
        Assert.IsTrue(player.HasTechPrerequisitesMet(child));
    }

    [Test]
    public void HasTechPrerequisitesMet_IgnoresNullEntries()
    {
        var player = new Player(0, "Test");
        var a = MakeTech("A");
        var child = MakeTech("Child");
        child.requiredTechs = new List<TechSO> { null, a };

        Assert.IsFalse(player.HasTechPrerequisitesMet(child));
        player.TryBeginResearch(a);
        player.CompleteResearch(a);
        Assert.IsTrue(player.HasTechPrerequisitesMet(child));
    }

    [Test]
    public void HasTechPrerequisitesMet_PendingDoesNotCount()
    {
        var player = new Player(0, "Test");
        var a = MakeTech("A");
        var child = MakeTech("Child");
        child.requiredTechs = new List<TechSO> { a };

        Assert.IsTrue(player.TryBeginResearch(a));
        Assert.IsTrue(player.IsResearchPending(a));
        Assert.IsFalse(player.HasTechPrerequisitesMet(child));
    }

    [Test]
    public void HasTechPrerequisitesMet_Cycle_ReturnsFalse()
    {
        var player = new Player(0, "Test");
        var a = MakeTech("A");
        var b = MakeTech("B");
        a.requiredTechs = new List<TechSO> { b };
        b.requiredTechs = new List<TechSO> { a };

        Assert.IsFalse(player.HasTechPrerequisitesMet(a));
        Assert.IsFalse(player.HasTechPrerequisitesMet(b));
    }

    [Test]
    public void TryBeginResearch_FailsWhenPrereqsUnmet()
    {
        var player = new Player(0, "Test");
        var a = MakeTech("A");
        var child = MakeTech("Child");
        child.requiredTechs = new List<TechSO> { a };

        Assert.IsFalse(player.TryBeginResearch(child));
        Assert.IsFalse(player.IsResearchPending(child));
    }
}
