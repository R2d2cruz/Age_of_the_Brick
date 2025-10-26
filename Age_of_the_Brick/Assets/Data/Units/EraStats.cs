using UnityEngine;

/// <summary>
/// Contains all the statistical attributes of a unit for a specific Era.
/// Used to define how a unit evolves across different technological ages.
/// </summary>
[System.Serializable]
public class EraStats
{
    // -------------------------
    // BASIC ATTRIBUTES
    // -------------------------
    [Header("Basic Stats")]
    [Tooltip("Total hit points of the unit in this era.")]
    public int health;

    [Tooltip("Defense value against light attacks.")]
    public int lightDefense;

    [Tooltip("Defense value against heavy attacks.")]
    public int heavyDefense;

    [Tooltip("Vision range of the unit.")]
    public int visionRange;

    // -------------------------
    // MOBILITY AND RANGE
    // -------------------------
    [Header("Mobility & Range")]
    [Tooltip("Movement speed of the unit.")]
    public float moveSpeed;

    [Tooltip("Attack or interaction range of the unit.")]
    public float range;

    // -------------------------
    // ATTACK
    // -------------------------
    [Header("Attack")]
    [Tooltip("Number of attacks per second.")]
    public float attackSpeed;

    [Tooltip("Damage dealt against light defense.")]
    public int lightAttack;

    [Tooltip("Damage dealt against heavily armored defense.")]
    public int heavyAttack;

    [Tooltip("Damage multiplier against buildings")]
    public int buildingMultiplier;

    // -------------------------
    // SUPPORT
    // -------------------------

    [Header("Support")]
    [Tooltip("Amount of health restored per second.")]
    public float healSpeed;

    [Tooltip("Construction healt progress per second.")]
    public float buildSpeed;

    [Tooltip("Resource gathering rate per second.")]
    public float gatherSpeed;
}
