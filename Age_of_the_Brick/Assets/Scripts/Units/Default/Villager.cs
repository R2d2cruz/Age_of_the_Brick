using UnityEngine;

/// <summary>
/// Representa una unidad aldeana (Villager) capaz de realizar tareas de recolección,
/// construcción y combate básico. Hereda de Unit e integra la máquina de estados.
/// </summary>
public class Villager : Unit
{
    [Header("Tools (attached to Right_Hand)")]
    [SerializeField] private GameObject axe;
    [SerializeField] private GameObject pickaxe;
    [SerializeField] private GameObject pitchfork;
    [SerializeField] private GameObject dot;
    [SerializeField] private GameObject fishingRod;

    // -------------------------
    // INITIALIZATION
    // -------------------------
    protected override void Start()
    {
        base.Start();
        HideAllTools();
    }

    // -------------------------
    // STATE BEHAVIORS
    // -------------------------

    /// <summary>
    /// Reproduce la animación Idle y desactiva herramientas.
    /// </summary>
    public override void PlayIdleAnimation()
    {
        base.PlayIdleAnimation();
        HideAllTools();
    }

    /// <summary>
    /// Reproduce la animación de ataque con herramienta activa.
    /// </summary>
    protected override void PlayAttackAnimation()
    {
        ShowTool(axe);
        base.PlayAttackAnimation();
    }

    /// <summary>
    /// Overrides context commands to handle resource gathering and building construction.
    /// </summary>
    /// <param name="target">The target Selectable clicked by the player.</param>
    public override void ExecuteContextCommand(Selectable target)
    {
        if (target == null || target.state == SelectableState.Dead) return;

        // 1. Interaction with Resource Nodes (Wood, Gold, Stone, Food)
        if (target.CompareTag("Resource"))
        {
            // ResourceNode resource = target.GetComponent<ResourceNode>();
            // StartGathering(resource);
        }
        // 2. Interaction with Allied Buildings (Construction or Repair)
        else if (target.CompareTag("Building") && !target.IsEnemyTo(ownerPlayerId))
        {
            /* Building building = target.GetComponent<Building>();
            if (building != null && !building.IsFullyBuilt)
            {
                StartBuilding(building);
            } */
        }
        // 3. Fallback to base behavior (e.g., attack if enemy)
        else
        {
            base.ExecuteContextCommand(target);
        }
    }

    // -------------------------
    // SPECIFIC ACTIONS
    // -------------------------

    /// <summary>
    /// Acción de talar árboles (usa hacha).
    /// </summary>
    public void ChopWood()
    {
        ChangeState(new AttackingState(this));
        PlayAttackAnimation();
        ShowTool(axe);
    }

    /// <summary>
    /// Acción de minería (usa pico).
    /// </summary>
    public void Mine()
    {
        ChangeState(new AttackingState(this));
        PlayAttackAnimation();
        ShowTool(pickaxe);
    }

    /// <summary>
    /// Acción de agricultura (usa tridente/horqueta).
    /// </summary>
    public void Farm()
    {
        ChangeState(new AttackingState(this));
        PlayAttackAnimation();
        ShowTool(pitchfork);
    }

    /// <summary>
    /// Acción de pesca (usa caña y flotador).
    /// </summary>
    public void Fish()
    {
        ChangeState(new AttackingState(this));
        PlayAttackAnimation();
        ShowTool(dot);
        ShowTool(fishingRod);
    }

    /// <summary>
    /// Acción de construcción (se mueve al edificio y activa herramienta adecuada).
    /// </summary>
    public void StartBuilding(Building target)
    {
        if (target == null) return;

        MoveTo(target.transform.position);
        ShowTool(pickaxe);
    }

    /// <summary>
    /// Lleva los recursos recolectados hasta un punto de entrega.
    /// </summary>
    public void DeliverResources(Vector3 dropPoint)
    {
        MoveTo(dropPoint);
    }

    // -------------------------
    // TOOL MANAGEMENT
    // -------------------------

    /// <summary>
    /// Oculta todas las herramientas del aldeano.
    /// </summary>
    private void HideAllTools()
    {
        if (axe) axe.SetActive(false);
        if (pickaxe) pickaxe.SetActive(false);
        if (pitchfork) pitchfork.SetActive(false);
        if (dot) dot.SetActive(false);
        if (fishingRod) fishingRod.SetActive(false);
    }

    /// <summary>
    /// Muestra una herramienta específica (ocultando las demás).
    /// </summary>
    private void ShowTool(GameObject tool)
    {
        HideAllTools();
        if (tool) tool.SetActive(true);
    }

    // -------------------------
    // ANIMATION CALLBACKS
    // -------------------------

    /// <summary>
    /// Evento llamado desde la animación de ataque.
    /// Se ejecuta justo cuando el golpe debe aplicarse.
    /// </summary>
    /* public override void OnAttackHit()
    {
        base.OnAttackHit();
        ShowTool(axe); // por defecto el ataque usa el hacha
    } */
}
