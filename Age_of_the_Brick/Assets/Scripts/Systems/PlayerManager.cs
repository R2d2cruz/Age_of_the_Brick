using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }

    private Dictionary<int, Player> players = new Dictionary<int, Player>();

    [Header("Materiales por defecto de los jugadores")]
    [Tooltip("Asigna aquí los materiales en el inspector (índice = playerId)")]
    public Material[] defaultMaterials;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        CreatePlayers(3); // 👈 crea 3 jugadores de ejemplo
    }

    // Crear N jugadores con materiales y nombres
    public void CreatePlayers(int count)
    {
        players.Clear();
        for (int i = 0; i < count; i++)
        {
            Material playerMat = defaultMaterials[i % defaultMaterials.Length];
            players.Add(i, new Player(i, $"Player{i + 1}", 0, playerMat));
        }
    }

    // Obtener objeto Player
    public Player GetPlayer(int playerId)
    {
        players.TryGetValue(playerId, out var p);
        return p;
    }

    // Obtener directamente el material de un jugador
    public Material GetPlayerMaterial(int playerId)
    {
        var player = GetPlayer(playerId);
        if (player != null)
            return player.playerMaterial;
        return null; // null = neutral
    }
}
