using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Building))]
public class ResourceDropoff : MonoBehaviour
{
    [Header("Configuración de Acopio")]
    [SerializeField] private List<ResourceType> acceptedResources = new List<ResourceType>();

    private Building building;

    private void Awake()
    {
        building = GetComponent<Building>();
    }

    /// <summary>
    /// Verifica si este centro de acopio acepta un tipo específico de recurso.
    /// </summary>
    public bool AcceptsResource(ResourceType type)
    {
        return acceptedResources.Contains(type);
    }

    /// <summary>
    /// Recibe los recursos transferidos por una unidad y los entrega al jugador dueño del edificio.
    /// </summary>
    public bool ReceiveResource(ResourceType type, int amount)
    {
        if (!AcceptsResource(type)) return false;
        if (!building.IsFullyBuilt()) return false;

        // Entregar los recursos al Player propietario del edificio
        if (building.Owner != null)
        {
            building.Owner.AddResource(type, amount);
            return true;
        }

        return false;
    }
}