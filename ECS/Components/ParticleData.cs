using Microsoft.Xna.Framework;

namespace EldmeresTale.ECS.Components;

public struct ParticleData {
	public Color Color;
	public float Size;           // Pixel size (radius)
	public float InitialSize;    // For size fade
	public float FadeSpeed;      // How fast it fades (alpha per second)
	public bool FadeSize;        // Should size shrink over time?

	public ParticleData(Color color, float size, float fadeSpeed = 1f, bool fadeSize = false) {
		Color = color;
		Size = size;
		InitialSize = size;
		FadeSpeed = fadeSpeed;
		FadeSize = fadeSize;
	}
}