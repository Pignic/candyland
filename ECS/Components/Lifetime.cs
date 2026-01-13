namespace EldmeresTale.ECS.Components;

public struct Lifetime {
	public float Duartion;
	public float Remaining;

	public Lifetime(float duration) {
		Duartion = duration;
		Remaining = duration;
	}
}