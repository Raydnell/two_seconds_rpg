// ============================================================
// App — Entry point, wires all modules together
//
// Matches the console client's flow:
//   1. Show connection form (like "Push any key for start a game!")
//   2. Connect to server
//   3. Receive PlayerGuid -> send SkeletonInfo
//   4. Game loop: handle CycleGuid, Map, SkeletonInfo, EndGame
//   5. Keyboard input -> action queue -> sent on next cycle
// ============================================================

import { StanceType, DirectionsEnum, ActionNames, DirectionNames } from './constants.js';
import { gameState } from './game-state.js';
import { socketClient } from './socket-client.js';
import { initMapRenderer, renderMap } from './map-renderer.js';
import { initInputHandler, setOnActionAdded, destroyInputHandler } from './input-handler.js';

// ---- DOM references ----
let elements = {};

function cacheDomElements() {
    elements = {
        // Connection form
        connectionForm:  document.getElementById('connectionForm'),
        connectBtn:      document.getElementById('connectBtn'),
        wsUrl:           document.getElementById('wsUrl'),
        skeletonName:    document.getElementById('skeletonNameInput'),
        skeletonHP:      document.getElementById('skeletonHPInput'),
        skeletonAttack:  document.getElementById('skeletonAttackInput'),

        // Game UI
        gameContainer:   document.getElementById('gameContainer'),
        gameCanvas:      document.getElementById('gameCanvas'),
        statusIndicator: document.getElementById('statusIndicator'),
        statusText:      document.getElementById('statusText'),

        // Panels
        actionsList:     document.getElementById('actionsList'),
        messagesList:    document.getElementById('messagesList'),
        skelName:        document.getElementById('skelName'),
        skelHP:          document.getElementById('skelHP'),
        skelAttack:      document.getElementById('skelAttack'),
        skelArmor:       document.getElementById('skelArmor'),
        skelCoords:      document.getElementById('skelCoords'),
        skelStance:      document.getElementById('skelStance'),

        // Game over
        gameOverOverlay: document.getElementById('gameOverOverlay'),
    };
}

// ---- Init ----

document.addEventListener('DOMContentLoaded', () => {
    cacheDomElements();
    initMapRenderer(elements.gameCanvas);

    elements.connectBtn.addEventListener('click', onConnect);

    // Allow pressing Enter in form to connect
    elements.connectionForm.addEventListener('keydown', (e) => {
        if (e.key === 'Enter') onConnect();
    });
});

// ---- Connection ----

function onConnect() {
    const url = elements.wsUrl.value.trim();
    if (!url) return;

    const skeletonData = {
        Name:        elements.skeletonName.value || 'Classssssic',
        HitPoints:   parseInt(elements.skeletonHP.value) || 10,
        AttackPower:  parseInt(elements.skeletonAttack.value) || 3,
        Xcoord: 0,
        Ycoord: 0,
        ArmorClass: 0,
        Stats: null,
        FightStance: {
            Type:      StanceType.Battle,
            Direction: DirectionsEnum.Up,
        },
    };

    // Store skeleton locally
    gameState.skeleton = skeletonData;

    // Wire up socket callbacks
    socketClient.onConnected = () => {
        updateStatus(true);
        elements.connectionForm.style.display = 'none';
        elements.gameContainer.style.display = 'grid';
        updateMessagesPanel();
    };

    socketClient.onDisconnected = () => {
        updateStatus(false);
        updateMessagesPanel();
    };

    socketClient.onPlayerInfo = (playerGuid) => {
        // Step 3: received PlayerGuid -> send SkeletonInfo
        socketClient.sendSkeletonInfo(playerGuid, skeletonData);
        updateMessagesPanel();
    };

    socketClient.onCycleGuid = (cycleGuid) => {
        // If we have a queued action, dequeue and send it
        // (matches console client line 118: if actionsList.Count > 0)
        if (gameState.hasActions()) {
            const action = gameState.dequeueAction();
            socketClient.sendCycleAction(action, cycleGuid);
            gameState.addMessage('Action sent to server');
            updateActionsPanel();
        }
        updateMessagesPanel();
    };

    socketClient.onMap = (mapData) => {
        renderMap();
        updateMessagesPanel();
    };

    socketClient.onSkeletonInfo = (skeleton) => {
        updateSkeletonPanel();
        updateMessagesPanel();
    };

    socketClient.onEndGame = () => {
        elements.gameOverOverlay.style.display = 'flex';
        destroyInputHandler();
        updateMessagesPanel();
    };

    // Set up keyboard input
    initInputHandler();
    setOnActionAdded(() => {
        updateActionsPanel();
        updateMessagesPanel();
    });

    // Connect!
    socketClient.connect(url);
}

// ---- UI Update Functions ----

function updateStatus(connected) {
    elements.statusIndicator.className = connected ? 'status-dot connected' : 'status-dot disconnected';
    elements.statusText.textContent = connected ? 'Connected' : 'Disconnected';
}

function updateMessagesPanel() {
    const html = gameState.messages
        .map(m => `<div class="message-item"><span class="msg-time">${m.time}</span> ${m.text}</div>`)
        .join('');
    elements.messagesList.innerHTML = html;
    elements.messagesList.scrollTop = elements.messagesList.scrollHeight;
}

function updateActionsPanel() {
    if (gameState.actionsQueue.length === 0) {
        elements.actionsList.innerHTML = '<div class="empty-hint">Press WASD / UHJK to add actions</div>';
        return;
    }

    const html = gameState.actionsQueue
        .map((action, i) => {
            const typeName = ActionNames[action.PlayerActionType] || '?';
            const dirName  = DirectionNames[action.Direction] || '?';
            return `<div class="action-item">${i + 1}. ${typeName} ${dirName}</div>`;
        })
        .join('');
    elements.actionsList.innerHTML = html;
}

function updateSkeletonPanel() {
    const skel = gameState.skeleton;
    if (!skel) return;

    elements.skelName.textContent   = skel.Name || '-';
    elements.skelHP.textContent     = skel.HitPoints ?? '-';
    elements.skelAttack.textContent = skel.AttackPower ?? '-';
    elements.skelArmor.textContent  = skel.ArmorClass ?? 0;
    elements.skelCoords.textContent = `${skel.Xcoord}, ${skel.Ycoord}`;

    if (skel.FightStance) {
        const stanceName = skel.FightStance.Type === StanceType.Battle ? 'Battle' : 'Defence';
        const dirName = DirectionNames[skel.FightStance.Direction] || '?';
        elements.skelStance.textContent = `${stanceName} (${dirName})`;
    } else {
        elements.skelStance.textContent = '-';
    }
}
