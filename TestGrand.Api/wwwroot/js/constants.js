// ============================================================
// Server -> Client message types
// Matches: TestGrand.Core/Models/ServerSendTypes/BaseServerSendType.cs
// ============================================================
export const ServerSendTypes = Object.freeze({
    SendPlayerInfo: 0,
    SendCycleGuid:  1,
    SendMap:        2,
    SkeletonInfo:   3,
    EndGame:        4,
});

// ============================================================
// Client -> Server message types
// Matches: TestGrand.Core/Models/ClientSendTypes/BaseClientSendType.cs
// ============================================================
export const ClientSendTypes = Object.freeze({
    CycleAction:  0,
    SkeletonInfo: 1,
});

// ============================================================
// Player action types
// Matches: TestGrand.Core/Models/BasePlayerAction.cs -> PlayerActionsTypes
// ============================================================
export const PlayerActionsTypes = Object.freeze({
    Move:    0,
    Attack:  1,
    Defense: 2,
});

// ============================================================
// Directions  —  CRITICAL: order is Up, Left, Down, Right
// Matches: TestGrand.Core/Models/DirectionsEnum.cs
// The broken prototype had Up=0, Down=1, Left=2, Right=3 — WRONG!
// ============================================================
export const DirectionsEnum = Object.freeze({
    Up:    0,
    Left:  1,
    Down:  2,
    Right: 3,
});

// ============================================================
// Map block types
// Matches: TestGrand.Core/Models/MapBase.cs -> MapBlockTypes
// ============================================================
export const MapBlockTypes = Object.freeze({
    Wall:  0,
    Floor: 1,
});

// ============================================================
// Map entity types
// Matches: TestGrand.Core/Models/MapBase.cs -> MapEntityTypes
// ============================================================
export const MapEntityTypes = Object.freeze({
    Skeleton:  0,
    Door:      1,
    Woodstick: 2,
});

// ============================================================
// Stance types
// Matches: TestGrand.Core/Models/Skeleton.cs -> StanceType
// ============================================================
export const StanceType = Object.freeze({
    Battle:  0,
    Defence: 1,
});

// ============================================================
// Direction labels (for UI display)
// ============================================================
export const DirectionNames = Object.freeze({
    [DirectionsEnum.Up]:    'Up',
    [DirectionsEnum.Left]:  'Left',
    [DirectionsEnum.Down]:  'Down',
    [DirectionsEnum.Right]: 'Right',
});

export const ActionNames = Object.freeze({
    [PlayerActionsTypes.Move]:    'Move',
    [PlayerActionsTypes.Attack]:  'Attack',
    [PlayerActionsTypes.Defense]: 'Defense',
});
