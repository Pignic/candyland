using EldmeresTale.Core;
using EldmeresTale.Core.UI;
using EldmeresTale.Dialog;
using EldmeresTale.Entities;
using EldmeresTale.Quests;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace EldmeresTale.Scenes;

internal class DialogScene : Scene {

	private UIDialog _dialogUI;

	public DialogScene(ApplicationContext appContext, string dialogId) : base(appContext, exclusive: true) {
		appContext.gameState.DialogManager.startDialog(dialogId);
		appContext.gameState.QuestManager.updateObjectiveProgress("talk_to_npc", dialogId, 1);
		// Create dialog UI
		_dialogUI = new UIDialog(
			appContext.gameState.DialogManager,
			appContext.Font,
			appContext.graphicsDevice,
			appContext.Display.VirtualWidth,
			appContext.Display.VirtualHeight,
			appContext.Display.Scale
		);
	}

	public override void Load() {
		// Load portraits
		var portrait = appContext.assetManager.LoadTexture("Assets/Portrait/npc_villager_concerned.png");
		if(portrait != null) {
			_dialogUI.loadPortrait("npc_shepherd", portrait);
		}
		base.Load();
	}

	public override void Update(GameTime time) {
		if(appContext.gameState.DialogManager.isDialogActive) {
			_dialogUI.update(time);
		} else {
			appContext.CloseScene();
		}
		base.Update(time);
	}

	public override void Draw(SpriteBatch spriteBatch) {
		// End previous scene's batch
		spriteBatch.End();

		// Begin fresh for menu
		spriteBatch.Begin(samplerState: SamplerState.PointClamp);

		_dialogUI.draw(spriteBatch);

		spriteBatch.End();

		spriteBatch.Begin(samplerState: SamplerState.PointClamp);
		base.Draw(spriteBatch);
	}
}
