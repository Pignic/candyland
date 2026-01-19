using DefaultEcs;
using DefaultEcs.System;
using EldmeresTale.Audio;
using EldmeresTale.ECS.Components.Command;

namespace EldmeresTale.ECS.Systems;

public sealed class SoundSystem : AEntitySetSystem<float> {
	private readonly SoundEffectPlayer _soundPlayer;

	private readonly EntitySet _soundEntities;

	public SoundSystem(World world, SoundEffectPlayer soundPlayer)
		: base(world.GetEntities()
			.With<PlaySound>()
			.AsSet()) {
		_soundPlayer = soundPlayer;
		_soundEntities = World.GetEntities().With<PlaySound>().AsSet();
	}

	protected override void Update(float deltaTime, in Entity entity) {
		PlaySound sound = entity.Get<PlaySound>();

		// Play the sound
		// TODO: add the relative location of the sound to the player to pan left or right
		_soundPlayer.Play(sound.SoundName);
	}

	protected override void PostUpdate(float state) {
		foreach (Entity e in _soundEntities.GetEntities()) {
			e.Remove<PlaySound>();
		}
	}
}