namespace EldmeresTale.ECS.Components;

public struct Health {
	public float MaxHealth;
	public float CurrentHealth;

	public Health(float maxHealth) : this(maxHealth, maxHealth) { }

	public Health(float maxHealth, float currentHealth) {
		MaxHealth = maxHealth;
		CurrentHealth = currentHealth;
	}
}
