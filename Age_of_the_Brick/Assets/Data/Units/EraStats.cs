using UnityEngine;

[System.Serializable]
public class EraStats
{
    [Header("Básicos")]
    public int vida;                // Vida total de la unidad en esta era
    public int defensaLigera;
    public int defensaPesada;
    public int vision;

    [Header("Movilidad y rango")]
    public float velocidadMovimiento;
    public float alcance;

    [Header("Ataque")]
    public float velocidadAtaque;   // golpes por segundo
    public int ataqueLigero;
    public int ataquePesado;

    [Header("Soporte")]
    public float velocidadCuracion;     // unidades de vida por segundo
    public float velocidadConstruccion; // progreso por segundo
}
