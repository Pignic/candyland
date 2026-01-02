using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace EldmeresTale.Dialog;

/// <summary>
/// Base class for all cutscene commands
/// </summary>
public abstract class CutsceneCommand {
	public string id { get; set; }
	public string nextNodeId { get; set; }
	public bool wait { get; set; } = false; // If true, wait for command to complete before continuing

	/// <summary>
	/// Start executing the command
	/// </summary>
	public abstract void Execute(CutsceneContext context);

	/// <summary>
	/// Update the command (called each frame while executing)
	/// Returns true when complete
	/// </summary>
	public abstract bool Update(GameTime gameTime, CutsceneContext context);

	/// <summary>
	/// Check if this command has completed
	/// </summary>
	public abstract bool IsComplete();
}

/// <summary>
/// Wait for a specified duration
/// </summary>
public class WaitCommand : CutsceneCommand {
	public float duration { get; set; } // Seconds
	private float _elapsed = 0f;

	public override void Execute(CutsceneContext context) {
		_elapsed = 0f;
	}

	public override bool Update(GameTime gameTime, CutsceneContext context) {
		_elapsed += (float)gameTime.ElapsedGameTime.TotalSeconds;
		return IsComplete();
	}

	public override bool IsComplete() {
		return _elapsed >= duration;
	}
}

/// <summary>
/// Fade screen in/out
/// </summary>
public class FadeCommand : CutsceneCommand {
	public string direction { get; set; } // "in" or "out"
	public float duration { get; set; } = 1.0f;
	private float _elapsed = 0f;

	public override void Execute(CutsceneContext context) {
		_elapsed = 0f;
		context.isFading = true;
		context.fadeDirection = direction;
		context.fadeDuration = duration;
	}

	public override bool Update(GameTime gameTime, CutsceneContext context) {
		_elapsed += (float)gameTime.ElapsedGameTime.TotalSeconds;

		float progress = Math.Clamp(_elapsed / duration, 0f, 1f);
		context.fadeAlpha = direction == "out" ? progress : 1f - progress;

		if(IsComplete()) {
			context.isFading = false;
		}

		return IsComplete();
	}

	public override bool IsComplete() {
		return _elapsed >= duration;
	}
}

/// <summary>
/// Move an NPC to a target position
/// </summary>
public class MoveNPCCommand : CutsceneCommand {
	public string npcId { get; set; }
	public Vector2 target { get; set; }
	public float speed { get; set; } = 50f; // Pixels per second

	private bool _started = false;
	private bool _complete = false;

	public override void Execute(CutsceneContext context) {
		_started = true;
		_complete = false;
	}

	public override bool Update(GameTime gameTime, CutsceneContext context) {
		if(!_started) return false;

		var npc = context.GetNPC(npcId);
		if(npc == null) {
			_complete = true;
			return true;
		}

		Vector2 currentPos = npc.Position;
		Vector2 direction = target - currentPos;
		float distance = direction.Length();

		if(distance < 2f) {
			npc.Position = target;
			_complete = true;
			return true;
		}

		direction.Normalize();
		float moveAmount = speed * (float)gameTime.ElapsedGameTime.TotalSeconds;
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
	public Vector2 target { get; set; }
	public float duration { get; set; } = 1.0f;

	private Vector2 _startPos;
	private float _elapsed = 0f;
	private bool _started = false;

	public override void Execute(CutsceneContext context) {
		_startPos = context.GetCameraPosition();
		_elapsed = 0f;
		_started = true;
	}

	public override bool Update(GameTime gameTime, CutsceneContext context) {
		if(!_started) return false;

		_elapsed += (float)gameTime.ElapsedGameTime.TotalSeconds;
		float progress = Math.Clamp(_elapsed / duration, 0f, 1f);

		// Smooth lerp
		float smoothProgress = progress * progress * (3f - 2f * progress); // Smoothstep
		Vector2 newPos = Vector2.Lerp(_startPos, target, smoothProgress);
		context.SetCameraPosition(newPos);

		return IsComplete();
	}

	public override bool IsComplete() {
		return _elapsed >= duration;
	}
}

/// <summary>
/// Play a sound effect
/// </summary>
public class PlaySoundCommand : CutsceneCommand {
	public string soundId { get; set; }
	public float volume { get; set; } = 1.0f;

	public override void Execute(CutsceneContext context) {
		context.PlaySound(soundId, volume);
	}

	public override bool Update(GameTime gameTime, CutsceneContext context) {
		return true; // Instant
	}

	public override bool IsComplete() {
		return true; // Sounds don't wait
	}
}

/// <summary>
/// Change background music
/// </summary>
public class ChangeMusicCommand : CutsceneCommand {
	public string musicId { get; set; }

	public override void Execute(CutsceneContext context) {
		context.ChangeMusic(musicId);
	}

	public override bool Update(GameTime gameTime, CutsceneContext context) {
		return true; // Instant
	}

	public override bool IsComplete() {
		return true;
	}
}

/// <summary>
/// Give an item to the player
/// </summary>
public class GiveItemCommand : CutsceneCommand {
	public string itemId { get; set; }
	public int quantity { get; set; } = 1;

	public override void Execute(CutsceneContext context) {
		context.GiveItem(itemId, quantity);
	}

	public override bool Update(GameTime gameTime, CutsceneContext context) {
		return true; // Instant
	}

	public override bool IsComplete() {
		return true;
	}
}

/// <summary>
/// Start a quest
/// </summary>
public class StartQuestCommand : CutsceneCommand {
	public string questId { get; set; }

	public override void Execute(CutsceneContext context) {
		context.StartQuest(questId);
	}

	public override bool Update(GameTime gameTime, CutsceneContext context) {
		return true; // Instant
	}

	public override bool IsComplete() {
		return true;
	}
}

/// <summary>
/// Set a game flag
/// </summary>
public class SetFlagCommand : CutsceneCommand {
	public string flagId { get; set; }
	public string value { get; set; }

	public override void Execute(CutsceneContext context) {
		context.SetFlag(flagId, value);
	}

	public override bool Update(GameTime gameTime, CutsceneContext context) {
		return true; // Instant
	}

	public override bool IsComplete() {
		return true;
	}
}