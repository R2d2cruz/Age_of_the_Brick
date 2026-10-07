using System;
using System.Collections.Generic;
using UnityEngine;
public class Player
{
    public int playerId;
    public string playerName;

    public Material playerMaterial; // 👈 reemplaza al Color

    private int currentEra = 0;
    public int CurrentEra => currentEra;

    // ✅ Registro de objetos poseídos
    public UnitRegistry Registry { get; private set; } = new UnitRegistry();

    private Dictionary<ResourceType, int> resources = new Dictionary<ResourceType, int>
    {
        { ResourceType.TreeBricks, 0 },
        { ResourceType.BrickFood, 0 },
        { ResourceType.BricklonBlocks, 0 },
        { ResourceType.RoyalCoins, 0 },
        { ResourceType.BrikionFragments, 0 }
    };

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
    public void RegisterUnit(Unit unit) => Registry.RegisterUnit(unit);
    public void UnregisterUnit(Unit unit) => Registry.UnregisterUnit(unit);
    public void RegisterBuilding(Building b) => Registry.RegisterBuilding(b);
    public void UnregisterBuilding(Building b) => Registry.UnregisterBuilding(b);

    public void Update(float deltaTime)
    {
    }

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

    /// <summary>
    /// Adds resources to the player's global storage.
    /// </summary>
    public void AddResource(ResourceType resourceType, int amount)
    {
        if (resources.ContainsKey(resourceType))
        {
            resources[resourceType] += amount;
            Debug.Log($"Jugador {playerName} recibió {amount} de {resourceType}. Total: {resources[resourceType]}");
        }
    }

    public int GetResourceAmount(ResourceType resourceType)
    {
        return resources.TryGetValue(resourceType, out int amount) ? amount : 0;
    }
}
