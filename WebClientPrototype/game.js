class GameClient {
    constructor() {
        this.playerGuid = '';
        this.skeleton = {
            Name: '',
            HitPoints: 0,
            AttackPower: 0,
            Xcoord: 0,
            Ycoord: 0
        };
        this.actionsQueue = [];
        this.messages = [];
        this.map = null;
        this.canvas = null;
        this.ctx = null;
        this.tileSize = 24; // Увеличил размер тайла для лучшей визуализации
        
        // Типы сообщений
        this.ServerSendTypes = {
            SendPlayerInfo: 'SendPlayerInfo',
            SendCycleGuid: 'SendCycleGuid',
            SendMap: 'SendMap',
            SkeletonInfo: 'SkeletonInfo'
        };
        
        this.ClientSendTypes = {
            SkeletonInfo: 'SkeletonInfo',
            CycleAction: 'CycleAction'
        };
        
        this.PlayerActionsTypes = {
            Move: 0,
            Attack: 1,
            Defense: 2
        };
        
        this.DirectionsEnum = {
            Up: 0,
            Down: 1,
            Left: 2,
            Right: 3
        };
        
        // Типы блоков карты
        this.MapBlockTypes = {
            Wall: 0,
            Floor: 1
        };
        
        // Типы сущностей
        this.MapEntityTypes = {
            Skeleton: 0,
            Door: 1,
            Woodstick: 2
        };
        
        // Цвета для разных типов сущностей
        this.entityColors = {
            0: '#ff5555', // Skeleton - красный
            1: '#55aa55', // Door - зеленый
            2: '#ffaa55'  // Woodstick - оранжевый
        };
        
        // Символы для сущностей
        this.entitySymbols = {
            0: '👤', // Skeleton
            1: '🚪', // Door
            2: '🪵'  // Woodstick
        };
        
        this.ws = null;
        this.init();
    }
    
    init() {
        // Инициализация интерфейса
        this.canvas = document.getElementById('gameCanvas');
        this.ctx = this.canvas.getContext('2d');
        
        // Установка обработчиков
        document.getElementById('connectBtn').addEventListener('click', () => this.connect());
        document.addEventListener('keydown', (e) => this.handleKeyPress(e));
        
        // Показать форму подключения
        document.getElementById('connectionForm').classList.add('visible');
        
        // Фокус на поле ввода URL
        document.getElementById('wsUrl').focus();
    }
    
    connect() {
        const wsUrl = document.getElementById('wsUrl').value;
        const skeletonName = document.getElementById('skeletonNameInput').value;
        const skeletonHP = parseInt(document.getElementById('skeletonHP').value);
        const skeletonAttack = parseInt(document.getElementById('skeletonAttack').value);
        
        // Создаем скелетона
        this.skeleton = {
            Name: skeletonName,
            HitPoints: skeletonHP,
            AttackPower: skeletonAttack,
            Xcoord: 0,
            Ycoord: 0
        };
        
        this.ws = new WebSocket(wsUrl);
        
        this.ws.onopen = () => {
            this.updateStatus(true, 'Подключено');
            document.getElementById('connectionForm').classList.remove('visible');
            document.getElementById('gameContainer').style.display = 'grid';
            this.addMessage('Подключено к серверу');
            
            // Запрашиваем начальное состояние
            this.sendToServer({
                Type: 'GetInitialState',
                PlayerGuid: this.playerGuid
            });
        };
        
        this.ws.onclose = () => {
            this.updateStatus(false, 'Отключено');
            this.addMessage('Отключено от сервера');
        };
        
        this.ws.onerror = (error) => {
            this.addMessage(`Ошибка WebSocket: ${error}`);
        };
        
        this.ws.onmessage = (event) => {
            this.handleServerMessage(event.data);
        };
    }
    
    handleServerMessage(data) {
        try {
            const message = JSON.parse(data);
            console.log('Получено от сервера:', message);
            
            switch(message.Type) {
                case this.ServerSendTypes.SendPlayerInfo:
                    this.playerGuid = message.PlayerGuid;
                    this.addMessage(`Игрок инициализирован с GUID: ${this.playerGuid}`);
                    
                    // Отправляем информацию о скелетоне
                    const skeletonInfo = {
                        Type: this.ClientSendTypes.SkeletonInfo,
                        PlayerGuid: this.playerGuid,
                        Skeleton: this.skeleton
                    };
                    this.sendToServer(skeletonInfo);
                    this.addMessage('Информация о скелетоне отправлена');
                    break;
                    
                case this.ServerSendTypes.SendCycleGuid:
                    this.addMessage('Получен cycle guid от сервера');
                    
                    if (this.actionsQueue.length > 0) {
                        const action = this.actionsQueue.shift();
                        action.CycleGuid = message.CycleGuid;
                        
                        const cycleAction = {
                            Type: this.ClientSendTypes.CycleAction,
                            PlayerAction: JSON.stringify(action)
                        };
                        
                        this.sendToServer(cycleAction);
                        this.addMessage('Действие отправлено на сервер');
                        this.updateActionsList();
                    }
                    break;
                    
                case this.ServerSendTypes.SendMap:
                    this.map = message.Map;
                    this.addMessage(`Карта получена: ${this.map.Width}x${this.map.Height}`);
                    this.renderMap();
                    break;
                    
                case this.ServerSendTypes.SkeletonInfo:
                    this.skeleton = message.Skeleton;
                    this.addMessage(`Скелетон обновлен: ${this.skeleton.Name} (HP: ${this.skeleton.HitPoints})`);
                    this.updateSkeletonInfo();
                    break;
                    
                default:
                    this.addMessage(`Получено неизвестное сообщение типа: ${message.Type}`);
            }
        } catch (error) {
            this.addMessage(`Ошибка обработки сообщения: ${error.message}`);
            console.error('Ошибка парсинга:', error, 'Данные:', data);
        }
    }
    
    sendToServer(data) {
        if (this.ws && this.ws.readyState === WebSocket.OPEN) {
            this.ws.send(JSON.stringify(data));
        } else {
            this.addMessage('WebSocket не подключен');
        }
    }
    
    handleKeyPress(event) {
        if (!this.playerGuid) return;
        
        let action = null;
        const key = event.key.toLowerCase();
        
        switch(key) {
            case 'w':
                action = {
                    PlayerGuid: this.playerGuid,
                    PlayerActionType: this.PlayerActionsTypes.Move,
                    Direction: this.DirectionsEnum.Up
                };
                break;
                
            case 'a':
                action = {
                    PlayerGuid: this.playerGuid,
                    PlayerActionType: this.PlayerActionsTypes.Move,
                    Direction: this.DirectionsEnum.Left
                };
                break;
                
            case 's':
                action = {
                    PlayerGuid: this.playerGuid,
                    PlayerActionType: this.PlayerActionsTypes.Move,
                    Direction: this.DirectionsEnum.Down
                };
                break;
                
            case 'd':
                action = {
                    PlayerGuid: this.playerGuid,
                    PlayerActionType: this.PlayerActionsTypes.Move,
                    Direction: this.DirectionsEnum.Right
                };
                break;
                
            case 'u':
                action = {
                    PlayerGuid: this.playerGuid,
                    PlayerActionType: this.PlayerActionsTypes.Attack,
                    Direction: this.DirectionsEnum.Up
                };
                break;
                
            case 'h':
                action = {
                    PlayerGuid: this.playerGuid,
                    PlayerActionType: this.PlayerActionsTypes.Attack,
                    Direction: this.DirectionsEnum.Left
                };
                break;
                
            case 'j':
                action = {
                    PlayerGuid: this.playerGuid,
                    PlayerActionType: this.PlayerActionsTypes.Attack,
                    Direction: this.DirectionsEnum.Down
                };
                break;
                
            case 'k':
                action = {
                    PlayerGuid: this.playerGuid,
                    PlayerActionType: this.PlayerActionsTypes.Attack,
                    Direction: this.DirectionsEnum.Right
                };
                break;
                
            default:
                action = {
                    PlayerGuid: this.playerGuid,
                    PlayerActionType: this.PlayerActionsTypes.Defense,
                    DefenseStance: "Glory"
                };
                break;
        }
        
        if (action) {
            this.actionsQueue.push(action);
            this.updateActionsList();
            this.addMessage(`Добавлено действие: ${this.getActionDescription(action)}`);
        }
    }
    
    getActionDescription(action) {
        const types = ['Движение', 'Атака', 'Защита'];
        const directions = ['Вверх', 'Вниз', 'Влево', 'Вправо'];
        
        let desc = types[action.PlayerActionType];
        
        if (action.Direction !== undefined) {
            desc += ` ${directions[action.Direction]}`;
        }
        
        if (action.DefenseStance) {
            desc += ` (${action.DefenseStance})`;
        }
        
        return desc;
    }
    
    renderMap() {
        if (!this.map || !this.map.MapBlocks) {
            this.addMessage('Карта не загружена или имеет неверный формат');
            return;
        }
        
        try {
            const width = this.map.Width * this.tileSize;
            const height = this.map.Height * this.tileSize;
            
            this.canvas.width = width;
            this.canvas.height = height;
            
            this.ctx.clearRect(0, 0, width, height);
            
            // Проверяем структуру данных
            console.log('Рендеринг карты:', {
                width: this.map.Width,
                height: this.map.Height,
                blocks: this.map.MapBlocks.length,
                firstRow: this.map.MapBlocks[0]?.length
            });
            
            for (let y = 0; y < this.map.Height; y++) {
                for (let x = 0; x < this.map.Width; x++) {
                    const block = this.map.MapBlocks[y][x];
                    const posX = x * this.tileSize;
                    const posY = y * this.tileSize;
                    
                    // Отрисовка фона блока
                    if (block.BlockType === this.MapBlockTypes.Wall || !block.IsPassable) {
                        // Стена
                        this.ctx.fillStyle = '#8B4513'; // Коричневый для стен
                        this.ctx.fillRect(posX, posY, this.tileSize, this.tileSize);
                        
                        // Текстура стены
                        this.ctx.fillStyle = '#A0522D';
                        for (let i = 0; i < 4; i++) {
                            const stoneX = posX + Math.random() * this.tileSize;
                            const stoneY = posY + Math.random() * this.tileSize;
                            this.ctx.beginPath();
                            this.ctx.arc(stoneX, stoneY, 2, 0, Math.PI * 2);
                            this.ctx.fill();
                        }
                    } else {
                        // Пол
                        this.ctx.fillStyle = '#2F4F4F'; // Темно-серый для пола
                        this.ctx.fillRect(posX, posY, this.tileSize, this.tileSize);
                        
                        // Текстура пола (светлые точки)
                        this.ctx.fillStyle = '#708090';
                        for (let i = 0; i < 3; i++) {
                            const dotX = posX + Math.random() * this.tileSize;
                            const dotY = posY + Math.random() * this.tileSize;
                            this.ctx.beginPath();
                            this.ctx.arc(dotX, dotY, 1, 0, Math.PI * 2);
                            this.ctx.fill();
                        }
                    }
                    
                    // Отрисовка сущностей на блоке
                    if (block.Entities && block.Entities.length > 0) {
                        const entityCount = block.Entities.length;
                        
                        // Если много сущностей, показываем счетчик
                        if (entityCount > 1) {
                            this.ctx.fillStyle = 'rgba(255, 255, 0, 0.7)';
                            this.ctx.beginPath();
                            this.ctx.arc(
                                posX + this.tileSize / 2,
                                posY + this.tileSize / 2,
                                this.tileSize / 2 - 2,
                                0,
                                Math.PI * 2
                            );
                            this.ctx.fill();
                            
                            // Количество сущностей
                            this.ctx.fillStyle = '#000';
                            this.ctx.font = 'bold 12px Arial';
                            this.ctx.textAlign = 'center';
                            this.ctx.textBaseline = 'middle';
                            this.ctx.fillText(
                                entityCount.toString(),
                                posX + this.tileSize / 2,
                                posY + this.tileSize / 2
                            );
                        } else if (entityCount === 1) {
                            // Для одной сущности показываем ее тип
                            const entity = block.Entities[0];
                            const entityType = entity.Type;
                            
                            // Цветная точка для сущности
                            this.ctx.fillStyle = this.entityColors[entityType] || '#ffffff';
                            this.ctx.beginPath();
                            this.ctx.arc(
                                posX + this.tileSize / 2,
                                posY + this.tileSize / 2,
                                this.tileSize / 3,
                                0,
                                Math.PI * 2
                            );
                            this.ctx.fill();
                            
                            // Черная обводка для контраста
                            this.ctx.strokeStyle = '#000';
                            this.ctx.lineWidth = 2;
                            this.ctx.stroke();
                            
                            // Символ сущности (если достаточно места)
                            if (this.tileSize >= 20) {
                                this.ctx.fillStyle = '#fff';
                                this.ctx.font = `${this.tileSize / 2}px Arial`;
                                this.ctx.textAlign = 'center';
                                this.ctx.textBaseline = 'middle';
                                const symbol = this.entitySymbols[entityType] || '?';
                                this.ctx.fillText(
                                    symbol,
                                    posX + this.tileSize / 2,
                                    posY + this.tileSize / 2
                                );
                            }
                        }
                    }
                    
                    // Сетка (границы клеток)
                    this.ctx.strokeStyle = '#444';
                    this.ctx.lineWidth = 1;
                    this.ctx.strokeRect(posX, posY, this.tileSize, this.tileSize);
                    
                    // Отладочная информация (координаты)
                    if (this.tileSize >= 30) {
                        this.ctx.fillStyle = '#666';
                        this.ctx.font = '8px Arial';
                        this.ctx.textAlign = 'left';
                        this.ctx.textBaseline = 'top';
                        this.ctx.fillText(
                            `${x},${y}`,
                            posX + 2,
                            posY + 2
                        );
                    }
                }
            }
            
            // Легенда типов сущностей
            this.renderLegend();
            
        } catch (error) {
            console.error('Ошибка при рендеринге карты:', error);
            this.addMessage(`Ошибка отрисовки карты: ${error.message}`);
        }
    }
    
    renderLegend() {
        // Отображаем легенду типов сущностей
        const legendX = 10;
        const legendY = 10;
        const legendItemHeight = 20;
        
        this.ctx.fillStyle = 'rgba(0, 0, 0, 0.7)';
        this.ctx.fillRect(legendX, legendY, 150, Object.keys(this.entityColors).length * legendItemHeight + 10);
        
        this.ctx.fillStyle = '#fff';
        this.ctx.font = '12px Arial';
        this.ctx.textAlign = 'left';
        this.ctx.textBaseline = 'middle';
        
        Object.entries(this.entityColors).forEach(([type, color], index) => {
            const y = legendY + 15 + index * legendItemHeight;
            
            // Цветной кружок
            this.ctx.fillStyle = color;
            this.ctx.beginPath();
            this.ctx.arc(legendX + 15, y, 6, 0, Math.PI * 2);
            this.ctx.fill();
            
            // Текст
            this.ctx.fillStyle = '#fff';
            let typeName = 'Unknown';
            switch(parseInt(type)) {
                case this.MapEntityTypes.Skeleton: typeName = 'Скелетон'; break;
                case this.MapEntityTypes.Door: typeName = 'Дверь'; break;
                case this.MapEntityTypes.Woodstick: typeName = 'Палка'; break;
            }
            this.ctx.fillText(typeName, legendX + 30, y);
        });
    }
    
    updateStatus(connected, message) {
        const statusEl = document.getElementById('status');
        statusEl.textContent = message;
        statusEl.className = `status ${connected ? 'connected' : 'disconnected'}`;
    }
    
    addMessage(message) {
        this.messages.push(message);
        if (this.messages.length > 15) {
            this.messages.shift();
        }
        
        const messagesList = document.getElementById('messagesList');
        messagesList.innerHTML = this.messages
            .map(msg => `<div class="message">${new Date().toLocaleTimeString()}: ${msg}</div>`)
            .join('');
        
        // Автопрокрутка вниз
        messagesList.scrollTop = messagesList.scrollHeight;
    }
    
    updateActionsList() {
        const actionsList = document.getElementById('actionsList');
        actionsList.innerHTML = this.actionsQueue
            .map((action, index) => 
                `<div class="action-item">${index + 1}. ${this.getActionDescription(action)}</div>`
            )
            .join('');
    }
    
    updateSkeletonInfo() {
        document.getElementById('skeletonName').textContent = this.skeleton.Name || '-';
        document.getElementById('skeletonHP').textContent = this.skeleton.HitPoints || '-';
        document.getElementById('skeletonAttack').textContent = this.skeleton.AttackPower || '-';
        document.getElementById('skeletonCoords').textContent = 
            `${this.skeleton.Xcoord || 0}, ${this.skeleton.Ycoord || 0}`;
    }
}

// Запуск игры при загрузке страницы
window.addEventListener('DOMContentLoaded', () => {
    window.gameClient = new GameClient();
    
    // Добавляем горячие клавиши для отладки
    document.addEventListener('keydown', (e) => {
        if (e.key === 'F2') {
            // Обновить карту
            if (window.gameClient.ws && window.gameClient.ws.readyState === WebSocket.OPEN) {
                window.gameClient.sendToServer({
                    Type: 'RequestMap',
                    PlayerGuid: window.gameClient.playerGuid
                });
            }
        }
    });
});