using UnityEngine;

/// <summary>
/// Ranged combat capability that spawns a shared projectile instead of dealing direct damage.
/// </summary>
public class RangedCombatComponent : BaseCombatComponent
{
    [SerializeField]
    [Tooltip("Projectile prefab that must include a Projectile component.")]
    private GameObject projectilePrefab;

    [SerializeField]
    [Tooltip("World spawn transform for the projectile (configured on the unit prefab).")]
    private Transform projectileSpawnPoint;

    /// <summary>
    /// Spawns and initializes a projectile aimed at the current target.
    /// </summary>
    protected override void ExecuteAttackLogic()
    {
        if (currentTarget == null || currentTarget.state == SelectableState.Dead) return;
        if (projectilePrefab == null || projectileSpawnPoint == null) return;
        if (unit == null || unit.currentStats == null) return;

        GameObject instance = Instantiate(
            projectilePrefab,
            projectileSpawnPoint.position,
            projectileSpawnPoint.rotation);

        Projectile projectile = instance.GetComponent<Projectile>();
        if (projectile == null)
        {
            Destroy(instance);
            return;
        }

        projectile.Initialize(
            unit.ownerPlayerId,
            unit,
            currentTarget,
            unit.currentStats.lightAttack,
            unit.currentStats.heavyAttack,
            unit.currentStats.projectileSpeed,
            unit.currentStats.projectileTrackTime);
    }
}
