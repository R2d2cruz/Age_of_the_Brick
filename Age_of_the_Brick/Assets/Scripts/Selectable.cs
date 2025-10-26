using UnityEngine;

/// <summary>
/// Represents any object that can be selected by the player (units, buildings, etc.).
/// It handles ownership, visual feedback, and color/material assignment based on the owning player.
/// </summary>
public enum SelectableState
{
    Alive,  // The object is active and functional
    Dying,  // The object has reached 0 HP but may continue acting for a short time
    Dead    // The object is fully dead and should be removed or deactivated
}

public class Selectable : MonoBehaviour
{
    // -------------------------
    // OWNERSHIP
    // -------------------------
    [Header("Ownership")]
    [Tooltip("0 = Neutral/Independent, >0 = Player who owns this object.")]
    public int ownerPlayerId;

    // -------------------------
    // SELECTION VISUALS
    // -------------------------
    [Header("Selection Visuals")]
    [Tooltip("Visual circle or indicator displayed when selected.")]
    public GameObject selectionCircle;

    // -------------------------
    // COLOR / MATERIAL CONTROL
    // -------------------------
    [Header("Meshes to Recolor")]
    [Tooltip("List of renderers whose materials change based on player ownership.")]
    public Renderer[] meshesToRecolor;

    // Whether this selectable is currently selected
    protected bool isSelected;

    // -------------------------
    // GENERAL STATE
    // -------------------------
    [Header("General State")]
    [Tooltip("Indicates whether the selectable is alive, dying, or dead.")]
    public SelectableState state = SelectableState.Alive;

    // Cached reference to PlayerManager to avoid repeated lookups
    private static PlayerManager cachedManager;

    // Currently applied material (to avoid reassigning the same one)
    private Material currentMaterialApplied;

    // -------------------------
    // UNITY METHODS
    // -------------------------
    protected virtual void Start()
    {
        // Disable selection circle by default
        if (selectionCircle != null)
            selectionCircle.SetActive(false);

        // Cache PlayerManager (once per session)
        if (cachedManager == null)
            cachedManager = PlayerManager.Instance;

        // Apply the correct material based on the current owner
        ApplyOwnerMaterial();
    }

    // -------------------------
    // SELECTION HANDLING
    // -------------------------
    /// <summary>
    /// Marks this object as selected and activates the visual indicator.
    /// </summary>
    public void Select()
    {
        isSelected = true;
        if (selectionCircle != null)
            selectionCircle.SetActive(true);
    }

    /// <summary>
    /// Deselects this object and hides the selection indicator.
    /// </summary>
    public void Deselect()
    {
        isSelected = false;
        if (selectionCircle != null)
            selectionCircle.SetActive(false);
    }

    /// <summary>
    /// Returns whether the object is currently selected.
    /// </summary>
    public bool IsSelected => isSelected;

    // -------------------------
    // OWNERSHIP CHECKS
    // -------------------------
    /// <summary>
    /// Checks if the object is owned by the given player.
    /// </summary>
    public bool IsOwnedBy(int playerId)
    {
        return ownerPlayerId == playerId;
    }

    /// <summary>
    /// Determines whether this object belongs to an enemy of the given player.
    /// </summary>
    public bool IsEnemyTo(int playerId)
    {
        return ownerPlayerId != 0 && ownerPlayerId != playerId;
    }

    // -------------------------
    // OWNER CHANGE / COLOR UPDATE
    // -------------------------
    /// <summary>
    /// Assigns a new owner to this object and updates its material accordingly.
    /// </summary>
    public void SetOwner(int newOwnerId)
    {
        if (ownerPlayerId == newOwnerId)
            return; // Skip if owner hasn't changed

        ownerPlayerId = newOwnerId;
        ApplyOwnerMaterial();
    }

    /// <summary>
    /// Applies the material associated with the owning player.
    /// </summary>
    private void ApplyOwnerMaterial()
    {
        if (meshesToRecolor == null || meshesToRecolor.Length == 0)
            return;

        if (cachedManager == null)
            cachedManager = PlayerManager.Instance;

        if (cachedManager == null)
        {
            Debug.LogWarning($"[Selectable] PlayerManager not found for {gameObject.name}");
            return;
        }

        Material playerMaterial = cachedManager.GetPlayerMaterial(ownerPlayerId);
        if (playerMaterial == null) return;

        // Avoid reassigning the same material
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
