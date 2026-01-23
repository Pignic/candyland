namespace EldmeresTale.ECS.Components;

public struct EnemyType {
	public string TypeName;
	public float PatrolSpeed;

	public EnemyType(string typeName, float patrolSpeed) {
		TypeName = typeName;
		PatrolSpeed = patrolSpeed;
	}
}