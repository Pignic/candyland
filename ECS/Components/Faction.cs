namespace EldmeresTale.ECS.Components;

public enum FactionName {
	NPC,
	Player,
	Wildlife,
	Enemy,
	Prop
}

public struct Faction {

	public static readonly bool[][] AttackMatrix = [
//               NPC	Player	Wild	Enemy	Prop <-- Attacker
/* NPC	    */	[false, false,  false,  true,   true ],
/* Player	*/	[false, false,  false,  true,   true ],
/* Wildlife	*/	[true,  true,   true,   true,   true ],
/* Enemy	*/	[true,  true,   true,   true,   true ],
/* Prop	    */	[true,  true,   true,   true,   false]
	];

	public FactionName Name;
	public Faction(FactionName name) {
		Name = name;
	}

	public bool CanAttack(Faction other) {
		return AttackMatrix[(int)other.Name][(int)Name];
	}

	public bool CanBeAttacked(Faction other) {
		return AttackMatrix[(int)Name][(int)other.Name];
	}
}