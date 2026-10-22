using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Core class for all structures. Manages health, ownership, selection, and era updates.
/// Capabilities (production, resource dropoff, defense) are handled by attached components.
/// </summary>
public class Building : Selectable, IActionProvider
{
    [Header("Config")]
    public BuildingStats stats;
    public EraStatsBuilding currentStats;

    public float currentHealth { get; set; }
    public Player Owner { get; private set; }

    // Cached optional components
    [SerializeField] public ResourceDropoff DropoffComponent;
    [SerializeField] public UnitProducer ProducerComponent;
    [SerializeField] public BuildingConstruction ConstructionComponent;

    [Header("Command Asset Lists")]
    [SerializeField] private List<CommandSO> mainCommands;
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
    }

    public void Initialize(Player owner)
    {
        Owner = owner;
        ownerPlayerId = owner.playerId;
        UpdateStats(owner.CurrentEra);

        if (currentHealth <= 0) currentHealth = currentStats.health;

        owner.Registry.RegisterBuilding(this);
    }

    public void UpdateStats(int era)
    {
        EraStatsBuilding oldStats = currentStats;
        currentStats = stats.statsPerEra[era];

        if (oldStats != null && oldStats.health > 0)
        {
            float porcentaje = currentHealth / oldStats.health;
            currentHealth = Mathf.Clamp(currentStats.health * porcentaje, 0f, currentStats.health);
        }
        else
        {
            currentHealth = currentStats.health;
        }
    }

    public float GetMaxVida() => currentStats.health;

    public bool IsFullyBuilt()
    {
        return currentHealth >= GetMaxVida();
    }

    public void TakeDamage(int dmgLigero, int dmgPesado)
    {
        int finalDamage = Mathf.Max(0, dmgLigero - currentStats.lightDefense) +
                          Mathf.Max(0, dmgPesado - currentStats.heavyDefense);

        currentHealth -= finalDamage;
        if (currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        if (ConstructionComponent != null)
            ConstructionComponent.Destruir();
        else
            Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (Owner != null)
            Owner.Registry.UnregisterBuilding(this);
    }

    public virtual List<CommandAction> GetAvailableActions()
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
}