using EldmeresTale.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EldmeresTale.Scenes;

internal class MapEditorScene : Scene {

	private MapEditor _mapEditor;
	private Texture2D editorTexture;

	public MapEditorScene(ApplicationContext appContext, Camera camera) : base(appContext, exclusive: false) {
		// Create camera for this scene
		this.camera = camera;

		// Create map editor
		_mapEditor = new MapEditor(
			appContext.Font, camera,
			appContext.Display.Scale, appContext.assetManager,
			appContext.graphicsDevice
		);
		_mapEditor.SetRoom(appContext.gameState.RoomManager.currentRoom);
	}

	public override void Load() {
		editorTexture = Graphics.CreateColoredTexture(appContext.graphicsDevice, 1, 1, Color.White);
		base.Load();
	}

	public override void Update(GameTime time) {
		_mapEditor.Update(time);
		base.Update(time);
	}

	public override void Draw(SpriteBatch spriteBatch) {
		// End previous scene's batch
		spriteBatch.End();

		spriteBatch.Begin(samplerState: SamplerState.PointClamp);

		var cursorRect = _mapEditor.GetCursorTileRect();
		if(cursorRect != Rectangle.Empty) {
			spriteBatch.Draw(editorTexture, cursorRect, _mapEditor.GetSelectedTileColor() * 0.5f);
		}

		var propRect = _mapEditor.GetCursorPropRect();
		if(propRect != Rectangle.Empty) {
			spriteBatch.Draw(editorTexture, propRect, _mapEditor.GetSelectedPropColor() * 0.6f);
		}

		_mapEditor.Draw(spriteBatch);

		spriteBatch.End();

		spriteBatch.Begin(samplerState: SamplerState.PointClamp);

		base.Draw(spriteBatch);
	}
}
