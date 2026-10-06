using UnityEngine;
using UnityEngine.AI;

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

    // -------------------------
    // INITIALIZATION
    // -------------------------
    protected override void Start()
    {
        base.Start();
        gatherState = CreateGatherState();
        HideAllTools();
    }

    protected virtual UnitState CreateGatherState()
    {
        return new VillagerGatherState(this);
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
        if (animator != null)
        {
            animator.SetBool("cut", true);
            animator.SetBool("isMoving", false);
        }
    }

    protected void PlayMinningAnimation()
    {
        if (animator != null)
        {
            animator.SetBool("mine", true);
            animator.SetBool("isMoving", false);
        }
    }

    protected void PlayFarmingAnimation()
    {
        if (animator != null)
        {
            animator.SetBool("farm", true);
            animator.SetBool("isMoving", false);
        }
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
            /* Building building = target.GetComponent<Building>();
            if (building != null && !building.IsFullyBuilt)
            {
                StartBuilding(building);
            } */
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

        // Validate the distance to the target surface before applying the gathering
        if (GetDistanceToTargetSurface(targetResource) > GetInteractionRange()) return;

        // Extract and store
        int extracted = targetResource.Extract(extractionAmountPerCycle);
        AddCarriedResources(extracted);
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
        Debug.Log(currentCarriedAmount);
    }

    /// <summary>
    /// Resets the current inventory carried quantity to zero.
    /// </summary>
    public void EmptyCarriedResources()
    {
        currentCarriedAmount = 0;
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
    /// Finds the closest allied drop-off location (Town Center or Storage Building).
    /// </summary>
    /// <returns>World space position vector of the target drop-off.</returns>
    public Vector3 FindNearestDropoffPoint()
    {
        // Placeholder: Returns current transform position or finds closest Town Center
        return transform.position;
    }

    /// <summary>
    /// Transfers carried inventory to the player's global economy pool.
    /// </summary>
    public void DeliverResources()
    {
        if (currentCarriedAmount > 0)
        {
            // PlayerManager.Instance.AddResource(ownerPlayerId, currentResourceType, currentCarriedAmount);
            Debug.Log($"[Villager] Delivered {currentCarriedAmount} {currentResourceType} to dropoff.");
            EmptyCarriedResources();
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
        private float extractionTimer;

        public VillagerGatherState(Villager unit) : base(unit)
        {
            villager = unit;
            agent = villager.GetComponent<NavMeshAgent>();
        }

        public override void Enter()
        {
            base.Enter();

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
                if (villager.CurrentCarriedAmount > 0)
                {
                    SetSubState(GatherSubState.MovingToDropoff);
                }
                else
                {
                    TryRelocateResource();
                }
                return;
            }

            // If the inventory is full (modified by the Animation Event OnGatherHit)
            if (villager.CurrentCarriedAmount >= villager.MaxCarryCapacity)
            {
                SetSubState(GatherSubState.MovingToDropoff);
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
        }

        private void HandleMovingToDropoff()
        {
            Vector3 dropoffPos = villager.FindNearestDropoffPoint();

            // If FindNearestDropoffPoint returns a Building reference, pass its collider for precise bounds
            // For now, using point surface calculation:
            float surfaceDistance = villager.GetDistanceToPointSurface(dropoffPos);
            float interactionRange = villager.GetInteractionRange();

            if (surfaceDistance <= interactionRange)
            {
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
}