namespace EldmeresTale.ECS.Components;

public struct BobAnimation {
	public float Timer;
	public float Frequency;  // How fast it bobs
	public float Amplitude;  // How much it bobs
	public float BaseY;      // Original Y position

	public BobAnimation(float baseY, float frequency = 3f, float amplitude = 2f) {
		Timer = 0f;
		Frequency = frequency;
		Amplitude = amplitude;
		BaseY = baseY;
	}
}