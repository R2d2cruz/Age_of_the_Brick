using System;
using UnityEngine;

public class Player
{
    public int playerId;
    public string playerName;

    public Material playerMaterial; // 👈 reemplaza al Color

    private int currentEra = 0;
    public int CurrentEra => currentEra;

    // Evento que notifica la nueva era (solo para las unidades/estructuras de este jugador)
    public event Action<int> OnEraChanged;

    public Player(int id, string name, int startingEra = 0, Material mat = null)
    {
        playerId = id;
        playerName = name;
        playerMaterial = mat; // 👈 guarda el material
        currentEra = startingEra;
    }

    public void AdvanceEra()
    {
        currentEra++;
        OnEraChanged?.Invoke(currentEra);
        Debug.Log($"Player {playerId} ({playerName}) avanzó a era {currentEra + 1}");
    }

    public void SetEra(int era)
    {
        currentEra = era;
        OnEraChanged?.Invoke(currentEra);
    }
}
