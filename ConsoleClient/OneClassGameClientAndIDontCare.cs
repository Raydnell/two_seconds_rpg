using System.Net.WebSockets;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using TestGrand.Core.Models;
using TestGrand.Core.Models.ServerSendTypes;
using TestGrand.Core.Models.ClientSendTypes;
using Spectre.Console;
using Microsoft.VisualBasic;
using TestGrand.Core.Services;
using System.Data.Common;

namespace ConsoleClient;

public class OneClassGameClientAndIDontCare
{
    private string _playerGuid = string.Empty;
    private Skeleton _skeleton = new Skeleton();
    private ConsoleKeyInfo _playerInput = new ConsoleKeyInfo();
    private object _lock = new object();
    private Queue<BasePlayerAction> actionsList = new Queue<BasePlayerAction>();
    private Panel _mapPanel = new Panel("Empty map");
    private Queue<string> _messagesBox = new Queue<string>();

    public void GameLoop()
    {
        var isExit = false;
        while (isExit == false)
        {
            Console.Clear();
            AnsiConsole.Cursor.Hide();
            AnsiConsole.Write(
                new FigletText("Reign of flesh")
                    .LeftJustified()
                    .Color(Color.Red));

            AnsiConsole.Write("\nPush any key for start a game! Or Q for exit");

            var pushedKey = Console.ReadKey();

            if (pushedKey.KeyChar == 'q')
            {
                isExit = true;
            }
            else
            {
                StartGame();
            }
        }
    }

    public void StartGame()
    {
        var inputTask = Task.Run(ReadInput);

        _skeleton = new Skeleton
        {
            Name = "Classssssic",
            HitPoints = 10,
            AttackPower = 3,
            FightStance = new FightStance {Type = StanceType.Battle, Direction = DirectionsEnum.Up},
        };

        Console.Clear();
        AnsiConsole.Cursor.Hide();

        var layout = new Layout("Root")
            .SplitColumns(
                new Layout("Left")
                    .SplitRows(
                        new Layout("Top"),
                        new Layout("Bottom")),
                new Layout("Right")
                    .SplitRows(
                        new Layout("Top"),
                        new Layout("Bottom")));

        var webSocketTask = Task.Run(WebSocketRun);

        while (!webSocketTask.IsCompleted)
        {
            UiDrawProcess(layout);
        }
    }

    private async Task WebSocketRun()
    {
        try
        {
            var ws = new ClientWebSocket();
            await ws.ConnectAsync(new Uri("ws://localhost:5031/ws"), CancellationToken.None);
            var isExit = false;
            while (isExit == false)
            {
                var buffer = new byte[64 * 1024];
                var result = await ws.ReceiveAsync(buffer, CancellationToken.None);

                var action = Encoding.UTF8.GetString(buffer, 0, result.Count);
                var actionDeserialized = JsonSerializer.Deserialize<BaseServerSendType>(action);

                if (actionDeserialized.Type == ServerSendTypes.SendPlayerInfo)
                {
                    var playerInfo = JsonSerializer.Deserialize<SendPlayerInfo>(action);
                    _playerGuid = playerInfo.PlayerGuid;

                    AddMessageToBox($"Player initialized with gui {_playerGuid}");
                    var skeletonCreationAction = new SendSkeletonInfo
                    {
                        PlayerGuid = _playerGuid,
                        Skeleton = _skeleton,
                        Type = ClientSendTypes.SkeletonInfo
                    };

                    var serializedAction = JsonSerializer.Serialize(skeletonCreationAction);
                    await ws.SendAsync(Encoding.UTF8.GetBytes(serializedAction), WebSocketMessageType.Text, true, CancellationToken.None);
                    AddMessageToBox($"Skeleton info sended");
                }
                else if (actionDeserialized.Type == ServerSendTypes.SendCycleGuid && actionsList.Count > 0)
                {
                    AddMessageToBox("Server send cycle guid");
                    var cycleInfo = JsonSerializer.Deserialize<SendCycleGuid>(action);
                    var playerAction = actionsList.Dequeue();
                    playerAction.CycleGuid = cycleInfo.CycleGuid;
                    var playerActionSerialized = JsonSerializer.Serialize(playerAction);

                    var sendCycleAction = new SendCycleAction
                    {
                        Type = ClientSendTypes.CycleAction,
                        PlayerAction = playerActionSerialized,
                    };

                    var serializedAction = JsonSerializer.Serialize(sendCycleAction);

                    await ws.SendAsync(Encoding.UTF8.GetBytes(serializedAction), WebSocketMessageType.Text, true, CancellationToken.None);
                    AddMessageToBox("Send action to server");
                }
                else if (actionDeserialized.Type == ServerSendTypes.SendMap)
                {
                    var desMap = JsonSerializer.Deserialize<SendMap>(action);
                    _mapPanel = MapPanelDrawer(desMap.Map);
                }
                else if (actionDeserialized.Type == ServerSendTypes.SkeletonInfo)
                {
                    var skelNew = JsonSerializer.Deserialize<ServerSendSkeleton>(action);
                    _skeleton = skelNew.Skeleton;
                }
                else if (actionDeserialized.Type == ServerSendTypes.EndGame)
                {
                    isExit = true;
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "just exit", new CancellationToken());
                }
            }
        }
        catch (Exception ex)
        {
            AddMessageToBox($"ERROR: {ex.Message}");
        }
    }

    private void ReadInput()
    {
        while (true)
        {
            var input = Console.ReadKey(intercept: true);
            lock (_lock)
                _playerInput = input;

            ActionsSwitch(input.KeyChar.ToString());
        }
    }

    private Skeleton CreateSkeleton()
    {
        var playerSkeleton = new Skeleton
        {
            Name = "TestSkelll",
            HitPoints = 10,
            AttackPower = 15
        };

        return playerSkeleton;
    }

    private void ActionsSwitch(string action)
    {
        switch (action)
        {
            case "w":
                actionsList.Enqueue(new MovePlayerAction { PlayerGuid = _playerGuid, PlayerActionType = PlayerActionsTypes.Move, Direction = DirectionsEnum.Up });
                break;

            case "a":
                actionsList.Enqueue(new MovePlayerAction { PlayerGuid = _playerGuid, PlayerActionType = PlayerActionsTypes.Move, Direction = DirectionsEnum.Left });
                break;

            case "s":
                actionsList.Enqueue(new MovePlayerAction { PlayerGuid = _playerGuid, PlayerActionType = PlayerActionsTypes.Move, Direction = DirectionsEnum.Down });
                break;

            case "d":
                actionsList.Enqueue(new MovePlayerAction { PlayerGuid = _playerGuid, PlayerActionType = PlayerActionsTypes.Move, Direction = DirectionsEnum.Right });
                break;

            case "u":
                actionsList.Enqueue(new AttackPlayerAction { PlayerGuid = _playerGuid, PlayerActionType = PlayerActionsTypes.Attack, Direction = DirectionsEnum.Up });
                break;

            case "h":
                actionsList.Enqueue(new AttackPlayerAction { PlayerGuid = _playerGuid, PlayerActionType = PlayerActionsTypes.Attack, Direction = DirectionsEnum.Left });
                break;

            case "j":
                actionsList.Enqueue(new AttackPlayerAction { PlayerGuid = _playerGuid, PlayerActionType = PlayerActionsTypes.Attack, Direction = DirectionsEnum.Down });
                break;

            case "k":
                actionsList.Enqueue(new AttackPlayerAction { PlayerGuid = _playerGuid, PlayerActionType = PlayerActionsTypes.Attack, Direction = DirectionsEnum.Right });
                break;

            case "U":
                actionsList.Enqueue(new DefensePlayerAction { PlayerGuid = _playerGuid, PlayerActionType = PlayerActionsTypes.Defense, Direction = DirectionsEnum.Up });
                break;

            case "H":
                actionsList.Enqueue(new DefensePlayerAction { PlayerGuid = _playerGuid, PlayerActionType = PlayerActionsTypes.Defense, Direction = DirectionsEnum.Left });
                break;

            case "J":
                actionsList.Enqueue(new DefensePlayerAction { PlayerGuid = _playerGuid, PlayerActionType = PlayerActionsTypes.Defense, Direction = DirectionsEnum.Down });
                break;

            case "K":
                actionsList.Enqueue(new DefensePlayerAction { PlayerGuid = _playerGuid, PlayerActionType = PlayerActionsTypes.Defense, Direction = DirectionsEnum.Right });
                break;

            default:
                actionsList.Enqueue(new DefensePlayerAction { PlayerGuid = _playerGuid, PlayerActionType = PlayerActionsTypes.Defense, Direction = DirectionsEnum.Right });
                break;
        }
    }

    private void UiDrawProcess(Layout layout)
    {
        var actions = actionsList.ToList().Select(x => new Text(x.PlayerActionType.ToString())).ToList();
        var rows = new Rows(actions);

        var actionsPanel = new Panel(Align.Center(
                    rows,
                    VerticalAlignment.Middle)
                ).Expand();
        actionsPanel.Header = new PanelHeader("Actions queue");

        var skeletonPanel = new Panel(
            Align.Center(
                new Rows(
                    new Text($"Name: {_skeleton.Name}"),
                    new Text($"Attack power: {_skeleton.AttackPower.ToString()}"),
                    new Text($"Hit points: {_skeleton.HitPoints.ToString()}"),
                    new Text($"Coords: {_skeleton.Xcoord} - {_skeleton.Ycoord}"),
                    new Text($"Stance: {_skeleton.FightStance.Type.ToString()}")
                ), VerticalAlignment.Middle)
            ).Expand();
        skeletonPanel.Header = new PanelHeader("Skeleton");

        layout["Left"]["Top"].Update(_mapPanel);
        layout["Right"]["Top"].Update(actionsPanel);
        layout["Left"]["Bottom"].Update(CreateMessageBoxPanel());
        layout["Right"]["Bottom"].Update(skeletonPanel.Expand());

        Thread.Sleep(50);
        AnsiConsole.Cursor.SetPosition(0, 0);
        AnsiConsole.Write(layout);
    }

    private Panel MapPanelDrawer(MapBase map)
    {
        var mapTexts = new List<Text>();
        var stringBuilder = new StringBuilder();
        for (int y = 0; y < map.Height; y++)
        {
            for (int x = 0; x < map.Width; x++)
            {
                var currentBlock = map.MapBlocks[y][x];
                if (!currentBlock.IsPassable)
                {
                    stringBuilder.Append($"▓▓");
                }
                else
                {
                    if (currentBlock.Entities.Any(x => x.Type == MapEntityTypes.Skeleton))
                    {
                        stringBuilder.Append($"S+");
                    }
                    else if (currentBlock.Entities.Count > 0)
                    {
                        stringBuilder.Append($"x{currentBlock.Entities.Count}");
                    }
                    else
                    {
                        stringBuilder.Append($"··");
                    }
                }
            }

            var textLine = new Text(stringBuilder.ToString());
            stringBuilder.Clear();
            mapTexts.Add(textLine);
        }


        var mapRows = new Rows(mapTexts);

        var mapPanel = new Panel(Align.Center(mapRows, VerticalAlignment.Middle)).Expand();

        return mapPanel;
    }

    private Panel CreateMessageBoxPanel()
    {
        var texts = _messagesBox.ToList().Select(x => new Text(x)).ToList();

        var panel = new Panel(Align.Center(new Rows(texts), VerticalAlignment.Middle)).Expand();

        return panel;
    }

    private void AddMessageToBox(string message)
    {
        if (_messagesBox.Count > 9)
            _messagesBox.Dequeue();

        _messagesBox.Enqueue(message);
    }
}
