using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

/// <summary>
/// Represents a villager unit capable of gathering resources, building structures, and participating in basic combat.
/// Inherits from Unit and integrates an internal state machine to manage economic loops.
/// </summary>
public class Villager : Unit
{
    [Header("Tools (attached to Right_Hand)")]
    [SerializeField] private GameObject axe;
    [SerializeField] private GameObject pickaxe;
    [SerializeField] private GameObject pitchfork;
    [SerializeField] private GameObject dot;
    [SerializeField] private GameObject fishingRod;

    [Header("Gathering Settings")]
    [SerializeField] private int maxCarryCapacity = 10;
    [SerializeField] private int extractionAmountPerCycle = 2;

    [Header("Build Settings")]
    protected UnitState buildState;
    public Building targetBuilding { get; private set; }

    [Header("Gathering Runtime Data")]
    private int currentCarriedAmount;
    private Resource targetResource;
    private ResourceType currentResourceType;
    private Vector3 lastResourcePosition;

    // Cached state instance for gathering routine
    protected UnitState gatherState;

    public int MaxCarryCapacity => maxCarryCapacity;
    public int CurrentCarriedAmount => currentCarriedAmount;
    public ResourceType CurrentResourceType => currentResourceType;
    public Resource TargetResource => targetResource;
    public Vector3 LastResourcePosition => lastResourcePosition;
    public int ExtractionAmountPerCycle => extractionAmountPerCycle;

    /// <summary>
    /// Tells the Placement Handler to start the ghost placement mode.
    /// </summary>
    public void StartPlacingBuilding(BuildingStats buildingStats)
    {
        Player owner = PlayerManager.Instance.GetPlayer(ownerPlayerId);
        if (owner != null)
        {
            BuildingPlacementHandler.Instance.BeginPlacement(buildingStats, this, owner.CurrentEra);
        }
    }

    // -------------------------
    // INITIALIZATION
    // -------------------------
    protected override void Start()
    {
        base.Start();
        gatherState = CreateGatherState();
        buildState = CreateBuildState();
        HideAllTools();
    }

    protected virtual UnitState CreateGatherState()
    {
        return new VillagerGatherState(this);
    }

    protected virtual UnitState CreateBuildState()
    {
        return new VillagerBuildState(this);
    }

    /// <summary>
    /// Updates stats according to the era, maintaining health proportionally.
    /// </summary>
    public override void UpdateStats(int era)
    {
        base.UpdateStats(era);
        if (animator != null)
        {
            animator.SetFloat("gatherSpeedMultiplier", currentStats.gatherSpeed);
            animator.SetFloat("buildSpeed", currentStats.buildSpeed);
        }
    }

    // -------------------------
    // STATE BEHAVIORS
    // -------------------------

    public override void PlayIdleAnimation()
    {
        base.PlayIdleAnimation();
        HideAllTools();
    }

    protected override void PlayAttackAnimation()
    {
        ShowTool(axe);
        base.PlayAttackAnimation();
    }

    protected void PlayCutAnimation()
    {
        ChangeAnimtion("cut");
    }

    protected void PlayMinningAnimation()
    {
        ChangeAnimtion("mine");
    }

    protected void PlayFarmingAnimation()
    {
        ChangeAnimtion("farm");
    }

    public void PlayBuildAnimation()
    {
        ChangeAnimtion("build");
        HideAllTools();
    }

    /// <summary>
    /// Evaluates context-sensitive player commands targeting resources, buildings, or enemy entities.
    /// </summary>
    /// <param name="target">The target Selectable clicked by the player.</param>
    public override void ExecuteContextCommand(Selectable target)
    {
        if (target == null || target.state == SelectableState.Dead) return;

        // 1. Interaction with Resource Nodes
        if (target.CompareTag("Resource"))
        {
            if (target is Resource resource)
            {
                StartGathering(resource);
                return;
            }
        }
        // 2. Interaction with Allied Buildings (Construction or Repair)
        else if (target.CompareTag("Building") && !target.IsEnemyTo(ownerPlayerId))
        {
            Building building = target.GetComponent<Building>();
            // Si el edificio no está terminado, ir a construirlo
            if (building != null && !building.IsFullyBuilt)
            {
                StartBuilding(building);
                return;
            }
        }
        // 3. Fallback to base Unit command execution (e.g. attacking enemy units)
        else
        {
            base.ExecuteContextCommand(target);
        }
    }

    /// <summary>
    /// Event invoked from an Animation Event in the corresponding animation clip (Chop/Mine/Farm).
    /// Executes resource extraction synchronized with the visual impact.
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

        ChangeState(gatherState);
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
    /// Resets the current inventory carried quantity to zero.
    /// </summary>
    public void EmptyCarriedResources()
    {
        currentCarriedAmount = 0;
    }

    public void StartBuilding(Building building)
    {
        if (building == null || building.IsFullyBuilt) return;

        targetBuilding = building;
        ChangeState(buildState);
    }

    // -------------------------
    // SPECIFIC ACTIONS & TOOLS
    // -------------------------

    public void ChopWood()
    {
        ShowTool(axe);
        PlayAttackAnimation();
    }

    public void Mine()
    {
        ShowTool(pickaxe);
        PlayAttackAnimation();
    }

    public void Farm()
    {
        ShowTool(pitchfork);
        PlayAttackAnimation();
    }

    public void Fish()
    {
        ShowTool(dot);
        ShowTool(fishingRod);
        PlayAttackAnimation();
    }

    /// <summary>
    /// Equips the corresponding visual tool based on the resource type.
    /// </summary>
    /// <param name="type">Resource type being collected.</param>
    public void UpdateToolForResourceType(ResourceType type)
    {
        switch (type)
        {
            case ResourceType.TreeBricks: ShowTool(axe); break;
            case ResourceType.RoyalCoins:
            case ResourceType.BricklonBlocks:
            case ResourceType.BrikionFragments: ShowTool(pickaxe); break;
            case ResourceType.BrickFood: ShowTool(pitchfork); break;
            default: HideAllTools(); break;
        }
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
                PlayMinningAnimation();
                break;
            case ResourceType.BrickFood:
                PlayFarmingAnimation();
                break;
            default:
                PlayAttackAnimation();
                break;
        }
    }

    /// <summary>
    /// Disables all equipped tool GameObjects.
    /// </summary>
    public void HideAllTools()
    {
        if (axe) axe.SetActive(false);
        if (pickaxe) pickaxe.SetActive(false);
        if (pitchfork) pitchfork.SetActive(false);
        if (dot) dot.SetActive(false);
        if (fishingRod) fishingRod.SetActive(false);
    }

    private void ShowTool(GameObject tool)
    {
        HideAllTools();
        if (tool) tool.SetActive(true);
    }

    #region Dropoff & Resource Search Logic

    /// <summary>
    /// Finds the nearest allied dropoff building that accepts the gathered resource
    /// using the player's entity registry without relying on physical queries.
    /// </summary>
    public Building FindNearestDropoffBuilding()
    {
        if (Owner == null || Owner.Registry == null) return null;

        Building nearestBuilding = null;
        float minDistance = float.MaxValue;

        // Iterate only through the buildings belonging to the player
        var playerBuildings = Owner.Registry.GetBuildings();

        for (int i = 0; i < playerBuildings.Count; i++)
        {
            Building building = playerBuildings[i];
            if (building == null || !building.IsFullyBuilt) continue;

            // Check if the building has a dropoff component and accepts the resource
            ResourceDropoff dropoff = building.DropoffComponent;
            if (dropoff != null && dropoff.AcceptsResource(currentResourceType))
            {
                float distance = GetDistanceToTargetSurface(building);
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
            searchRadius = (currentStats != null && currentStats.visionRange > 0)
                ? currentStats.visionRange
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

    #region Build Logic

    /// <summary>
    /// Invoked by an Animation Event during the build animation.
    /// </summary>
    public void OnBuildHit()
    {
        if (targetBuilding == null || targetBuilding.IsFullyBuilt)
            return;

        // Implement sound feedback
    }

    /// <summary>
    /// Finds the nearest allied building that is still under construction
    /// within the specified search radius.
    /// </summary>
    /// <param name="originPosition">
    /// Position from which the search is performed.
    /// </param>
    /// <param name="searchRadius">
    /// Search radius in world units. If less than or equal to zero,
    /// the unit's vision range will be used.
    /// </param>
    /// <returns>
    /// The closest unfinished allied building, or null if none is found.
    /// </returns>
    public Building FindNearestUnbuiltBuilding(Vector3 originPosition, float searchRadius = -1f)
    {
        if (Owner == null || Owner.Registry == null)
            return null;

        if (searchRadius <= 0f)
        {
            searchRadius =
                (currentStats != null && currentStats.visionRange > 0)
                ? currentStats.visionRange
                : 15f;
        }

        Building nearest = null;
        float minDistance = float.MaxValue;

        // Search all buildings owned by the player.
        var playerBuildings = Owner.Registry.GetBuildings();

        for (int i = 0; i < playerBuildings.Count; i++)
        {
            Building building = playerBuildings[i];

            if (building != null && !building.IsFullyBuilt)
            {
                float distance = Vector3.Distance(
                    originPosition,
                    building.transform.position);

                if (distance <= searchRadius &&
                    distance < minDistance)
                {
                    minDistance = distance;
                    nearest = building;
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
        private enum GatherSubState { MovingToResource, Extracting, MovingToDropoff }

        private readonly Villager villager;
        private readonly NavMeshAgent agent;
        private GatherSubState currentSubState;
        private float extractionTimer = 0;
        private const float BaseTimeBetweenHits = 1.2f;

        public VillagerGatherState(Villager unit) : base(unit)
        {
            villager = unit;
            agent = villager.GetComponent<NavMeshAgent>();
        }

        public override void Enter()
        {
            base.Enter();
            extractionTimer = 0f;

            if (villager.CurrentCarriedAmount >= villager.MaxCarryCapacity)
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
                    HandleExtracting();
                    break;

                case GatherSubState.MovingToDropoff:
                    HandleMovingToDropoff();
                    break;
            }
        }

        private void SetSubState(GatherSubState newSubState)
        {
            currentSubState = newSubState;

            switch (currentSubState)
            {
                case GatherSubState.MovingToResource:
                    villager.HideAllTools();
                    if (villager.TargetResource != null && agent != null)
                    {
                        agent.isStopped = false;
                        agent.SetDestination(villager.TargetResource.GetPosition());
                        villager.PlayWalkAnimation();
                    }
                    break;

                case GatherSubState.Extracting:
                    if (agent != null) agent.isStopped = true;
                    villager.UpdateToolForResourceType(villager.CurrentResourceType);
                    villager.PlayGatherAnimationForType(villager.CurrentResourceType);
                    break;

                case GatherSubState.MovingToDropoff:
                    villager.HideAllTools();
                    Vector3 dropoffPos = villager.FindNearestDropoffPoint();
                    if (agent != null)
                    {
                        agent.isStopped = false;
                        agent.SetDestination(dropoffPos);
                        villager.PlayWalkAnimation();
                    }
                    break;
            }
        }

        private void HandleMovingToResource()
        {
            if (villager.TargetResource == null || villager.TargetResource.GetQuantity() <= 0)
            {
                TryRelocateResource();
                return;
            }

            villager.lastResourcePosition = villager.TargetResource.GetPosition();

            // Measures distance from villager's bounds to the resource collider surface
            float surfaceDistance = villager.GetDistanceToTargetSurface(villager.TargetResource);
            float interactionRange = villager.GetInteractionRange();

            if (surfaceDistance <= interactionRange)
            {
                SetSubState(GatherSubState.Extracting);
            }
        }

        private void HandleExtracting()
        {
            if (villager.TargetResource == null || villager.TargetResource.GetQuantity() <= 0)
            {
                if (villager.CurrentCarriedAmount > 0) SetSubState(GatherSubState.MovingToDropoff);
                else TryRelocateResource();
                return;
            }

            // Gradually rotate towards the resource while the animation is playing
            Vector3 lookDir = villager.TargetResource.GetPosition() - villager.transform.position;
            lookDir.y = 0;
            if (lookDir.sqrMagnitude > 0.001f)
            {
                villager.transform.rotation = Quaternion.Slerp(
                    villager.transform.rotation,
                    Quaternion.LookRotation(lookDir),
                    Time.deltaTime * 10f
                );
            }

            // 2. Lógica del Temporizador de Extracción
            // Multiplicamos por la velocidad de recolección (así el timer avanza más rápido si hay mejoras)
            float gatherSpeed = (villager.currentStats != null) ? villager.currentStats.gatherSpeed : 1f;
            extractionTimer += Time.deltaTime * gatherSpeed;

            // 3. ¿El temporizador superó el tiempo de un golpe?
            if (extractionTimer >= BaseTimeBetweenHits)
            {
                // Restamos el tiempo en lugar de ponerlo en 0 para no perder precisión en los decimales
                extractionTimer -= BaseTimeBetweenHits;

                // ¡Golpe Lógico! Hacemos la extracción real de recursos
                int extracted = villager.TargetResource.Extract(villager.ExtractionAmountPerCycle);
                villager.AddCarriedResources(extracted);

                // Si se llenó el inventario tras este golpe, ir a dejar los recursos
                if (villager.CurrentCarriedAmount >= villager.MaxCarryCapacity)
                {
                    SetSubState(GatherSubState.MovingToDropoff);
                    return;
                }
            }
        }

        private void HandleMovingToDropoff()
        {
            Building dropoffBuilding = villager.FindNearestDropoffBuilding();

            if (dropoffBuilding == null)
            {
                villager.Wait();
                return;
            }

            // Measure the distance directly against the building's collider/surface
            float surfaceDistance = villager.GetDistanceToTargetSurface(dropoffBuilding);
            float interactionRange = villager.GetInteractionRange();

            if (surfaceDistance <= interactionRange)
            {
                if (agent != null) agent.isStopped = true; // Stop movement at the edge

                villager.DeliverResources();

                if (villager.TargetResource != null && villager.TargetResource.GetQuantity() > 0)
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
            Resource nextResource = villager.FindNearestResourceOfType(
                villager.CurrentResourceType,
                villager.LastResourcePosition
            );

            if (nextResource != null)
            {
                villager.StartGathering(nextResource);
            }
            else
            {
                villager.Wait(); // Return cleanly to IdleState when no resources remain nearby
            }
        }

        public override UnitBehaviourState GetBehaviourState() => UnitBehaviourState.Moving;
    }

    #endregion

    #region Nested Build State Machine

    protected class VillagerBuildState : UnitState
    {
        private enum BuildSubState { MovingToBuilding, Constructing }

        private readonly Villager villager;
        private readonly NavMeshAgent agent;
        private BuildSubState currentSubState;
        private float buildProgressAccumulator = 0f;

        public VillagerBuildState(Villager unit) : base(unit)
        {
            villager = unit;
            agent = villager.GetComponent<NavMeshAgent>();
        }

        public override void Enter()
        {
            base.Enter();
            buildProgressAccumulator = 0f;
            SetSubState(BuildSubState.MovingToBuilding);
        }

        public override void Tick()
        {
            base.Tick();

            switch (currentSubState)
            {
                case BuildSubState.MovingToBuilding:
                    HandleMoving();
                    break;
                case BuildSubState.Constructing:
                    HandleConstructing();
                    break;
            }
        }

        private void SetSubState(BuildSubState newSubState)
        {
            currentSubState = newSubState;

            if (currentSubState == BuildSubState.MovingToBuilding)
            {
                villager.HideAllTools();
                if (villager.targetBuilding != null && agent != null)
                {
                    agent.isStopped = false;
                    agent.SetDestination(villager.targetBuilding.transform.position);
                    villager.PlayWalkAnimation();
                }
            }
            else if (currentSubState == BuildSubState.Constructing)
            {
                if (agent != null) agent.isStopped = true;
                villager.PlayBuildAnimation();
            }
        }

        private void HandleMoving()
        {
            if (villager.targetBuilding == null || villager.targetBuilding.IsFullyBuilt)
            {
                TryFindAnotherBuilding();
                return;
            }

            float surfaceDistance = villager.GetDistanceToTargetSurface(villager.targetBuilding);
            if (surfaceDistance <= villager.GetInteractionRange())
            {
                SetSubState(BuildSubState.Constructing);
            }
        }

        /// <summary>
        /// Handles the active building process while the villager
        /// is assigned to an unfinished structure.
        /// </summary>
        private void HandleConstructing()
        {
            if (villager.targetBuilding == null || villager.targetBuilding.IsFullyBuilt)
            {
                TryFindAnotherBuilding();
                return;
            }

            Vector3 lookDir =
                villager.targetBuilding.transform.position -
                villager.transform.position;

            lookDir.y = 0;

            if (lookDir.sqrMagnitude > 0.001f)
            {
                villager.transform.rotation = Quaternion.Slerp(
                    villager.transform.rotation,
                    Quaternion.LookRotation(lookDir),
                    Time.deltaTime * 10f);
            }

            if (villager.GetDistanceToTargetSurface(villager.targetBuilding) <=
                villager.GetInteractionRange() + 0.2f)
            {
                float baseBuildHealth = (villager.currentStats != null)
                        ? villager.currentStats.buildHealth : 10f;

                float buildSpeedMultiplier = (villager.currentStats != null)
                        ? villager.currentStats.buildSpeed : 1f;

                // Accumulate fractional construction progress for this frame.
                buildProgressAccumulator += baseBuildHealth *
                    buildSpeedMultiplier * Time.deltaTime;

                // Apply construction progress only when at least one full
                // health point has been accumulated.
                if (buildProgressAccumulator >= 1f)
                {
                    // Extract the integer portion of the accumulated progress.
                    int healthPointsToAdd = Mathf.FloorToInt(buildProgressAccumulator);

                    // Keep the remaining fractional progress for future frames.
                    buildProgressAccumulator -= healthPointsToAdd;

                    // Apply the accumulated construction progress.
                    villager.targetBuilding.AddHealth(healthPointsToAdd);
                }
            }
            else
            {
                SetSubState(BuildSubState.MovingToBuilding);
            }
        }

        private void TryFindAnotherBuilding()
        {
            Building nextBuilding = villager.FindNearestUnbuiltBuilding(villager.transform.position);

            if (nextBuilding != null)
            {
                villager.StartBuilding(nextBuilding);
            }
            else
            {
                villager.Wait();
            }
        }

        public override UnitBehaviourState GetBehaviourState() => UnitBehaviourState.Moving;
    }

    #endregion
}