using UnityEngine;

public class Villager : Unit
{
    [Header("Herramientas (en Right_Hand)")]
    [SerializeField] private GameObject axe;
    [SerializeField] private GameObject pickaxe;
    [SerializeField] private GameObject pitchFork;
    [SerializeField] private GameObject dot;
    [SerializeField] private GameObject fishingRod;

    private void Start()
    {
        OcultarTodasLasHerramientas();
    }
    public override void PlayIdleAnimation()
    {
        base.PlayIdleAnimation();
    }

    protected override void PlayAttackAnimation()
    {
        MostrarHerramienta(axe);
        base.PlayAttackAnimation();
    }

    // -------------------------
    // ACCIONES ESPECÍFICAS DEL ALDEANO
    // -------------------------

    public void ChopWood()
    {
        PlayAttackAnimation(); // Usa la animación de Unit
        MostrarHerramienta(axe);
    }

    public void Mine()
    {
        PlayAttackAnimation();
        MostrarHerramienta(pickaxe);
    }

    public void Farm()
    {
        PlayAttackAnimation();
        MostrarHerramienta(pitchFork);
    }

    public void Fish()
    {
        PlayAttackAnimation();
        MostrarHerramienta(dot);
        MostrarHerramienta(fishingRod);
    }

    public void StartBuilding(Building target)
    {
        MoveTo(target.transform.position); // heredado de Unit
        MostrarHerramienta(pickaxe); // puedes cambiar a animación de construcción si existe
    }

    public void DeliverResources(Vector3 dropPoint)
    {
        MoveTo(dropPoint);
    }

    // -------------------------
    // HERRAMIENTAS
    // -------------------------
    private void OcultarTodasLasHerramientas()
    {
        if (axe != null) axe.SetActive(false);
        if (pickaxe != null) pickaxe.SetActive(false);
        if (pitchFork != null) pitchFork.SetActive(false);
        if (dot != null) dot.SetActive(false);
        if (fishingRod != null) fishingRod.SetActive(false);
    }

    private void MostrarHerramienta(GameObject herramienta)
    {
        OcultarTodasLasHerramientas();
        if (herramienta != null) herramienta.SetActive(true);
    }

    // Se puede sobrescribir OnAttackHit para agregar tool anims si se quiere
    public override void OnAttackHit()
    {
        base.OnAttackHit();
        MostrarHerramienta(axe); // por defecto el ataque usa el hacha
    }
}
