/// <summary>
/// Lightweight formation order phase tracked on <see cref="FormationGroup"/>.
/// </summary>
public enum FormationOrderState
{
    Idle = 0,
    ReformFacing = 1,
    Advance = 2,
    AttackApproach = 3,
    Engaged = 4
}
