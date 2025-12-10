using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Quartz;
using TestGrand.Core.Models;
using TestGrand.Core.Models.ClientSendTypes;
using TestGrand.Core.Models.ServerSendTypes;
using TestGrand.Core.Services;

namespace TestGrand.Api.QuartzJobs;

public class MainEventsJob(SocketPlayersManager manager, IActionsStashService actionsStashService, ISkeletonsService skeletonsService, IMapService mapService) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var currentCycleId = Guid.NewGuid().ToString();
        actionsStashService.SetCycleId(currentCycleId);
        var actionsToDo = actionsStashService.GetAllActions();

        //Console.WriteLine($"Cycle {currentCycleId} start recieving actions");
        await SendCycleGuidToPlayers(currentCycleId);

        DoActions(actionsToDo);

        await SendPlayersResults();

        //Console.WriteLine("\n\n");
    }

    private async Task SendCycleGuidToPlayers(string guid)
    {
        foreach (var (playerId, ws) in manager.Players)
        {
            if (ws.State == WebSocketState.Open)
            {
                var serverCall = new SendCycleGuid
                {
                    CycleGuid = guid,
                    Type = ServerSendTypes.SendCycleGuid
                };

                var callSerialized = JsonSerializer.Serialize(serverCall);

                await ws.SendAsync(
                    Encoding.UTF8.GetBytes(callSerialized),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None
                );
            }
        }
    }

    private void DoActions(List<string> actions)
    {
        foreach (var action in actions)
        {
            var baseAction = JsonSerializer.Deserialize<BasePlayerAction>(action);
            var skeleton = skeletonsService.GetSkeleton(baseAction.PlayerGuid);
            switch (baseAction.PlayerActionType)
            {
                case PlayerActionsTypes.Move:
                    DoMovement(action, skeleton);
                    break;

                case PlayerActionsTypes.Attack:
                    DoAttack(action, skeleton);
                    break;

                case PlayerActionsTypes.Defense:
                    DoDefense(action, skeleton);
                    break;

                default:
                    break;
            }
        }
    }

    private async Task SendPlayersResults()
    {
        foreach (var (playerId, ws) in manager.Players)
        {
            if (ws.State == WebSocketState.Open)
            {
                var mapCall = new SendMap
                {
                    Type = ServerSendTypes.SendMap,
                    Map = mapService.GetMap()
                };

                var callSerialized = JsonSerializer.Serialize(mapCall);

                await ws.SendAsync(
                    Encoding.UTF8.GetBytes(callSerialized),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None
                );

                var playerSkel = skeletonsService.GetSkeleton(playerId);

                var playerInfoMessage = string.Empty;

                if (playerSkel == null)
                {
                    var endGameInfo = new BaseServerSendType { Type = ServerSendTypes.EndGame };
                    playerInfoMessage = JsonSerializer.Serialize(endGameInfo);
                }
                else
                {
                    var skeletonInfo = new ServerSendSkeleton
                    {
                        Type = ServerSendTypes.SkeletonInfo,
                        Skeleton = playerSkel
                    };

                    playerInfoMessage = JsonSerializer.Serialize(skeletonInfo);
                }

                if (!string.IsNullOrEmpty(playerInfoMessage))
                {
                    await ws.SendAsync(
                        Encoding.UTF8.GetBytes(playerInfoMessage),
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None
                    );
                }
            }
        }
    }

    private void DoMovement(string action, Skeleton skeleton)
    {
        var moveAction = JsonSerializer.Deserialize<MovePlayerAction>(action);

        skeleton.FightStance.Direction = moveAction.Direction;
        skeleton.FightStance.Type = StanceType.Battle;

        //Console.WriteLine($"Skeleton {skeleton.SkelGuid} want to move {moveAction.Direction.ToString()}");
        MoveEntity(moveAction.Direction, skeleton);
    }

    private void DoAttack(string action, Skeleton skeleton)
    {
        var attackAction = JsonSerializer.Deserialize<AttackPlayerAction>(action);
        //Console.WriteLine($"Skeleton {skeleton.SkelGuid} want to attack {attackAction.Direction.ToString()}");
        var currentBlock = mapService.GetMapBlock(skeleton.Xcoord, skeleton.Ycoord);
        var blockToAttack = new MapBlock();

        var quanterDirection = DirectionsEnum.Right;

        switch (attackAction.Direction)
        {
            case DirectionsEnum.Up:
                blockToAttack = mapService.GetMapBlock(skeleton.Xcoord, skeleton.Ycoord - 1);
                quanterDirection = DirectionsEnum.Down;
                break;

            case DirectionsEnum.Left:
                blockToAttack = mapService.GetMapBlock(skeleton.Xcoord - 1, skeleton.Ycoord);
                quanterDirection = DirectionsEnum.Right;
                break;

            case DirectionsEnum.Down:
                blockToAttack = mapService.GetMapBlock(skeleton.Xcoord, skeleton.Ycoord + 1);
                quanterDirection = DirectionsEnum.Up;
                break;

            case DirectionsEnum.Right:
                blockToAttack = mapService.GetMapBlock(skeleton.Xcoord + 1, skeleton.Ycoord);
                quanterDirection = DirectionsEnum.Left;
                break;
        }

        skeleton.FightStance.Direction = attackAction.Direction;
        skeleton.FightStance.Type = StanceType.Battle;

        // todo attack only skels - bad
        var skeletonToAttack = blockToAttack.Entities.FirstOrDefault(x => x.Type == MapEntityTypes.Skeleton);

        if (skeletonToAttack == null)
            return;

        var attackedSkeleton = (Skeleton)skeletonToAttack;

        if (attackedSkeleton.FightStance.Type == StanceType.Defence && quanterDirection == attackedSkeleton.FightStance.Direction)
        {
            MoveEntity(attackAction.Direction, attackedSkeleton);
        }
        else
        {
            attackedSkeleton.HitPoints -= skeleton.AttackPower;

            if (attackedSkeleton.HitPoints < 1)
            {
                mapService.RemoveEntity(attackedSkeleton.Guid);
                skeletonsService.RemoveSkeletonBySkelGuid(attackedSkeleton.Guid);
            }
        }

        //skeletonsService.RemoveHitPointsFromSkeleton(skeletonToAttack.Guid, skeleton.AttackPower);
    }

    private void DoDefense(string action, Skeleton skeleton)
    {
        var defenseAction = JsonSerializer.Deserialize<DefensePlayerAction>(action);

        skeleton.FightStance.Direction = defenseAction.Direction;
        skeleton.FightStance.Type = StanceType.Defence;
    }
    
    private void MoveEntity(DirectionsEnum direction, Skeleton skeleton)
    {
        var newXcoord = skeleton.Xcoord;
        var newYcoord = skeleton.Ycoord;
        var currentBlock = mapService.GetMapBlock(newXcoord, newYcoord);
        var newBlock = new MapBlock();

        switch (direction)
        {
            case DirectionsEnum.Up:
                newYcoord = newYcoord - 1;
                newBlock = mapService.GetMapBlock(newXcoord, newYcoord);
                break;

            case DirectionsEnum.Left:
                newXcoord = newXcoord - 1;
                newBlock = mapService.GetMapBlock(newXcoord, newYcoord);
                break;

            case DirectionsEnum.Down:
                newYcoord = newYcoord + 1;
                newBlock = mapService.GetMapBlock(newXcoord, newYcoord);
                break;

            case DirectionsEnum.Right:
                newXcoord = newXcoord + 1;
                newBlock = mapService.GetMapBlock(newXcoord, newYcoord);
                break;
        }

        if (newBlock.IsPassable)
        {
            skeleton.Ycoord = newYcoord;
            skeleton.Xcoord = newXcoord;
            skeleton.AssignedToEntity = newBlock.BlockGuid;
            currentBlock.Entities.Remove(skeleton);
            newBlock.Entities.Add(skeleton);
        }
    }
}
