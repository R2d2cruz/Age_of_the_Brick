using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIResourceItem : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private ResourceType resourceType;

    [Header("UI References")]
    [SerializeField] private Image resourceIcon;
    [SerializeField] private TextMeshProUGUI amountText;

    public ResourceType ResourceType => resourceType;

    /// <summary>
    /// Updates the numeric value visible in the interface.
    /// </summary>
    public void UpdateAmount(int amount)
    {
        if (amountText != null)
        {
            amountText.text = amount.ToString("N0"); // Format with thousand separators
        }
    }

    /// <summary>
    /// Assigns the icon if configured dynamically from the manager.
    /// </summary>
    public void SetIcon(Sprite icon)
    {
        if (resourceIcon != null && icon != null)
        {
            resourceIcon.sprite = icon;
        }
    }
}
