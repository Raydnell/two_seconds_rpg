using System.Collections.Concurrent;
using System.Net.WebSockets;
using TestGrand.Core.Models;

namespace TestGrand.Api;

public class SocketPlayersManager
{
    public ConcurrentDictionary<string, WebSocket> Players { get; } = new();
    public ConcurrentDictionary<string, Skeleton> Skeletons { get; set; } = new();

    public void AddPlayer(string id, WebSocket ws) => Players.TryAdd(id, ws);
    public void RemovePlayer(string id) => Players.TryRemove(id, out _);
    public void AddSkeleton(string guid, Skeleton skel) => Skeletons.TryAdd(guid, skel);
}