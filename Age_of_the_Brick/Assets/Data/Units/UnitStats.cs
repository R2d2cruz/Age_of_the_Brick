using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Defines the strategic role of a unit for UI presentation and AI behavior.
/// </summary>
public enum UnitRole
{
    Frontline,
    Skirmisher,
    Healer,
    Siege,
    Worker,
    Hero
}

/// <summary>
/// Defines the global configuration of a unit type, including its progression
/// through different eras, gameplay data, and optional visual evolution.
/// </summary>
[CreateAssetMenu(fileName = "NewUnitStats", menuName = "AgeOfTheBrick/Unit Stats")]
public class UnitStats : ScriptableObject
{
    // -------------------------
    // GENERAL INFORMATION
    // -------------------------

    [Header("General Info")]

    /// <summary>
    /// Display name of the unit shown in the user interface.
    /// </summary>
    [Tooltip("Display name of the unit.")]
    public string unitName;

    // -------------------------
    // DESCRIPTION
    // -------------------------

    [Header("Description")]

    /// <summary>
    /// Description of the unit displayed in the user interface.
    /// Provides information about its purpose and characteristics.
    /// </summary>
    [Tooltip("Description of the unit.")]
    public string description = "";

    /// <summary>
    /// Strategic role used to categorize the unit for gameplay,
    /// UI presentation, and AI decision-making.
    /// </summary>
    [Tooltip("Strategic role of the unit (e.g., Frontline, Siege, Healer).")]
    public UnitRole role;

    /// <summary>
    /// Population space required to create and maintain this unit.
    /// </summary>
    [Tooltip("Required population space.")]
    public int unitSpace;

    // -------------------------
    // COST
    // -------------------------

    [Header("Cost")]

    /// <summary>
    /// Resources required to create the unit.
    /// Each entry represents a resource type and its corresponding cost.
    /// </summary>
    [Tooltip("Resources required to create this unit.")]
    public ResourceCostDictionary creationCosts = new();

    // -------------------------
    // TRAINING
    // -------------------------

    [Header("Training Time")]

    /// <summary>
    /// Time required to train the unit.
    /// </summary>
    [Tooltip("Time required to train the unit.")]
    public int trainingTime;

    // -------------------------
    // ERA PROGRESSION
    // -------------------------

    [Header("Era Progression")]

    /// <summary>
    /// Stat configuration for each available era.
    /// Each element defines the unit's attributes for a specific era.
    /// </summary>
    [Tooltip("Stats configuration for each available era.")]
    public EraStats[] statsPerEra;

    // -------------------------
    // VISUAL EVOLUTION
    // -------------------------

    [Header("Visual Evolution")]

    /// <summary>
    /// Determines whether the unit changes its appearance
    /// as it progresses through different eras.
    /// </summary>
    [Tooltip("Determines whether the unit changes its appearance between eras.")]
    public bool hasVisualEvolution = false;

    /// <summary>
    /// Optional prefab variants used for each era.
    /// These are only used when visual evolution is enabled.
    /// </summary>
    [Tooltip("Optional prefab for each era.")]
    public GameObject[] prefabsPerEra;
}