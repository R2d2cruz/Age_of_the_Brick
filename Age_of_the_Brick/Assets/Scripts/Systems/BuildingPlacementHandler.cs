using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Handles the grid-based placement of buildings.
/// Projects a ghost model on the ground, validates placement,
/// and confirms construction.
/// </summary>
public class BuildingPlacementHandler : MonoBehaviour
{
    /// <summary>
    /// Singleton instance used to manage building placement.
    /// </summary>
    public static BuildingPlacementHandler Instance { get; private set; }

    [Header("Grid Settings")]

    /// <summary>
    /// Size of a grid cell measured in studs.
    /// </summary>
    [Tooltip("Size of each grid cell (e.g., 4 studs).")]
    [SerializeField] private float gridSize = 4f;

    /// <summary>
    /// Physical world-space size represented by a single stud.
    /// Used to convert logical grid dimensions into Unity units.
    /// </summary>
    [Tooltip("Physical size of one stud in world-space units.")]
    [SerializeField] private float studSize = 0.8f;

    /// <summary>
    /// Layer used to detect valid ground surfaces.
    /// </summary>
    [SerializeField] private LayerMask groundLayer;

    /// <summary>
    /// Layer used to detect placement obstacles.
    /// </summary>
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Visuals")]

    /// <summary>
    /// Material applied when the current placement location is valid.
    /// </summary>
    [Tooltip("Material used when placement is valid (green translucent).")]
    [SerializeField] private Material validMaterial;

    /// <summary>
    /// Material applied when the current placement location is invalid.
    /// </summary>
    [Tooltip("Material used when placement is blocked (red translucent).")]
    [SerializeField] private Material invalidMaterial;

    /// <summary>
    /// Building configuration currently being placed.
    /// </summary>
    private BuildingStats currentStatsToBuild;

    /// <summary>
    /// Villager responsible for constructing the building.
    /// </summary>
    private BuilderComponent currentBuilder;

    /// <summary>
    /// Current player era used to determine stats and visuals.
    /// </summary>
    private int playerEra;

    /// <summary>
    /// Ghost object displayed during placement.
    /// </summary>
    private GameObject ghostInstance;

    /// <summary>
    /// Cached mesh renderers used to update placement visuals.
    /// </summary>
    private MeshRenderer[] ghostRenderers;

    /// <summary>
    /// Indicates whether the current placement location is valid.
    /// </summary>
    private bool isPlacementValid;

    /// <summary>
    /// Cached reference to the main camera.
    /// </summary>
    private Camera mainCamera;

    /// <summary>
    /// Returns whether a building placement operation is currently active.
    /// </summary>
    public bool IsPlacing => ghostInstance != null;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        mainCamera = Camera.main;
    }

    /// <summary>
    /// Starts the building placement process.
    /// Creates a ghost preview and prepares placement validation.
    /// </summary>
    /// <param name="stats">
    /// Building definition to place.
    /// </param>
    /// <param name="builder">
    /// Unit responsible for constructing the building.
    /// </param>
    /// <param name="era">
    /// Current player era.
    /// </param>
    public void BeginPlacement(BuildingStats stats, BuilderComponent builder, int era)
    {
        CancelPlacement();

        currentStatsToBuild = stats;
        currentBuilder = builder;
        playerEra = era;

        GameObject prefabToBuild = GetPrefabForEra(stats, era);

        if (prefabToBuild == null)
            return;

        // Retrieve the Building component from the prefab
        // without instantiating the full object.
        Building buildingScript = prefabToBuild.GetComponent<Building>();

        // Instantiate only the final visual model used as the placement preview.
        if (buildingScript != null && buildingScript.FinalMesh != null)
        {
            ghostInstance = Instantiate(buildingScript.FinalMesh);
        }
        else
        {
            // Fallback in case no final mesh has been assigned.
            ghostInstance = GameObject.CreatePrimitive(PrimitiveType.Cube);
        }

        // Disable all colliders on the ghost object.
        foreach (Collider col in ghostInstance.GetComponentsInChildren<Collider>())
        {
            col.enabled = false;
        }

        ghostRenderers = ghostInstance.GetComponentsInChildren<MeshRenderer>();
    }

    /// <summary>
    /// Updates the placement preview and handles confirmation
    /// or cancellation input.
    /// </summary>
    private void Update()
    {
        if (!IsPlacing)
            return;

        var inputGameplay = InputManager.Instance.Actions.Gameplay;

        // Cancel placement when the player presses
        // the right mouse button or Escape.
        if (inputGameplay.RightClick.WasPressedThisFrame() ||
            (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame))
        {
            CancelPlacement();
            return;
        }

        UpdateGhostPositionAndValidation();

        // Confirm placement when the left mouse button is pressed.
        if (inputGameplay.LeftClick.WasPressedThisFrame() && isPlacementValid)
        {
            if (EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject())
                return;

            ConfirmPlacement();
        }
    }

    /// <summary>
    /// Updates the ghost position using grid snapping
    /// and performs placement validation checks.
    /// </summary>
    private void UpdateGhostPositionAndValidation()
    {
        Vector2 mousePos = InputManager.Instance.GetMousePosition();
        Ray ray = mainCamera.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayer))
        {
            // Compute the actual grid cell size in world units.
            float cellWorldSize = gridSize * studSize;

            // Snap the building position to the placement grid.
            float snappedX = Mathf.Round(hit.point.x / cellWorldSize) * cellWorldSize;
            float snappedZ = Mathf.Round(hit.point.z / cellWorldSize) * cellWorldSize;

            Vector3 snappedPosition = new Vector3(snappedX, hit.point.y, snappedZ);
            ghostInstance.transform.position = snappedPosition;

            Vector2Int bSize = currentStatsToBuild.statsPerEra[playerEra].gridSize;

            // Total building size in world units.
            float worldWidth = bSize.x * studSize;
            float worldLength = bSize.y * studSize;

            // Compute collision extents for placement validation.
            Vector3 extents = new Vector3(worldWidth / 2f, 2f, worldLength / 2f);

            // Verify whether the placement area is blocked by any obstacle.
            bool isBlocked = Physics.CheckBox(
                snappedPosition + Vector3.up * 2f,
                extents - Vector3.one * 0.1f,
                Quaternion.identity,
                obstacleLayer);

            // Verify that the player has enough resources to construct the selected building.
            Player player = PlayerManager.Instance.GetPlayer(currentBuilder.OwnerPlayerId);
            bool hasResources = player != null && player.HasResourcesForBuilding(currentStatsToBuild);

            // Placement is allowed only when all requirements are satisfied.
            isPlacementValid = !isBlocked && hasResources;

            UpdateGhostVisuals();
        }
    }

    /// <summary>
    /// Updates the ghost material to reflect
    /// the current placement validity state.
    /// </summary>
    private void UpdateGhostVisuals()
    {
        Material targetMat = isPlacementValid ? validMaterial : invalidMaterial;

        foreach (var renderer in ghostRenderers)
        {
            renderer.material = targetMat;
        }
    }

    /// <summary>
    /// Finalizes the placement and spawns the actual building instance.
    /// </summary>
    private void ConfirmPlacement()
    {
        Vector3 finalPosition = ghostInstance.transform.position;
        GameObject prefabToBuild = GetPrefabForEra(currentStatsToBuild, playerEra);

        Player player = PlayerManager.Instance.GetPlayer(currentBuilder.OwnerPlayerId);

        player?.UseResources(currentStatsToBuild.constructionCosts);

        GameObject realBuildingObj = Instantiate(prefabToBuild, finalPosition, Quaternion.identity);
        Building realBuilding = realBuildingObj.GetComponent<Building>();

        realBuilding.Initialize(player);

        if (currentBuilder != null && realBuilding != null)
        {
            currentBuilder.StartBuilding(realBuilding);
        }

        CancelPlacement();
    }

    /// <summary>
    /// Cancels the current placement operation
    /// and removes the active ghost preview.
    /// </summary>
    public void CancelPlacement()
    {
        if (ghostInstance != null)
        {
            Destroy(ghostInstance);
            ghostInstance = null;
        }

        currentStatsToBuild = null;
        currentBuilder = null;
    }

    /// <summary>
    /// Returns the building prefab corresponding to the specified era.
    /// </summary>
    /// <param name="stats">
    /// Building configuration asset.
    /// </param>
    /// <param name="era">
    /// Era index requested.
    /// </param>
    /// <returns>
    /// Prefab associated with the specified era.
    /// </returns>
    private GameObject GetPrefabForEra(BuildingStats stats, int era)
    {
        if (stats.prefabsPerEra != null && stats.prefabsPerEra.Length > 0)
        {
            return (stats.hasVisualEvolution && era < stats.prefabsPerEra.Length) 
                ? stats.prefabsPerEra[era] : stats.prefabsPerEra[0];
        }
        return null;
    }

    /// <summary>
    /// Draws placement validation gizmos in the Scene view.
    /// Used for debugging building footprint calculations.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (ghostInstance != null && currentStatsToBuild != null)
        {
            Gizmos.color = isPlacementValid ? new Color(0, 1, 0, 0.3f) : new Color(1, 0, 0, 0.3f);

            Vector2Int bSize = currentStatsToBuild.statsPerEra[playerEra].gridSize;

            float worldWidth = bSize.x * studSize;
            float worldLength = bSize.y * studSize;

            Vector3 extents = new Vector3(worldWidth / 2f, 2f, worldLength / 2f);
            Vector3 center = ghostInstance.transform.position + Vector3.up * 2f;

            Gizmos.DrawCube(center, extents * 2f);
            Gizmos.DrawWireCube(center, extents * 2f);
        }
    }
}