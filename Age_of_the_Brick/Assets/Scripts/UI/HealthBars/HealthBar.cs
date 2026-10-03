using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private Image fillImage;
    [SerializeField] private Color healthyColor = Color.green;
    [SerializeField] private Color warningColor = Color.yellow;
    [SerializeField] private Color dangerColor = Color.red;
    [SerializeField] private float showDuration = 3f; // tiempo visible tras cambio

    private Transform target;
    private Vector3 offset;
    private Camera cam;
    private float lastChangeTime;
    private bool isVisible;

    public void Initialize(Transform target, Vector3 offset)
    {
        this.target = target;
        this.offset = offset;
        cam = Camera.main;
        
        // Inicializamos forzando el estado para evitar bugs visuales
        gameObject.SetActive(false);
        isVisible = false;
    }

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