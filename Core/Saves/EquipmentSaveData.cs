
namespace EldmeresTale.Core.Saves;

public class EquipmentSaveData {

	// IDENTITY
	public string ItemId { get; set; }

	public string Name { get; set; }
	public string Description { get; set; }

	// TYPE
	public string Slot { get; set; }
	public string Rarity { get; set; }

	// REQUIREMENTS
	public int RequiredLevel { get; set; }

	// STAT BONUSES
	// Combat
	public int AttackDamageBonus { get; set; }
	public int DefenseBonus { get; set; }
	public int MaxHealthBonus { get; set; }

	// Advanced combat
	public float AttackSpeedBonus { get; set; }
	public float CritChanceBonus { get; set; }
	public float CritMultiplierBonus { get; set; }
	public float LifeStealBonus { get; set; }
	public float DodgeChanceBonus { get; set; }

	// Regeneration
	public float HealthRegenBonus { get; set; }

	// Movement
	public float SpeedBonus { get; set; }

	public EquipmentSaveData() {
		ItemId = "";
		Name = "";
		Description = "";
		Slot = "";
		Rarity = "";
	}
}