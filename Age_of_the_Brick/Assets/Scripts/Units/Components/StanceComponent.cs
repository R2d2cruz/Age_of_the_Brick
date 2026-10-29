using UnityEngine;

/// <summary>
/// Tracks unit combat stance, anchor position, and periodic stance evaluation.
/// Behavior is filled in by later tasks.
/// </summary>
public class StanceComponent : BaseUnitComponent
{
    [SerializeField] private float evaluateInterval = 0.35f;

    public UnitStance CurrentStance { get; private set; } = UnitStance.Aggressive;
    public Vector3 Anchor { get; private set; }

    private IAttacker attacker;
    private float evaluateTimer;
    private bool stanceInitiatedCombat;
    private bool returningToAnchor;
    private Selectable stanceTarget;

    public override void Initialize(Unit coreUnit)
    {
        base.Initialize(coreUnit);
        attacker = coreUnit.GetComponent<IAttacker>();
        Anchor = coreUnit.transform.position;
        CurrentStance = UnitStance.Aggressive;
    }

    public void SetStance(UnitStance stance)
    {
        CurrentStance = stance;
        stanceInitiatedCombat = false;
        returningToAnchor = false;
        if (stance == UnitStance.Defensive || stance == UnitStance.HoldPosition)
            SetAnchor(unit.transform.position);
    }

    public void SetAnchor(Vector3 worldPosition) => Anchor = worldPosition;

    public void OnMoveCompleted(Vector3 destination)
    {
        if (CurrentStance == UnitStance.Defensive || CurrentStance == UnitStance.HoldPosition)
            SetAnchor(destination);
        returningToAnchor = false;
    }

    public void NotifyAllyDamaged(Selectable damagedAlly, Selectable aggressor) { /* Task 4 */ }

    private void Update()
    {
        if (unit == null || unit.state != SelectableState.Alive) return;
        evaluateTimer += Time.deltaTime;
        if (evaluateTimer < evaluateInterval) return;
        evaluateTimer = 0f;
        Evaluate();
    }

    public void Evaluate() { /* Tasks 3–4 fill behavior */ }
}
