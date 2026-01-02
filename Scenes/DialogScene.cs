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

	private DialogManager _dialogManager;
	private CutsceneCommandExecutor _cutsceneExecutor;

	private UIDialog _dialogUI;

	private Camera _camera;

	public DialogScene(ApplicationContext appContext, string dialogId, Camera camera) : base(appContext, exclusive: true) {

		_dialogManager = appContext.gameState.DialogManager;
		_camera = camera;

		// Create cutscene context and executor
		var cutsceneContext = new CutsceneContext(appContext, _camera);
		_cutsceneExecutor = new CutsceneCommandExecutor(cutsceneContext);

		// Handle command completion
		_cutsceneExecutor.OnCommandComplete += (nextNodeId) => {
			if(nextNodeId == "end" || nextNodeId == null) {
				appContext.CloseScene();
			} else {
				_dialogManager.currentDialog.goToNode(nextNodeId);
				ProcessCurrentNode(); // Process next node
			}
		};
		if(!appContext.gameState.DialogManager.startCutscene(dialogId)) {
			appContext.gameState.DialogManager.startDialog(dialogId);
		}
		System.Diagnostics.Debug.WriteLine($"[CUTSCENE] Started dialog: {dialogId}, currentDialog={(appContext.gameState.DialogManager.currentDialog != null ? "exists" : "NULL")}");
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
		_dialogUI.OnResponseChosen += () => ProcessCurrentNode();
		ProcessCurrentNode();
	}

	private void ProcessCurrentNode() {
		var node = _dialogManager.getCurrentNode();
		System.Diagnostics.Debug.WriteLine($"[CUTSCENE] ProcessCurrentNode: node={(node != null ? node.id : "NULL")}, nodeType={(node != null ? node.nodeType : "N/A")}");
		if(node == null) {
			appContext.CloseScene();
			return;
		}

		// If it's a command node, execute it
		if(node.IsCommand()) {
			_cutsceneExecutor.ExecuteCommand(node.command);
		}
		// Otherwise it's a dialog node - show UI as normal
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
		_cutsceneExecutor.Update(time);

		// Only process input if not executing a command
		if(!_cutsceneExecutor.IsExecuting) {
			var input = appContext.Input.GetCommands();

			if(input.CancelPressed) {
				appContext.CloseScene();
				return;
			}
		}
		if(appContext.gameState.DialogManager.isDialogActive) {
			_dialogUI.update(time);
		} else {
			System.Diagnostics.Debug.WriteLine($"[CUTSCENE] Dialog NOT active - closing! currentDialog={(appContext.gameState.DialogManager.currentDialog != null ? "exists" : "NULL")}");
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

		if(_cutsceneExecutor.Context.isFading) {
			var fadeColor = Color.Black * _cutsceneExecutor.Context.fadeAlpha;
			spriteBatch.Draw(
				appContext.assetManager.DefaultTexture,
				new Rectangle(0, 0, 640, 360),
				fadeColor
			);
		}
		base.Draw(spriteBatch);
	}
}
