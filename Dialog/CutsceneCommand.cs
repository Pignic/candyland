using EldmeresTale.Entities;
using Microsoft.Xna.Framework;
using System;
using System.Text.Json.Serialization;

namespace EldmeresTale.Dialog;

public abstract class CutsceneCommand {

	[JsonPropertyName("id")]
	public string Id { get; set; }

	[JsonPropertyName("nextNodeId")]
	public string NextNodeId { get; set; }

	[JsonPropertyName("wait")]
	public bool Wait { get; set; } = false; // If true, wait for command to complete before continuing

	public abstract void Execute(CutsceneContext context);

	public abstract bool Update(GameTime gameTime, CutsceneContext context);

	public abstract bool IsComplete();
}

public class WaitCommand : CutsceneCommand {

	[JsonPropertyName("duration")]
	public float Duration { get; set; }

	private float _elapsed = 0f;

	public override void Execute(CutsceneContext context) {
		_elapsed = 0f;
	}

	public override bool Update(GameTime gameTime, CutsceneContext context) {
		_elapsed += (float)gameTime.ElapsedGameTime.TotalSeconds;
		return IsComplete();
	}

	public override bool IsComplete() {
		return _elapsed >= Duration;
	}
}

public class FadeCommand : CutsceneCommand {

	[JsonPropertyName("direction")]
	public string Direction { get; set; } // "in" or "out"

	[JsonPropertyName("duration")]
	public float Duration { get; set; } = 1.0f;

	private float _elapsed = 0f;

	public override void Execute(CutsceneContext context) {
		_elapsed = 0f;
		context.IsFading = true;
		context.FadeDirection = Direction;
		context.FadeDuration = Duration;
	}

	public override bool Update(GameTime gameTime, CutsceneContext context) {
		_elapsed += (float)gameTime.ElapsedGameTime.TotalSeconds;

		float progress = Math.Clamp(_elapsed / Duration, 0f, 1f);
		context.FadeAlpha = Direction == "out" ? progress : 1f - progress;

		if (IsComplete()) {
			context.IsFading = false;
		}

		return IsComplete();
	}

	public override bool IsComplete() {
		return _elapsed >= Duration;
	}
}

public class MoveNPCCommand : CutsceneCommand {

	[JsonPropertyName("npcId")]
	public string NPCId { get; set; }

	[JsonPropertyName("target")]
	public Vector2 Target { get; set; }

	[JsonPropertyName("speed")]
	public float Speed { get; set; } = 50f; // Pixels per second

	private bool _started = false;
	private bool _complete = false;

	public override void Execute(CutsceneContext context) {
		_started = true;
		_complete = false;
	}

	public override bool Update(GameTime gameTime, CutsceneContext context) {
		if (!_started) {
			return false;
		}

		NPC npc = context.GetNPC(NPCId);
		if (npc == null) {
			_complete = true;
			return true;
		}

		Vector2 currentPos = npc.Position;
		Vector2 direction = Target - currentPos;
		float distance = direction.Length();

		if (distance < 2f) {
			npc.Position = Target;
			_complete = true;
			return true;
		}

		direction.Normalize();
		float moveAmount = Speed * (float)gameTime.ElapsedGameTime.TotalSeconds;
		npc.Position += direction * Math.Min(moveAmount, distance);

		return false;
	}

	public override bool IsComplete() {
		return _complete;
	}
}

/// <summary>
/// Move camera to a target position
/// </summary>
public class MoveCameraCommand : CutsceneCommand {

	[JsonPropertyName("target")]
	public Vector2 Target { get; set; }

	[JsonPropertyName("duration")]
	public float Duration { get; set; } = 1.0f;

	private Vector2 _startPos;
	private float _elapsed = 0f;
	private bool _started = false;

	public override void Execute(CutsceneContext context) {
		_startPos = context.GetCameraPosition();
		_elapsed = 0f;
		_started = true;
	}

	public override bool Update(GameTime gameTime, CutsceneContext context) {
		if (!_started) {
			return false;
		}

		_elapsed += (float)gameTime.ElapsedGameTime.TotalSeconds;
		float progress = Math.Clamp(_elapsed / Duration, 0f, 1f);

		// Smooth lerp
		float smoothProgress = progress * progress * (3f - (2f * progress)); // Smoothstep
		Vector2 newPos = Vector2.Lerp(_startPos, Target, smoothProgress);
		context.SetCameraPosition(newPos);

		return IsComplete();
	}

	public override bool IsComplete() {
		return _elapsed >= Duration;
	}
}

/// <summary>
/// Play a sound effect
/// </summary>
public class PlaySoundCommand : CutsceneCommand {

	[JsonPropertyName("soundId")]
	public string SoundId { get; set; }

	[JsonPropertyName("volume")]
	public float Volume { get; set; } = 1.0f;

	public override void Execute(CutsceneContext context) {
		context.PlaySound(SoundId, Volume);
	}

	public override bool Update(GameTime gameTime, CutsceneContext context) {
		return true; // Instant
	}

	public override bool IsComplete() {
		return true; // Sounds don't wait
	}
}

public class ChangeMusicCommand : CutsceneCommand {

	[JsonPropertyName("musicId")]
	public string MusicId { get; set; }

	public override void Execute(CutsceneContext context) {
		context.ChangeMusic(MusicId);
	}

	public override bool Update(GameTime gameTime, CutsceneContext context) {
		return true; // Instant
	}

	public override bool IsComplete() {
		return true;
	}
}

public class GiveItemCommand : CutsceneCommand {

	[JsonPropertyName("itemId")]
	public string ItemId { get; set; }

	[JsonPropertyName("quantity")]
	public int Quantity { get; set; } = 1;

	public override void Execute(CutsceneContext context) {
		context.GiveItem(ItemId, Quantity);
	}

	public override bool Update(GameTime gameTime, CutsceneContext context) {
		return true; // Instant
	}

	public override bool IsComplete() {
		return true;
	}
}

public class StartQuestCommand : CutsceneCommand {

	[JsonPropertyName("questId")]
	public string QuestId { get; set; }

	public override void Execute(CutsceneContext context) {
		context.StartQuest(QuestId);
	}

	public override bool Update(GameTime gameTime, CutsceneContext context) {
		return true; // Instant
	}

	public override bool IsComplete() {
		return true;
	}
}

public class SetFlagCommand : CutsceneCommand {

	[JsonPropertyName("flagId")]
	public string FlagId { get; set; }

	[JsonPropertyName("value")]
	public string Value { get; set; }

	public override void Execute(CutsceneContext context) {
		context.SetFlag(FlagId, Value);
	}

	public override bool Update(GameTime gameTime, CutsceneContext context) {
		return true; // Instant
	}

	public override bool IsComplete() {
		return true;
	}
}