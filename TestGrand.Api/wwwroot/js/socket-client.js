// ============================================================
// WebSocket Client — handles connection and message protocol
//
// Protocol flow (matches ConsoleClient/OneClassGameClientAndIDontCare.cs):
//   1. Connect to ws://host/ws
//   2. Receive SendPlayerInfo (Type=0) with PlayerGuid
//   3. Send SkeletonInfo (Type=1) with player skeleton data
//   4. Loop:
//      a. Receive SendCycleGuid (Type=1) — if queue non-empty, send CycleAction
//      b. Receive SendMap (Type=2) — full map state
//      c. Receive SkeletonInfo (Type=3) or EndGame (Type=4)
//
// CRITICAL: CycleAction uses DOUBLE SERIALIZATION.
// The PlayerAction field is a JSON STRING, not an object.
// ============================================================

import { ServerSendTypes, ClientSendTypes, StanceType, DirectionsEnum } from './constants.js';
import { gameState } from './game-state.js';

class SocketClient {
    constructor() {
        this.ws = null;

        // Callbacks — set by app.js
        this.onPlayerInfo  = null;  // (playerGuid) => {}
        this.onCycleGuid   = null;  // (cycleGuid) => {}
        this.onMap         = null;  // (mapData) => {}
        this.onSkeletonInfo = null; // (skeleton) => {}
        this.onEndGame     = null;  // () => {}
        this.onConnected   = null;  // () => {}
        this.onDisconnected = null; // () => {}
        this.onError       = null;  // (error) => {}
    }

    connect(url) {
        if (this.ws) {
            this.ws.close();
        }

        this.ws = new WebSocket(url);

        this.ws.onopen = () => {
            gameState.isConnected = true;
            gameState.addMessage('Connected to server');
            if (this.onConnected) this.onConnected();
        };

        this.ws.onclose = () => {
            gameState.isConnected = false;
            gameState.addMessage('Disconnected from server');
            if (this.onDisconnected) this.onDisconnected();
        };

        this.ws.onerror = (event) => {
            gameState.addMessage('WebSocket error');
            if (this.onError) this.onError(event);
        };

        this.ws.onmessage = (event) => {
            this._handleMessage(event.data);
        };
    }

    disconnect() {
        if (this.ws) {
            this.ws.close();
            this.ws = null;
        }
    }

    // ---- Send methods ----

    /**
     * Send skeleton info to server after receiving PlayerGuid.
     * Matches: SendSkeletonInfo in BaseClientSendType.cs
     */
    sendSkeletonInfo(playerGuid, skeleton) {
        const message = {
            Type: ClientSendTypes.SkeletonInfo,
            PlayerGuid: playerGuid,
            Skeleton: skeleton,
        };
        this._send(message);
        gameState.addMessage('Skeleton info sent');
    }

    /**
     * Send a player action for the current cycle.
     * Uses DOUBLE SERIALIZATION — PlayerAction is a JSON string.
     *
     * Matches console client lines 124-134:
     *   var playerActionSerialized = JsonSerializer.Serialize(playerAction);
     *   var sendCycleAction = new SendCycleAction {
     *       Type = ClientSendTypes.CycleAction,
     *       PlayerAction = playerActionSerialized,
     *   };
     */
    sendCycleAction(action, cycleGuid) {
        // Set the CycleGuid on the action (matches line 123 of console client)
        action.CycleGuid = cycleGuid;

        const message = {
            Type: ClientSendTypes.CycleAction,
            PlayerAction: JSON.stringify(action),  // DOUBLE SERIALIZATION — string, not object!
        };
        this._send(message);
    }

    // ---- Private ----

    _send(data) {
        if (this.ws && this.ws.readyState === WebSocket.OPEN) {
            this.ws.send(JSON.stringify(data));
        }
    }

    _handleMessage(raw) {
        try {
            const message = JSON.parse(raw);

            // Type is a NUMERIC enum value (0, 1, 2, 3, 4)
            switch (message.Type) {

                case ServerSendTypes.SendPlayerInfo:
                    // { Type: 0, PlayerGuid: "..." }
                    gameState.playerGuid = message.PlayerGuid;
                    gameState.addMessage(`Player initialized: ${message.PlayerGuid.substring(0, 8)}...`);
                    if (this.onPlayerInfo) this.onPlayerInfo(message.PlayerGuid);
                    break;

                case ServerSendTypes.SendCycleGuid:
                    // { Type: 1, CycleGuid: "..." }
                    gameState.currentCycleGuid = message.CycleGuid;
                    if (this.onCycleGuid) this.onCycleGuid(message.CycleGuid);
                    break;

                case ServerSendTypes.SendMap:
                    // { Type: 2, Map: { Width, Height, MapBlocks: [[...]] } }
                    gameState.map = message.Map;
                    if (this.onMap) this.onMap(message.Map);
                    break;

                case ServerSendTypes.SkeletonInfo:
                    // { Type: 3, Skeleton: { Name, HitPoints, ... } }
                    gameState.skeleton = message.Skeleton;
                    if (this.onSkeletonInfo) this.onSkeletonInfo(message.Skeleton);
                    break;

                case ServerSendTypes.EndGame:
                    // { Type: 4 }
                    gameState.isGameOver = true;
                    gameState.addMessage('GAME OVER — your skeleton has been destroyed');
                    if (this.onEndGame) this.onEndGame();
                    break;

                default:
                    gameState.addMessage(`Unknown message type: ${message.Type}`);
                    break;
            }
        } catch (error) {
            gameState.addMessage(`Message parse error: ${error.message}`);
            console.error('Parse error:', error, 'Raw:', raw);
        }
    }
}

export const socketClient = new SocketClient();
