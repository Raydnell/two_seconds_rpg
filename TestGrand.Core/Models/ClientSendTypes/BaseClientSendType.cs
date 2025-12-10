using System.Text.Json.Serialization;

namespace TestGrand.Core.Models.ClientSendTypes;

[JsonDerivedType(typeof(SendCycleAction), nameof(SendCycleAction))]
[JsonDerivedType(typeof(SendSkeletonInfo), nameof(SendSkeletonInfo))]
public class BaseClientSendType
{
    public ClientSendTypes Type { get; set; }
}

public enum ClientSendTypes
{
    CycleAction,
    SkeletonInfo
}

public class SendCycleAction : BaseClientSendType
{
    public string PlayerAction { get; set; }
}

public class SendSkeletonInfo : BaseClientSendType
{
    public string PlayerGuid { get; set; }
    public Skeleton Skeleton { get; set; }
}