using System;
using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;

/// <summary>
/// Core class for all structures. Manages health, ownership, selection, and era updates.
/// Capabilities (production, resource dropoff, defense) are handled by attached components.
/// </summary>
public class Building : Selectable, IActionProvider
{
    [Header("Editor Setup")]
    [Tooltip("If true, this building starts fully built with 100% health when placed directly in the scene.")]
    public bool startFullyConstructed = false;

    /// <summary>
    /// Determines whether the building has completed construction
    /// and can provide its full functionality.
    /// </summary>
    public bool IsFullyBuilt { get; private set; } = false;

    /// <summary>
    /// Raised when construction completes (including startFullyConstructed).
    /// </summary>
    public event Action OnConstructionFinished;

    [Header("Config")]
    /// <summary>
    /// Configuration asset that defines the building's progression,
    /// costs, visual evolution, and era-dependent attributes.
    /// </summary>
    public BuildingStats stats;
    /// <summary>
    /// Active statistics corresponding to the owner's current era.
    /// </summary>
    public EraStatsBuilding currentStats;

    /// <summary>
    /// Current health of the building.
    /// </summary>
    public float currentHealth { get; set; }
    /// <summary>
    /// Owner of the building.
    /// </summary>
    public Player Owner { get; private set; }

    /// <summary>
    /// Optional component used to receive gathered resources.
    /// </summary>
    public ResourceDropoff DropoffComponent;
    /// <summary>
    /// Optional component responsible for unit production.
    /// </summary>
    public UnitProducerComponent ProducerComponent;
    /// <summary>
    /// Optional component responsible for construction progress
    /// and destruction handling.
    /// </summary>
    public BuildingConstruction ConstructionComponent;

    [Header("Command Asset Lists")]
    [SerializeField] private List<CommandSO> mainCommands;
    [SerializeField] private List<CommandSO> constructionCommands;

    [Header("Visual Meshes")]
    /// <summary>
    /// Construction meshes displayed during building progress.
    /// </summary>
    [SerializeField] private List<GameObject> meshes;
    /// <summary>
    /// Final mesh displayed once construction is complete.
    /// </summary>
    [SerializeField] private GameObject finalMesh;
    /// <summary>
    /// Currently active visual construction stage.
    /// </summary>
    private int currentVisualStage = -1;

    /// <summary>
    /// Final visual representation shown after construction is completed.
    /// </summary>
    public GameObject FinalMesh => finalMesh;

    private void Awake()
    {
        TryGetComponent(out DropoffComponent);
        TryGetComponent(out ProducerComponent);
        TryGetComponent(out ConstructionComponent);
    }

    protected override void Start()
    {
        base.Start();

        // Try to auto-assign owner
        if (Owner == null && PlayerManager.Instance != null)
        {
            Player possibleOwner = PlayerManager.Instance.GetPlayer(ownerPlayerId);
            if (possibleOwner != null)
                Initialize(possibleOwner);
        }
        HideAllMeshes();
        if (startFullyConstructed)
        {
            currentHealth = currentStats.health;
            FinishConstruction();
        }
        else
        {
            Debug.Log("Not fully constructed");
            currentHealth = 0;
            IsFullyBuilt = false;
            RefreshBuildingVisuals();
        }
    }

    /// <summary>
    /// Initializes the building and assigns its owner.
    /// Registers the building in the owner's registry, subscribes to era and tech
    /// changes, and loads stats for the current era including researched flats.
    /// </summary>
    /// <param name="owner">
    /// Player that owns the building.
    /// </param>
    public void Initialize(Player owner)
    {
        UnsubscribeFromOwner();
        Owner = owner;
        ownerPlayerId = owner.playerId;
        SubscribeToOwner();
        RefreshStats(owner.CurrentEra);

        if (currentHealth <= 0) currentHealth = currentStats.health;

        owner.Registry.RegisterBuilding(this);
    }

    private void SubscribeToOwner()
    {
        if (Owner == null) return;
        Owner.OnEraChanged += HandleEraChange;
        Owner.OnTechResearched += HandleTechResearched;
    }

    private void UnsubscribeFromOwner()
    {
        if (Owner == null) return;
        Owner.OnEraChanged -= HandleEraChange;
        Owner.OnTechResearched -= HandleTechResearched;
    }

    private void HandleEraChange(int newEra) => RefreshStats(newEra);

    private void HandleTechResearched(TechSO _) => RefreshStats();

    /// <summary>
    /// Updates stats for the given era, including researched tech flats, and keeps health proportional.
    /// </summary>
    /// <param name="era">
    /// Era index to load.
    /// </param>
    public void UpdateStats(int era) => RefreshStats(era);

    /// <summary>
    /// Recomputes runtime stats from the owner's current era base plus researched tech flats and multipliers.
    /// </summary>
    public void RefreshStats()
    {
        if (Owner != null)
            RefreshStats(Owner.CurrentEra);
        else
            RefreshStats(0);
    }

    /// <summary>
    /// Clones the era base stats and applies matching researched tech flats then multipliers without mutating the ScriptableObject.
    /// </summary>
    private void RefreshStats(int era)
    {
        if (stats == null || stats.statsPerEra == null || stats.statsPerEra.Length == 0) return;
        int clampedEra = Mathf.Clamp(era, 0, stats.statsPerEra.Length - 1);

        EraStatsBuilding oldStats = currentStats;
        EraStatsBuilding next = TechStatApply.CloneEraStatsBuilding(stats.statsPerEra[clampedEra]);
        if (next == null) return;

        if (Owner != null)
        {
            foreach (TechSO tech in Owner.ResearchedTechs)
            {
                if (tech == null) continue;
                if (!TechStatApply.MatchesBuilding(tech, stats)) continue;
                TechStatApply.ApplyBuildingModifiers(next, tech.buildingModifiers);
            }

            foreach (TechSO tech in Owner.ResearchedTechs)
            {
                if (tech == null) continue;
                if (!TechStatApply.MatchesBuilding(tech, stats)) continue;
                TechStatApply.ApplyBuildingMultipliers(next, tech.buildingMultipliers);
            }
        }

        currentStats = next;

        if (oldStats != null && oldStats.health > 0)
        {
            float percent = currentHealth / oldStats.health;
            currentHealth = Mathf.Clamp(currentStats.health * percent, 0f, currentStats.health);
        }
        else
        {
            currentHealth = currentStats.health;
        }
    }
    /// <summary>
    /// Returns the maximum health of the building.
    /// </summary>
    public float GetMaxHealth() => currentStats.health;

    /// <summary>
    /// Restores health to the building.
    /// Construction progress is also represented through health,
    /// therefore reaching maximum health completes construction.
    /// </summary>
    /// <param name="amount">
    /// Amount of health to restore.
    /// </param>
    public void AddHealth(float amount)
    {
        currentHealth += amount;
        // Notificar a la UI que la vida cambió para que la barra se mueva en tiempo real (sin implementar)
        // OnHealthChanged?.Invoke(currentHealth, currentStats.health);

        RefreshBuildingVisuals();

        if (currentHealth >= currentStats.health)
        {
            currentHealth = currentStats.health;
            FinishConstruction();
        }
    }

    /// <summary>
    /// Applies incoming light and heavy damage after defenses
    /// have been accounted for.
    /// </summary>
    /// <param name="lightDamage">
    /// Incoming light damage value.
    /// </param>
    /// <param name="heavyDamage">
    /// Incoming heavy damage value.
    /// </param>
    public void TakeDamage(int lightDamage, int heavyDamage)
    {
        TakeDamage(lightDamage, heavyDamage, null);
    }

    /// <summary>
    /// Applies incoming damage considering defenses. The <paramref name="source"/> is accepted for API
    /// consistency with <see cref="Unit.TakeDamage(int, int, Selectable)"/>; buildings do not broadcast assist.
    /// </summary>
    public void TakeDamage(int lightDamage, int heavyDamage, Selectable source)
    {
        int finalDamage = Mathf.Max(0, lightDamage - currentStats.lightDefense) +
                          Mathf.Max(0, heavyDamage - currentStats.heavyDefense);

        currentHealth -= finalDamage;

        RefreshBuildingVisuals();

        if (currentHealth <= 0)
            Die();
    }

    /// <summary>
    /// Destroys the building and triggers any attached
    /// destruction logic.
    /// </summary>
    private void Die()
    {
        if (ConstructionComponent != null)
            ConstructionComponent.Destruir();
        else
            Destroy(gameObject);
    }

    private void OnDestroy()
    {
        UnsubscribeFromOwner();
        if (Owner != null)
            Owner.Registry.UnregisterBuilding(this);
    }

    /// <summary>
    /// Returns the command actions currently available for this building.
    /// Available commands depend on the construction state.
    /// </summary>
    public virtual List<CommandAction> GetAvailableActions()
    {
        return IsFullyBuilt ? GetCompletedBuildingActions() : GetBuildingActions();
    }

    /// <summary>
    /// Retrieves the actions available once the building
    /// has been fully constructed.
    /// </summary>
    protected List<CommandAction> GetCompletedBuildingActions()
    {
        List<CommandAction> combinedActions = new List<CommandAction>();

        // Build runtime actions for current active menu
        foreach (CommandSO cmdSO in mainCommands)
        {
            if (cmdSO != null && cmdSO.ShouldDisplayAction(this))
            {
                combinedActions.Add(cmdSO.CreateRuntimeAction(this));
            }
        }

        // Busca todos los componentes adjuntos que ofrezcan acciones (UnitProducer, TechResearcher, etc.)
        IActionProvider[] modules = GetComponents<IActionProvider>();

        foreach (var module in modules)
        {
            // Evita autoreferenciarse si Building implementa la interfaz
            if (module == (IActionProvider)this) continue;

            combinedActions.AddRange(module.GetAvailableActions());
        }

        return combinedActions;
    }

    /// <summary>
    /// Retrieves the actions available while the building
    /// is still under construction.
    /// </summary>
    protected List<CommandAction> GetBuildingActions()
    {
        List<CommandAction> combinedActions = new List<CommandAction>();

        // Build runtime actions for current active menu
        foreach (CommandSO cmdSO in constructionCommands)
        {
            if (cmdSO != null && cmdSO.ShouldDisplayAction(this))
            {
                combinedActions.Add(cmdSO.CreateRuntimeAction(this));
            }
        }

        return combinedActions;
    }

    /// <summary>
    /// Marks the building as fully constructed,
    /// updates visual representation,
    /// and refreshes the command interface.
    /// </summary>
    private void FinishConstruction()
    {
        IsFullyBuilt = true;
        // Activa Finished_Mesh, desactiva las mallas de construcción
        RefreshBuildingVisuals();
        // Refresca la UI de comandos para mostrar las acciones del edificio terminado
        UICommandController.Instance?.RefreshGrid();
        OnConstructionFinished?.Invoke();
    }

    /// <summary>
    /// Updates the visual construction stage based on
    /// the current health percentage of the building.
    /// </summary>F
    public void RefreshBuildingVisuals()
    {
        if (meshes.Count == 0)
            return;

        float percent = Mathf.Clamp01(
            (float)currentHealth / currentStats.health);
        int newStage;

        if (percent >= 1f)
        {
            newStage = meshes.Count;
        }
        else
        {
            newStage = Mathf.Clamp(
                Mathf.CeilToInt(percent * meshes.Count) - 1,
                0,
                meshes.Count - 1);
        }

        if (newStage == currentVisualStage)
            return;

        if (currentVisualStage >= 0)
        {
            if (currentVisualStage < meshes.Count)
                meshes[currentVisualStage].SetActive(false);
            else
                finalMesh.SetActive(false);
        }

        if (newStage < meshes.Count)
            meshes[newStage].SetActive(true);
        else
            finalMesh.SetActive(true);

        currentVisualStage = newStage;
    }

    /// <summary>
    /// Disables all construction and final meshes.
    /// Used before activating the correct visual stage.
    /// </summary>
    protected void HideAllMeshes()
    {
        foreach (var mesh in meshes)
        {
            mesh.SetActive(false);
        }
        finalMesh.SetActive(false);
    }
}