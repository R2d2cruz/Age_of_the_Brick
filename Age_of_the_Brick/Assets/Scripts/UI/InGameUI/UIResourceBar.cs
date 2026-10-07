using System.Collections.Generic;
using UnityEngine;

public class UIResourceBar : MonoBehaviour
{
    [Header("Resource Slots Container")]
    [SerializeField] private List<UIResourceItem> resourceItems = new List<UIResourceItem>();

    private Dictionary<ResourceType, UIResourceItem> itemMap = new Dictionary<ResourceType, UIResourceItem>();
    private Player localPlayer;

    private void Awake()
    {
        // Map each element according to its resource type for quick access
        foreach (var item in resourceItems)
        {
            if (item != null && !itemMap.ContainsKey(item.ResourceType))
            {
                itemMap.Add(item.ResourceType, item);
            }
        }
    }

    
    /// <summary>
    /// Links the UI to a specific player.
    /// </summary>
    public void Initialize(Player player)
    {
        // Unlink from the previous player if one existed
        if (localPlayer != null)
        {
            localPlayer.OnResourceChanged -= HandleResourceChanged;
        }

        localPlayer = player;

        if (localPlayer != null)
        {
            // Subscribe to future changes
            localPlayer.OnResourceChanged += HandleResourceChanged;

            // Load initial values
            RefreshAllResources();
        }
    }

    private void OnDestroy()
    {
        if (localPlayer != null)
        {
            localPlayer.OnResourceChanged -= HandleResourceChanged;
        }
    }

    private void RefreshAllResources()
    {
        if (localPlayer == null) return;

        foreach (var KVP in itemMap)
        {
            int currentAmount = localPlayer.GetResourceAmount(KVP.Key);
            KVP.Value.UpdateAmount(currentAmount);
        }
    }

    private void HandleResourceChanged(ResourceType type, int newAmount)
    {
        if (itemMap.TryGetValue(type, out UIResourceItem item))
        {
            item.UpdateAmount(newAmount);
        }
    }
}
