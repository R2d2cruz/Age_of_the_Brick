using UnityEngine;

public class MeleeCombatComponent : BaseCombatComponent
{
    protected override void ExecuteAttackLogic()

    {
        if (currentTarget == null || currentTarget.state == SelectableState.Dead) return;

        // Validamos la distancia nuevamente por si el objetivo se movió en el último frame
        float distance = unit.GetDistanceToTargetSurface(currentTarget);
        if (distance > unit.GetInteractionRange()) return;
        if (currentTarget is Unit unitTarget)
        {
            unitTarget.TakeDamage(unit.currentStats.lightAttack, unit.currentStats.heavyAttack, unit);
            return;
        }
        if (currentTarget is Building buildingTarget)
        {
            buildingTarget.TakeDamage(unit.currentStats.lightAttack, unit.currentStats.heavyAttack, unit);
            return;
        }
    }
}
