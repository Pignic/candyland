namespace EldmeresTale.ECS.Components;

public struct ZPosition {
	public float Z;
	public float ZSpeed;
	public float Absorption = 0.5f;

	public ZPosition(float z, float zSpeed) {
		Z = z;
		ZSpeed = zSpeed;
	}
}
