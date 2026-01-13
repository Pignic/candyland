namespace EldmeresTale.ECS.Components;

public struct Gravity {
	public float Value;  // Pixels per second squared

	public Gravity(float value = 300f) {
		Value = value;
	}
}