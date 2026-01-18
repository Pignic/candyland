using EldmeresTale.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EldmeresTale.Entities.Factories;

public static class PlayerFactory {

	private const int TILE_SIZE = 16;

	public static Player Create(Texture2D playerTexture, GraphicsDevice graphicsDevice) {
		Vector2 startPosition = Vector2.Zero;
		Player player;

		// Check if animated sprite (96px width = 3 frames of 32px)
		if (playerTexture?.Width == 96) {
			// Animated sprite sheet (3 frames, 32x32 each)
			player = new Player(
				playerTexture,
				startPosition,
				frameCount: 3,
				frameWidth: 32,
				frameHeight: 32,
				frameTime: 0.1f,
				width: 32,
				height: 32
			);
		} else {
			// Static sprite
			player = new Player(
				playerTexture,
				startPosition,
				width: 32,
				height: 32
			);
		}

		// Initialize attack effect
		player.InitializeAttackEffect(graphicsDevice);

		System.Diagnostics.Debug.WriteLine("[PLAYER FACTORY] Created player");

		return player;
	}

	public static Player Create(AssetManager assetManager, GraphicsDevice graphicsDevice) {
		Texture2D playerTexture = assetManager.LoadTextureOrFallback(
			"Assets/Sprites/player.png",
			() => Graphics.CreateColoredTexture(graphicsDevice, TILE_SIZE, TILE_SIZE, Color.Yellow)
		);

		return Create(playerTexture, graphicsDevice);
	}
}