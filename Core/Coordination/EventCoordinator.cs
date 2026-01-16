using EldmeresTale.Audio;
using EldmeresTale.ECS.Components;
using EldmeresTale.ECS.Systems;
using EldmeresTale.Entities;
using EldmeresTale.Events;
using EldmeresTale.Quests;
using EldmeresTale.Systems;
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

	// ECS
	private readonly MovementSystem _movementSystem;

	private readonly ECS.Factories.ParticleEmitter _particleEmitter;

	public EventCoordinator(
		GameEventBus eventBus,
		ParticleSystem particleSystem,
		ECS.Factories.ParticleEmitter particleEmitter,
		VFXSystem vfxSystem,
		LootSystem lootSystem,
		QuestManager questManager,
		Player player,
		Camera camera,
		SoundEffectPlayer soundPlayer,
		CombatSystem combatSystem,
		NotificationSystem notificationSystem,
		MovementSystem movementSystem
	) {
		_eventBus = eventBus;
		_particleSystem = particleSystem;
		_particleEmitter = particleEmitter;
		_vfxSystem = vfxSystem;
		_lootSystem = lootSystem;
		_questManager = questManager;
		_player = player;
		_camera = camera;
		_soundPlayer = soundPlayer;
		_combatSystem = combatSystem;
		_notificationSystem = notificationSystem;
		_movementSystem = movementSystem;
	}

	public void Initialize() {
		// Combat events
		//_subscriptions.Add(_eventBus.Subscribe<EnemyHitEvent>(OnEnemyHit));
		//_subscriptions.Add(_eventBus.Subscribe<EnemyKilledEvent>(OnEnemyKilled));
		//_subscriptions.Add(_eventBus.Subscribe<PropHitEvent>(OnPropHit));
		//_subscriptions.Add(_eventBus.Subscribe<PropDestroyedEvent>(OnPropDestroyed));
		//_subscriptions.Add(_eventBus.Subscribe<PlayerHitEvent>(OnPlayerHit));

		// Physics events
		_subscriptions.Add(_eventBus.Subscribe<PropCollectedEvent>(OnPropCollected));
		_subscriptions.Add(_eventBus.Subscribe<PropPushedEvent>(OnPropPushed));

		// Loot events
		//_subscriptions.Add(_eventBus.Subscribe<PickupSpawnedEvent>(OnPickupSpawned));
		_subscriptions.Add(_eventBus.Subscribe<Events.PickupCollectedEvent>(OnPickupCollected));

		// Quest events
		_subscriptions.Add(_eventBus.Subscribe<QuestStartedEvent>(OnQuestStarted));
		_subscriptions.Add(_eventBus.Subscribe<QuestCompletedEvent>(OnQuestCompleted));
		_subscriptions.Add(_eventBus.Subscribe<QuestObjectiveUpdatedEvent>(OnQuestObjectiveUpdated));

		// Player events
		_subscriptions.Add(_eventBus.Subscribe<PlayerLevelUpEvent>(OnPlayerLevelUp));

		// Room change event
		_subscriptions.Add(_eventBus.Subscribe<RoomChangedEvent>(OnRoomChange));

		System.Diagnostics.Debug.WriteLine($"[EVENT COORDINATOR] Initialized with {_subscriptions.Count} event subscriptions");
	}

	//private void OnEnemyHit(EnemyHitEvent e) {
	//	// TODO: no access to component
	//	Vector2 hitDirection = e.Enemy.Get<Position>().Value - _player.Position;
	//	//_particleSystem.Emit(ParticleType.Blood, e.DamagePosition, 8, hitDirection);
	//	_particleEmitter.SpawnBloodSplatter(e.DamagePosition, hitDirection, 8);
	//	Color damageColor = e.WasCritical ? Color.Orange : Color.White;
	//	_vfxSystem.ShowDamage(e.Damage, e.DamagePosition, e.WasCritical, damageColor);

	//	if (e.WasCritical) {
	//		_soundPlayer.Play("crit_attack", 0.5f);
	//		_camera.Shake(2f, 0.15f);
	//		_combatSystem.Pause(0.08f);
	//	}
	//	_soundPlayer.Play("monster_hurt_mid", 0.5f);
	//}

	//private void OnEnemyKilled(EnemyKilledEvent e) {
	//	//_particleSystem.Emit(ParticleType.Blood, e.DeathPosition, 20);
	//	_particleEmitter.SpawnBloodSplatter(e.DeathPosition, Vector2.Zero, 20);
	//	//_lootSystem.SpawnLootFromEnemy(e.Enemy);
	//	//e.Enemy.HasDroppedLoot = true;
	//	_camera.Shake(2f, 0.15f);
	//	_combatSystem.Pause(0.06f);
	//	// TODO: propagate events without accessing components
	//	_questManager.UpdateObjectiveProgress("kill_enemy", e.Enemy.Get<EnemyType>().TypeName, 1);

	//	//_player.GainXP(e.Enemy.XPValue);
	//	_soundPlayer.Play("monster_growl_mid", 0.8f);
	//}

	//private void OnPropHit(PropHitEvent e) {
	//	_vfxSystem.ShowDamage(e.Damage, e.DamagePosition, e.WasCritical, Color.Gray);
	//	_soundPlayer.Play("material_hit", 0.5f);
	//}

	//private void OnPropDestroyed(PropDestroyedEvent e) {
	//	//_particleSystem.Emit(ParticleType.Destruction, e.DestructionPosition, 15);
	//	_particleEmitter.SpawnDustCloud(e.DestructionPosition, Vector2.Zero, 15);
	//	_soundPlayer.Play("equip_armor", 0.6f);

	//	//if (e.Prop.Type == PropType.Breakable) {
	//	//Random random = new Random();
	//	//if (random.NextDouble() < 0.7) {
	//	//	_lootSystem.SpawnPickup(ECS.Components.PickupType.Coin, e.DestructionPosition);
	//	//}
	//	//if (random.NextDouble() < 0.3) {
	//	//	_lootSystem.SpawnPickup(ECS.Components.PickupType.Health, e.DestructionPosition);
	//	//}
	//	//}

	//	//_questManager.UpdateObjectiveProgress("destroy_prop", e.Prop.Type.ToString(), 1);
	//}

	//private void OnPlayerHit(PlayerHitEvent e) {
	//	_vfxSystem.ShowDamage(e.Damage, e.DamagePosition, false, Color.Red);
	//	_camera.Shake(5f, 0.2f);
	//	_soundPlayer.Play("player_hurt", 1.0f);
	//}

	private void OnPropCollected(PropCollectedEvent e) {
		//System.Diagnostics.Debug.WriteLine($"Collected prop: {e.Prop.Type}");
		_soundPlayer.Play("buy_item", 0.7f);
		//_questManager.UpdateObjectiveProgress("collect_item", e.Prop.Type.ToString(), 1);
	}

	private void OnPropPushed(PropPushedEvent e) {
		_soundPlayer.Play("equip_armor", 0.3f);
		//System.Diagnostics.Debug.WriteLine($"Pushed prop: {e.Prop.Type}");
	}

	//private void OnPickupSpawned(PickupSpawnedEvent e) {
	//	//_particleSystem.Emit(ParticleType.Sparkle, e.SpawnPosition, 6);
	//	_particleEmitter.SpawnImpactSparks(e.SpawnPosition, Vector2.Zero, 6);
	//	_soundPlayer.Play("buy_item", 0.2f);
	//	System.Diagnostics.Debug.WriteLine($"[LOOT] Spawned {e.Pickup.Type}");
	//}

	private void OnPickupCollected(Events.PickupCollectedEvent e) {
		if (e.Collector is Player player) {
			player.CollectPickup(e.Pickup);
		}
		string sound = e.Pickup.Type switch {
			PickupType.Health => "use_potion",
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
		System.Diagnostics.Debug.WriteLine("[QUEST PROGRESS] Objective updated");
		// Notification shown by NotificationSystem
	}

	private void OnPlayerLevelUp(PlayerLevelUpEvent e) {
		_vfxSystem.ShowLevelUp(_player.Position);
		_soundPlayer.Play("level_up", 1.0f);
		System.Diagnostics.Debug.WriteLine($"[LEVEL UP] Player reached level {e.NewLevel}");
	}

	public void OnRoomChange(RoomChangedEvent e) {
		_movementSystem.SetCurrentMap(e.NewRoom.Map);
	}

	public void Dispose() {
		_subscriptions?.Dispose();
		System.Diagnostics.Debug.WriteLine("[EVENT COORDINATOR] Disposed");
	}
}