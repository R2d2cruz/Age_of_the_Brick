using System;
using UnityEngine;

/// <summary>
/// Represents a gatherable resource node in the world (Trees, Quarries, Mines, etc.)[cite: 1].
/// </summary>
public class Resource : Selectable
{
    [Header("Resource Settings")]
    [SerializeField] private ResourceType resourceType;
    [SerializeField] private int resourceQuantity = 1000;
    [SerializeField] private float interactionRadius = 2.5f;

    private int maxQuantity;

    public event Action<Resource> OnDepleted;

    public ResourceType Type => resourceType;
    public float InteractionRadius => interactionRadius;

    protected override void Start()
    {
        base.Start();
        maxQuantity = resourceQuantity;
    }

    public int GetQuantity()
    {
        return resourceQuantity;
    }

    public Vector3 GetPosition()
    {
        return transform.position;
    }

    /// <summary>
    /// Extracts a specified amount of resources from this node.
    /// </summary>
    /// <param name="requestedAmount">Amount the gatherer wants to extract.</param>
    /// <returns>The actual amount extracted (limited by remaining quantity).</returns>
    public int Extract(int requestedAmount)
    {
        if (resourceQuantity <= 0) return 0;

        int extracted = Mathf.Min(requestedAmount, resourceQuantity);
        resourceQuantity -= extracted;

        // Trigger depletion if exhausted
        if (resourceQuantity <= 0)
        {
            DepleteNode();
        }

        return extracted;
    }

    /// <summary>
    /// Handles node depletion when resources reach zero.
    /// </summary>
    private void DepleteNode()
    {
        OnDepleted?.Invoke(this);
        
        // Disable interaction collider immediately
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // Destroy or return to object pool
        Destroy(gameObject);
    }
}