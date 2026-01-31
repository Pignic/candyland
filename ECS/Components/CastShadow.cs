using Microsoft.Xna.Framework;

namespace EldmeresTale.ECS.Components;

public struct CastShadow {

	public Color Tint;

	public CastShadow() {
		Tint = Color.Black;
	}

	public CastShadow(Color tint) {
		Tint = tint;
	}
}
