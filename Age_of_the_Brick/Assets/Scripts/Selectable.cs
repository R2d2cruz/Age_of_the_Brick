using UnityEngine;

public class Selectable : MonoBehaviour
{
    [Header("Ownership")]
    [Tooltip("0 = Neutral/independiente, >0 = jugador dueño")]
    public int ownerPlayerId;  

    [Header("Selection Visuals")]
    [Tooltip("Círculo u objeto que aparece al seleccionar")]
    public GameObject selectionCircle; 

    private bool isSelected;

    private void Awake()
    {
        if (selectionCircle != null)
            selectionCircle.SetActive(false);
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

    // En caso de que quieras saber desde fuera si sigue seleccionado
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
}
