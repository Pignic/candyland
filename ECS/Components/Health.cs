namespace EldmeresTale.ECS.Components;

public struct Health {
	public int Current;
	public int Max;
	public bool IsDead;

	// Invincibility frames (for damage cooldown)
	public float InvincibilityTimer;
	public float InvincibilityDuration;  // How long after hit

	public Health(int max, float invincibilityDuration = 0.5f) {
		Current = max;
		Max = max;
		IsDead = false;
		InvincibilityTimer = 0f;
		InvincibilityDuration = invincibilityDuration;
	}

	public readonly bool IsInvincible => InvincibilityTimer > 0;

	public readonly float HealthRatio => Max > 0 ? (float)Current / Max : 0f;

	public void TakeDamage(int amount) {
		if (IsInvincible || IsDead) {
			return;
		}

		Current -= amount;
		InvincibilityTimer = InvincibilityDuration;

		if (Current <= 0) {
			Current = 0;
			IsDead = true;
		}
	}

	public void Heal(int amount) {
		if (IsDead) {
			return;
		}

		Current += amount;
		if (Current > Max) {
			Current = Max;
		}
	}
}