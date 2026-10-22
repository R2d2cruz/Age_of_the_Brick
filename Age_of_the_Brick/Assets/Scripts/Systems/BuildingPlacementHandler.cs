using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem; // Required for Keyboard.current

/// <summary>
/// Handles the grid-based placement of buildings.
/// Projects a ghost model on the ground, validates placement, and confirms construction.
/// </summary>
public class BuildingPlacementHandler : MonoBehaviour
{
    public static BuildingPlacementHandler Instance { get; private set; }

    [Header("Grid Settings")]
    [Tooltip("Size of each grid cell (e.g., 4 studs).")]
    [SerializeField] private float gridSize = 4f;
    [Tooltip("Escala física en Unity: cuánto mide 1 stud en coordenadas del mundo (ej. 0.8).")]
    [SerializeField] private float studSize = 0.8f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Visuals")]
    [Tooltip("Material used when placement is valid (Green Translucent).")]
    [SerializeField] private Material validMaterial;
    [Tooltip("Material used when placement is blocked (Red Translucent).")]
    [SerializeField] private Material invalidMaterial;

    private BuildingStats currentStatsToBuild;
    private Villager currentBuilder;
    private int playerEra;
    
    private GameObject ghostInstance;
    private MeshRenderer[] ghostRenderers;
    private bool isPlacementValid;
    private Camera mainCamera;

    public bool IsPlacing => ghostInstance != null;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        mainCamera = Camera.main;
    }

    public void BeginPlacement(BuildingStats stats, Villager builder, int era)
    {
        CancelPlacement();

        currentStatsToBuild = stats;
        currentBuilder = builder;
        playerEra = era;

        GameObject prefabToBuild = GetPrefabForEra(stats, era);
        if (prefabToBuild == null) return;

        ghostInstance = Instantiate(prefabToBuild);
        
        Destroy(ghostInstance.GetComponent<Building>());
        Destroy(ghostInstance.GetComponent<BuildingConstruction>());
        Destroy(ghostInstance.GetComponent<Collider>()); 
        
        ghostRenderers = ghostInstance.GetComponentsInChildren<MeshRenderer>();
    }

    private void Update()
    {
        if (!IsPlacing) return;

        var inputGameplay = InputManager.Instance.Actions.Gameplay;

        // 1. Cancel placement on Right Click or Escape Key
        // Fíjate en los paréntesis () al final de WasPressedThisFrame()
        if (inputGameplay.RightClick.WasPressedThisFrame() || (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame))
        {
            CancelPlacement();
            return;
        }

        UpdateGhostPositionAndValidation();

        // 2. Confirm placement on Left Click
        if (inputGameplay.LeftClick.WasPressedThisFrame() && isPlacementValid)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            ConfirmPlacement();
        }
    }

    private void UpdateGhostPositionAndValidation()
    {
        Vector2 mousePos = InputManager.Instance.GetMousePosition();
        Ray ray = mainCamera.ScreenPointToRay(mousePos);
        
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayer))
        {
            // 1. Calcular el tamaño real de la celda en el mundo de Unity
            float cellWorldSize = gridSize * studSize;

            // 2. Snap to Grid (Multiplicando por la escala real)
            float snappedX = Mathf.Round(hit.point.x / cellWorldSize) * cellWorldSize;
            float snappedZ = Mathf.Round(hit.point.z / cellWorldSize) * cellWorldSize;
            Vector3 snappedPosition = new Vector3(snappedX, hit.point.y, snappedZ);

            ghostInstance.transform.position = snappedPosition;

            // 3. Extents para Physics.CheckBox (Mitad del tamaño total)
            Vector2Int bSize = currentStatsToBuild.statsPerEra[playerEra].gridSize;
            
            // Tamaño total = (cantidad de studs ) * (tamaño del stud)
            float worldWidth = bSize.x * studSize;
            float worldLength = bSize.y * studSize;

            Vector3 extents = new Vector3(worldWidth / 2f, 2f, worldLength / 2f);

            isPlacementValid = !Physics.CheckBox(
                snappedPosition + Vector3.up * 2f, 
                extents - Vector3.one * 0.1f, // Margen de error pequeño para bordes
                Quaternion.identity, 
                obstacleLayer
            );

            UpdateGhostVisuals();
        }
    }

    private void UpdateGhostVisuals()
    {
        Material targetMat = isPlacementValid ? validMaterial : invalidMaterial;
        foreach (var r in ghostRenderers) r.material = targetMat;
    }

    private void ConfirmPlacement()
    {
        Vector3 finalPosition = ghostInstance.transform.position;
        GameObject prefabToBuild = GetPrefabForEra(currentStatsToBuild, playerEra);

        GameObject realBuildingObj = Instantiate(prefabToBuild, finalPosition, Quaternion.identity);
        Building realBuilding = realBuildingObj.GetComponent<Building>();
        
        if (currentBuilder != null && realBuilding != null)
        {
            //currentBuilder.StartBuilding(realBuilding); 
        }

        CancelPlacement();
    }

    public void CancelPlacement()
    {
        if (ghostInstance != null) Destroy(ghostInstance);
        currentStatsToBuild = null;
        currentBuilder = null;
    }

    private GameObject GetPrefabForEra(BuildingStats stats, int era)
    {
        if (stats.prefabsPerEra != null && stats.prefabsPerEra.Length > 0)
        {
            return (stats.hasVisualEvolution && era < stats.prefabsPerEra.Length) 
                ? stats.prefabsPerEra[era] : stats.prefabsPerEra[0];
        }
        return null;
    }
    
    private void OnDrawGizmos()
    {
        if (ghostInstance != null && currentStatsToBuild != null)
        {
            Gizmos.color = isPlacementValid ? new Color(0, 1, 0, 0.3f) : new Color(1, 0, 0, 0.3f);
            
            Vector2Int bSize = currentStatsToBuild.statsPerEra[playerEra].gridSize;
            
            // Mismo cálculo para los Gizmos visuales
            float worldWidth = bSize.x * studSize;
            float worldLength = bSize.y * studSize;
            Vector3 extents = new Vector3(worldWidth / 2f, 2f, worldLength / 2f);
            
            Vector3 center = ghostInstance.transform.position + Vector3.up * 2f;

            Gizmos.DrawCube(center, extents * 2f); 
            Gizmos.DrawWireCube(center, extents * 2f);
        }
    }
}