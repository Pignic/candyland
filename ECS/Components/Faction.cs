namespace EldmeresTale.ECS.Components;

public enum FactionName {
	NPC,
	Player,
	Wildlife,
	Enemy,
	Prop
}

public struct Faction {
	public FactionName Name;
	public Faction(FactionName name) {
		Name = name;
	}
}
