using UnityEngine;
using System.Collections.Generic;

public class UnitProducer : MonoBehaviour
{
    [SerializeField] private Transform rallyPoint;
    [SerializeField] private Transform spawnPoint;

    private Queue<UnitStats> productionQueue = new Queue<UnitStats>();
    private float currentProductionTimer;

    public void QueueUnit(UnitStats unitToBuild)
    {
        productionQueue.Enqueue(unitToBuild);
    }

    private void Update()
    {
        if (productionQueue.Count == 0) return;

        currentProductionTimer += Time.deltaTime;
        // Lógica de progreso de reclutamiento...
    }
}