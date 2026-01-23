namespace EldmeresTale.ECS.Components;

public struct BobAnimation {
	public float Timer;
	public float Frequency;
	public float Amplitude;
	public float BobOffset;

	public BobAnimation(float frequency, float amplitude) : this() {
		Frequency = frequency;
		Amplitude = amplitude;
	}

	public BobAnimation() {
		Timer = 0f;
		Frequency = 3;
		Amplitude = 2;
		BobOffset = 0f;
	}
}