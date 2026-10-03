using UnityEngine;
using System.Collections.Generic;

// ERROR CORREGIDO: Faltaba heredar de MonoBehaviour
public class HealthBarsManager : MonoBehaviour
{
    public static HealthBarsManager Instance;

    [SerializeField] private GameObject healthBarPrefab;
    [SerializeField] private Canvas globalCanvas;

    // 🔸 Nuevo: nodo vacío donde se instancian las barras
    [SerializeField] private Transform barsParent;

    private Dictionary<Unit, HealthBar> bars = new Dictionary<Unit, HealthBar>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Se intentó crear otro HealthBarsManager. Destruyendo duplicado.");
            Destroy(this.gameObject);
            return;
        }

        Instance = this;

        if (globalCanvas == null)
            globalCanvas = GetComponentInChildren<Canvas>();

        // Si no se asignó en el inspector, lo creamos automáticamente
        if (barsParent == null && globalCanvas != null)
        {
            GameObject parentGO = new GameObject("HealthBarsContainer");
            parentGO.transform.SetParent(globalCanvas.transform, false);
            barsParent = parentGO.transform;
        }
    }

    public void RegisterUnit(Unit unit, Vector3 offset)
    {
        if (bars.ContainsKey(unit)) return;

        // Instanciamos como hijo del nodo vacío
        GameObject barGO = Instantiate(healthBarPrefab, barsParent);
        HealthBar bar = barGO.GetComponent<HealthBar>();
        bar.Initialize(unit.transform, offset);
        bars.Add(unit, bar);
    }

    public void UpdateUnitHealth(Unit unit, float current, float max)
    {
        if (bars.TryGetValue(unit, out HealthBar bar))
            bar.UpdateHealth(current, max);
    }

    public void UnregisterUnit(Unit unit)
    {
        if (bars.TryGetValue(unit, out HealthBar bar))
        {
            if (bar != null)
                Destroy(bar.gameObject);

            bars.Remove(unit);
        }
    }

    // Opcional: ocultar barras de unidades fuera de cámara
    public void UpdateVisibility(Camera cam, List<Unit> visibleUnits)
    {
        foreach (var kv in bars)
        {
            bool isVisible = visibleUnits.Contains(kv.Key);
            kv.Value.SetVisibility(isVisible);
        }
    }
}
