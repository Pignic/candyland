using EldmeresTale.Core;
using EldmeresTale.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EldmeresTale.Scenes;

internal class MapEditorScene : Scene {

	private readonly MapEditor _mapEditor;
	private Texture2D _editorTexture;
	private GameServices _gameServices;

	public MapEditorScene(ApplicationContext appContext, GameServices gameServices, Camera camera) : base(appContext, exclusive: false) {
		// Create camera for this scene
		this.camera = camera;

		// Create map editor
		_mapEditor = new MapEditor(
			appContext.Font, camera,
			appContext.Display.Scale, appContext.assetManager,
			appContext.graphicsDevice
		);
		_gameServices = gameServices;
		_mapEditor.SetRoom(_gameServices.RoomManager.CurrentRoom);
	}

	public override void Load() {
		_editorTexture = Graphics.CreateColoredTexture(appContext.graphicsDevice, 1, 1, Color.White);
		base.Load();
	}

	public override void Update(GameTime time) {
		InputCommands input = appContext.Input.GetCommands();
		if (input.MapEditor) {
			appContext.Input.ConsumeAction(GameAction.MapEditor);
			appContext.CloseScene();  // Close this scene
			return;
		}
		_mapEditor.Update(time);
		base.Update(time);
	}

	public override void Draw(SpriteBatch spriteBatch) {
		// End previous scene's batch
		spriteBatch.End();

		spriteBatch.Begin(samplerState: SamplerState.PointClamp);

		Rectangle cursorRect = _mapEditor.GetCursorTileRect();
		if (cursorRect != Rectangle.Empty) {
			spriteBatch.Draw(_editorTexture, cursorRect, _mapEditor.GetSelectedTileColor() * 0.5f);
		}

		Rectangle propRect = _mapEditor.GetCursorPropRect();
		if (propRect != Rectangle.Empty) {
			spriteBatch.Draw(_editorTexture, propRect, _mapEditor.GetSelectedPropColor() * 0.6f);
		}

		_mapEditor.Draw(spriteBatch);

		spriteBatch.End();

		spriteBatch.Begin(samplerState: SamplerState.PointClamp);

		base.Draw(spriteBatch);
	}
}
