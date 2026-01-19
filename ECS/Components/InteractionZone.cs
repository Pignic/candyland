using Microsoft.Xna.Framework;

namespace EldmeresTale.ECS.Components;

public struct InteractionZone {
	public float Radius;         // Interaction range in pixels
	public bool IsPlayerNearby;  // Is player in range?
	public string Prompt;        // Text to show
	public string InteractionId;

	public InteractionZone(string interactionId, float radius = 50f, string prompt = "Press E") {
		InteractionId = interactionId;
		Radius = radius;
		IsPlayerNearby = false;
		Prompt = prompt;
	}

	public bool IsInRange(Vector2 propPosition, Vector2 playerPosition) {
		float distance = Vector2.Distance(propPosition, playerPosition);
		return distance <= Radius;
	}
}