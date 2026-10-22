using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Handles the production queue, training timer, and spawning of units from a building.
/// </summary>
[RequireComponent(typeof(Selectable))]
public class UnitProducer : MonoBehaviour, IActionProvider
{
    [Header("Spawn Settings")]
    [Tooltip("The exact transform where the unit will be instantiated. If null, the script calculates a point outside the building's collider.")]
    [SerializeField] private Transform spawnPoint;
    
    [Tooltip("The destination transform where the unit will walk to immediately after spawning.")]
    [SerializeField] private Transform rallyPoint;

    // Production data
    private Queue<UnitStats> productionQueue = new Queue<UnitStats>(); 
    private float currentProductionTimer;
    
    // Cached reference to identify the owner of this building
    private Selectable buildingSelectable;

    [Header("Command Asset Lists")]
    [SerializeField] private List<CommandSO> mainCommands;

    private void Awake()
    {
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
            owner.UseResources(unitToBuild.creationCosts);
            
            productionQueue.Enqueue(unitToBuild);
            Debug.Log($"Queued {unitToBuild.unitName}. Total in queue: {productionQueue.Count}");
        }
    }

    private void Update()
    {
        if (productionQueue.Count == 0) return;

        UnitStats currentUnit = productionQueue.Peek();
        currentProductionTimer += Time.deltaTime;

        // Check if training time is complete
        if (currentProductionTimer >= currentUnit.trainingTime)
        {
            SpawnUnit(currentUnit);
            
            // Reset timer and remove the finished unit from the queue
            currentProductionTimer = 0f;
            productionQueue.Dequeue();
        }
    }

    /// <summary>
    /// Instantiates the unit, determines its visual era prefab, and sends it to the rally point.
    /// </summary>
    private void SpawnUnit(UnitStats unitStats)
    {
        Player owner = PlayerManager.Instance?.GetPlayer(buildingSelectable.ownerPlayerId);
        int currentEra = owner != null ? owner.CurrentEra : 0;

        // 1. Determine the correct prefab based on visual evolution and current era
        GameObject prefabToSpawn = null;
        if (unitStats.prefabsPerEra != null && unitStats.prefabsPerEra.Length > 0)
        {
            if (unitStats.hasVisualEvolution && currentEra < unitStats.prefabsPerEra.Length)
            {
                prefabToSpawn = unitStats.prefabsPerEra[currentEra];
            }
            else
            {
                // Fallback to the base era prefab if it doesn't evolve or the array is short
                prefabToSpawn = unitStats.prefabsPerEra[0]; 
            }
        }

        if (prefabToSpawn == null)
        {
            Debug.LogError($"[UnitProducer] No valid prefab found for {unitStats.unitName}");
            return;
        }

        // 2. Determine spawn position
        Vector3 spawnPos = transform.position;
        if (spawnPoint != null)
        {
            spawnPos = spawnPoint.position;
        }
        else
        {
            // Dynamic fallback: spawn safely on the edge of the building's bounding box
            Collider buildingCollider = GetComponentInChildren<Collider>();
            if (buildingCollider != null)
            {
                // Spawns the unit slightly in front of the building's forward bounds
                spawnPos = buildingCollider.bounds.center + (transform.forward * (buildingCollider.bounds.extents.z + 1.5f));
                spawnPos.y = transform.position.y;
            }
        }

        // 3. Instantiate and initialize the unit
        GameObject unitInstance = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
        Unit newUnit = unitInstance.GetComponent<Unit>();

        if (newUnit != null)
        {
            // Assign ownership and register to the player's active units
            newUnit.ownerPlayerId = buildingSelectable.ownerPlayerId;
            owner?.RegisterUnit(newUnit);

            // 4. Send to rally point if one is set
            if (rallyPoint != null)
            {
                newUnit.MoveTo(rallyPoint.position);
            }
        }
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
}