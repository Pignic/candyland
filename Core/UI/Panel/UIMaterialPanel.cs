using EldmeresTale.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace EldmeresTale.Core.UI.Panel;

public class UIMaterialPanel : UIPanel {

	readonly ApplicationContext _appContext;
	private readonly Player _player;

	private readonly NavigationController _navController;

	public UIMaterialPanel(ApplicationContext appcContext, Player player) {
		_appContext = appcContext;
		_player = player;
		_navController = new NavigationController {
			Mode = NavigationMode.Spatial,
			WrapAround = true
		};
		Width = -1;
		Height = -1;
		SetPadding(5);
		Layout = LayoutMode.Grid;
	}

	public override bool HandleMouse(MouseState mouse, MouseState previousMouse) {
		return base.HandleMouse(mouse, previousMouse);
	}

	public override void Update(GameTime gameTime) {
		base.Update(gameTime);
	}

	public override void Draw(SpriteBatch spriteBatch) {
		base.Draw(spriteBatch);
	}

}
