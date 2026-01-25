namespace EldmeresTale.ECS.Components;

public struct Lifetime {
	public float Duration;
	public float Remaining;
	public bool Fade;

	public Lifetime(float duration) {
		Duration = duration;
		Remaining = duration;
	}

	public Lifetime(float duration, bool fade) : this(duration) {
		Fade = fade;
	}
}