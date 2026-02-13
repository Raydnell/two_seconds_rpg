// ============================================================
// Game State — single source of truth for the client
// ============================================================

class GameState {
    constructor() {
        this.reset();
    }

    reset() {
        this.playerGuid = '';
        this.currentCycleGuid = '';
        this.skeleton = null;
        this.map = null;
        this.actionsQueue = [];       // FIFO queue of action objects
        this.messages = [];           // Log messages (capped)
        this.isConnected = false;
        this.isGameOver = false;
    }

    // --- Actions Queue (FIFO, matches console client's Queue<BasePlayerAction>) ---

    enqueueAction(action) {
        this.actionsQueue.push(action);
    }

    dequeueAction() {
        return this.actionsQueue.shift() || null;
    }

    hasActions() {
        return this.actionsQueue.length > 0;
    }

    // --- Messages Log ---

    addMessage(text) {
        const time = new Date().toLocaleTimeString();
        this.messages.push({ time, text });
        if (this.messages.length > 15) {
            this.messages.shift();
        }
    }
}

export const gameState = new GameState();
