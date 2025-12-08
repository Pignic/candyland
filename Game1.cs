using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Candyland.Entities;
using Candyland.Core;
using Candyland.World;
using System.IO;

namespace Candyland
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        // Player, Camera, and World
        private Player _player;
        private Camera _camera;
        private RoomManager _roomManager;

        // Current room entities (references to current room's lists)
        private System.Collections.Generic.List<Enemy> _currentEnemies;
        private System.Collections.Generic.List<Pickup> _currentPickups;

        // Damage numbers
        private System.Collections.Generic.List<DamageNumber> _damageNumbers;
        private System.Collections.Generic.List<LevelUpEffect> _levelUpEffects;

        // Pickup textures
        private Texture2D _healthPotionTexture;
        private Texture2D _coinTexture;
        private Texture2D _doorTexture;

        // UI
        private BitmapFont _font;
        private GameMenu _gameMenu;
        private MapEditor _mapEditor;
        private KeyboardState _previousKeyState;

        // Tile settings
        private const int TILE_SIZE = 32;
        private const int MAP_WIDTH = 50;  // tiles
        private const int MAP_HEIGHT = 40; // tiles

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            // Set window size
            _graphics.PreferredBackBufferWidth = 800;
            _graphics.PreferredBackBufferHeight = 600;
        }

        protected override void Initialize()
        {
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // Initialize font
            _font = new BitmapFont(GraphicsDevice);

            // Initialize room manager
            _roomManager = new RoomManager();
            _damageNumbers = new System.Collections.Generic.List<DamageNumber>();
            _levelUpEffects = new System.Collections.Generic.List<LevelUpEffect>();

            // Create pickup and door textures
            _healthPotionTexture = CreateColoredTexture(16, 16, Color.LimeGreen);
            _coinTexture = CreateColoredTexture(12, 12, Color.Gold);
            _doorTexture = CreateColoredTexture(1, 1, Color.White);

            // Load player texture/spritesheet
            Texture2D playerTexture = LoadTextureFromFile("Assets/Sprites/player.png");

            // Create placeholder player first (will be repositioned when rooms are created)
            Vector2 tempPosition = new Vector2(400, 400);

            // Check if we're using an animated sprite sheet or static sprite
            if (playerTexture != null)
            {
                int frameWidth = 32;
                int frameHeight = 32;
                int frameCount = 4;
                float frameTime = 0.1f;

                if (playerTexture.Width >= frameWidth * frameCount && playerTexture.Height >= frameHeight * 4)
                {
                    _player = new Player(playerTexture, tempPosition, frameCount, frameWidth, frameHeight, frameTime, width: 24, height: 24);
                }
                else
                {
                    _player = new Player(playerTexture, tempPosition, width: 24, height: 24);
                }
            }
            else
            {
                playerTexture = CreateColoredTexture(28, 28, Color.Yellow);
                _player = new Player(playerTexture, tempPosition, width: 24, height: 24);
            }

            // Create rooms (now that player exists)
            CreateRooms();

            // Set starting room
            _roomManager.SetCurrentRoom("room1");
            _currentEnemies = _roomManager.CurrentRoom.Enemies;
            _currentPickups = _roomManager.CurrentRoom.Pickups;

            // Position player at spawn
            _player.Position = _roomManager.CurrentRoom.PlayerSpawnPosition;

            // Initialize attack effect
            _player.InitializeAttackEffect(GraphicsDevice);

            // Create camera
            _camera = new Camera(
                _graphics.PreferredBackBufferWidth,
                _graphics.PreferredBackBufferHeight
            );

            // Set world bounds to match current room map size
            _camera.WorldBounds = new Rectangle(0, 0, _roomManager.CurrentRoom.Map.PixelWidth, _roomManager.CurrentRoom.Map.PixelHeight);

            // Create game menu
            var pixelTexture = CreateColoredTexture(1, 1, Color.White);
            _gameMenu = new GameMenu(_font, pixelTexture, _player, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);

            // Create map editor
            _mapEditor = new MapEditor(_font, pixelTexture, _camera);
            _mapEditor.SetMap(_roomManager.CurrentRoom.Map);

            _previousKeyState = Keyboard.GetState();
        }

        protected override void Update(GameTime gameTime)
        {
            KeyboardState currentKeyState = Keyboard.GetState();

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // Toggle menu with Tab
            if (currentKeyState.IsKeyDown(Keys.Tab) && _previousKeyState.IsKeyUp(Keys.Tab))
            {
                _gameMenu.IsOpen = !_gameMenu.IsOpen;
            }

            // Toggle map editor with E
            if (currentKeyState.IsKeyDown(Keys.E) && _previousKeyState.IsKeyUp(Keys.E))
            {
                _mapEditor.IsActive = !_mapEditor.IsActive;
                if (_mapEditor.IsActive)
                {
                    _mapEditor.SetMap(_roomManager.CurrentRoom.Map);
                }
            }

            // Update map editor if active
            if (_mapEditor.IsActive)
            {
                _mapEditor.Update(gameTime);
                _camera.Update(); // Still update camera for panning
                _previousKeyState = currentKeyState;
                return; // Don't update game when editor is active
            }

            // Update menu if open
            if (_gameMenu.IsOpen)
            {
                _gameMenu.Update(gameTime);
                _previousKeyState = currentKeyState;
                return; // Don't update game when menu is open
            }

            var currentMap = _roomManager.CurrentRoom.Map;

            // Update player with collision detection
            _player.Update(gameTime, currentMap);

            // Clamp player to world bounds
            _player.Position = new Vector2(
                MathHelper.Clamp(_player.Position.X, 0, currentMap.PixelWidth - _player.Width),
                MathHelper.Clamp(_player.Position.Y, 0, currentMap.PixelHeight - _player.Height)
            );

            // Check door collisions
            var door = _roomManager.CurrentRoom.CheckDoorCollision(_player.Bounds);
            if (door != null)
            {
                System.Diagnostics.Debug.WriteLine($"Transitioning from {_roomManager.CurrentRoom.Id} to {door.TargetRoomId}");

                _roomManager.TransitionToRoom(door.TargetRoomId, _player, door.TargetDoorDirection);
                _currentEnemies = _roomManager.CurrentRoom.Enemies;
                _currentPickups = _roomManager.CurrentRoom.Pickups;
                _camera.WorldBounds = new Rectangle(0, 0, _roomManager.CurrentRoom.Map.PixelWidth, _roomManager.CurrentRoom.Map.PixelHeight);

                System.Diagnostics.Debug.WriteLine($"Now in room: {_roomManager.CurrentRoom.Id}, Player pos: {_player.Position}");
            }

            // Update all enemies
            foreach (var enemy in _currentEnemies)
            {
                enemy.Update(gameTime);

                // Apply collision constraints for enemies that hit walls
                enemy.ApplyCollisionConstraints(currentMap);

                // Clamp enemies to world bounds
                enemy.Position = new Vector2(
                    MathHelper.Clamp(enemy.Position.X, 0, currentMap.PixelWidth - enemy.Width),
                    MathHelper.Clamp(enemy.Position.Y, 0, currentMap.PixelHeight - enemy.Height)
                );
            }

            // Check player attack hitting enemies
            if (_player.AttackBounds != Rectangle.Empty)
            {
                foreach (var enemy in _currentEnemies)
                {
                    // Only hit each enemy once per attack
                    if (enemy.IsAlive && !_player.HasHitEntity(enemy) && _player.AttackBounds.Intersects(enemy.Bounds))
                    {
                        Vector2 playerCenter = _player.Position + new Vector2(_player.Width / 2f, _player.Height / 2f);
                        bool wasAlive = enemy.IsAlive;
                        enemy.TakeDamage(_player.AttackDamage, playerCenter);

                        // Mark this enemy as hit during this attack
                        _player.MarkEntityAsHit(enemy);

                        // Show damage number
                        Vector2 damagePos = enemy.Position + new Vector2(enemy.Width / 2f, 0);
                        _damageNumbers.Add(new DamageNumber(_player.AttackDamage, damagePos, _font, false));

                        // Check if this attack killed the enemy
                        if (wasAlive && !enemy.IsAlive && !enemy.HasDroppedLoot)
                        {
                            // Award XP
                            bool leveledUp = _player.GainXP(enemy.XPValue);
                            if (leveledUp)
                            {
                                // Show level up effect
                                _levelUpEffects.Add(new LevelUpEffect(_player.Position, _font));
                            }

                            SpawnLoot(enemy);
                            enemy.HasDroppedLoot = true;
                        }
                    }
                }
            }

            // Update pickups
            foreach (var pickup in _currentPickups)
            {
                pickup.Update(gameTime);

                // Check if player collects it
                if (pickup.CheckCollision(_player))
                {
                    _player.CollectPickup(pickup);
                }
            }

            // Remove collected pickups
            _currentPickups.RemoveAll(p => p.IsCollected);

            // Update damage numbers
            foreach (var damageNumber in _damageNumbers)
            {
                damageNumber.Update(gameTime);
            }

            // Remove expired damage numbers
            _damageNumbers.RemoveAll(d => d.IsExpired);

            // Update level up effects
            foreach (var effect in _levelUpEffects)
            {
                effect.Update(gameTime);
            }

            // Remove expired effects
            _levelUpEffects.RemoveAll(e => e.IsExpired);

            // Remove dead enemies
            _currentEnemies.RemoveAll(e => !e.IsAlive);

            // Check enemies hitting player
            foreach (var enemy in _currentEnemies)
            {
                if (enemy.IsAlive && enemy.CollidesWith(_player))
                {
                    Vector2 enemyCenter = enemy.Position + new Vector2(enemy.Width / 2f, enemy.Height / 2f);

                    // Check if player wasn't already invincible to avoid duplicate damage numbers
                    bool wasInvincible = _player.IsInvincible;
                    _player.TakeDamage(enemy.AttackDamage, enemyCenter);

                    // Show damage number only if damage was actually taken
                    if (!wasInvincible && _player.IsInvincible)
                    {
                        Vector2 damagePos = _player.Position + new Vector2(_player.Width / 2f, 0);
                        _damageNumbers.Add(new DamageNumber(enemy.AttackDamage, damagePos, _font, true));
                    }
                }
            }

            // Make camera follow player smoothly
            _camera.FollowSmooth(_player.Position + new Vector2(_player.Width / 2f, _player.Height / 2f), 0.1f);
            _camera.Update();

            _previousKeyState = currentKeyState;

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            // Draw world with camera transform
            _spriteBatch.Begin(
                samplerState: SamplerState.PointClamp,
                transformMatrix: _camera.Transform
            );

            // Draw the tilemap
            _roomManager.CurrentRoom.Map.Draw(_spriteBatch, _camera.GetVisibleArea());

            // Draw doors
            _roomManager.CurrentRoom.DrawDoors(_spriteBatch, _doorTexture);

            // Draw pickups
            foreach (var pickup in _currentPickups)
            {
                pickup.Draw(_spriteBatch);
            }

            // Draw enemies
            foreach (var enemy in _currentEnemies)
            {
                enemy.Draw(_spriteBatch);
            }

            // Draw player (on top of enemies)
            _player.Draw(_spriteBatch);

            // Draw attack effect
            _player.DrawAttackEffect(_spriteBatch);

            // Draw map editor cursor
            if (_mapEditor.IsActive)
            {
                var cursorRect = _mapEditor.GetCursorTileRect();
                if (cursorRect != Rectangle.Empty)
                {
                    var editorTexture = CreateColoredTexture(1, 1, Color.White);
                    _spriteBatch.Draw(editorTexture, cursorRect, _mapEditor.GetSelectedTileColor() * 0.5f);
                }
            }

            // Draw damage numbers
            foreach (var damageNumber in _damageNumbers)
            {
                damageNumber.Draw(_spriteBatch);
            }

            // Draw level up effects
            foreach (var effect in _levelUpEffects)
            {
                effect.Draw(_spriteBatch);
            }

            _spriteBatch.End();

            // Draw UI (no camera transform)
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            // Draw health bar
            DrawHealthBar(_spriteBatch, 20, 20, _player.Health, _player.MaxHealth);

            // Draw coin counter
            DrawCoinCounter(_spriteBatch, 20, 50);

            // Draw XP bar
            DrawXPBar(_spriteBatch, 20, 80);

            // Draw level indicator
            DrawLevelIndicator(_spriteBatch, 240, 20);

            _spriteBatch.End();

            // Draw menu on top of everything
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _gameMenu.Draw(_spriteBatch);

            // Draw map editor UI
            if (_mapEditor.IsActive)
            {
                _mapEditor.Draw(_spriteBatch);
            }

            _spriteBatch.End();

            base.Draw(gameTime);
        }

        // Helper: Load texture directly from file (bypasses Content Pipeline)
        private Texture2D LoadTextureFromFile(string path)
        {
            if (!File.Exists(path))
                return null;

            using var fileStream = new FileStream(path, FileMode.Open);
            return Texture2D.FromStream(GraphicsDevice, fileStream);
        }

        // Helper: Create a solid color texture for placeholder graphics
        private Texture2D CreateColoredTexture(int width, int height, Color color)
        {
            var texture = new Texture2D(GraphicsDevice, width, height);
            var colorData = new Color[width * height];
            for (int i = 0; i < colorData.Length; i++)
                colorData[i] = color;
            texture.SetData(colorData);
            return texture;
        }

        private void DrawHealthBar(SpriteBatch spriteBatch, int x, int y, int health, int maxHealth)
        {
            int barWidth = 200;
            int barHeight = 20;

            // Background (empty health)
            var bgTexture = CreateColoredTexture(1, 1, Color.White);
            spriteBatch.Draw(bgTexture, new Rectangle(x, y, barWidth, barHeight), Color.DarkRed);

            // Foreground (current health)
            int currentWidth = (int)((health / (float)maxHealth) * barWidth);
            spriteBatch.Draw(bgTexture, new Rectangle(x, y, currentWidth, barHeight), Color.Red);

            // Border
            spriteBatch.Draw(bgTexture, new Rectangle(x - 2, y - 2, barWidth + 4, 2), Color.White);
            spriteBatch.Draw(bgTexture, new Rectangle(x - 2, y + barHeight, barWidth + 4, 2), Color.White);
            spriteBatch.Draw(bgTexture, new Rectangle(x - 2, y, 2, barHeight), Color.White);
            spriteBatch.Draw(bgTexture, new Rectangle(x + barWidth, y, 2, barHeight), Color.White);

            // Draw health text centered on the bar
            string healthText = $"{health} / {maxHealth}";
            int textWidth = _font.MeasureString(healthText);
            int textX = x + (barWidth - textWidth) / 2;
            int textY = y + (barHeight - 14) / 2; // 14 is approximately the font height (7 * scale 2)

            // Draw text with a shadow for better visibility
            _font.DrawText(spriteBatch, healthText, new Vector2(textX + 1, textY + 1), Color.Black);
            _font.DrawText(spriteBatch, healthText, new Vector2(textX, textY), Color.White);
        }

        private void DrawCoinCounter(SpriteBatch spriteBatch, int x, int y)
        {
            // Draw coin icon
            spriteBatch.Draw(_coinTexture, new Vector2(x, y), Color.White);

            // Draw coin count text
            string coinText = $"x {_player.Coins}";
            _font.DrawText(spriteBatch, coinText, new Vector2(x + 20, y + 2), Color.Gold);
        }

        private void DrawXPBar(SpriteBatch spriteBatch, int x, int y)
        {
            int barWidth = 200;
            int barHeight = 15;

            // Background
            var bgTexture = CreateColoredTexture(1, 1, Color.White);
            spriteBatch.Draw(bgTexture, new Rectangle(x, y, barWidth, barHeight), Color.DarkGray);

            // Foreground (current XP)
            float xpPercent = _player.XP / (float)_player.XPToNextLevel;
            int currentWidth = (int)(xpPercent * barWidth);
            spriteBatch.Draw(bgTexture, new Rectangle(x, y, currentWidth, barHeight), Color.Cyan);

            // Border
            spriteBatch.Draw(bgTexture, new Rectangle(x - 2, y - 2, barWidth + 4, 2), Color.White);
            spriteBatch.Draw(bgTexture, new Rectangle(x - 2, y + barHeight, barWidth + 4, 2), Color.White);
            spriteBatch.Draw(bgTexture, new Rectangle(x - 2, y, 2, barHeight), Color.White);
            spriteBatch.Draw(bgTexture, new Rectangle(x + barWidth, y, 2, barHeight), Color.White);

            // Draw XP text
            string xpText = $"{_player.XP} / {_player.XPToNextLevel}";
            int textWidth = _font.MeasureString(xpText);
            int textX = x + (barWidth - textWidth) / 2;
            int textY = y + 2;

            _font.DrawText(spriteBatch, xpText, new Vector2(textX + 1, textY + 1), Color.Black);
            _font.DrawText(spriteBatch, xpText, new Vector2(textX, textY), Color.White);
        }

        private void DrawLevelIndicator(SpriteBatch spriteBatch, int x, int y)
        {
            string levelText = $"LV {_player.Level}";
            _font.DrawText(spriteBatch, levelText, new Vector2(x + 1, y + 1), Color.Black);
            _font.DrawText(spriteBatch, levelText, new Vector2(x, y), Color.Yellow);
        }

        private void SpawnLoot(Enemy enemy)
        {
            var random = new System.Random();
            Vector2 dropPosition = enemy.Position + new Vector2(enemy.Width / 2f - 8, enemy.Height / 2f - 8);

            // Check if health potion drops
            if (random.NextDouble() < enemy.HealthDropChance)
            {
                _currentPickups.Add(new Pickup(PickupType.HealthPotion, dropPosition, _healthPotionTexture));
            }

            // Check if coins drop
            if (random.NextDouble() < enemy.CoinDropChance)
            {
                // Random chance for big coin
                PickupType coinType = random.NextDouble() < 0.2 ? PickupType.BigCoin : PickupType.Coin;
                Vector2 coinPos = dropPosition + new Vector2(random.Next(-10, 10), random.Next(-10, 10));
                _currentPickups.Add(new Pickup(coinType, coinPos, _coinTexture));
            }
        }

        private void CreateRooms()
        {
            // Load enemy sprites (with fallback to colored blobs)
            Texture2D idleEnemySprite = LoadTextureFromFile("Assets/Sprites/enemy_idle.png")
                ?? CreateEnemySprite(Color.Red, Color.DarkRed);
            Texture2D patrolEnemySprite = LoadTextureFromFile("Assets/Sprites/enemy_patrol.png")
                ?? CreateEnemySprite(Color.Blue, Color.DarkBlue);
            Texture2D wanderEnemySprite = LoadTextureFromFile("Assets/Sprites/enemy_wander.png")
                ?? CreateEnemySprite(Color.Orange, Color.DarkOrange);
            Texture2D chaseEnemySprite = LoadTextureFromFile("Assets/Sprites/enemy_chase.png")
                ?? CreateEnemySprite(Color.Purple, Color.DarkMagenta);

            // Room 1 - Starting room
            // Try to load from file, fallback to procedural generation
            var room1MapData = MapData.LoadFromFile("Assets/Maps/room1.json");
            var room1Map = room1MapData != null
                ? room1MapData.ToTileMap(GraphicsDevice)
                : new TileMap(MAP_WIDTH, MAP_HEIGHT, TILE_SIZE, GraphicsDevice, seed: 100);
            var room1 = new Room("room1", room1Map, 100);

            // Check if sprites are animated (128x128 for 4x4 frames)
            bool idleAnimated = idleEnemySprite.Width == 128 && idleEnemySprite.Height == 128;
            bool patrolAnimated = patrolEnemySprite.Width == 128 && patrolEnemySprite.Height == 128;

            // Add enemies to room 1
            if (idleAnimated)
            {
                room1.Enemies.Add(new Enemy(idleEnemySprite, new Vector2(400, 300), EnemyBehavior.Idle, 4, 32, 32, 0.15f));
            }
            else
            {
                room1.Enemies.Add(new Enemy(idleEnemySprite, new Vector2(400, 300), EnemyBehavior.Idle));
            }

            if (patrolAnimated)
            {
                var patrolEnemy = new Enemy(patrolEnemySprite, new Vector2(600, 400), EnemyBehavior.Patrol, 4, 32, 32, 0.15f, speed: 80f);
                patrolEnemy.SetPatrolPoints(new Vector2(600, 400), new Vector2(800, 400));
                room1.Enemies.Add(patrolEnemy);
            }
            else
            {
                var patrolEnemy = new Enemy(patrolEnemySprite, new Vector2(600, 400), EnemyBehavior.Patrol, speed: 80f);
                patrolEnemy.SetPatrolPoints(new Vector2(600, 400), new Vector2(800, 400));
                room1.Enemies.Add(patrolEnemy);
            }

            // Add doors to room 1
            room1.AddDoor(DoorDirection.North, "room2", DoorDirection.South);
            room1.AddDoor(DoorDirection.East, "room3", DoorDirection.West);

            _roomManager.AddRoom(room1);

            // Room 2 - North room
            var room2MapData = MapData.LoadFromFile("Assets/Maps/room2.json");
            var room2Map = room2MapData != null
                ? room2MapData.ToTileMap(GraphicsDevice)
                : new TileMap(MAP_WIDTH, MAP_HEIGHT, TILE_SIZE, GraphicsDevice, seed: 200);
            var room2 = new Room("room2", room2Map, 200);

            // Add enemies to room 2
            bool wanderAnimated = wanderEnemySprite.Width == 128 && wanderEnemySprite.Height == 128;
            if (wanderAnimated)
            {
                room2.Enemies.Add(new Enemy(wanderEnemySprite, new Vector2(300, 600), EnemyBehavior.Wander, 4, 32, 32, 0.15f, speed: 60f));
                room2.Enemies.Add(new Enemy(wanderEnemySprite, new Vector2(700, 400), EnemyBehavior.Wander, 4, 32, 32, 0.15f, speed: 60f));
            }
            else
            {
                room2.Enemies.Add(new Enemy(wanderEnemySprite, new Vector2(300, 600), EnemyBehavior.Wander, speed: 60f));
                room2.Enemies.Add(new Enemy(wanderEnemySprite, new Vector2(700, 400), EnemyBehavior.Wander, speed: 60f));
            }

            // Add door back to room 1
            room2.AddDoor(DoorDirection.South, "room1", DoorDirection.North);

            _roomManager.AddRoom(room2);

            // Room 3 - East room
            var room3MapData = MapData.LoadFromFile("Assets/Maps/room3.json");
            var room3Map = room3MapData != null
                ? room3MapData.ToTileMap(GraphicsDevice)
                : new TileMap(MAP_WIDTH, MAP_HEIGHT, TILE_SIZE, GraphicsDevice, seed: 300);
            var room3 = new Room("room3", room3Map, 300);

            // Add chase enemy to room 3
            bool chaseAnimated = chaseEnemySprite.Width == 128 && chaseEnemySprite.Height == 128;
            if (chaseAnimated)
            {
                var chaseEnemy = new Enemy(chaseEnemySprite, new Vector2(900, 450), EnemyBehavior.Chase, 4, 32, 32, 0.15f, speed: 120f);
                chaseEnemy.DetectionRange = 200f;
                chaseEnemy.SetChaseTarget(_player, room3Map);
                room3.Enemies.Add(chaseEnemy);
            }
            else
            {
                var chaseEnemy = new Enemy(chaseEnemySprite, new Vector2(900, 450), EnemyBehavior.Chase, speed: 120f);
                chaseEnemy.DetectionRange = 200f;
                chaseEnemy.SetChaseTarget(_player, room3Map);
                room3.Enemies.Add(chaseEnemy);
            }

            // Add door back to room 1
            room3.AddDoor(DoorDirection.West, "room1", DoorDirection.East);

            _roomManager.AddRoom(room3);
        }

        private Texture2D CreateEnemySprite(Color primaryColor, Color secondaryColor)
        {
            int size = 28;
            Texture2D texture = new Texture2D(GraphicsDevice, size, size);
            Color[] data = new Color[size * size];

            // Simple enemy design - blob with eyes
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int index = y * size + x;
                    int centerX = size / 2;
                    int centerY = size / 2;

                    // Calculate distance from center
                    int dx = x - centerX;
                    int dy = y - centerY;
                    int distSq = dx * dx + dy * dy;

                    // Create circular body
                    if (distSq < (size / 2 - 2) * (size / 2 - 2))
                    {
                        // Bottom half darker
                        if (y > centerY)
                        {
                            data[index] = secondaryColor;
                        }
                        else
                        {
                            data[index] = primaryColor;
                        }

                        // Eyes (white with black pupils)
                        if (y >= centerY - 4 && y <= centerY - 2)
                        {
                            // Left eye
                            if (x >= centerX - 6 && x <= centerX - 4)
                            {
                                if (x == centerX - 5 && y == centerY - 3)
                                    data[index] = Color.Black; // Pupil
                                else
                                    data[index] = Color.White; // Eye white
                            }
                            // Right eye
                            if (x >= centerX + 4 && x <= centerX + 6)
                            {
                                if (x == centerX + 5 && y == centerY - 3)
                                    data[index] = Color.Black; // Pupil
                                else
                                    data[index] = Color.White; // Eye white
                            }
                        }

                        // Simple mouth
                        if (y == centerY + 3)
                        {
                            if (x >= centerX - 4 && x <= centerX + 4)
                            {
                                data[index] = Color.Black;
                            }
                        }
                    }
                    else
                    {
                        data[index] = Color.Transparent;
                    }
                }
            }

            texture.SetData(data);
            return texture;
        }
    }
}