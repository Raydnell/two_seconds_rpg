using System.Text.Json.Serialization;

namespace TestGrand.Core.Models.ServerSendTypes;

[JsonDerivedType(typeof(SendPlayerInfo), nameof(SendPlayerInfo))]
[JsonDerivedType(typeof(SendCycleGuid), nameof(SendCycleGuid))]
[JsonDerivedType(typeof(SendMap), nameof(SendMap))]
public class BaseServerSendType
{
    public ServerSendTypes Type { get; set; }
}

public enum ServerSendTypes
{
    SendPlayerInfo,
    SendCycleGuid,
    SendMap,
    SkeletonInfo,
    EndGame,
}

public class SendPlayerInfo : BaseServerSendType
{
    public string PlayerGuid { get; set; } = string.Empty;
}

public class SendCycleGuid : BaseServerSendType
{
    public string CycleGuid { get; set; }
}

public class SendMap : BaseServerSendType
{
    public MapBase Map { get; set; }
}

public class ServerSendSkeleton : BaseServerSendType
{
    public Skeleton Skeleton { get; set; }
}