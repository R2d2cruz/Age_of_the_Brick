using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Unit))]
public class BuilderComponent : BaseUnitComponent, IBuilder
{
    [Header("Build Settings")]
    protected UnitState buildState;
    [Header("Builder Commands")]
    [SerializeField] private List<CommandSO> builderCommands;

    /// <summary>
    /// Building currently assigned for construction.
    /// </summary>
    public Building TargetBuilding { get; private set; }

    /// <summary>
    /// Initializes the builder component and creates
    /// the state responsible for construction logic.
    /// </summary>
    /// <param name="coreUnit">
    /// Unit that owns this component.
    /// </param>
    public override void Initialize(Unit coreUnit)
    {
        base.Initialize(coreUnit);
        buildState = CreateBuildState();
    }

    /// <summary>
    /// Creates the state responsible for construction behavior.
    /// Can be overridden by derived classes to provide
    /// custom building implementations.
    /// </summary>
    /// <returns>
    /// Construction state used by this builder.
    /// </returns>
    protected virtual UnitState CreateBuildState()
    {
        return new VillagerBuildState(unit, this);
    }

    /// <summary>
    /// Assigns a building as the current construction target
    /// and switches the unit to its building state.
    /// </summary>
    /// <param name="building">
    /// Building to construct.
    /// </param>
    public void StartBuilding(Building building)

    {
        if (building == null || building.IsFullyBuilt) return;

        TargetBuilding = building;
        unit.ChangeState(buildState);
    }

    /// <summary>
    /// Returns all command actions provided by this builder component.
    /// </summary>
    /// <returns>
    /// List of available builder commands.
    /// </returns>
    public override List<CommandAction> GetAvailableActions()
    {
        List<CommandAction> actions = new();
        foreach (var cmd in builderCommands)
        {
            if (cmd.ShouldDisplayAction(unit))
            {
                actions.Add(cmd.CreateRuntimeAction(unit));
            }
        }
        return actions;
    }

    /// <summary>
    /// Updates construction animation speed when the owning player
    /// advances to a new era.
    /// </summary>
    /// <param name="newEra">
    /// New era index.
    /// </param>
    public override void OnEraChanged(int newEra)
    {
        unit.ChangeAnimatorMultiplier("buildSpeed", unit.currentStats.buildSpeed);
    }


    /// <summary>
    /// Plays the construction animation.
    /// </summary>
    public void PlayBuildAnimation()
    {
        unit.ChangeAnimation("build");
    }

    /// <summary>
    /// Tells the Placement Handler to start the ghost placement mode.
    /// </summary>
    public void StartPlacingBuilding(BuildingStats buildingStats)
    {
        if (unit.Owner != null)
        {
            BuildingPlacementHandler.Instance.BeginPlacement(buildingStats, this, unit.Owner.CurrentEra);
        }
    }

    #region Build Logic

    /// <summary>
    /// Invoked by an Animation Event during the build animation.
    /// </summary>
    public void OnBuildHit()
    {
        if (TargetBuilding == null || TargetBuilding.IsFullyBuilt)
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
        if (unit.Owner == null || unit.Owner.Registry == null)
            return null;

        if (searchRadius <= 0f)
        {
            searchRadius =
                (unit.currentStats != null && unit.currentStats.visionRange > 0)
                ? unit.currentStats.visionRange
                : 15f;
        }

        Building nearest = null;
        float minDistance = float.MaxValue;

        // Search all buildings owned by the player.
        var playerBuildings = unit.Owner.Registry.GetBuildings();

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

    #region Nested Build State Machine

    protected class VillagerBuildState : UnitState
    {
        /// <summary>
        /// Internal workflow states for construction behavior.
        /// </summary>
        private enum BuildSubState
        {
            /// <summary>
            /// Moving toward the assigned building.
            /// </summary>
            MovingToBuilding,

            /// <summary>
            /// Actively constructing the assigned building.
            /// </summary>
            Constructing
        }

        private readonly BuilderComponent builderComponent;
        private readonly UnityEngine.AI.NavMeshAgent agent;
        private BuildSubState currentSubState;
        private float buildProgressAccumulator = 0f;

        public VillagerBuildState(Unit unit, BuilderComponent builderComponent) : base(unit)
        {
            agent = unit.GetComponent<UnityEngine.AI.NavMeshAgent>();
            this.builderComponent = builderComponent;
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

        /// <summary>
        /// Switches the active construction sub-state
        /// and updates movement and animations accordingly.
        /// </summary>
        /// <param name="newSubState">
        /// New construction sub-state.
        /// </param>
        private void SetSubState(BuildSubState newSubState)
        {
            currentSubState = newSubState;

            if (currentSubState == BuildSubState.MovingToBuilding)
            {
                if (builderComponent.TargetBuilding != null && agent != null)
                {
                    agent.isStopped = false;
                    agent.SetDestination(builderComponent.TargetBuilding.transform.position);
                    unit.PlayWalkAnimation();
                }
            }
            else if (currentSubState == BuildSubState.Constructing)
            {
                if (agent != null) agent.isStopped = true;
                builderComponent.PlayBuildAnimation();
            }
        }

        /// <summary>
        /// Handles movement toward the target building.
        /// </summary>
        private void HandleMoving()
        {
            if (builderComponent.TargetBuilding == null || builderComponent.TargetBuilding.IsFullyBuilt)
            {
                TryFindAnotherBuilding();
                return;
            }

            float surfaceDistance = unit.GetDistanceToTargetSurface(builderComponent.TargetBuilding);
            if (surfaceDistance <= unit.GetInteractionRange())
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
            if (builderComponent.TargetBuilding == null || builderComponent.TargetBuilding.IsFullyBuilt)
            {
                TryFindAnotherBuilding();
                return;
            }

            Vector3 lookDir =
                builderComponent.TargetBuilding.transform.position -
                unit.transform.position;

            lookDir.y = 0;

            if (lookDir.sqrMagnitude > 0.001f)
            {
                unit.transform.rotation = Quaternion.Slerp(
                    unit.transform.rotation,
                    Quaternion.LookRotation(lookDir),
                    Time.deltaTime * 10f);
            }

            if (unit.GetDistanceToTargetSurface(builderComponent.TargetBuilding) <=
                unit.GetInteractionRange() + 0.2f)
            {
                float baseBuildHealth = (unit.currentStats != null)
                        ? unit.currentStats.buildHealth : 10f;

                float buildSpeedMultiplier = (unit.currentStats != null)
                        ? unit.currentStats.buildSpeed : 1f;

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
                    builderComponent.TargetBuilding.AddHealth(healthPointsToAdd);
                }
            }
            else
            {
                SetSubState(BuildSubState.MovingToBuilding);
            }
        }

        /// <summary>
        /// Attempts to find another unfinished allied building
        /// and continue construction automatically.
        /// </summary>
        private void TryFindAnotherBuilding()
        {
            Building nextBuilding = builderComponent.FindNearestUnbuiltBuilding(unit.transform.position);

            if (nextBuilding != null)
            {
                builderComponent.StartBuilding(nextBuilding);
            }
            else
            {
                unit.Wait();
            }
        }

        public override UnitBehaviourState GetBehaviourState()
        {
            return currentSubState switch
            {
                BuildSubState.MovingToBuilding => UnitBehaviourState.Moving,
                BuildSubState.Constructing => UnitBehaviourState.Working,
                _ => UnitBehaviourState.Idle
            };
        }
    }

    #endregion
}
