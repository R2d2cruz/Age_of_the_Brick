using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Building))]
public class ProductionQueueComponent : MonoBehaviour
{
    // Usamos List en lugar de Queue para permitir lectura desde la UI y cancelación arbitraria
    private List<IProductionTask> productionQueue = new List<IProductionTask>();
    private float currentProgressTimer = 0f;

    // Evento para que la GUI se suscriba y sepa cuándo redibujar los botones
    public event Action OnQueueChanged;

    // Propiedades públicas para lectura desde el script de la GUI
    public IReadOnlyList<IProductionTask> CurrentQueue => productionQueue;
    
    /// <summary>
    /// Devuelve el progreso de la tarea actual entre 0.0 y 1.0 para la barra de carga de la GUI.
    /// </summary>
    public float CurrentTaskProgress => 
        (productionQueue.Count > 0 && productionQueue[0].ProductionTime > 0) 
        ? currentProgressTimer / productionQueue[0].ProductionTime 
        : 0f;

    public void AddTask(IProductionTask task)
    {
        productionQueue.Add(task);
        OnQueueChanged?.Invoke();
    }

    /// <summary>
    /// Llamado por la GUI cuando el jugador hace clic en el botón de una tarea encolada.
    /// </summary>
    public void CancelTask(int index)
    {
        if (index < 0 || index >= productionQueue.Count) return;
        
        // 1. Reembolsar recursos
        productionQueue[index].Cancel();
        
        // 2. Eliminar de la cola
        productionQueue.RemoveAt(index);
        
        // 3. Si cancelamos la primera tarea (la que se estaba haciendo), reiniciamos el temporizador
        if (index == 0) currentProgressTimer = 0f;
        
        OnQueueChanged?.Invoke();
    }

    private void Update()
    {
        Building building = GetComponent<Building>();
        if (building == null || !building.IsFullyBuilt) return;

        if (productionQueue.Count == 0) return;

        currentProgressTimer += Time.deltaTime;

        if (currentProgressTimer >= productionQueue[0].ProductionTime)
        {
            productionQueue[0].Execute();
            
            productionQueue.RemoveAt(0);
            currentProgressTimer = 0f;
            
            OnQueueChanged?.Invoke();
        }
    }
}