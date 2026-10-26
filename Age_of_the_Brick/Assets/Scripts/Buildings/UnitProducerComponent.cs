using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;

/// <summary>
/// Handles the production queue, training timer, and spawning of units from a building.
/// </summary>
[RequireComponent(typeof(ProductionQueueComponent), typeof(Selectable))]
public class UnitProducerComponent : MonoBehaviour, IActionProvider
{
    [Header("Spawn Settings")]
    [Tooltip("The exact transform where the unit will be instantiated. If null, the script calculates a point outside the building's collider.")]
    [SerializeField] private Transform spawnPoint;
    
    [Tooltip("The destination transform where the unit will walk to immediately after spawning.")]
    [SerializeField] private Transform rallyPoint;

    // Production data
    private ProductionQueueComponent queueComponent;
    
    // Cached reference to identify the owner of this building
    private Selectable buildingSelectable;

    [Header("Command Asset Lists")]
    [SerializeField] private List<CommandSO> mainCommands;

    public Transform RallyPoint => rallyPoint;
    public int OwnerPlayerId => buildingSelectable.ownerPlayerId;

    private void Awake()
    {
        queueComponent = GetComponent<ProductionQueueComponent>();
        buildingSelectable = GetComponent<Selectable>();
    }

    /// <summary>
    /// Adds a unit to the production queue.
    /// This is called by the CommandAction triggered in the UI.
    /// </summary>
    public void QueueUnit(UnitStats unitToBuild)
    {
        if (unitToBuild == null) return;
        
        Player owner = PlayerManager.Instance?.GetPlayer(buildingSelectable.ownerPlayerId);
        
        if (owner != null && owner.HasResourcesForUnit(unitToBuild))
        {
            owner.UseResources(unitToBuild.creationCosts); // Cobra los recursos
            queueComponent.AddTask(new UnitTask(unitToBuild, this, owner));
        }
    }

    public Vector3 GetSpawnPosition()
    {
        if (spawnPoint != null) return spawnPoint.position;
        
        Collider buildingCollider = GetComponentInChildren<Collider>();
        if (buildingCollider != null)
        {
            Vector3 spawnPos = buildingCollider.bounds.center + (transform.forward * (buildingCollider.bounds.extents.z + 1.5f));
            spawnPos.y = transform.position.y;
            return spawnPos;
        }
        return transform.position;
    }

    /// <summary>
    /// Returns any internal actions. Usually managed by external SOs, so returning empty.
    /// </summary>
    public List<CommandAction> GetAvailableActions()
    {
        Debug.Log("Agregando creador");
        List<CommandAction> runtimeActions = new List<CommandAction>();

        // Build runtime actions for current active menu
        foreach (CommandSO cmdSO in mainCommands)
        {
            if (cmdSO != null && cmdSO.ShouldDisplayAction(buildingSelectable))
            {
                runtimeActions.Add(cmdSO.CreateRuntimeAction(buildingSelectable));
            }
        }

        return runtimeActions;
    }

    // -----------------------------------------------------
    // LA TAREA: Representa una unidad formándose en la cola
    // -----------------------------------------------------
    private class UnitTask : IProductionTask
    {
        private readonly UnitStats stats;
        private readonly UnitProducerComponent producer;
        private readonly Player owner;
        private readonly Sprite icon;

        public UnitTask(UnitStats stats, UnitProducerComponent producer, Player owner)
        {
            this.stats = stats;
            this.producer = producer;
            this.owner = owner;
            icon = stats.statsPerEra[stats.hasVisualEvolution ? owner.CurrentEra : 0].image;
        }

        public string TaskName => stats.unitName;
        public float ProductionTime => stats.trainingTime;
        public Sprite Icon => icon;

        public void Execute()
        {
            int currentEra = owner != null ? owner.CurrentEra : 0;
            
            // Selección del modelo por Era
            GameObject prefabToSpawn = stats.prefabsPerEra[0];
            if (stats.hasVisualEvolution && currentEra < stats.prefabsPerEra.Length)
            {
                prefabToSpawn = stats.prefabsPerEra[currentEra];
            }

            // Usamos la función auxiliar del productor
            Vector3 spawnPos = producer.GetSpawnPosition();
            GameObject unitInstance = Object.Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
            
            if (unitInstance.TryGetComponent(out Unit newUnit))
            {
                newUnit.Initialize(owner); // Se encarga del registro y la era

                if (producer.RallyPoint != null)
                {
                    newUnit.MoveTo(producer.RallyPoint.position);
                }
            }
        }

        public void Cancel()
        {
            if (owner == null) return;
            
            owner.RefundResources(stats.creationCosts);
        }
    }
}