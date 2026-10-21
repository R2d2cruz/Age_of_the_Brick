using System.Collections.Generic;

public interface IActionProvider
{
    /// <summary>
    /// Retorna la lista de acciones disponibles actualmente para mostrar en el HUD.
    /// </summary>
    List<CommandAction> GetAvailableActions();
}