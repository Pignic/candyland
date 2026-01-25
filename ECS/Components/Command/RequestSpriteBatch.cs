using Microsoft.Xna.Framework.Graphics;
using System;

namespace EldmeresTale.ECS.Components.Command;

public struct RequestSpriteBatch {

	public Action<SpriteBatch> action;

}
