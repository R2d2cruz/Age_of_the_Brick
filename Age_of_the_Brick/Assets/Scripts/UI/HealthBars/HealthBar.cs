using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// Handles the health bar UI for a unit. 
/// Displays a world-space bar that updates position, value, and color dynamically.
/// Works under a global Canvas (recommended for performance).
/// </summary>

public class HealthBar : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Slider component representing health percentage.")]
    [SerializeField] private Slider slider;

    [Tooltip("Image used to display the filled part of the health bar.")]
    [SerializeField] private Image fillImage;

    [Header("Appearance")]
    [SerializeField] private Color healthyColor = Color.green;
    [SerializeField] private Color warningColor = Color.yellow;
    [SerializeField] private Color dangerColor = Color.red;

    [Header("Visibility Settings")]
    [Tooltip("Time in seconds that the bar remains visible after a change.")]
    [SerializeField] private float showDuration = 3f; // tiempo visible tras cambio

    private Transform target;
    private Vector3 offset;
    private Camera cam;
    private float lastChangeTime;
    private bool isVisible;

    /// <summary>
    /// Initializes the health bar with its target transform and offset.
    /// </summary>
    public void Initialize(Transform target, Vector3 offset)
    {
        this.target = target;
        this.offset = offset;
        cam = Camera.main;
        
        // Inicializamos forzando el estado para evitar bugs visuales
        gameObject.SetActive(false);
        isVisible = false;
    }

    /// <summary>
    /// Updates the health bar value and color.
    /// </summary>
    public void UpdateHealth(float current, float max)
    {
        slider.maxValue = max;
        slider.value = current;

        float percent = current / max;
        if (percent > 0.6f) fillImage.color = healthyColor;
        else if (percent > 0.3f) fillImage.color = warningColor;
        else fillImage.color = dangerColor;

        // mostrar barra y resetear temporizador
        lastChangeTime = Time.time;
        SetVisibility(true);
    }

    private void LateUpdate()
    {
        if (target == null || cam == null) return;

        Vector3 screenPos = cam.WorldToScreenPoint(target.position + offset);
        transform.position = screenPos;

        // ocultar si pasó demasiado tiempo sin cambios
        if (isVisible && Time.time - lastChangeTime > showDuration)
            SetVisibility(false);
    }

    // Se cambia a public para que el Manager no rompa el estado "isVisible" si necesita ocultarlo
    public void SetVisibility(bool visible) 
    {
        if (visible != isVisible)
        {
            gameObject.SetActive(visible);
            isVisible = visible;
        }
    }
}