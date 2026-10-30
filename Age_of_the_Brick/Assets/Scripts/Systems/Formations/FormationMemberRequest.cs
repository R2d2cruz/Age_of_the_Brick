/// <summary>
/// Pure formation layout input: stable id + tactical role (no Unity scene dependency).
/// </summary>
public readonly struct FormationMemberRequest
{
    public readonly int Id;
    public readonly UnitRole Role;

    public FormationMemberRequest(int id, UnitRole role)
    {
        Id = id;
        Role = role;
    }
}
