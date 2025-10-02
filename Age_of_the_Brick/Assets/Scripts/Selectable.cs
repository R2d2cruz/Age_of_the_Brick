using UnityEngine;

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

    private bool isSelected;

    protected virtual void Awake()
    {
        if (selectionCircle != null)
            selectionCircle.SetActive(false);

        // 👇 importante: aplicar color inicial
        ApplyOwnerMaterial();
    }

    // --------------------
    // Selección / Deselección
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
        ownerPlayerId = newOwnerId;
        ApplyOwnerMaterial();
    }

    private void ApplyOwnerMaterial()
    {
        if (meshesToRecolor == null || meshesToRecolor.Length == 0)
            return;

        Material playerMaterial = PlayerManager.Instance.GetPlayerMaterial(ownerPlayerId);

        foreach (var mesh in meshesToRecolor)
        {
            if (mesh != null)
            {
                mesh.material = playerMaterial;
            }
        }
    }

}
