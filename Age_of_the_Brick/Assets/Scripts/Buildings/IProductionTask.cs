using UnityEngine;

public interface IProductionTask
{
    string TaskName { get; }
    float ProductionTime { get; }
    Sprite Icon { get; } // Necesario para pintar los botones en la GUI
    
    void Execute();
    void Cancel(); // Para devolver recursos si el jugador aborta
}