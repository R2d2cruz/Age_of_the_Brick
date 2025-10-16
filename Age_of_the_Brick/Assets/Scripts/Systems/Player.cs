using System;
using UnityEngine;
using System.Collections.Generic;

public class Player
{
    public int playerId;
    public string playerName;

    public Material playerMaterial; // 👈 reemplaza al Color

    private int currentEra = 0;
    public int CurrentEra => currentEra;

    // ✅ Listas de objetos poseídos
    private readonly List<Unit> ownedUnits = new List<Unit>();
    private readonly List<Building> ownedBuildings = new List<Building>();

    // Evento que notifica la nueva era (solo para las unidades/estructuras de este jugador)
    public event Action<int> OnEraChanged;

    public Player(int id, string name, int startingEra = 0, Material mat = null)
    {
        playerId = id;
        playerName = name;
        playerMaterial = mat; // 👈 guarda el material
        currentEra = startingEra;
    }

    // --- Gestión de unidades ---
    public void RegisterUnit(Unit unit)
    {
        if (unit != null && !ownedUnits.Contains(unit))
            ownedUnits.Add(unit);
    }

    public void UnregisterUnit(Unit unit)
    {
        if (unit != null)
            ownedUnits.Remove(unit);
    }

    public List<Unit> GetOwnedUnits() => ownedUnits;

    // --- Gestión de edificios ---
    public void RegisterBuilding(Building building)
    {
        if (building != null && !ownedBuildings.Contains(building))
            ownedBuildings.Add(building);
    }

    public void UnregisterBuilding(Building building)
    {
        if (building != null)
            ownedBuildings.Remove(building);
    }

    public List<Building> GetOwnedBuildings() => ownedBuildings;

    // --- Eras ---
    public void AdvanceEra()
    {
        currentEra++;
        OnEraChanged?.Invoke(currentEra);
        Debug.Log($"Player {playerId} ({playerName}) avanzó a era {currentEra + 1}");
    }

    public void SetEra(int era)
    {
        currentEra = era;
        OnEraChanged?.Invoke(currentEra);
    }
}
