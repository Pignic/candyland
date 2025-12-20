using Candyland.Core;
using Candyland.Entities;

namespace Candyland.Scenes;

internal class GameMenuScene : Scene {

	public GameMenuScene(ApplicationContext appContext, bool exclusive = true) : base(appContext, exclusive) {

		//_gameMenu.SetGameData(player, appContext.gameState.QuestManager);
	}
}
