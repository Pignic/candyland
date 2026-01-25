using System;

namespace EldmeresTale.ECS.Components;

public struct BobAnimation {
	private static readonly Random _random = new Random();

	public float Timer;
	public float Frequency;
	public float Amplitude;
	public float BobOffset;
	public bool BobX;
	public bool BobY;

	public BobAnimation(float frequency, float amplitude) : this() {
		Frequency = frequency;
		Amplitude = amplitude;
	}

	public BobAnimation(float frequency, float amplitude, bool bobX, bool bobY) : this(frequency, amplitude) {
		BobX = bobX;
		BobY = bobY;
	}

	public BobAnimation() {
		Timer = _random.NextSingle();
		Frequency = 3;
		Amplitude = 2;
		BobOffset = 0f;
		BobY = true;
	}
}