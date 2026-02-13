// ============================================================
// Map Renderer — Canvas-based map drawing
//
// Renders the game map received from server.
// Indexing: map.MapBlocks[y][x] — outer = row, inner = column
// (matches GetMapBlock in MapService.cs line 130)
//
// NO Math.random() — all rendering is deterministic (no flickering).
// ============================================================

import { MapBlockTypes, MapEntityTypes, DirectionsEnum } from './constants.js';
import { gameState } from './game-state.js';

const TILE_SIZE = 32;

// Colors
const COLOR_WALL       = '#5C3A1E';
const COLOR_WALL_EDGE  = '#3D2610';
const COLOR_FLOOR      = '#2F4F4F';
const COLOR_FLOOR_DOT  = '#3A5A5A';
const COLOR_GRID       = '#1A1A1A';

const COLOR_SKELETON     = '#CC3333';
const COLOR_PLAYER_SKEL  = '#3399FF';
const COLOR_DOOR         = '#55AA55';
const COLOR_WOODSTICK    = '#FFAA55';

const ENTITY_LABELS = {
    [MapEntityTypes.Skeleton]:  'S',
    [MapEntityTypes.Door]:      'D',
    [MapEntityTypes.Woodstick]: 'W',
};

const ENTITY_COLORS = {
    [MapEntityTypes.Skeleton]:  COLOR_SKELETON,
    [MapEntityTypes.Door]:      COLOR_DOOR,
    [MapEntityTypes.Woodstick]: COLOR_WOODSTICK,
};

let canvas = null;
let ctx = null;

export function initMapRenderer(canvasElement) {
    canvas = canvasElement;
    ctx = canvas.getContext('2d');
}

export function renderMap() {
    const map = gameState.map;
    if (!map || !map.MapBlocks) return;

    const width  = map.Width  * TILE_SIZE;
    const height = map.Height * TILE_SIZE;

    canvas.width  = width;
    canvas.height = height;

    ctx.clearRect(0, 0, width, height);

    // Draw tiles
    for (let y = 0; y < map.Height; y++) {
        for (let x = 0; x < map.Width; x++) {
            const block = map.MapBlocks[y][x];
            const px = x * TILE_SIZE;
            const py = y * TILE_SIZE;

            drawTile(block, px, py);
            drawEntities(block, px, py, x, y);
            drawGrid(px, py);
        }
    }

    // Draw player position indicator
    drawPlayerHighlight();

    // Draw legend
    drawLegend();
}

// ---- Private drawing functions ----

function drawTile(block, px, py) {
    if (block.BlockType === MapBlockTypes.Wall || !block.IsPassable) {
        // Wall — solid brown with darker edge pattern
        ctx.fillStyle = COLOR_WALL;
        ctx.fillRect(px, py, TILE_SIZE, TILE_SIZE);

        // Brick-like pattern (deterministic, based on position)
        ctx.fillStyle = COLOR_WALL_EDGE;
        ctx.fillRect(px, py, TILE_SIZE, 1);
        ctx.fillRect(px, py + Math.floor(TILE_SIZE / 2), TILE_SIZE, 1);
        ctx.fillRect(px, py, 1, TILE_SIZE);
    } else {
        // Floor — solid dark gray with subtle dots
        ctx.fillStyle = COLOR_FLOOR;
        ctx.fillRect(px, py, TILE_SIZE, TILE_SIZE);

        // Deterministic dot pattern based on coordinates
        ctx.fillStyle = COLOR_FLOOR_DOT;
        const cx = px + TILE_SIZE / 2;
        const cy = py + TILE_SIZE / 2;
        ctx.beginPath();
        ctx.arc(cx, cy, 1.5, 0, Math.PI * 2);
        ctx.fill();
    }
}

function drawEntities(block, px, py, tileX, tileY) {
    if (!block.Entities || block.Entities.length === 0) return;

    const entityCount = block.Entities.length;

    if (entityCount > 1) {
        // Multiple entities — show yellow circle with count
        ctx.fillStyle = 'rgba(255, 200, 0, 0.8)';
        ctx.beginPath();
        ctx.arc(px + TILE_SIZE / 2, py + TILE_SIZE / 2, TILE_SIZE / 2 - 3, 0, Math.PI * 2);
        ctx.fill();

        ctx.fillStyle = '#000';
        ctx.font = 'bold 14px monospace';
        ctx.textAlign = 'center';
        ctx.textBaseline = 'middle';
        ctx.fillText(entityCount.toString(), px + TILE_SIZE / 2, py + TILE_SIZE / 2);
    } else {
        // Single entity
        const entity = block.Entities[0];
        const isPlayerSkeleton = isPlayerAt(tileX, tileY);

        // Choose color
        let color = ENTITY_COLORS[entity.Type] || '#FFFFFF';
        if (entity.Type === MapEntityTypes.Skeleton && isPlayerSkeleton) {
            color = COLOR_PLAYER_SKEL;
        }

        // Circle
        ctx.fillStyle = color;
        ctx.beginPath();
        ctx.arc(px + TILE_SIZE / 2, py + TILE_SIZE / 2, TILE_SIZE / 3, 0, Math.PI * 2);
        ctx.fill();

        // Border
        ctx.strokeStyle = '#000';
        ctx.lineWidth = 2;
        ctx.stroke();

        // Label
        const label = ENTITY_LABELS[entity.Type] || '?';
        ctx.fillStyle = '#FFF';
        ctx.font = 'bold 14px monospace';
        ctx.textAlign = 'center';
        ctx.textBaseline = 'middle';
        ctx.fillText(label, px + TILE_SIZE / 2, py + TILE_SIZE / 2);
    }
}

function drawGrid(px, py) {
    ctx.strokeStyle = COLOR_GRID;
    ctx.lineWidth = 0.5;
    ctx.strokeRect(px, py, TILE_SIZE, TILE_SIZE);
}

function drawPlayerHighlight() {
    const skel = gameState.skeleton;
    if (!skel) return;

    const px = skel.Xcoord * TILE_SIZE;
    const py = skel.Ycoord * TILE_SIZE;

    // Glowing border around the player's tile
    ctx.strokeStyle = COLOR_PLAYER_SKEL;
    ctx.lineWidth = 2;
    ctx.strokeRect(px + 1, py + 1, TILE_SIZE - 2, TILE_SIZE - 2);

    // Draw direction indicator (small arrow showing FightStance direction)
    if (skel.FightStance) {
        drawDirectionArrow(px, py, skel.FightStance.Direction);
    }
}

function drawDirectionArrow(px, py, direction) {
    const cx = px + TILE_SIZE / 2;
    const cy = py + TILE_SIZE / 2;
    const arrowLen = TILE_SIZE / 2 - 2;

    let endX = cx, endY = cy;
    switch (direction) {
        case DirectionsEnum.Up:    endY = cy - arrowLen; break;
        case DirectionsEnum.Left:  endX = cx - arrowLen; break;
        case DirectionsEnum.Down:  endY = cy + arrowLen; break;
        case DirectionsEnum.Right: endX = cx + arrowLen; break;
    }

    ctx.strokeStyle = 'rgba(255, 255, 255, 0.5)';
    ctx.lineWidth = 2;
    ctx.beginPath();
    ctx.moveTo(cx, cy);
    ctx.lineTo(endX, endY);
    ctx.stroke();
}

function isPlayerAt(x, y) {
    const skel = gameState.skeleton;
    return skel && skel.Xcoord === x && skel.Ycoord === y;
}

function drawLegend() {
    const items = [
        { color: COLOR_PLAYER_SKEL, label: 'You' },
        { color: COLOR_SKELETON,    label: 'Enemy' },
        { color: COLOR_DOOR,        label: 'Door' },
        { color: COLOR_WOODSTICK,   label: 'Item' },
    ];

    const legendX = 8;
    const legendY = 8;
    const itemH = 18;
    const legendW = 90;
    const legendH = items.length * itemH + 10;

    // Background
    ctx.fillStyle = 'rgba(0, 0, 0, 0.7)';
    ctx.fillRect(legendX, legendY, legendW, legendH);

    // Items
    ctx.font = '11px monospace';
    ctx.textAlign = 'left';
    ctx.textBaseline = 'middle';

    items.forEach((item, i) => {
        const y = legendY + 12 + i * itemH;

        // Color circle
        ctx.fillStyle = item.color;
        ctx.beginPath();
        ctx.arc(legendX + 12, y, 5, 0, Math.PI * 2);
        ctx.fill();

        // Label
        ctx.fillStyle = '#DDD';
        ctx.fillText(item.label, legendX + 24, y);
    });
}
