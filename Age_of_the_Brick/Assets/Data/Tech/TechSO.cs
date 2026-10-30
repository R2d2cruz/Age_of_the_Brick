using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewTech", menuName = "AgeOfTheBrick/Tech")]
public class TechSO : ScriptableObject
{
    [Header("Presentation")]
    public string techName;
    [TextArea] public string description = "";
    public Sprite icon;

    [Header("Research")]
    public ResourceCostDictionary researchCosts = new();
    public float researchTime = 10f;
    public int requiredEra = 0;

    [Header("Prerequisites (reserved — not evaluated in slice 1)")]
    public List<TechSO> requiredTechs = new();

    [Header("Unit targeting")]
    public List<UnitRole> targetUnitRoles = new();
    public List<UnitStats> targetUnitStats = new();

    [Header("Building targeting")]
    public List<BuildingStats> targetBuildingStats = new();

    [Header("Flat modifiers")]
    public UnitStatModifiers unitModifiers = new();
    public BuildingStatModifiers buildingModifiers = new();
}
