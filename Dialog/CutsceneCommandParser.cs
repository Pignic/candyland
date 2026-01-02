using Microsoft.Xna.Framework;
using System;
using System.Text.Json;

namespace EldmeresTale.Dialog;

/// <summary>
/// Parses cutscene commands from JSON
/// </summary>
public static class CutsceneCommandParser {
	public static CutsceneCommand ParseCommand(JsonElement commandElement) {
		if(!commandElement.TryGetProperty("action", out JsonElement actionProp)) {
			throw new Exception("Command must have 'action' property");
		}

		string action = actionProp.GetString();
		CutsceneCommand command = action.ToLower() switch {
			"wait" => ParseWaitCommand(commandElement),
			"fade_in" => ParseFadeCommand(commandElement, "in"),
			"fade_out" => ParseFadeCommand(commandElement, "out"),
			"move_npc" => ParseMoveNPCCommand(commandElement),
			"move_camera" => ParseMoveCameraCommand(commandElement),
			"play_sound" => ParsePlaySoundCommand(commandElement),
			"change_music" => ParseChangeMusicCommand(commandElement),
			"give_item" => ParseGiveItemCommand(commandElement),
			"start_quest" => ParseStartQuestCommand(commandElement),
			"set_flag" => ParseSetFlagCommand(commandElement),
			_ => throw new Exception($"Unknown command action: {action}")
		};

		// Parse common properties
		if(commandElement.TryGetProperty("wait", out JsonElement waitProp)) {
			command.wait = waitProp.GetBoolean();
		}

		if(commandElement.TryGetProperty("next", out JsonElement nextProp)) {
			command.nextNodeId = nextProp.GetString();
		}

		return command;
	}

	private static WaitCommand ParseWaitCommand(JsonElement element) {
		var cmd = new WaitCommand();
		if(element.TryGetProperty("duration", out JsonElement durationProp)) {
			cmd.duration = durationProp.GetSingle();
		}
		cmd.wait = true; // Always wait for wait commands
		return cmd;
	}

	private static FadeCommand ParseFadeCommand(JsonElement element, string direction) {
		var cmd = new FadeCommand { direction = direction };
		if(element.TryGetProperty("duration", out JsonElement durationProp)) {
			cmd.duration = durationProp.GetSingle();
		}
		cmd.wait = true; // Always wait for fades
		return cmd;
	}

	private static MoveNPCCommand ParseMoveNPCCommand(JsonElement element) {
		var cmd = new MoveNPCCommand();

		if(element.TryGetProperty("npc", out JsonElement npcProp)) {
			cmd.npcId = npcProp.GetString();
		}

		if(element.TryGetProperty("target", out JsonElement targetProp)) {
			float x = targetProp.GetProperty("x").GetSingle();
			float y = targetProp.GetProperty("y").GetSingle();
			cmd.target = new Vector2(x, y);
		}

		if(element.TryGetProperty("speed", out JsonElement speedProp)) {
			cmd.speed = speedProp.GetSingle();
		}

		return cmd;
	}

	private static MoveCameraCommand ParseMoveCameraCommand(JsonElement element) {
		var cmd = new MoveCameraCommand();

		if(element.TryGetProperty("target", out JsonElement targetProp)) {
			float x = targetProp.GetProperty("x").GetSingle();
			float y = targetProp.GetProperty("y").GetSingle();
			cmd.target = new Vector2(x, y);
		}

		if(element.TryGetProperty("duration", out JsonElement durationProp)) {
			cmd.duration = durationProp.GetSingle();
		}

		return cmd;
	}

	private static PlaySoundCommand ParsePlaySoundCommand(JsonElement element) {
		var cmd = new PlaySoundCommand();

		if(element.TryGetProperty("sound", out JsonElement soundProp)) {
			cmd.soundId = soundProp.GetString();
		}

		if(element.TryGetProperty("volume", out JsonElement volumeProp)) {
			cmd.volume = volumeProp.GetSingle();
		}

		return cmd;
	}

	private static ChangeMusicCommand ParseChangeMusicCommand(JsonElement element) {
		var cmd = new ChangeMusicCommand();

		if(element.TryGetProperty("music", out JsonElement musicProp)) {
			cmd.musicId = musicProp.GetString();
		}

		return cmd;
	}

	private static GiveItemCommand ParseGiveItemCommand(JsonElement element) {
		var cmd = new GiveItemCommand();

		if(element.TryGetProperty("item", out JsonElement itemProp)) {
			cmd.itemId = itemProp.GetString();
		}

		if(element.TryGetProperty("quantity", out JsonElement quantityProp)) {
			cmd.quantity = quantityProp.GetInt32();
		}

		return cmd;
	}

	private static StartQuestCommand ParseStartQuestCommand(JsonElement element) {
		var cmd = new StartQuestCommand();

		if(element.TryGetProperty("quest", out JsonElement questProp)) {
			cmd.questId = questProp.GetString();
		}

		return cmd;
	}

	private static SetFlagCommand ParseSetFlagCommand(JsonElement element) {
		var cmd = new SetFlagCommand();

		if(element.TryGetProperty("flag", out JsonElement flagProp)) {
			cmd.flagId = flagProp.GetString();
		}

		if(element.TryGetProperty("value", out JsonElement valueProp)) {
			cmd.value = valueProp.GetString();
		}

		return cmd;
	}
}