using UnityEngine;

/// <summary>
/// Core class for all structures. Manages health, ownership, selection, and era updates.
/// Capabilities (production, resource dropoff, defense) are handled by attached components.
/// </summary>
public class Building : Selectable
{
    [Header("Config")]
    public BuildingStats stats;
    private EraStatsBuilding currentStats;

    public float currentHealth { get; set; }
    public Player Owner { get; private set; }

    // Cached optional components
    [SerializeField] public ResourceDropoff DropoffComponent;
    [SerializeField] public UnitProducer ProducerComponent;
    [SerializeField] public BuildingConstruction ConstructionComponent;
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

        if (currentHealth <= 0) currentHealth = currentStats.vida;

        owner.Registry.RegisterBuilding(this);
    }

    public void UpdateStats(int era)
    {
        EraStatsBuilding oldStats = currentStats;
        currentStats = stats.statsPorEra[era];

        if (oldStats != null && oldStats.vida > 0)
        {
            float porcentaje = currentHealth / oldStats.vida;
            currentHealth = Mathf.Clamp(currentStats.vida * porcentaje, 0f, currentStats.vida);
        }
        else
        {
            currentHealth = currentStats.vida;
        }
    }

    public float GetMaxVida() => currentStats.vida;

    public bool IsFullyBuilt()
    {
        return currentHealth >= GetMaxVida();
    }

    public void TakeDamage(int dmgLigero, int dmgPesado)
    {
        int finalDamage = Mathf.Max(0, dmgLigero - currentStats.defensaLigera) +
                          Mathf.Max(0, dmgPesado - currentStats.defensaPesada);

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
}