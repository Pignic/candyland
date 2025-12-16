using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Candyland.Core;

namespace Candyland.Entities
{
    /// <summary>
    /// NPC entity that can be placed in the world and interacted with
    /// </summary>
    public class NPC : ActorEntity
    {
        // Dialog system integration
        public string DialogId { get; set; }

        // Interaction
        public float InteractionRange { get; set; } = 50f;
        public bool CanInteract { get; set; } = true;

        // Visual feedback
        private bool _isPlayerNearby = false;
        private float _indicatorTimer = 0f;

        // Static sprite constructor
        public NPC(Texture2D texture, Vector2 position, string dialogId, int width = 24, int height = 24)
            : base(texture, position, width, height, 0f) // NPCs don't move (speed = 0)
        {
            DialogId = dialogId;

            // NPCs don't take damage or attack
            MaxHealth = 999999;
            health = MaxHealth;
            AttackDamage = 0;
        }

        // Animated sprite constructor
        public NPC(Texture2D spriteSheet, Vector2 position, string dialogId, int frameCount, int frameWidth, int frameHeight, float frameTime, int width = 24, int height = 24)
            : base(spriteSheet, position, frameCount, frameWidth, frameHeight, frameTime, width, height, 0f)
        {
            DialogId = dialogId;

            // NPCs don't take damage or attack
            MaxHealth = 999999;
            health = MaxHealth;
            AttackDamage = 0;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (!IsAlive) return;

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Update indicator animation
            _indicatorTimer += deltaTime;

            // Update animation if using one (idle animation)
            if (_useAnimation && _animationController != null)
            {
                _animationController.Update(gameTime, Vector2.Zero); // No movement
            }
        }

        /// <summary>
        /// Check if player is in interaction range
        /// </summary>
        public bool IsPlayerInRange(Vector2 playerPosition)
        {
            Vector2 npcCenter = Position + new Vector2(Width / 2f, Height / 2f);
            float distance = Vector2.Distance(playerPosition, npcCenter);

            bool inRange = distance <= InteractionRange;
            _isPlayerNearby = inRange;

            return inRange && CanInteract;
        }

        /// <summary>
        /// Draw NPC with interaction indicator
        /// </summary>
        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);

            // Draw interaction indicator when player is nearby
            if (_isPlayerNearby && CanInteract)
            {
                DrawInteractionIndicator(spriteBatch);
            }
        }

        /// <summary>
        /// Draw floating interaction indicator above NPC
        /// </summary>
        private void DrawInteractionIndicator(SpriteBatch spriteBatch)
        {
            // Create pixel texture if needed (you might want to pass this in instead)
            // For now, we'll draw a simple exclamation mark using the sprite

            // Floating motion
            float bobOffset = (float)System.Math.Sin(_indicatorTimer * 3f) * 3f;

            Vector2 indicatorPos = new Vector2(
                Position.X + Width / 2f - 4,
                Position.Y - 10 + bobOffset
            );

            // Draw exclamation mark indicator (simplified - you might want to use a proper texture)
            // This is just a placeholder - replace with actual indicator sprite

            // You could also use the BitmapFont to draw "E" or "!" above the NPC
            // _font.DrawText(spriteBatch, "!", indicatorPos, Color.Yellow);
        }

        /// <summary>
        /// Get the center position of the NPC (useful for dialog positioning)
        /// </summary>
        public Vector2 GetCenterPosition()
        {
            return Position + new Vector2(Width / 2f, Height / 2f);
        }
    }
}