using System;
using UnityEngine;

/// <summary>
/// Per-field multiplicative factors for building runtime <see cref="EraStatsBuilding"/>.
/// <c>1</c> means no change. Does not affect image or gridSize.
/// </summary>
[Serializable]
public class BuildingStatMultipliers
{
    public float health = 1f;
    public float lightDefense = 1f;
    public float heavyDefense = 1f;
    public float visionRange = 1f;
    public float garrisonCapacity = 1f;
    public float attackRange = 1f;
    public float lightAttack = 1f;
    public float heavyAttack = 1f;
}
