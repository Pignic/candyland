namespace EldmeresTale.ECS.Components;

public struct EnemyType {
	public string TypeName;
	public float PatrolSpeed;
	public int XPValue;

	public EnemyType(string typeName, float patrolSpeed, int xpValue) {
		TypeName = typeName;
		PatrolSpeed = patrolSpeed;
		XPValue = xpValue;
	}
}