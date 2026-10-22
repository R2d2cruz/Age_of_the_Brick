using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the bottom selection stats panel in the HUD.
/// Displays portrait, health, attack, and defense for the primary selected entity.
/// </summary>
public class UISelectionStatsPanel : MonoBehaviour
{
    [Header("UI Containers")]
    [SerializeField] private GameObject panelContainer;

    [Header("Identity Elements")]
    [SerializeField] private Image portraitImage;
    [SerializeField] private TextMeshProUGUI nameText;

    [Header("Health Elements")]
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private Slider slider;
    [SerializeField] private Image healthBarFill;

    [Header("Attributes Elements")]
    [SerializeField] private TextMeshProUGUI attackText;
    [SerializeField] private TextMeshProUGUI defenseText;
    [SerializeField] private TextMeshProUGUI rangeText;
    [SerializeField] private TextMeshProUGUI speedText;

    [Header("Appearance")]
    [SerializeField] private Color healthyColor = Color.green;
    [SerializeField] private Color warningColor = Color.yellow;
    [SerializeField] private Color dangerColor = Color.red;

    private Selectable currentTrackedEntity;

    private void Start()
    {
        if (SelectionHandler.Instance != null)
        {
            SelectionHandler.Instance.OnSelectionChanged += HandleSelectionChanged;
        }

        HidePanel();
    }

    private void OnDestroy()
    {
        if (SelectionHandler.Instance != null)
        {
            SelectionHandler.Instance.OnSelectionChanged -= HandleSelectionChanged;
        }
    }

    /// <summary>
    /// Event callback triggered when the selection changes in SelectionHandler.
    /// </summary>
    private void HandleSelectionChanged(List<Selectable> selectedList)
    {
        if (selectedList == null || selectedList.Count == 0)
        {
            currentTrackedEntity = null;
            HidePanel();
            return;
        }

        // Always pick the FIRST element in the selection list
        Selectable primary = selectedList[0];

        if (primary != currentTrackedEntity)
        {
            currentTrackedEntity = primary;
            ShowPanel();
            UpdateStaticInfo(primary);
        }

        UpdateDynamicInfo();
    }

    private void Update()
    {
        // Keep dynamic UI elements (like health) updated in real time
        if (currentTrackedEntity != null && panelContainer.activeSelf)
        {
            UpdateDynamicInfo();
        }
    }

    /// <summary>
    /// Updates static information (Portrait from unit.currentStats.image, Name, Base Attack, Base Defense).
    /// </summary>
    private void UpdateStaticInfo(Selectable selectable)
    {
        if (selectable is Unit unit)
        {
            // Name formatting (stripping "(Clone)" if instantiated)
            nameText.text = unit.gameObject.name.Replace("(Clone)", "").Trim();

            // Fetch image sprite directly from unit.currentStats (EraStats)
            if (unit.currentStats != null && unit.currentStats.image != null)
            {
                portraitImage.sprite = unit.currentStats.image;
                portraitImage.gameObject.SetActive(true);
            }
            else
            {
                portraitImage.gameObject.SetActive(false);
            }

            // Consolidated Attack & Defense from EraStats
            if (unit.currentStats != null)
            {
                int totalAttack = unit.currentStats.lightAttack + unit.currentStats.heavyAttack;
                int totalDefense = unit.currentStats.lightDefense + unit.currentStats.heavyDefense;

                if (attackText != null) attackText.text = totalAttack.ToString();
                if (defenseText != null) defenseText.text = totalDefense.ToString();
                if (rangeText != null) { rangeText.text = unit.currentStats.visionRange.ToString() + (unit.currentStats.range > 1 ? " / " + unit.currentStats.range.ToString() : ""); }
                if (speedText != null) speedText.text = unit.currentStats.moveSpeed.ToString();
            }
        }
        else if (selectable is Building building)
        {
            nameText.text = building.gameObject.name.Replace("(Clone)", "").Trim();

            // Fetch image sprite directly from building.currentStats (EraStats)
            if (building.currentStats != null && building.currentStats.image != null)
            {
                portraitImage.sprite = building.currentStats.image;
                portraitImage.gameObject.SetActive(true);
            }
            else
            {
                portraitImage.gameObject.SetActive(false);
            }
            int totalAttack = building.currentStats.lightAttack + building.currentStats.heavyAttack;
            int totalDefense = building.currentStats.lightDefense + building.currentStats.heavyDefense;

            if (attackText != null) attackText.text = totalAttack > 0 ? totalAttack.ToString() : "";
            if (defenseText != null) defenseText.text = totalDefense > 0 ? totalDefense.ToString() : "";
            if (rangeText != null) rangeText.text = "-";
            if (speedText != null) speedText.text = "-";
        }
    }

    /// <summary>
    /// Updates dynamic information (Current HP vs Max HP, Health Bar Fill).
    /// </summary>
    private void UpdateDynamicInfo()
    {
        if (currentTrackedEntity == null) return;

        float currentHp = 0f;
        float maxHp = 1f;

        if (currentTrackedEntity is Unit unit)
        {
            currentHp = unit.currentHealth;
            // Get max health from current EraStats
            maxHp = unit.currentStats != null ? unit.currentStats.health : 1f;
        }
        else if (currentTrackedEntity is Building building)
        {
            currentHp = building.currentHealth;
            maxHp = building.GetMaxVida();
        }

        if (healthText != null)
        {
            healthText.text = $"{Mathf.Max(Mathf.CeilToInt(currentHp), 0)} / {Mathf.CeilToInt(maxHp)}";
        }

        if (healthBarFill != null)
        {
            float percent = currentHp / maxHp;
            slider.value = percent;
            if (percent > 0.6f) healthBarFill.color = healthyColor;
            else if (percent > 0.3f) healthBarFill.color = warningColor;
            else healthBarFill.color = dangerColor;
        }
    }

    private void ShowPanel() => panelContainer.SetActive(true);
    private void HidePanel() => panelContainer.SetActive(false);
}