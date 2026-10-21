using UnityEngine;

/// <summary>
/// Contains all statistical attributes of a building for a specific era.
/// Used to define how a structure evolves across different technological ages.
/// </summary>
[System.Serializable]
public class EraStatsBuilding
{
    // -------------------------
    // BASIC ATTRIBUTES
    // -------------------------

    [Header("Basic Stats")]

    /// <summary>
    /// UI portrait displayed when the building is selected.
    /// </summary>
    [Tooltip("UI portrait displayed when the building is selected.")]
    public Sprite image;

    /// <summary>
    /// Maximum health of the building during this era.
    /// </summary>
    [Tooltip("Total structural hit points of the building in this era.")]
    public int health;

    /// <summary>
    /// Defensive value against light attacks, such as arrows or basic infantry.
    /// </summary>
    [Tooltip("Defense value against light attacks, such as arrows or basic infantry.")]
    public int lightDefense;

    /// <summary>
    /// Defensive value against heavy attacks, such as siege weapons.
    /// </summary>
    [Tooltip("Defense value against heavy attacks, such as siege weapons.")]
    public int heavyDefense;

    /// <summary>
    /// Maximum vision range provided by the building.
    /// Used to reveal areas covered by the fog of war.
    /// </summary>
    [Tooltip("Vision range used to reveal the fog of war.")]
    public int visionRange;

    // -------------------------
    // GRID & DIMENSIONS
    // -------------------------

    [Header("Grid & Dimensions")]

    /// <summary>
    /// Footprint size occupied by the building on the construction grid.
    /// Width and length are measured in grid cells (studs).
    /// </summary>
    [Tooltip("Building footprint size on the construction grid (width × length in studs).")]
    public Vector2Int gridSize = new Vector2Int(2, 2);

    // -------------------------
    // GARRISON & SUPPORT
    // -------------------------

    [Header("Garrison & Support")]

    /// <summary>
    /// Maximum number of units that can be garrisoned inside the building.
    /// </summary>
    [Tooltip("Maximum number of units that can be garrisoned inside the building.")]
    public int garrisonCapacity;

    /// <summary>
    /// Maximum range of the building's defensive attack.
    /// A value greater than zero enables ranged attacks.
    /// </summary>
    [Tooltip("If greater than zero, the building can perform ranged attacks.")]
    public float attackRange;

    /// <summary>
    /// Base damage dealt against targets vulnerable to light attacks.
    /// </summary>
    [Tooltip("Base damage dealt against light-armored targets.")]
    public int lightAttack;

    /// <summary>
    /// Base damage dealt against targets vulnerable to heavy attacks.
    /// </summary>
    [Tooltip("Base damage dealt against heavy-armored targets.")]
    public int heavyAttack;
}