using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class TaskButton : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("La imagen principal donde se mostrará el ícono de la unidad/investigación.")]
    [SerializeField] private Image iconImage;
    
    [Tooltip("Una imagen superpuesta (Image Type: Filled) para mostrar el progreso.")]
    [SerializeField] private Image progressFillImage;

    private Button button;
    private int currentTaskIndex;
    private ProductionQueueComponent linkedQueue;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnButtonClicked);
    }

    /// <summary>
    /// Configura el botón con los datos de la tarea y su posición en la cola.
    /// </summary>
    public void Setup(IProductionTask task, int index, ProductionQueueComponent queue)
    {
        currentTaskIndex = index;
        linkedQueue = queue;

        if (iconImage != null && task.Icon != null)
        {
            iconImage.sprite = task.Icon;
        }

        // Si no es la primera tarea (index 0), el progreso siempre debe verse vacío
        UpdateProgress(0f);
    }

    /// <summary>
    /// Actualiza visualmente la imagen de relleno (fillAmount).
    /// </summary>
    public void UpdateProgress(float progressPercentage)
    {
        float half = progressPercentage * 0.5f;
        if (progressFillImage != null)
        {
            progressFillImage.rectTransform.anchorMin = new Vector2(0.5f - half, 0.5f - half);
            progressFillImage.rectTransform.anchorMax = new Vector2(0.5f + half, 0.5f + half);
        }
    }

    /// <summary>
    /// Se ejecuta al hacer clic en el botón.
    /// </summary>
    private void OnButtonClicked()
    {
        if (linkedQueue != null)
        {
            linkedQueue.CancelTask(currentTaskIndex);
        }
    }

    private void OnDestroy()
    {
        button.onClick.RemoveListener(OnButtonClicked);
    }
}