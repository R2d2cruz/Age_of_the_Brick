using UnityEngine;

[System.Serializable]
public class EraStatsBuilding
{
    [Header("Estadísticas por Era")]
    public float vida = 1000f;
    public int defensaLigera = 0;
    public int defensaPesada = 0;
    public float vision = 10f;
    public Vector2Int tamano = new Vector2Int(2, 2); // opcional, para pathfinding o grid
}
