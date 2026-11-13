using NUnit.Framework;
using UnityEngine;

/// <summary>
/// EditMode tests for pure flee away-point math.
/// </summary>
public class FleeDestinationTests
{
    [Test]
    public void ComputeAwayPoint_MovesAwayFromThreatOnXZ()
    {
        Vector3 self = new Vector3(0f, 1f, 0f);
        Vector3 threat = new Vector3(10f, 1f, 0f);
        Vector3 result = FleeDestination.ComputeAwayPoint(self, threat, 10f, Vector3.forward);

        Assert.Less(result.x, self.x);
        Assert.AreEqual(self.y, result.y, 0.001f);
        float dist = Vector3.Distance(new Vector3(result.x, 0f, result.z), new Vector3(self.x, 0f, self.z));
        Assert.AreEqual(10f, dist, 0.01f);
    }

    [Test]
    public void ComputeAwayPoint_UsesFallbackWhenOverlappingThreat()
    {
        Vector3 self = Vector3.zero;
        Vector3 result = FleeDestination.ComputeAwayPoint(self, self, 8f, Vector3.forward);
        Assert.AreEqual(8f, result.z, 0.01f);
    }
}
