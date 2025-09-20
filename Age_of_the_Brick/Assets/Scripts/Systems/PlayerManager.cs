using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }

    private Dictionary<int, Player> players = new Dictionary<int, Player>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        CreatePlayers(2); // 👈 crea un solo jugador al inicio (temporal, no usar en produccion)
    }

    // Crear N jugadores (ejemplo), puedes personalizar nombres/ids
    public void CreatePlayers(int count)
    {
        players.Clear();
        for (int i = 0; i < count; i++)
        {
            players.Add(i, new Player(i, $"Player{i}", 0));
        }
    }

    public Player GetPlayer(int playerId)
    {
        players.TryGetValue(playerId, out var p);
        return p;
    }
}
