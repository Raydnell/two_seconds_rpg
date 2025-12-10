using System.Text.Json.Serialization;

namespace TestGrand.Core.Models;

[JsonDerivedType(typeof(MovePlayerAction), nameof(MovePlayerAction))]
[JsonDerivedType(typeof(AttackPlayerAction), nameof(AttackPlayerAction))]
[JsonDerivedType(typeof(DefensePlayerAction), nameof(DefensePlayerAction))]
public class BasePlayerAction
{
    public string PlayerGuid { get; set; }
    public string CycleGuid { get; set; }
    public PlayerActionsTypes PlayerActionType { get; set; }
}

public enum PlayerActionsTypes
{
    Move,
    Attack,
    Defense
}

public class MovePlayerAction : BasePlayerAction
{
    public DirectionsEnum Direction { get; set; }
}

public class AttackPlayerAction : BasePlayerAction
{
    public DirectionsEnum Direction { get; set; }
}

public class DefensePlayerAction : BasePlayerAction
{
    public DirectionsEnum Direction { get; set; }
}
