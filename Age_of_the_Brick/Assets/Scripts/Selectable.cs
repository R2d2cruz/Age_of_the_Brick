using UnityEngine;

public enum SelectableState
{
    Alive,
    Dying,
    Dead
}

public class Selectable : MonoBehaviour
{
    [Header("Ownership")]
    [Tooltip("0 = Neutral/independiente, >0 = jugador dueño")]
    public int ownerPlayerId;

    [Header("Selection Visuals")]
    [Tooltip("Círculo u objeto que aparece al seleccionar")]
    public GameObject selectionCircle;

    [Header("Meshes a recolorear")]
    [Tooltip("Meshes de esta unidad que cambiarán según el jugador dueño")]
    public Renderer[] meshesToRecolor;

    protected bool isSelected;
    
    [Header("Estado general")]
    public SelectableState state = SelectableState.Alive;

    // Cache estático para evitar búsquedas repetidas
    private static PlayerManager cachedManager;

    // Color/material actual aplicado (evita reasignar el mismo material)
    private Material currentMaterialApplied;

    protected virtual void Start()
    {
        // Desactivar el círculo de selección al iniciar
        if (selectionCircle != null)
            selectionCircle.SetActive(false);

        // Cachear el PlayerManager (solo una vez)
        if (cachedManager == null)
            cachedManager = PlayerManager.Instance;

        // Aplicar color inicial
        ApplyOwnerMaterial();
    }

    // --------------------
    // Selección
    // --------------------
    public void Select()
    {
        isSelected = true;
        if (selectionCircle != null)
            selectionCircle.SetActive(true);
    }

    public void Deselect()
    {
        isSelected = false;
        if (selectionCircle != null)
            selectionCircle.SetActive(false);
    }

    public bool IsSelected => isSelected;

    // --------------------
    // Ownership
    // --------------------
    public bool IsOwnedBy(int playerId)
    {
        return ownerPlayerId == playerId;
    }

    public bool IsEnemyTo(int playerId)
    {
        return ownerPlayerId != 0 && ownerPlayerId != playerId;
    }

    // --------------------
    // Cambio de dueño / Color
    // --------------------
    public void SetOwner(int newOwnerId)
    {
        if (ownerPlayerId == newOwnerId)
            return; // Evitar reconfigurar si no cambia el dueño

        ownerPlayerId = newOwnerId;
        ApplyOwnerMaterial();
    }

    private void ApplyOwnerMaterial()
    {
        if (meshesToRecolor == null || meshesToRecolor.Length == 0)
            return;

        if (cachedManager == null)
            cachedManager = PlayerManager.Instance;

        if (cachedManager == null)
        {
            Debug.LogWarning($"[Selectable] No se encontró PlayerManager para {gameObject.name}");
            return;
        }

        Material playerMaterial = cachedManager.GetPlayerMaterial(ownerPlayerId);
        if (playerMaterial == null) return;

        // Evitar reasignar el mismo material
        if (currentMaterialApplied == playerMaterial)
            return;

        currentMaterialApplied = playerMaterial;

        foreach (var mesh in meshesToRecolor)
        {
            if (mesh != null)
                mesh.material = playerMaterial;
        }
    }
}
