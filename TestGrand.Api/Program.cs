using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Quartz;
using TestGrand.Api;
using TestGrand.Api.QuartzJobs;
using TestGrand.Core.Models;
using TestGrand.Core.Models.ServerSendTypes;
using TestGrand.Core.Models.ClientSendTypes;
using TestGrand.Core.Services;



var builder = WebApplication.CreateBuilder(args);



builder.Services.AddQuartz(q =>
{
    q.AddJob<MainEventsJob>(j => j.WithIdentity("gameTick"));
    q.AddTrigger(t => t
        .WithIdentity("gameTickTrigger")
        .ForJob("gameTick")
        .WithSimpleSchedule(s => s.WithInterval(TimeSpan.FromSeconds(1)).RepeatForever())
    );
});

builder.Services.AddQuartzHostedService();
builder.Services.AddSingleton<SocketPlayersManager>();
builder.Services.AddSingleton<MainEventsJob>();
builder.Services.AddSingleton<IActionsStashService, ActionsStashService>();
builder.Services.AddSingleton<ISkeletonsService, SkeletonsService>();
builder.Services.AddSingleton<IMapService, MapService>();
builder.Services.AddSingleton<IRollerService, RollerService>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseWebSockets();

app.Map("/ws", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        using var ws = await context.WebSockets.AcceptWebSocketAsync();
        var playerId = Guid.NewGuid().ToString();
        var manager = context.RequestServices.GetRequiredService<SocketPlayersManager>();
        var skelService = context.RequestServices.GetRequiredService<ISkeletonsService>();
        var mapService = context.RequestServices.GetRequiredService<IMapService>();
        var isMapCreated = mapService.IsMapCreated();
        if (!isMapCreated)
        {
            mapService.CreateMap();
            Console.WriteLine("--- Map created ---");
        }

        manager.AddPlayer(playerId, ws);

        Console.WriteLine($"\n-- PLAYER CONNECTED {playerId} --");

        var buffer = new byte[64 * 1024];

        if (ws.State == WebSocketState.Open)
        {
            var playerInitialCall = new SendPlayerInfo
            {
                PlayerGuid = playerId,
                Type = ServerSendTypes.SendPlayerInfo
            };

            var callSerialized = JsonSerializer.Serialize(playerInitialCall);
            await ws.SendAsync(
                Encoding.UTF8.GetBytes(callSerialized),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
            );
        }

        try
        {
            var eventJobMachine = context.RequestServices.GetRequiredService<IActionsStashService>();
            while (ws.State == WebSocketState.Open)
            {
                var result = await ws.ReceiveAsync(buffer, CancellationToken.None);

                var action = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Console.WriteLine($"Player send: {action})");

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    if (ws.State == WebSocketState.Open || ws.State == WebSocketState.CloseReceived)
                    {
                        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure,
                            "Closed by client", CancellationToken.None);
                    }

                    var playerSkeleton = skelService.GetSkeleton(playerId);
                    if (playerSkeleton != null)
                    {
                        skelService.RemoveSkeleton(playerId);
                        mapService.RemoveEntity(playerSkeleton.Guid);
                    }

                    manager.RemovePlayer(playerId);

                    break;
                }

                var deserialized = JsonSerializer.Deserialize<BaseClientSendType>(action);

                if (deserialized != null && deserialized.Type == ClientSendTypes.CycleAction)
                {
                    var cycleAction = JsonSerializer.Deserialize<SendCycleAction>(action);
                    eventJobMachine.AddAction(cycleAction.PlayerAction);
                }
                else if (deserialized != null && deserialized.Type == ClientSendTypes.SkeletonInfo)
                {
                    var skelInfo = JsonSerializer.Deserialize<SendSkeletonInfo>(action);
                    mapService.AddEntity(4, 4, skelInfo.Skeleton);

                    skelInfo.Skeleton.Xcoord = 4;
                    skelInfo.Skeleton.Ycoord = 4;
                    skelService.AddSkeleton(skelInfo.PlayerGuid, skelInfo.Skeleton);
                }
            }
        }
        catch (WebSocketException wsEx)
        {
            var playerSkeleton = skelService.GetSkeleton(playerId);
            if (playerSkeleton != null)
            {
                skelService.RemoveSkeleton(playerId);
                mapService.RemoveEntity(playerSkeleton.Guid);
            }

            manager.RemovePlayer(playerId);
        }
    }
});

app.Run();