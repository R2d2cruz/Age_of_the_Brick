using UnityEngine;

/// <summary>
/// Contains all statistical attributes of a unit for a specific era.
/// Used to define how a unit evolves across different technological ages.
/// </summary>
[System.Serializable]
public class EraStats
{
    // -------------------------
    // BASIC ATTRIBUTES
    // -------------------------

    [Header("Basic Stats")]

    /// <summary>
    /// UI portrait displayed when the unit is selected.
    /// </summary>
    [Tooltip("UI portrait displayed when the unit is selected.")]
    public Sprite image;

    /// <summary>
    /// Maximum health of the unit during this era.
    /// </summary>
    [Tooltip("Total hit points of the unit in this era.")]
    public int health;

    /// <summary>
    /// Defensive value against light attacks, such as arrows or basic infantry.
    /// </summary>
    [Tooltip("Defense value against light attacks.")]
    public int lightDefense;

    /// <summary>
    /// Defensive value against heavy attacks, such as siege weapons or heavily armored units.
    /// </summary>
    [Tooltip("Defense value against heavy attacks.")]
    public int heavyDefense;

    /// <summary>
    /// Maximum vision range provided by the unit.
    /// Used to reveal areas covered by the fog of war.
    /// </summary>
    [Tooltip("Vision range of the unit.")]
    public int visionRange;

    // -------------------------
    // MOBILITY & RANGE
    // -------------------------

    [Header("Mobility & Range")]

    /// <summary>
    /// Movement speed of the unit.
    /// </summary>
    [Tooltip("Movement speed of the unit.")]
    public float moveSpeed;

    /// <summary>
    /// Maximum range at which the unit can attack or interact.
    /// </summary>
    [Tooltip("Attack or interaction range of the unit.")]
    public float range;

    // -------------------------
    // ATTACK
    // -------------------------

    [Header("Attack")]

    /// <summary>
    /// Number of attacks performed per second.
    /// </summary>
    [Tooltip("Number of attacks per second.")]
    public float attackSpeed;

    /// <summary>
    /// Base damage dealt against targets vulnerable to light attacks.
    /// </summary>
    [Tooltip("Damage dealt against light defense.")]
    public int lightAttack;

    /// <summary>
    /// Base damage dealt against targets vulnerable to heavy attacks.
    /// </summary>
    [Tooltip("Damage dealt against heavily armored defense.")]
    public int heavyAttack;

    /// <summary>
    /// Damage multiplier applied when attacking buildings.
    /// </summary>
    [Tooltip("Damage multiplier against buildings.")]
    public int buildingMultiplier;

    // -------------------------
    // SUPPORT
    // -------------------------

    [Header("Support")]

    /// <summary>
    /// Amount of health restored per second.
    /// Used by healing or support units.
    /// </summary>
    [Tooltip("Amount of health restored per second.")]
    public float healSpeed;

    /// <summary>
    /// Construction progress contributed per second.
    /// Used when building or repairing structures.
    /// </summary>
    [Tooltip("Construction progress per second.")]
    public float buildSpeed;

    /// <summary>
    /// Resource gathering rate per second.
    /// </summary>
    [Tooltip("Resource gathering rate per second.")]
    public float gatherSpeed;
}