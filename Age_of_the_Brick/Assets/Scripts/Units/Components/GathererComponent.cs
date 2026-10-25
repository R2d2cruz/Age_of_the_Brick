using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.PlayerLoop;

public class GathererComponent : BaseUnitComponent, IGatherer
{
    [Header("Tools (attached to Right_Hand)")]
    [SerializeField] private ResourceToolDictionary tools;
    private ResourceType activeToolType;

    [Header("Gathering Settings")]
    [SerializeField] private int maxCarryCapacity = 10;
    [SerializeField] private int extractionAmountPerCycle = 2;

    [Header("Gathering Runtime Data")]
    private int currentCarriedAmount;
    private Resource targetResource;
    private ResourceType currentResourceType;
    private Vector3 lastResourcePosition;
    protected UnitState gatherState;

    /// <summary>
    /// Maximum amount of resources that can be carried before a drop-off is required.
    /// </summary>
    public int MaxCarryCapacity => maxCarryCapacity;

    /// <summary>
    /// Current amount of resources being carried.
    /// </summary>
    public int CurrentCarriedAmount => currentCarriedAmount;


    /// <summary>
    /// Resource type currently being gathered.
    /// </summary>
    public ResourceType CurrentResourceType => currentResourceType;

    /// <summary>
    /// Active resource node being harvested.
    /// </summary>
    public Resource TargetResource => targetResource;

    /// <summary>
    /// Last known position of the active resource node.
    /// Used for relocation searches when the resource is depleted.
    /// </summary>
    public Vector3 LastResourcePosition => lastResourcePosition;

    /// <summary>
    /// Resource units extracted on each gathering cycle.
    /// </summary>
    public int ExtractionAmountPerCycle => extractionAmountPerCycle;

    public override void Initialize(Unit coreUnit)
    {
        base.Initialize(coreUnit);
        gatherState = CreateGatherState();
        HideAllTools();
    }

    public override void OnEraChanged(int newEra)
    {
        unit.ChangeAnimatorMultiplier("gatherSpeedMultiplier", unit.currentStats.gatherSpeed);
    }

    public override void OnUnitChange(IUnitComponent previous)
    {
        if (previous is not GathererComponent oldGatherer)
            return;

        currentCarriedAmount = oldGatherer.currentCarriedAmount;
        targetResource = oldGatherer.targetResource;
        currentResourceType = oldGatherer.currentResourceType;
        lastResourcePosition = oldGatherer.lastResourcePosition;
        if (targetResource != null)
        {
            StartGathering(targetResource);
        }
    }

    /// <summary>
    /// Creates the state instance responsible for gathering behavior.
    /// Can be overridden by derived classes to provide custom implementations.
    /// </summary>
    /// <returns>
    /// A unit state that handles resource gathering logic.
    /// </returns>
    protected virtual UnitState CreateGatherState()
    {
        return new VillagerGatherState(unit, this);
    }

    /// <summary>
    /// Invoked by an Animation Event during the corresponding
    /// gathering animation (chop, mine, or farm).
    /// Keeps resource extraction synchronized with visual feedback.
    /// </summary>
    public virtual void OnGatherHit()
    {
        if (targetResource == null || targetResource.GetQuantity() <= 0) return;

        // Add feedback
    }

    /// <summary>
    /// Configures resource parameters and transitions the villager to the gathering state.
    /// </summary>
    /// <param name="resource">Target resource node.</param>
    public void StartGathering(Resource resource)
    {
        if (resource == null || resource.GetQuantity() <= 0) return;

        targetResource = resource;
        currentResourceType = resource.Type;
        lastResourcePosition = resource.GetPosition();

        unit.ChangeState(gatherState);
    }

    /// <summary>
    /// Adds resource units to internal carrying capacity.
    /// </summary>
    /// <param name="amount">Quantity of extracted resources.</param>
    public void AddCarriedResources(int amount)
    {
        currentCarriedAmount = Mathf.Min(currentCarriedAmount + amount, maxCarryCapacity);
    }

    /// <summary>
    /// Triggers the correct gathering animation based on the resource type.
    /// </summary>
    public void PlayGatherAnimationForType(ResourceType type)
    {
        switch (type)
        {
            case ResourceType.TreeBricks:
                PlayCutAnimation();
                break;
            case ResourceType.RoyalCoins:
            case ResourceType.BricklonBlocks:
            case ResourceType.BrikionFragments:
                PlayMiningAnimation();
                break;
            case ResourceType.BrickFood:
                PlayFarmingAnimation();
                break;
            default:
                PlayDefaultAnimation();
                break;
        }
    }

    /// <summary>
    /// Resets the current inventory carried quantity to zero.
    /// </summary>
    public void EmptyCarriedResources()
    {
        currentCarriedAmount = 0;
    }

    protected void PlayCutAnimation()
    {
        unit.ChangeAnimation("cut");
    }
    protected void PlayMiningAnimation()
    {
        unit.ChangeAnimation("mine");
    }

    protected void PlayFarmingAnimation()
    {
        unit.ChangeAnimation("farm");
    }

    protected void PlayDefaultAnimation()
    {
        unit.ChangeAnimation("attack");
    }


    /// <summary>
    /// Equips the visual tool associated with the specified resource type.
    /// </summary>
    /// <param name="type">
    /// Resource type currently being gathered.
    /// </param>
    public void UpdateToolForResourceType(ResourceType type)
    {
        HideCurrentTool();
        if (tools[type] != null) tools[type].SetActive(true);
        activeToolType = type;
    }

    /// <summary>
    /// Hides the currently equipped tool.
    /// </summary>
    public void HideCurrentTool()
    {
        if (tools[activeToolType] != null) tools[activeToolType].SetActive(false);
    }

    /// <summary>
    /// Disables all equipped tool GameObjects.
    /// </summary>
    public void HideAllTools()
    {
        foreach (var tool in tools.Values)
        {
            tool.SetActive(false);
        }
    }

    #region Dropoff & Resource Search Logic

    /// <summary>
    /// Finds the nearest allied dropoff building that accepts the gathered resource
    /// using the player's entity registry without relying on physical queries.
    /// </summary>
    public Building FindNearestDropoffBuilding()
    {
        if (unit.Owner == null || unit.Owner.Registry == null) return null;

        Building nearestBuilding = null;
        float minDistance = float.MaxValue;

        // Iterate only through the buildings belonging to the player
        var playerBuildings = unit.Owner.Registry.GetBuildings();

        for (int i = 0; i < playerBuildings.Count; i++)
        {
            Building building = playerBuildings[i];
            if (building == null || !building.IsFullyBuilt) continue;

            // Check if the building has a dropoff component and accepts the resource
            ResourceDropoff dropoff = building.DropoffComponent;
            if (dropoff != null && dropoff.AcceptsResource(currentResourceType))
            {
                float distance = unit.GetDistanceToTargetSurface(building);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestBuilding = building;
                }
            }
        }
        return nearestBuilding;
    }

    /// <summary>
    /// Gets the position of the nearest dropoff point.
    /// </summary>
    public Vector3 FindNearestDropoffPoint()
    {
        Building dropoffBuilding = FindNearestDropoffBuilding();
        return dropoffBuilding != null ? dropoffBuilding.transform.position : transform.position;
    }

    /// <summary>
    /// Transfers the resource carried by the villager to the dropoff building.
    /// </summary>
    public void DeliverResources()
    {
        if (currentCarriedAmount <= 0) return;

        Building dropoffBuilding = FindNearestDropoffBuilding();

        if (dropoffBuilding != null && dropoffBuilding.DropoffComponent != null)
        {
            // The villager passes the resource type and amount to the building; the building passes it to the Player
            bool success = dropoffBuilding.DropoffComponent.ReceiveResource(currentResourceType, currentCarriedAmount);

            if (success)
            {
                currentCarriedAmount = 0; // Empty the villager's inventory
            }
        }
    }


    /// <summary>
    /// Searches nearby area for active resource nodes matching the given resource type.
    /// </summary>
    /// <param name="type">Target ResourceType.</param>
    /// <param name="originPosition">Center search position.</param>
    /// <param name="searchRadius">Radius to perform sphere overlap. Defaults to vision range.</param>
    /// <returns>Nearest matching active Resource component, or null.</returns>
    public Resource FindNearestResourceOfType(ResourceType type, Vector3 originPosition, float searchRadius = -1f)
    {
        if (searchRadius <= 0f)
        {
            searchRadius = (unit.currentStats != null && unit.currentStats.visionRange > 0)
                ? unit.currentStats.visionRange
                : 10f;
        }

        Collider[] hits = Physics.OverlapSphere(originPosition, searchRadius);
        Resource nearest = null;
        float minDistance = float.MaxValue;

        foreach (var hit in hits)
        {
            Resource resource = hit.GetComponent<Resource>();
            if (resource != null && resource.Type == type && resource.GetQuantity() > 0)
            {
                float dist = Vector3.Distance(originPosition, resource.GetPosition());
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearest = resource;
                }
            }
        }

        return nearest;
    }

    #endregion

    #region Nested Gather State Machine

    /// <summary>
    /// Dedicated gathering state managing movement, harvesting cycles, and resource drop-off steps.
    /// </summary>
    protected class VillagerGatherState : UnitState
    {
        /// <summary>
        /// Internal gathering workflow states.
        /// </summary>
        private enum GatherSubState
        {
            /// <summary>
            /// Moving toward the target resource node.
            /// </summary>
            MovingToResource,

            /// <summary>
            /// Actively gathering resources.
            /// </summary>
            Extracting,

            /// <summary>
            /// Transporting carried resources to a drop-off building.
            /// </summary>
            MovingToDropoff
        }

        private readonly GathererComponent gathererComponent;
        private readonly NavMeshAgent agent;
        private GatherSubState currentSubState;
        private float extractionTimer = 0;
        private const float BaseTimeBetweenHits = 1.25f;

        /// <summary>
        /// Creates a new gathering state for the specified unit and component.
        /// </summary>
        /// <param name="unit">
        /// Unit that owns this state.
        /// </param>
        /// <param name="gathererComponent">
        /// Gatherer component controlling resource collection.
        /// </param>
        public VillagerGatherState(Unit unit, GathererComponent gathererComponent) : base(unit)
        {
            agent = unit.GetComponent<NavMeshAgent>();
            this.gathererComponent = gathererComponent;
        }

        public override void Enter()
        {
            base.Enter();
            extractionTimer = 0f;

            if (gathererComponent.CurrentCarriedAmount >= gathererComponent.MaxCarryCapacity)
            {
                SetSubState(GatherSubState.MovingToDropoff);
            }
            else
            {
                SetSubState(GatherSubState.MovingToResource);
            }
        }

        public override void Tick()
        {
            base.Tick();

            switch (currentSubState)
            {
                case GatherSubState.MovingToResource:
                    HandleMovingToResource();
                    break;

                case GatherSubState.Extracting:
                    HandleGathering();
                    break;

                case GatherSubState.MovingToDropoff:
                    HandleMovingToDropoff();
                    break;
            }
        }

        /// <summary>
        /// Changes the active gathering sub-state and updates movement,
        /// animations, and equipped tools accordingly.
        /// </summary>
        /// <param name="newSubState">
        /// New gathering sub-state to activate.
        /// </param>
        private void SetSubState(GatherSubState newSubState)
        {
            currentSubState = newSubState;

            switch (currentSubState)
            {
                case GatherSubState.MovingToResource:
                    gathererComponent.HideCurrentTool();
                    if (gathererComponent.TargetResource != null && agent != null)
                    {
                        agent.isStopped = false;
                        agent.SetDestination(gathererComponent.TargetResource.GetPosition());
                        unit.PlayWalkAnimation();
                    }
                    break;

                case GatherSubState.Extracting:
                    if (agent != null) agent.isStopped = true;
                    gathererComponent.UpdateToolForResourceType(gathererComponent.CurrentResourceType);
                    gathererComponent.PlayGatherAnimationForType(gathererComponent.CurrentResourceType);
                    break;

                case GatherSubState.MovingToDropoff:
                    gathererComponent.HideCurrentTool();
                    Vector3 dropoffPos = gathererComponent.FindNearestDropoffPoint();
                    if (agent != null)
                    {
                        agent.isStopped = false;
                        agent.SetDestination(dropoffPos);
                        unit.PlayWalkAnimation();
                    }
                    break;
            }
        }

        private void HandleMovingToResource()
        {
            if (gathererComponent.TargetResource == null || gathererComponent.TargetResource.GetQuantity() <= 0)
            {
                TryRelocateResource();
                return;
            }

            gathererComponent.lastResourcePosition = gathererComponent.TargetResource.GetPosition();

            // Measures distance from villager's bounds to the resource collider surface
            float surfaceDistance = unit.GetDistanceToTargetSurface(gathererComponent.TargetResource);
            float interactionRange = unit.GetInteractionRange();

            if (surfaceDistance <= interactionRange)
            {
                SetSubState(GatherSubState.Extracting);
            }
        }

        private void HandleGathering()
        {
            if (gathererComponent.TargetResource == null || gathererComponent.TargetResource.GetQuantity() <= 0)
            {
                if (gathererComponent.CurrentCarriedAmount > 0) SetSubState(GatherSubState.MovingToDropoff);
                else TryRelocateResource();
                return;
            }

            // Gradually rotate towards the resource while the animation is playing
            Vector3 lookDir = gathererComponent.TargetResource.GetPosition() - unit.transform.position;
            lookDir.y = 0;
            if (lookDir.sqrMagnitude > 0.001f)
            {
                unit.transform.rotation = Quaternion.Slerp(
                    unit.transform.rotation,
                    Quaternion.LookRotation(lookDir),
                    Time.deltaTime * 10f
                );
            }

            // 2. Lógica del Temporizador de Extracción
            // Multiplicamos por la velocidad de recolección (así el timer avanza más rápido si hay mejoras)
            float gatherSpeed = (unit.currentStats != null) ? unit.currentStats.gatherSpeed : 1f;
            extractionTimer += Time.deltaTime * gatherSpeed;

            // 3. ¿El temporizador superó el tiempo de un golpe?
            if (extractionTimer >= BaseTimeBetweenHits)
            {
                // Restamos el tiempo en lugar de ponerlo en 0 para no perder precisión en los decimales
                extractionTimer -= BaseTimeBetweenHits;

                // ¡Golpe Lógico! Hacemos la extracción real de recursos
                int extracted = gathererComponent.TargetResource.Extract(gathererComponent.ExtractionAmountPerCycle);
                gathererComponent.AddCarriedResources(extracted);

                // Si se llenó el inventario tras este golpe, ir a dejar los recursos
                if (gathererComponent.CurrentCarriedAmount >= gathererComponent.MaxCarryCapacity)
                {
                    SetSubState(GatherSubState.MovingToDropoff);
                    return;
                }
            }
        }

        private void HandleMovingToDropoff()
        {
            Building dropoffBuilding = gathererComponent.FindNearestDropoffBuilding();

            if (dropoffBuilding == null)
            {
                unit.Wait();
                return;
            }

            // Measure the distance directly against the building's collider/surface
            float surfaceDistance = unit.GetDistanceToTargetSurface(dropoffBuilding);
            float interactionRange = unit.GetInteractionRange();

            if (surfaceDistance <= interactionRange)
            {
                if (agent != null) agent.isStopped = true; // Stop movement at the edge

                gathererComponent.DeliverResources();

                if (gathererComponent.TargetResource != null && gathererComponent.TargetResource.GetQuantity() > 0)
                {
                    SetSubState(GatherSubState.MovingToResource);
                }
                else
                {
                    TryRelocateResource();
                }
            }
        }


        private void TryRelocateResource()
        {
            Resource nextResource = gathererComponent.FindNearestResourceOfType(
                gathererComponent.CurrentResourceType,
                gathererComponent.LastResourcePosition
            );

            if (nextResource != null)
            {
                gathererComponent.StartGathering(nextResource);
            }
            else
            {
                unit.Wait(); // Return cleanly to IdleState when no resources remain nearby
            }
        }

        public override void Exit()
        {
            base.Exit();
            gathererComponent.HideCurrentTool();
        }

        public override UnitBehaviourState GetBehaviourState()
        {
            return currentSubState switch
            {
                GatherSubState.MovingToResource => UnitBehaviourState.Moving,
                GatherSubState.MovingToDropoff => UnitBehaviourState.Moving,
                GatherSubState.Extracting => UnitBehaviourState.Working,
                _ => UnitBehaviourState.Idle
            };
        }
    }

    #endregion


    [Serializable]
    public class ResourceToolDictionary
        : SerializableDictionary<ResourceType, GameObject>
    {
    }
}