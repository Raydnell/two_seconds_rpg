namespace TestGrand.Core.Models;

public class Skeleton : MapEntity
{
    public int HitPoints { get; set; }
    public string Name { get; set; } = string.Empty;
    public int AttackPower { get; set; }
    public Guid AssignedToEntity { get; set; }
    public int Xcoord { get; set; }
    public int Ycoord { get; set; }
    public int ArmorClass { get; set; }
    public BaseStats Stats { get; set; }
    public FightStance FightStance { get; set; }
}

public class BaseStats
{
    public int Str { get; set; }
    public int Dex { get; set; }
    public int Vit { get; set; }
    public int Wis { get; set; }
}

public class FightStance
{
    public StanceType Type { get; set; }
    public DirectionsEnum Direction { get; set; }
}

public enum StanceType
{
    Battle,
    Defence,
}
