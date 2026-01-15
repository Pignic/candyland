namespace EldmeresTale.ECS.Components;

public struct Lifetime {
	public float Duration;
	public float Remaining;

	public Lifetime(float duration) {
		Duration = duration;
		Remaining = duration;
	}
}