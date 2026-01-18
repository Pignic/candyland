using DefaultEcs;

namespace EldmeresTale.ECS.Components.Command;

public struct InteractionRequest {
	public Entity Interactor;
	public string InteractionId;

	public InteractionRequest(Entity interactor, string interactionId) {
		Interactor = interactor;
		InteractionId = interactionId;
	}
}