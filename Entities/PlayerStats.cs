using Microsoft.Xna.Framework;

namespace EldmeresTale.Entities;

public class PlayerStats {
	// === BASE STATS (from leveling) ===
	public int BaseMaxHealth { get; set; }
	public int BaseAttackDamage { get; set; }
	public float BaseSpeed { get; set; }
	public int BaseDefense { get; set; }
	public float BaseAttackSpeed { get; set; } // Attacks per second
	public float BaseCritChance { get; set; } // 0.0 to 1.0 (0% to 100%)
	public float BaseCritMultiplier { get; set; } // Default 2.0 for 2x damage
	public float BaseHealthRegen { get; set; } // HP per second
	public float BaseLifeSteal { get; set; } // 0.0 to 1.0 (percentage of damage healed)
	public float BaseDodgeChance { get; set; } // 0.0 to 1.0 (0% to 100%)
	public float BaseAttackRange { get; set; } // 1 + BaseAttackRange as a multiplier

	// === EQUIPMENT BONUSES ===
	public int EquipmentMaxHealth { get; set; }
	public int EquipmentAttackDamage { get; set; }
	public float EquipmentSpeed { get; set; }
	public int EquipmentDefense { get; set; }
	public float EquipmentAttackSpeed { get; set; }
	public float EquipmentCritChance { get; set; }
	public float EquipmentCritMultiplier { get; set; }
	public float EquipmentHealthRegen { get; set; }
	public float EquipmentLifeSteal { get; set; }
	public float EquipmentDodgeChance { get; set; }
	public float EquipmentAttackRange { get; set; } // In px

	// === TEMPORARY BUFFS/DEBUFFS ===
	public int BuffMaxHealth { get; set; }
	public int BuffAttackDamage { get; set; }
	public float BuffSpeed { get; set; }
	public int BuffDefense { get; set; }
	public float BuffAttackSpeed { get; set; }
	public float BuffCritChance { get; set; }
	public float BuffCritMultiplier { get; set; }
	public float BuffHealthRegen { get; set; }
	public float BuffLifeSteal { get; set; }
	public float BuffDodgeChance { get; set; }
	public float BuffAttackRange { get; set; } // 1 + BuffAttackRange as a multiplier

	// === FINAL CALCULATED STATS (read-only properties) ===
	public int MaxHealth => BaseMaxHealth + EquipmentMaxHealth + BuffMaxHealth;
	public int AttackDamage => BaseAttackDamage + EquipmentAttackDamage + BuffAttackDamage;
	public float Speed => BaseSpeed + EquipmentSpeed + BuffSpeed;
	public int Defense => BaseDefense + EquipmentDefense + BuffDefense;
	public float AttackSpeed => BaseAttackSpeed + EquipmentAttackSpeed + BuffAttackSpeed;
	public float CritChance => MathHelper.Clamp(BaseCritChance + EquipmentCritChance + BuffCritChance, 0f, 1f);
	public float CritMultiplier => BaseCritMultiplier + EquipmentCritMultiplier + BuffCritMultiplier;
	public float HealthRegen => BaseHealthRegen + EquipmentHealthRegen + BuffHealthRegen;
	public float LifeSteal => MathHelper.Clamp(BaseLifeSteal + EquipmentLifeSteal + BuffLifeSteal, 0f, 1f);
	public float DodgeChance => MathHelper.Clamp(BaseDodgeChance + EquipmentDodgeChance + BuffDodgeChance, 0f, 0.75f); // Cap at 75%
	public float AttackRange => (1 + BaseAttackRange) * EquipmentAttackRange * (1 + BuffAttackRange);

	// Attack cooldown calculated from attack speed
	public float AttackCooldownDuration => 1f / AttackSpeed;

	public PlayerStats() {
		// Default starting stats
		BaseMaxHealth = 100;
		BaseAttackDamage = 25;
		BaseSpeed = 200f;
		BaseDefense = 0;
		BaseAttackSpeed = 2f; // 2 attacks per second
		BaseCritChance = 0.05f; // 5% crit chance
		BaseCritMultiplier = 2f; // 2x damage on crit
		BaseHealthRegen = 0f; // No regen at start
		BaseLifeSteal = 0f; // No lifesteal at start
		BaseDodgeChance = 0f; // No dodge at start
		BaseAttackRange = 0f;

		// Initialize equipment and buff stats to 0
		ResetEquipmentStats();
		ResetBuffStats();
	}

	public void ResetEquipmentStats() {
		EquipmentMaxHealth = 0;
		EquipmentAttackDamage = 0;
		EquipmentSpeed = 0;
		EquipmentDefense = 0;
		EquipmentAttackSpeed = 0;
		EquipmentCritChance = 0;
		EquipmentCritMultiplier = 0;
		EquipmentHealthRegen = 0;
		EquipmentLifeSteal = 0;
		EquipmentDodgeChance = 0;
		EquipmentAttackRange = 30;
	}

	public void ResetBuffStats() {
		BuffMaxHealth = 0;
		BuffAttackDamage = 0;
		BuffSpeed = 0;
		BuffDefense = 0;
		BuffAttackSpeed = 0;
		BuffCritChance = 0;
		BuffCritMultiplier = 0;
		BuffHealthRegen = 0;
		BuffLifeSteal = 0;
		BuffDodgeChance = 0;
		BuffAttackRange = 0f;
	}

	// Helper method to apply level-up bonuses
	public void ApplyLevelUpBonus() {
		BaseMaxHealth += 20;
		BaseAttackDamage += 5;
		BaseSpeed += 10f;
		BaseDefense += 2;
		BaseAttackSpeed += 0.1f; // Slightly faster attacks
		BaseCritChance += 0.01f; // +1% crit per level
		BaseHealthRegen += 0.5f; // +0.5 HP/sec per level
		BaseAttackRange += 0.05f; // +5% range
	}

	// Calculate damage reduction from defense
	public int CalculateDamageReduction(int incomingDamage) {
		// Formula: Damage Reduction = Defense / (Defense + 100)
		// This creates diminishing returns: 
		// 50 def = 33% reduction, 100 def = 50% reduction, 200 def = 66% reduction
		float damageReduction = Defense / (Defense + 100f);
		int reducedDamage = (int)(incomingDamage * (1f - damageReduction));
		return System.Math.Max(1, reducedDamage); // Minimum 1 damage
	}

	// Roll for critical hit
	public bool RollCritical(System.Random random) {
		return random.NextDouble() < CritChance;
	}

	// Roll for dodge
	public bool RollDodge(System.Random random) {
		return random.NextDouble() < DodgeChance;
	}

	// Calculate final attack damage (with crit)
	public int CalculateAttackDamage(System.Random random) {
		int baseDamage = AttackDamage;

		if (RollCritical(random)) {
			return (int)(baseDamage * CritMultiplier);
		}

		return baseDamage;
	}
}