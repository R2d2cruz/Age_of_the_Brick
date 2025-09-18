using System.Collections.Generic;
using UnityEngine;

public class SelectionHandler : MonoBehaviour
{
    [Header("Selección con Click")]
    [SerializeField] private LayerMask selectableMask; // Unidades, edificios, recursos

    [Header("Selección con Drag Box")]
    [SerializeField] private RectTransform selectionBoxUI; // Imagen UI del rectángulo
    private Vector2 startPos;
    private Vector2 endPos;

    public List<Selectable> SelectedObjects { get; private set; } = new List<Selectable>();

    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
        if (selectionBoxUI != null)
            selectionBoxUI.gameObject.SetActive(false); // 🔹 aseguramos que inicie desactivado
    }

    private void Update()
    {
        HandleClickSelection();
        HandleDragSelection();
    }

    private void HandleClickSelection()
    {
        if (Input.GetMouseButtonDown(0))
        {
            startPos = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(0))
        {
            // 🔹 Usar tolerancia, porque Input.mousePosition nunca es exactamente igual
            if (Vector2.Distance(Input.mousePosition, startPos) < 5f)
            {
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, selectableMask))
                {
                    Selectable selectable = hit.collider.GetComponentInParent<Selectable>();
                    if (selectable != null)
                    {
                        ClearSelection();
                        AddToSelection(selectable);
                    }
                }
                else
                {
                    ClearSelection();
                }
            }
        }
    }

    private void HandleDragSelection()
    {
        if (Input.GetMouseButtonDown(0))
        {
            startPos = Input.mousePosition;
            if (selectionBoxUI != null)
                selectionBoxUI.gameObject.SetActive(true);
        }

        if (Input.GetMouseButton(0))
        {
            endPos = Input.mousePosition;
            UpdateSelectionBox();
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (selectionBoxUI != null)
                selectionBoxUI.gameObject.SetActive(false);

            Vector2 min = Vector2.Min(startPos, endPos);
            Vector2 max = Vector2.Max(startPos, endPos);

            foreach (var selectable in Object.FindObjectsByType<Selectable>(FindObjectsSortMode.None))
            {
                Vector3 screenPos = mainCamera.WorldToScreenPoint(selectable.transform.position);

                if (screenPos.z > 0 && // 🔹 evitar seleccionar cosas detrás de la cámara
                    screenPos.x >= min.x && screenPos.x <= max.x &&
                    screenPos.y >= min.y && screenPos.y <= max.y)
                {
                    AddToSelection(selectable);
                }
            }
        }
    }

    private void UpdateSelectionBox()
    {
        Vector2 boxStart = startPos;
        Vector2 boxEnd = endPos;
        Vector2 boxCenter = (boxStart + boxEnd) / 2;

        selectionBoxUI.anchoredPosition = boxCenter;

        Vector2 boxSize = new Vector2(Mathf.Abs(boxStart.x - boxEnd.x), Mathf.Abs(boxStart.y - boxEnd.y));
        selectionBoxUI.sizeDelta = boxSize;
    }

    private void AddToSelection(Selectable selectable)
    {
        if (!SelectedObjects.Contains(selectable))
        {
            SelectedObjects.Add(selectable);
            selectable.Select();
        }
    }

    private void ClearSelection()
    {
        foreach (var obj in SelectedObjects)
        {
            obj.Deselect();
        }
        SelectedObjects.Clear();
    }
}
