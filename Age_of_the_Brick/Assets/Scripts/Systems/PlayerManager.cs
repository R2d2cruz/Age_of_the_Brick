using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }

    [Header("Local Player Configuration")]
    [SerializeField] private int localPlayerId = 1; // 🔹 Default Player 1 (Player 1)

    [Header("UI References")]
    [SerializeField] private UIResourceBar resourceBarUI;

    [Header("Default Materials for Players")]
    public Material[] defaultMaterials;

    private Dictionary<int, Player> players = new Dictionary<int, Player>();

    /// <summary>
    /// Event invoked when the local player changes or is initialized.
    /// </summary>
    public event Action<Player> OnLocalPlayerChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        CreatePlayers(3); // Create 3 players (IDs: 0, 1, 2)
    }

    private void Start()
    {
        // Assign the initial local player and notify the UI and systems
        SetLocalPlayer(localPlayerId);
    }

    public void CreatePlayers(int count)
    {
        players.Clear();
        for (int i = 0; i < count; i++)
        {
            Material playerMat = (defaultMaterials != null && defaultMaterials.Length > i) ? defaultMaterials[i] : null;
            players.Add(i, new Player(i, $"Player{i + 1}", 0, playerMat));
        }
    }

    /// <summary>
    /// Sets the local player ID and notifies the UI and control scripts.
    /// </summary>
    public void SetLocalPlayer(int playerId)
    {
        if (players.TryGetValue(playerId, out Player player))
        {
            localPlayerId = playerId;

            // Initialize the UI resource bar
            if (resourceBarUI != null)
            {
                resourceBarUI.Initialize(player);
            }

            // Notify SelectionHandler, CommandHandler, etc.
            OnLocalPlayerChanged?.Invoke(player);

            Debug.Log($"[PlayerManager] Active local player set to: {player.playerName} (ID: {player.playerId})");
        }
        else
        {
            Debug.LogError($"[PlayerManager] No registered player exists with ID: {playerId}");
        }
    }

    public Player GetPlayer(int playerId)
    {
        players.TryGetValue(playerId, out var p);
        return p;
    }

    /// <summary>
    /// Gets the current local player instance.
    /// </summary>
    public Player GetLocalPlayer()
    {
        return GetPlayer(localPlayerId);
    }

    public int GetLocalPlayerId() => localPlayerId;

    public Material GetPlayerMaterial(int playerId)
    {
        var player = GetPlayer(playerId);
        return player != null ? player.playerMaterial : null;
    }
}
