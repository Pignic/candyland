namespace EldmeresTale.ECS.Components;

public struct Lifetime {
	public float Remaining;

	public Lifetime(float duration) {
		Remaining = duration;
	}
}