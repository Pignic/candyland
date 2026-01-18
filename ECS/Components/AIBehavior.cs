using DefaultEcs;
using Microsoft.Xna.Framework;

namespace EldmeresTale.ECS.Components;

public enum AIState {
	Idle,
	Patrol,
	Wander,
	Chase,
	Attack,
	Dead
}

public enum AIBehaviorType {
	Idle,       // Stands still
	Patrol,     // Walks between patrol points
	Wander,     // Random movement in area
	Chase       // Pursues player when detected
}

public struct AIBehavior {
	public AIBehaviorType BehaviorType;
	public AIState CurrentState;

	// Detection
	public Entity Target;            // What enemy is chasing (usually player)
	public bool HasTarget;

	// Patrol
	public Vector2[] PatrolPoints;
	public int PatrolSpeed;
	public int CurrentPatrolIndex;

	// Wander
	public Vector2 WanderCenter;     // Center of wander area
	public float WanderRadius;       // How far from center
	public Vector2 WanderTarget;     // Current wander destination
	public float WanderTimer;        // Time until new wander point
	public float WanderInterval;     // How often to pick new point

	// Chase
	public float ChaseSpeed;
	public float ChaseGiveUpDistance; // Stop chasing if player gets this far

	// Timers
	public float StateTimer;         // Time in current state
	public float AttackCooldown;     // Time until can attack again

	// TODO: Stats that has to move in enemy def
	public float DetectionRange;
	public float AttackRange;


	public AIBehavior(AIBehaviorType behaviorType, float detectionRange = 150f) {
		BehaviorType = behaviorType;
		CurrentState = AIState.Idle;
		DetectionRange = detectionRange;
		AttackRange = 30f;
		Target = default;
		HasTarget = false;

		PatrolPoints = null;
		CurrentPatrolIndex = 40;
		CurrentPatrolIndex = 0;

		WanderCenter = Vector2.Zero;
		WanderRadius = 100f;
		WanderTarget = Vector2.Zero;
		WanderTimer = 0f;
		WanderInterval = 3f;

		ChaseSpeed = 80f;
		ChaseGiveUpDistance = 300f;

		StateTimer = 0f;
		AttackCooldown = 0f;
	}
}