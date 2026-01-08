using EldmeresTale.Audio;
using EldmeresTale.Entities;
using EldmeresTale.Events;
using EldmeresTale.Quests;
using EldmeresTale.Systems;
using EldmeresTale.Systems.Particles;
using Microsoft.Xna.Framework;
using System;

namespace EldmeresTale.Core.Coordination;

public class EventCoordinator : IDisposable {
	private readonly GameEventBus _eventBus;
	private readonly EventSubscriptions _subscriptions = new();

	// System references
	private readonly ParticleSystem _particleSystem;
	private readonly VFXSystem _vfxSystem;
	private readonly LootSystem _lootSystem;
	private readonly QuestManager _questManager;
	private readonly Player _player;
	private readonly Camera _camera;
	private readonly SoundEffectPlayer _soundPlayer;
	private readonly CombatSystem _combatSystem;
	private readonly NotificationSystem _notificationSystem;

	public EventCoordinator(
		GameEventBus eventBus,
		ParticleSystem particleSystem,
		VFXSystem vfxSystem,
		LootSystem lootSystem,
		QuestManager questManager,
		Player player,
		Camera camera,
		SoundEffectPlayer soundPlayer,
		CombatSystem combatSystem,
		NotificationSystem notificationSystem
	) {
		_eventBus = eventBus;
		_particleSystem = particleSystem;
		_vfxSystem = vfxSystem;
		_lootSystem = lootSystem;
		_questManager = questManager;
		_player = player;
		_camera = camera;
		_soundPlayer = soundPlayer;
		_combatSystem = combatSystem;
		_notificationSystem = notificationSystem;
	}

	public void Initialize() {
		// Combat events
		_subscriptions.Add(_eventBus.Subscribe<EnemyHitEvent>(OnEnemyHit));
		_subscriptions.Add(_eventBus.Subscribe<EnemyKilledEvent>(OnEnemyKilled));
		_subscriptions.Add(_eventBus.Subscribe<PropHitEvent>(OnPropHit));
		_subscriptions.Add(_eventBus.Subscribe<PropDestroyedEvent>(OnPropDestroyed));
		_subscriptions.Add(_eventBus.Subscribe<PlayerHitEvent>(OnPlayerHit));

		// Physics events
		_subscriptions.Add(_eventBus.Subscribe<PropCollectedEvent>(OnPropCollected));
		_subscriptions.Add(_eventBus.Subscribe<PropPushedEvent>(OnPropPushed));

		// Loot events
		_subscriptions.Add(_eventBus.Subscribe<PickupSpawnedEvent>(OnPickupSpawned));
		_subscriptions.Add(_eventBus.Subscribe<PickupCollectedEvent>(OnPickupCollected));

		// Quest events
		_subscriptions.Add(_eventBus.Subscribe<QuestStartedEvent>(OnQuestStarted));
		_subscriptions.Add(_eventBus.Subscribe<QuestCompletedEvent>(OnQuestCompleted));
		_subscriptions.Add(_eventBus.Subscribe<QuestObjectiveUpdatedEvent>(OnQuestObjectiveUpdated));

		// Player events
		_subscriptions.Add(_eventBus.Subscribe<PlayerLevelUpEvent>(OnPlayerLevelUp));

		System.Diagnostics.Debug.WriteLine($"[EVENT COORDINATOR] Initialized with {_subscriptions.Count} event subscriptions");
	}

	private void OnEnemyHit(EnemyHitEvent e) {
		Vector2 hitDirection = e.Enemy.Position - _player.Position;
		_particleSystem.Emit(ParticleType.Blood, e.DamagePosition, 8, hitDirection);

		Color damageColor = e.WasCritical ? Color.Orange : Color.White;
		_vfxSystem.ShowDamage(e.Damage, e.DamagePosition, e.WasCritical, damageColor);

		if (e.WasCritical) {
			_soundPlayer.Play("crit_attack", 0.5f);
			_camera.Shake(2f, 0.15f);
			_combatSystem.Pause(0.08f);
		}
		_soundPlayer.Play("monster_hurt_mid", 0.5f);
	}

	private void OnEnemyKilled(EnemyKilledEvent e) {
		_particleSystem.Emit(ParticleType.Blood, e.DeathPosition, 20);
		_lootSystem.SpawnLootFromEnemy(e.Enemy);
		e.Enemy.HasDroppedLoot = true;
		_camera.Shake(2f, 0.15f);
		_combatSystem.Pause(0.06f);
		_questManager.UpdateObjectiveProgress("kill_enemy", e.Enemy.EnemyType, 1);

		bool leveledUp = _player.GainXP(e.Enemy.XPValue);
		_soundPlayer.Play("monster_growl_mid", 0.8f);
	}

	private void OnPropHit(PropHitEvent e) {
		_vfxSystem.ShowDamage(e.Damage, e.DamagePosition, e.WasCritical, Color.Gray);
		_soundPlayer.Play("material_hit", 0.5f);
	}

	private void OnPropDestroyed(PropDestroyedEvent e) {
		_particleSystem.Emit(ParticleType.Destruction, e.DestructionPosition, 15);
		_soundPlayer.Play("equip_armor", 0.6f);

		if (e.Prop.type == PropType.Breakable) {
			Random random = new Random();
			if (random.NextDouble() < 0.7) {
				_lootSystem.SpawnPickup(PickupType.Coin, e.DestructionPosition);
			}
			if (random.NextDouble() < 0.3) {
				_lootSystem.SpawnPickup(PickupType.HealthPotion, e.DestructionPosition);
			}
		}

		_questManager.UpdateObjectiveProgress("destroy_prop", e.Prop.type.ToString(), 1);
	}

	private void OnPlayerHit(PlayerHitEvent e) {
		_vfxSystem.ShowDamage(e.Damage, e.DamagePosition, false, Color.Red);
		_camera.Shake(5f, 0.2f);
		_soundPlayer.Play("player_hurt", 1.0f);
	}

	private void OnPropCollected(PropCollectedEvent e) {
		System.Diagnostics.Debug.WriteLine($"Collected prop: {e.Prop.type}");
		_soundPlayer.Play("buy_item", 0.7f);
		_questManager.UpdateObjectiveProgress("collect_item", e.Prop.type.ToString(), 1);
	}

	private void OnPropPushed(PropPushedEvent e) {
		_soundPlayer.Play("equip_armor", 0.3f);
		System.Diagnostics.Debug.WriteLine($"Pushed prop: {e.Prop.type}");
	}

	private void OnPickupSpawned(PickupSpawnedEvent e) {
		_particleSystem.Emit(ParticleType.Sparkle, e.SpawnPosition, 6);
		_soundPlayer.Play("buy_item", 0.2f);
		System.Diagnostics.Debug.WriteLine($"[LOOT] Spawned {e.Pickup.Type}");
	}

	private void OnPickupCollected(PickupCollectedEvent e) {
		if (e.Collector is Player) {
			((Player)e.Collector).CollectPickup(e.Pickup);
		}
		string sound = e.Pickup.Type switch {
			PickupType.HealthPotion => "use_potion",
			_ => "buy_item"
		};
		_soundPlayer.Play(sound, 0.8f);
		_questManager.UpdateObjectiveProgress("collect_item", e.Pickup.ItemId, 1);
		System.Diagnostics.Debug.WriteLine($"[LOOT] Collected {e.Pickup.Type}");
	}

	private void OnQuestStarted(QuestStartedEvent e) {
		System.Diagnostics.Debug.WriteLine($"[QUEST STARTED] {e.QuestName}");
		_soundPlayer.Play("menu_accept", 0.9f);
		_notificationSystem.ShowQuestStarted(e.QuestName);
	}

	private void OnQuestCompleted(QuestCompletedEvent e) {
		System.Diagnostics.Debug.WriteLine($"[QUEST COMPLETED] {e.QuestName}");
		_soundPlayer.Play("level_up", 0.9f);
		_notificationSystem.ShowQuestCompleted(e.QuestName, e.LastNode.Rewards.Xp, e.LastNode.Rewards.Gold);
	}

	private void OnQuestObjectiveUpdated(QuestObjectiveUpdatedEvent e) {
		System.Diagnostics.Debug.WriteLine($"[QUEST PROGRESS] Objective updated");
		// Notification shown by NotificationSystem
	}

	private void OnPlayerLevelUp(PlayerLevelUpEvent e) {
		_vfxSystem.ShowLevelUp(_player.Position);
		_soundPlayer.Play("level_up", 1.0f);
		System.Diagnostics.Debug.WriteLine($"[LEVEL UP] Player reached level {e.NewLevel}");
	}

	public void Dispose() {
		_subscriptions?.Dispose();
		System.Diagnostics.Debug.WriteLine("[EVENT COORDINATOR] Disposed");
	}
}