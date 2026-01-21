using Microsoft.Xna.Framework;

namespace EldmeresTale.ECS.Components.Result;

public struct JustDied {
	public Vector2 Location;

	public JustDied(Vector2 location) {
		Location = location;
	}
}
