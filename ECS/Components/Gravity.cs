namespace EldmeresTale.ECS.Components;

public struct Gravity {
	public float Value;  // Pixels per second squared

	public Gravity() {
		Value = 100;
	}

	public Gravity(float value) {
		Value = value;
	}
}