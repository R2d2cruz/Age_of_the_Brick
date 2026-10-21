using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines the global configuration of a building type, including its progression
/// through different eras, construction costs, and optional visual evolution.
/// </summary>
[CreateAssetMenu(fileName = "NewBuildingStats", menuName = "AgeOfTheBrick/Building Stats")]
public class BuildingStats : ScriptableObject
{
    // -------------------------
    // GENERAL INFORMATION
    // -------------------------

    [Header("General Info")]

    /// <summary>
    /// Display name of the building shown in the user interface.
    /// </summary>
    [Tooltip("Display name of the building.")]
    public string buildingName;

    /// <summary>
    /// Description of the building displayed in the user interface.
    /// Provides information about its purpose, functionality, and lore.
    /// </summary>
    [Tooltip("Description of the building's purpose and lore.")]
    public string description = "";

    // -------------------------
    // POPULATION & SPACE
    // -------------------------

    [Header("Population Capacity")]

    /// <summary>
    /// Amount of population capacity provided by this building.
    /// Used by structures such as Houses or Town Centers.
    /// </summary>
    [Tooltip("Amount of population capacity this building provides to the player (e.g., Houses or Town Centers).")]
    public int populationProvided;

    // -------------------------
    // COST
    // -------------------------

    [Header("Cost")]

    /// <summary>
    /// Resources required to construct the building.
    /// Each entry represents a resource type and its corresponding cost.
    /// </summary>
    [Tooltip("Resources required to construct this building.")]
    public Dictionary<ResourceType, int> constructionCosts;

    // -------------------------
    // CONSTRUCTION
    // -------------------------

    [Header("Construction Time")]

    /// <summary>
    /// Base time required for a worker to fully construct the building.
    /// Measured in seconds.
    /// </summary>
    [Tooltip("Base time required for a villager to fully construct this building (in seconds).")]
    public float buildTime;

    // -------------------------
    // ERA PROGRESSION
    // -------------------------

    [Header("Era Progression")]

    /// <summary>
    /// Stat configuration for each available era.
    /// Each element defines the building's attributes for a specific era.
    /// </summary>
    [Tooltip("Stats configuration for each available era.")]
    public EraStatsBuilding[] statsPerEra;

    // -------------------------
    // VISUAL EVOLUTION
    // -------------------------

    [Header("Visual Evolution")]

    /// <summary>
    /// Determines whether the building changes its appearance
    /// as it progresses through different eras.
    /// </summary>
    [Tooltip("Determines whether the building changes its physical appearance between eras.")]
    public bool hasVisualEvolution = false;

    /// <summary>
    /// Optional prefab variants for each era.
    /// Used when the building's model, mesh, or animations
    /// change after advancing to a new era.
    /// </summary>
    [Tooltip("Optional prefab for each era.")]
    public GameObject[] prefabsPerEra;
}