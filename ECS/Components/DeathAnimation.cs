using Microsoft.Xna.Framework;

namespace EldmeresTale.ECS.Components;

public struct DeathAnimation {
	public float Timer;
	public float Duration;           // Total death animation time
	public float RotationSpeed;      // Radians per second
	public float CurrentRotation;
	public float ScaleSpeed;         // Scale reduction per second
	public float CurrentScale;
	public Color InitialColor;

	public DeathAnimation(float duration = 0.8f) {
		Timer = 0f;
		Duration = duration;
		RotationSpeed = 5f;  // Fast spin
		CurrentRotation = 0f;
		ScaleSpeed = 1.5f;   // Shrink speed
		CurrentScale = 1f;
		InitialColor = Color.White;
	}

	public readonly bool IsComplete => Timer >= Duration;

	public readonly float Progress => Duration > 0 ? Timer / Duration : 1f;
}