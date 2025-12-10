using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using TestGrand.Core.Models;

namespace TestGrand.Core.Services;

public interface IActionsStashService
{
    public void AddAction(string action);
    public void SetCycleId(string guid);
    public List<string> GetAllActions();
}

public class ActionsStashService : IActionsStashService
{
    private ConcurrentBag<string> _actionsStash = new ConcurrentBag<string>();
    private string _cycleGuid { get; set; } = string.Empty;

    public void AddAction(string action)
    {
        var deserialized = JsonSerializer.Deserialize<BasePlayerAction>(action);

        if (_cycleGuid == deserialized.CycleGuid)
            _actionsStash.Add(action);
    }

    public void SetCycleId(string guid)
    {
        _cycleGuid = guid;
    }

    public List<string> GetAllActions()
    {
        var allActions = _actionsStash.ToList();
        _actionsStash.Clear();

        return allActions;
    }
}