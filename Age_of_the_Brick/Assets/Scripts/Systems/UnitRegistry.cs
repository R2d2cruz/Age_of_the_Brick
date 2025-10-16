using System.Collections.Generic;
using UnityEngine;


// para escalar a mas de 2000+ unidades: UnitRegistry con spatial hashing 3D
public class UnitRegistry
{
    private readonly List<Unit> allUnits = new List<Unit>();
    private readonly List<Building> allBuildings = new List<Building>();
    private readonly Dictionary<Unit, Vector3> positionCache = new Dictionary<Unit, Vector3>();

    private float updateTimer = 0f;
    private const float updateInterval = 0.2f;

    public void RegisterUnit(Unit unit)
    {
        if (unit == null || allUnits.Contains(unit)) return;
        allUnits.Add(unit);
        positionCache[unit] = unit.transform.position;
    }

    public void UnregisterUnit(Unit unit)
    {
        if (unit == null) return;
        allUnits.Remove(unit);
        positionCache.Remove(unit);
    }

    public void RegisterBuilding(Building building)
    {
        if (building == null || allBuildings.Contains(building)) return;
        allBuildings.Add(building);
    }

    public void UnregisterBuilding(Building building)
    {
        if (building == null) return;
        allBuildings.Remove(building);
    }

    public void UpdateCache(float deltaTime)
    {
        updateTimer += deltaTime;
        if (updateTimer < updateInterval) return;
        updateTimer = 0f;

        foreach (var u in allUnits)
        {
            if (u != null)
                positionCache[u] = u.transform.position;
        }
    }

    public List<Unit> GetUnitsInScreenRect(Camera cam, Vector2 min, Vector2 max)
    {
        List<Unit> result = new List<Unit>();

        foreach (var kv in positionCache)
        {
            Unit unit = kv.Key;
            if (unit == null) continue;

            Vector3 screenPos = cam.WorldToScreenPoint(kv.Value);
            if (screenPos.z < 0) continue;

            if (screenPos.x >= min.x && screenPos.x <= max.x &&
                screenPos.y >= min.y && screenPos.y <= max.y)
            {
                result.Add(unit);
            }
        }

        return result;
    }

    // 🔸 Nuevos métodos para frustum culling
    public List<Unit> GetVisibleUnits(Camera cam)
    {
        List<Unit> result = new();
        if (cam == null) return result;

        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cam);

        foreach (var unit in allUnits)
        {
            if (unit == null) continue;
            Renderer r = unit.GetComponentInChildren<Renderer>();
            if (r != null && GeometryUtility.TestPlanesAABB(planes, r.bounds))
                result.Add(unit);
        }

        return result;
    }

    public List<Building> GetVisibleBuildings(Camera cam)
    {
        List<Building> result = new();
        if (cam == null) return result;

        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cam);

        foreach (var building in allBuildings)
        {
            if (building == null) continue;
            Renderer r = building.GetComponentInChildren<Renderer>();
            if (r != null && GeometryUtility.TestPlanesAABB(planes, r.bounds))
                result.Add(building);
        }

        return result;
    }
}
