using UnityEngine;

/// <summary>
/// Defines the global configuration of a unit type, including its evolution
/// across eras, general data, and optional visual variations.
/// </summary>
[CreateAssetMenu(fileName = "NewUnitStats", menuName = "AgeOfTheBrick/Unit Stats")]
public class UnitStats : ScriptableObject
{
    // -------------------------
    // GENERAL INFO
    // -------------------------
    [Header("General Info")]
    [Tooltip("The display name of this unit type.")]
    public string unitName;

    [Tooltip("Required space.")]
    public float unitSpace;

    [Header("Cost")]
    [Tooltip("Creation costs for the unit (array of 4 resources).")]
    public int[] creationCosts = new int[5];

    [Header("Training time")]
    [Tooltip("Training time for the unit.")]
    public int trainingTime;

    // -------------------------
    // ERA PROGRESSION
    // -------------------------
    [Header("Era Progression")]
    [Tooltip("List of stats for each era (e.g. array size = number of eras).")]
    public EraStats[] statsPerEra; // Example: size 5 if there are 5 eras

    // -------------------------
    // VISUAL EVOLUTION
    // -------------------------
    [Header("Visual / Evolution")]
    [Tooltip("If true, the unit changes its visual model as it evolves through eras.")]
    public bool hasVisualEvolution = false;

    [Tooltip("Optional prefabs for each era if the mesh or animation changes.")]
    public GameObject[] prefabsPerEra;
}
