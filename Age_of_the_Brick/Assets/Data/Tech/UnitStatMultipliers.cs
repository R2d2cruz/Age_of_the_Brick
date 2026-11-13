using System;
using UnityEngine;

/// <summary>
/// Per-field multiplicative factors for unit runtime <see cref="EraStats"/>.
/// <c>1</c> means no change for that field.
/// </summary>
[Serializable]
public class UnitStatMultipliers
{
    public float health = 1f;
    public float lightDefense = 1f;
    public float heavyDefense = 1f;
    public float visionRange = 1f;
    public float moveSpeed = 1f;
    public float range = 1f;
    public float attackSpeed = 1f;
    public float lightAttack = 1f;
    public float heavyAttack = 1f;
    public float buildingMultiplier = 1f;
    public float projectileTrackTime = 1f;
    public float projectileSpeed = 1f;
    public float healSpeed = 1f;
    public float buildSpeed = 1f;
    public float buildHealth = 1f;
    public float gatherSpeed = 1f;
}
