using System;
using System.Collections.Concurrent;
using TestGrand.Core.Models;

namespace TestGrand.Core.Services;

public interface ISkeletonsService
{
    public void AddSkeleton(string playerGuid, Skeleton skeleton);
    public Skeleton GetSkeleton(string playerGuid);
    public void RemoveHitPointsFromSkeleton(string guid, int hitPointsToRemove);
    public void RemoveSkeleton(string playerGuid);
    public void RemoveSkeletonBySkelGuid(Guid skelGuid);
}

public class SkeletonsService : ISkeletonsService
{
    private ConcurrentDictionary<string, Skeleton> SkeletonsInGame = new ConcurrentDictionary<string, Skeleton>();

    public void AddSkeleton(string playerGuid, Skeleton skeleton)
    {
        SkeletonsInGame.TryAdd(playerGuid, skeleton);
    }

    public Skeleton GetSkeleton(string playerGuid)
    {
        return SkeletonsInGame.SingleOrDefault(x => x.Key == playerGuid).Value;
    }

    public void RemoveHitPointsFromSkeleton(string guid, int hitPointsToRemove)
    {
        if (SkeletonsInGame.TryGetValue(guid, out var skeleton))
        {
            skeleton.HitPoints -= hitPointsToRemove;
        }
    }

    public void RemoveSkeleton(string playerGuid)
    {
        var skeleton = SkeletonsInGame.SingleOrDefault(x => x.Key == playerGuid).Value;
        SkeletonsInGame.TryRemove(new KeyValuePair<string, Skeleton>(playerGuid, skeleton));
    }

    public void RemoveSkeletonBySkelGuid(Guid skelGuid)
    {
        var skelKey = SkeletonsInGame.FirstOrDefault(x => x.Value.Guid == skelGuid);

        if (!string.IsNullOrEmpty(skelKey.Key))
            RemoveSkeleton(skelKey.Key);
    }
}
