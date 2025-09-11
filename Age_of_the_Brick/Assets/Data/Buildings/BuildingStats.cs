using UnityEngine;

[CreateAssetMenu(fileName = "BuildingStats", menuName = "AgeOfTheBrick/Building Stats")]
public class BuildingStats : ScriptableObject
{
    [Header("Configuración de Eras")]
    public EraStatsBuilding[] statsPorEra;

    [Header("Prefabs por Era (Opcional)")]
    public GameObject[] prefabsPorEra; // si hay evolución visual
    public bool evolucionaVisual = false;
}
