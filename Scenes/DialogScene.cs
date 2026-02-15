using EldmeresTale.Core;
using EldmeresTale.Core.UI;
using EldmeresTale.Dialog;
using EldmeresTale.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EldmeresTale.Scenes;

internal class DialogScene : Scene {

	private readonly GameServices _gameServices;

	private readonly DialogManager _dialogManager;
	private readonly CutsceneCommandExecutor _cutsceneExecutor;
	private readonly UIDialog _dialogUI;
	private readonly Camera _camera;

	public DialogScene(ApplicationContext appContext, GameServices gameServices, string dialogId, Camera camera, Inventory targetInventory = null) : base(appContext, exclusive: true) {
		_gameServices = gameServices;
		_dialogManager = _gameServices.DialogManager;
		_camera = camera;

		// Create cutscene context and executor
		CutsceneContext cutsceneContext = new CutsceneContext(appContext, _gameServices, _camera);
		_cutsceneExecutor = new CutsceneCommandExecutor(cutsceneContext);

		// Handle command completion
		_cutsceneExecutor.OnCommandComplete += (nextNodeId) => {
			if (nextNodeId == "end" || nextNodeId == null) {
				appContext.CloseScene();
			} else {
				_dialogManager.CurrentDialog.GoToNode(nextNodeId);
				ProcessCurrentNode(); // Process next node
			}
		};
		if (!_gameServices.DialogManager.StartCutscene(dialogId)) {
			_gameServices.DialogManager.StartDialog(dialogId);
		}
		System.Diagnostics.Debug.WriteLine($"[CUTSCENE] Started dialog: {dialogId}, currentDialog={(_gameServices.DialogManager.CurrentDialog != null ? "exists" : "NULL")}");
		_gameServices.QuestManager.UpdateObjectiveProgress("talk_to_npc", dialogId, 1);
		// Create dialog UI
		_dialogUI = new UIDialog(
			_gameServices.DialogManager,
			appContext.Display.VirtualWidth,
			appContext.Display.VirtualHeight,
			appContext.Display.Scale
		);
		_dialogUI.OnResponseChosen += () => ProcessCurrentNode();
		_dialogUI.OnTradeRequested += () => RequestTrade(_gameServices, targetInventory);
		ProcessCurrentNode();
	}

	private void RequestTrade(GameServices gameServices, Inventory targetInventory) {
		appContext.RequestTrade(gameServices, targetInventory);
	}

	private void ProcessCurrentNode() {
		DialogNode node = _dialogManager.GetCurrentNode();
		System.Diagnostics.Debug.WriteLine($"[CUTSCENE] ProcessCurrentNode: node={(node != null ? node.Id : "NULL")}, nodeType={(node != null ? node.NodeType : "N/A")}");
		if (node == null) {
			appContext.CloseScene();
			return;
		}

		// If it's a command node, execute it
		if (node.IsCommand()) {
			_cutsceneExecutor.ExecuteCommand(node.Command);
		}
	}

	// TODO: load dynamic
	public override void Load() {
		// Load portraits
		Texture2D portrait = appContext.AssetManager.LoadTexture("Assets/Portrait/npc_villager_concerned.png");
		if (portrait != null) {
			_dialogUI.LoadPortrait("npc_shepherd", portrait);
		}
		base.Load();
	}

	public override void Update(GameTime time) {
		_cutsceneExecutor.Update(time);

		// Only process input if not executing a command
		if (!_cutsceneExecutor.IsExecuting) {
			InputCommands input = appContext.Input.GetCommands();

			if (input.CancelPressed) {
				appContext.CloseScene();
				return;
			}
		}
		if (_gameServices.DialogManager.IsDialogActive) {
			_dialogUI.Update(time);
		} else {
			System.Diagnostics.Debug.WriteLine($"[CUTSCENE] Dialog NOT active - closing! currentDialog={(_gameServices.DialogManager.CurrentDialog != null ? "exists" : "NULL")}");
			appContext.CloseScene();
		}
		base.Update(time);
	}

	public override void Draw(SpriteBatch spriteBatch) {
		// End previous scene's batch
		spriteBatch.End();

		// Begin fresh for menu
		spriteBatch.Begin(samplerState: SamplerState.PointClamp);

		_dialogUI.Draw(spriteBatch);

		spriteBatch.End();

		spriteBatch.Begin(samplerState: SamplerState.PointClamp);

		if (_cutsceneExecutor.Context.IsFading) {
			Color fadeColor = Color.Black * _cutsceneExecutor.Context.FadeAlpha;
			spriteBatch.Draw(
				appContext.AssetManager.DefaultTexture,
				new Rectangle(0, 0, 640, 360),
				fadeColor
			);
		}
		base.Draw(spriteBatch);
	}
}
