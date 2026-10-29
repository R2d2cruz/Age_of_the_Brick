using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD panel showing local player population as DisplayUsed / EffectiveMax.
/// </summary>
public class UICivController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI populationText;
    [SerializeField] private Image populationIcon;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color alertColor = new Color(1f, 0.35f, 0.2f);

    private Player localPlayer;

    /// <summary>
    /// Links the UI to a specific player and refreshes immediately.
    /// </summary>
    public void Initialize(Player player)
    {
        if (localPlayer != null)
        {
            localPlayer.OnPopulationChanged -= HandlePopulationChanged;
            localPlayer.OnPopulationReservedChanged -= HandlePopulationReservedChanged;
        }

        localPlayer = player;

        if (localPlayer != null)
        {
            localPlayer.OnPopulationChanged += HandlePopulationChanged;
            localPlayer.OnPopulationReservedChanged += HandlePopulationReservedChanged;
        }

        Refresh();
    }

    private void OnDestroy()
    {
        if (localPlayer == null)
            return;

        localPlayer.OnPopulationChanged -= HandlePopulationChanged;
        localPlayer.OnPopulationReservedChanged -= HandlePopulationReservedChanged;
    }

    private void HandlePopulationChanged(int used, int effectiveMax)
    {
        Refresh();
    }

    private void HandlePopulationReservedChanged(int reserved)
    {
        Refresh();
    }

    private void Refresh()
    {
        Debug.Log("refresh");
        if (populationText == null)
            return;
        if (localPlayer == null)
        {
            populationText.text = "0 / 0";
            populationText.color = normalColor;
            return;
        }

        int used = localPlayer.PopulationDisplayUsed;
        int max = localPlayer.EffectiveMax;
        populationText.text = $"{used} / {max}";
        populationText.color = (used > 0 && used >= max) ? alertColor : normalColor;
    }
}
