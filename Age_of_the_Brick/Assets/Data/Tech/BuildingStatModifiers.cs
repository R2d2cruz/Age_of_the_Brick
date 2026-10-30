using System;
using UnityEngine;

/// <summary>
/// Flat numeric deltas applied to a building's runtime <see cref="EraStatsBuilding"/>.
/// Zero means no change. Does not modify image or gridSize.
/// </summary>
[Serializable]
public class BuildingStatModifiers
{
    public int health;
    public int lightDefense;
    public int heavyDefense;
    public int visionRange;
    public int garrisonCapacity;
    public float attackRange;
    public int lightAttack;
    public int heavyAttack;
}
