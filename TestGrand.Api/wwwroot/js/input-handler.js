// ============================================================
// Input Handler — keyboard controls
//
// Matches ConsoleClient ActionsSwitch (lines 184-239):
//   w/a/s/d         -> Move Up/Left/Down/Right
//   u/h/j/k         -> Attack Up/Left/Down/Right  (lowercase)
//   U/H/J/K         -> Defense Up/Left/Down/Right  (uppercase / Shift)
//   any other key   -> Defense Right (default)
//
// CycleGuid is NOT set here — it is set when the action is
// dequeued for sending (on receiving SendCycleGuid from server).
// ============================================================

import { PlayerActionsTypes, DirectionsEnum } from './constants.js';
import { gameState } from './game-state.js';

// Map of key -> { actionType, direction }
const KEY_MAP = {
    // Movement (WASD)
    'w': { type: PlayerActionsTypes.Move,    dir: DirectionsEnum.Up    },
    'a': { type: PlayerActionsTypes.Move,    dir: DirectionsEnum.Left  },
    's': { type: PlayerActionsTypes.Move,    dir: DirectionsEnum.Down  },
    'd': { type: PlayerActionsTypes.Move,    dir: DirectionsEnum.Right },

    // Attack (lowercase uhjk)
    'u': { type: PlayerActionsTypes.Attack,  dir: DirectionsEnum.Up    },
    'h': { type: PlayerActionsTypes.Attack,  dir: DirectionsEnum.Left  },
    'j': { type: PlayerActionsTypes.Attack,  dir: DirectionsEnum.Down  },
    'k': { type: PlayerActionsTypes.Attack,  dir: DirectionsEnum.Right },

    // Defense (uppercase UHJK — Shift held)
    'U': { type: PlayerActionsTypes.Defense, dir: DirectionsEnum.Up    },
    'H': { type: PlayerActionsTypes.Defense, dir: DirectionsEnum.Left  },
    'J': { type: PlayerActionsTypes.Defense, dir: DirectionsEnum.Down  },
    'K': { type: PlayerActionsTypes.Defense, dir: DirectionsEnum.Right },
};

let onActionAdded = null;  // callback: () => {} — notify UI to update

export function setOnActionAdded(callback) {
    onActionAdded = callback;
}

export function initInputHandler() {
    document.addEventListener('keydown', handleKeyPress);
}

export function destroyInputHandler() {
    document.removeEventListener('keydown', handleKeyPress);
}

function handleKeyPress(event) {
    // Ignore if not connected or game over
    if (!gameState.playerGuid || gameState.isGameOver) return;

    // Ignore if user is typing in an input field
    if (event.target.tagName === 'INPUT' || event.target.tagName === 'TEXTAREA') return;

    const mapping = KEY_MAP[event.key];

    let actionType, direction;

    if (mapping) {
        actionType = mapping.type;
        direction  = mapping.dir;
    } else {
        // Default: Defense Right (matches console client line 237)
        actionType = PlayerActionsTypes.Defense;
        direction  = DirectionsEnum.Right;
    }

    const action = {
        PlayerGuid:       gameState.playerGuid,
        PlayerActionType: actionType,
        Direction:        direction,
        // CycleGuid will be set when this action is sent to server
    };

    gameState.enqueueAction(action);

    if (onActionAdded) onActionAdded();
}
