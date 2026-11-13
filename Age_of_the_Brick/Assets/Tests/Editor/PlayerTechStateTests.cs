using NUnit.Framework;
using UnityEngine;

public class PlayerTechStateTests
{
    static TechSO MakeTech(int cost)
    {
        var tech = ScriptableObject.CreateInstance<TechSO>();
        tech.techName = "Forge";
        tech.requiredEra = 0;
        tech.researchCosts = new ResourceCostDictionary();
        tech.researchCosts.Set(ResourceType.TreeBricks, cost);
        return tech;
    }

    [Test]
    public void TryBeginResearch_MarksPending_AndSpendsResources()
    {
        var player = new Player(0, "Test");
        player.AddResource(ResourceType.TreeBricks, 50);
        var tech = MakeTech(10);

        Assert.IsTrue(player.TryBeginResearch(tech));
        Assert.IsTrue(player.IsResearchPending(tech));
        Assert.IsFalse(player.HasResearched(tech));
        Assert.AreEqual(40, player.GetResourceAmount(ResourceType.TreeBricks));
    }

    [Test]
    public void CompleteResearch_FiresEvent_AndBlocksSecondBegin()
    {
        var player = new Player(0, "Test");
        player.AddResource(ResourceType.TreeBricks, 50);
        var tech = MakeTech(10);

        TechSO received = null;
        player.OnTechResearched += t => received = t;

        Assert.IsTrue(player.TryBeginResearch(tech));
        player.CompleteResearch(tech);

        Assert.AreSame(tech, received);
        Assert.IsTrue(player.HasResearched(tech));
        Assert.IsFalse(player.IsResearchPending(tech));
        Assert.IsFalse(player.TryBeginResearch(tech));
    }

    [Test]
    public void ClearResearchPending_AllowsRetry_AfterRefund()
    {
        var player = new Player(0, "Test");
        player.AddResource(ResourceType.TreeBricks, 50);
        var tech = MakeTech(10);

        Assert.IsTrue(player.TryBeginResearch(tech));
        player.ClearResearchPending(tech);
        player.RefundTechCosts(tech);

        Assert.IsFalse(player.IsResearchPending(tech));
        Assert.AreEqual(50, player.GetResourceAmount(ResourceType.TreeBricks));
        Assert.IsTrue(player.TryBeginResearch(tech));
    }
}
