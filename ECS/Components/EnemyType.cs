namespace EldmeresTale.ECS.Components;

public struct EnemyType {
	public string TypeName;
	public int Damage;
	public float AttackCooldown;
	public float MovementSpeed;
	public int XPValue;

	public EnemyType(string typeName, int damage, float attackCooldown, float movementSpeed, int xpValue) {
		TypeName = typeName;
		Damage = damage;
		AttackCooldown = attackCooldown;
		MovementSpeed = movementSpeed;
		XPValue = xpValue;
	}
}