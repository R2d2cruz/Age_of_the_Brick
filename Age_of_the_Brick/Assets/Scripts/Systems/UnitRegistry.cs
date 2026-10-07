using System.Collections.Generic;
using UnityEngine;

public class UnitRegistry
{
    // Exponemos las listas por si otros sistemas (como la UI de población) necesitan leerlas
    public List<Unit> allUnits { get; private set; } = new List<Unit>();
    public List<Building> allBuildings { get; private set; } = new List<Building>();

    public void RegisterUnit(Unit unit)
    {
        if (unit == null || allUnits.Contains(unit)) return;
        
        allUnits.Add(unit);
        
        // Excelente uso: centralizar el registro del Canvas global aquí
        if (HealthBarsManager.Instance != null)
        {
            HealthBarsManager.Instance.RegisterUnit(unit, unit.get_healthCanvasOffset());
        }
    }

    public void UnregisterUnit(Unit unit)
    {
        if (unit == null) return;
        
        allUnits.Remove(unit);
        
        if (HealthBarsManager.Instance != null)
        {
            HealthBarsManager.Instance.UnregisterUnit(unit);
        }
    }

    public void RegisterBuilding(Building building)
    {
        if (building == null || allBuildings.Contains(building)) return;
        allBuildings.Add(building);
        
        // Si los edificios luego tienen barra de vida, lo agregas aquí
    }

    public void UnregisterBuilding(Building building)
    {
        if (building == null) return;
        allBuildings.Remove(building);
    }

    // Selección por recuadro en tiempo real (sin usar caché)
    public List<Unit> GetUnitsInScreenRect(Camera cam, Vector2 min, Vector2 max)
    {
        List<Unit> result = new List<Unit>();

        foreach (var unit in allUnits)
        {
            if (unit == null) continue;

            // Calculamos la posición exacta en el fotograma actual
            Vector3 screenPos = cam.WorldToScreenPoint(unit.transform.position);
            
            // z > 0 asegura que la unidad está delante de la cámara (no detrás de ti)
            if (screenPos.z > 0 &&
                screenPos.x >= min.x && screenPos.x <= max.x &&
                screenPos.y >= min.y && screenPos.y <= max.y)
            {
                result.Add(unit);
            }
        }

        return result;
    }

    public List<Building> GetBuildings()
    {
        return allBuildings;
    }
}