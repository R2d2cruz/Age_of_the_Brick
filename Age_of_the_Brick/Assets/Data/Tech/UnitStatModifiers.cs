using System;
using UnityEngine;

/// <summary>
/// Flat numeric deltas applied to a unit's runtime <see cref="EraStats"/>.
/// Zero means no change for that field.
/// </summary>
[Serializable]
public class UnitStatModifiers
{
    public int health;
    public int lightDefense;
    public int heavyDefense;
    public int visionRange;
    public float moveSpeed;
    public float range;
    public float attackSpeed;
    public int lightAttack;
    public int heavyAttack;
    public int buildingMultiplier;
    public float projectileTrackTime;
    public float projectileSpeed;
    public float healSpeed;
    public float buildSpeed;
    public float buildHealth;
    public float gatherSpeed;
}
