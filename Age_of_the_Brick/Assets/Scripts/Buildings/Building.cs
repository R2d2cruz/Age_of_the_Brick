using UnityEngine;

public class Building : MonoBehaviour
{
    [Header("Config")]
    public BuildingStats stats;
    private EraStatsBuilding currentStats;

    // Estado
    public float currentHealth { get; set; }
    public Player Owner { get; private set; }

    private Renderer[] renderers;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
    }

    public void Initialize(Player owner)
    {
        Owner = owner;
        UpdateStats(owner.CurrentEra);
        if (currentHealth <= 0) currentHealth = currentStats.vida;
        owner.RegisterBuilding(this);
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
        // Avisar al BuildingConstruction que dispare animación de destrucción
        var construction = GetComponent<BuildingConstruction>();
        if (construction != null)
            construction.Destruir();
        else
            Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (Owner != null)
            Owner.UnregisterBuilding(this);
    }
}
