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

    /// <summary>
    /// Event that notifies the update of a resource (Type, NewAmount)
    /// </summary>
    public event Action<ResourceType, int> OnResourceChanged;

    private int populationUsed;
    private int populationCapacity;
    private int populationHardCap;

    /// <summary>Current population used. Public read surface for UI/commands.</summary>
    public int PopulationUsed => populationUsed;

    /// <summary>
    /// Housing capacity from buildings (and seeds). Owner state — consumers should prefer <see cref="EffectiveMax"/>.
    /// </summary>
    public int PopulationCapacity => populationCapacity;

    /// <summary>
    /// Match hard cap. Owner state — consumers should prefer <see cref="EffectiveMax"/>.
    /// </summary>
    public int PopulationHardCap => populationHardCap;

    /// <summary>Public max: min(capacity, hardCap).</summary>
    public int EffectiveMax => Math.Min(populationCapacity, populationHardCap);

    /// <summary>EffectiveMax - Used; may be negative if overpopulated.</summary>
    public int FreePopulation => EffectiveMax - populationUsed;

    /// <summary>Fired when Used and/or EffectiveMax change. Args: used, effectiveMax.</summary>
    public event Action<int, int> OnPopulationChanged;

    public Player(
        int id,
        string name,
        int startingEra = 0,
        Material mat = null,
        int startingPopulationUsed = 0,
        int startingPopulationCapacity = 0,
        int populationHardCap = 200)
    {
        playerId = id;
        playerName = name;
        playerMaterial = mat; // 👈 guarda el material
        currentEra = startingEra;

        this.populationUsed = Math.Max(0, startingPopulationUsed);
        this.populationCapacity = Math.Max(0, startingPopulationCapacity);
        this.populationHardCap = Math.Max(0, populationHardCap);
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
            OnResourceChanged?.Invoke(resourceType, resources[resourceType]);
            Debug.Log($"Jugador {playerName} recibió {amount} de {resourceType}. Total: {resources[resourceType]}");
        }
    }

    public int GetResourceAmount(ResourceType resourceType)
    {
        return resources.TryGetValue(resourceType, out int amount) ? amount : 0;
    }

    public bool HasResourcesForUnit(UnitStats stats)
    {
        if (stats.creationCosts.Count == 0 || stats.creationCosts.Count > resources.Count) return false;
        foreach (ResourceType resource in stats.creationCosts.Keys) {
            if (stats.creationCosts[resource] > resources[resource]) return false;
        }

        return true;
    }

    public bool HasResourcesForBuilding(BuildingStats stats)
    {
        if (stats.constructionCosts.Count == 0 || stats.constructionCosts.Count > resources.Count) return false;
        foreach (ResourceType resource in stats.constructionCosts.Keys) {
            if (stats.constructionCosts[resource] > resources[resource]) return false;
        }

        return true;
    }

    public void UseResources(Dictionary<ResourceType, int> resourcesForUse)
    {
        foreach (ResourceType resource in resourcesForUse.Keys) {
            resources[resource] -= resourcesForUse[resource];
            OnResourceChanged?.Invoke(resource, resources[resource]);
        }
    }

    public void RefundResources(Dictionary<ResourceType, int> resourcesForUse)
    {
        foreach (ResourceType resource in resourcesForUse.Keys) {
            resources[resource] += resourcesForUse[resource];
            OnResourceChanged?.Invoke(resource, resources[resource]);
        }
    }

    private static int NonNegativeDelta(int amount) => amount < 0 ? 0 : amount;

    private void NotifyPopulationChangedIfNeeded(int previousUsed, int previousEffectiveMax)
    {
        if (populationUsed == previousUsed && EffectiveMax == previousEffectiveMax)
            return;
        OnPopulationChanged?.Invoke(populationUsed, EffectiveMax);
    }

    public void AddPopulationUsed(int amount)
    {
        int previousUsed = populationUsed;
        int previousEffective = EffectiveMax;
        populationUsed = Math.Max(0, populationUsed + NonNegativeDelta(amount));
        NotifyPopulationChangedIfNeeded(previousUsed, previousEffective);
    }

    public void RemovePopulationUsed(int amount)
    {
        int previousUsed = populationUsed;
        int previousEffective = EffectiveMax;
        populationUsed = Math.Max(0, populationUsed - NonNegativeDelta(amount));
        NotifyPopulationChangedIfNeeded(previousUsed, previousEffective);
    }

    public void SetPopulationUsed(int value)
    {
        int previousUsed = populationUsed;
        int previousEffective = EffectiveMax;
        populationUsed = Math.Max(0, value);
        NotifyPopulationChangedIfNeeded(previousUsed, previousEffective);
    }

    public void AddPopulationCapacity(int amount)
    {
        int previousUsed = populationUsed;
        int previousEffective = EffectiveMax;
        populationCapacity = Math.Max(0, populationCapacity + NonNegativeDelta(amount));
        NotifyPopulationChangedIfNeeded(previousUsed, previousEffective);
    }

    public void RemovePopulationCapacity(int amount)
    {
        int previousUsed = populationUsed;
        int previousEffective = EffectiveMax;
        populationCapacity = Math.Max(0, populationCapacity - NonNegativeDelta(amount));
        NotifyPopulationChangedIfNeeded(previousUsed, previousEffective);
    }

    public void SetPopulationCapacity(int value)
    {
        int previousUsed = populationUsed;
        int previousEffective = EffectiveMax;
        populationCapacity = Math.Max(0, value);
        NotifyPopulationChangedIfNeeded(previousUsed, previousEffective);
    }

    public void SetPopulationHardCap(int value)
    {
        int previousUsed = populationUsed;
        int previousEffective = EffectiveMax;
        populationHardCap = Math.Max(0, value);
        NotifyPopulationChangedIfNeeded(previousUsed, previousEffective);
    }
}
