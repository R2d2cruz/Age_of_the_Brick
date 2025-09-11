using UnityEngine;

[CreateAssetMenu(fileName = "NewUnitStats", menuName = "AgeOfTheBrick/Unit Stats")]
public class UnitStats : ScriptableObject
{
    [Header("Info general")]
    public string unitName;

    [Header("Progresión por era")]
    public EraStats[] statsPorEra; // Ej: tamaño 5 si hay 5 eras

    [Header("Visual / Evolución")]
    public bool evolucionaVisual = false;
    public GameObject[] prefabsPorEra; // Prefabs distintos si cambia mesh/animación
}
