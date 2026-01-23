using DefaultEcs;
using EldmeresTale.Core;
using EldmeresTale.ECS.Components;
using EldmeresTale.ECS.Components.Tag;
using EldmeresTale.Entities.Definitions;
using EldmeresTale.Worlds;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace EldmeresTale.ECS.Factories;

public class NPCsFactory {

	private static readonly Dictionary<string, NPCDefinition> _catalog = [];
	private static bool _initialized = false;
	public static Dictionary<string, NPCDefinition> Catalog {
		get {
			if (!_initialized) {
				Initialize();
			}
			return _catalog;
		}
	}
	private readonly World _world;
	private readonly AssetManager _assetManager;

	public NPCsFactory(World world, AssetManager assetManager) {
		_world = world;
		_assetManager = assetManager;
	}

	public bool TryGetNPCDefinition(string npcId, out NPCDefinition value) => _catalog.TryGetValue(npcId, out value);

	public static void Initialize(string path = "Assets/Data/npcs.json") {
		_catalog.Clear();

		if (!File.Exists(path)) {
			System.Diagnostics.Debug.WriteLine($"NPC definitions file not found: {path}");
			return;
		}

		try {
			string json = File.ReadAllText(path);
			JsonDocument doc = JsonDocument.Parse(json);
			JsonElement root = doc.RootElement;

			if (root.TryGetProperty("npcs", out JsonElement npcsElement)) {
				foreach (JsonProperty npcProperty in npcsElement.EnumerateObject()) {
					string npcId = npcProperty.Name;
					_catalog[npcId] = ParseNPCDefinition(npcProperty.Value, npcId);
				}
			}

			System.Diagnostics.Debug.WriteLine($"Loaded {_catalog.Count} NPC definitions from {path}");
		} catch (System.Exception ex) {
			System.Diagnostics.Debug.WriteLine($"Error loading NPC definitions: {ex.Message}");
		}

		_initialized = true;
	}

	private static NPCDefinition ParseNPCDefinition(JsonElement npcElement, string npcId) {
		NPCDefinition npc = new NPCDefinition { Id = npcId };

		if (npcElement.TryGetProperty("name", out JsonElement nameProp)) {
			npc.NameKey = nameProp.GetString();
		}
		if (npcElement.TryGetProperty("health", out JsonElement healthProp)) {
			npc.Health = healthProp.GetInt32();
		}
		if (npcElement.TryGetProperty("width", out JsonElement widthProp)) {
			npc.Width = widthProp.GetInt32();
		}
		if (npcElement.TryGetProperty("height", out JsonElement heightProp)) {
			npc.Height = heightProp.GetInt32();
		}
		if (npcElement.TryGetProperty("frameCount", out JsonElement frameCountProp)) {
			npc.FrameCount = frameCountProp.GetInt32();
		}
		if (npcElement.TryGetProperty("frameTime", out JsonElement frameTimeProp)) {
			npc.FrameTime = frameTimeProp.GetInt32();
		}
		if (npcElement.TryGetProperty("behavior", out JsonElement behaviorProp)) {
			npc.BehaviorString = behaviorProp.GetString();
		}

		if (npcElement.TryGetProperty("defaultPortrait", out JsonElement portraitProp)) {
			npc.DefaultPortrait = portraitProp.GetString();
		}

		if (npcElement.TryGetProperty("requiresItem", out JsonElement itemProp)) {
			npc.RequiresItem = itemProp.GetString();
		}

		if (npcElement.TryGetProperty("refuseDialog", out JsonElement refuseProp)) {
			npc.RefuseDialogKey = refuseProp.GetString();
		}

		if (npcElement.TryGetProperty("dialogs", out JsonElement dialogsElement)) {
			foreach (JsonElement dialogElement in dialogsElement.EnumerateArray()) {
				NPCDialogEntry dialogEntry = new NPCDialogEntry();

				if (dialogElement.TryGetProperty("treeId", out JsonElement treeProp)) {
					dialogEntry.TreeId = treeProp.GetString();
				}

				if (dialogElement.TryGetProperty("priority", out JsonElement priorityProp)) {
					dialogEntry.Priority = priorityProp.GetInt32();
				}

				if (dialogElement.TryGetProperty("conditions", out JsonElement conditionsElement)) {
					foreach (JsonElement condition in conditionsElement.EnumerateArray()) {
						dialogEntry.Conditions.Add(condition.GetString());
					}
				}

				npc.Dialogs.Add(dialogEntry);
			}
		}

		return npc;
	}

	public Entity Create(string roomId, NPCData spawnData) {
		Entity e = _world.CreateEntity();
		if (!_initialized) {
			Initialize();
		}

		if (!_catalog.TryGetValue(spawnData.Id, out NPCDefinition def)) {
			System.Diagnostics.Debug.WriteLine($"[NPC FACTORY] NPC '{spawnData.Id}' not found!");
			return e;
		}

		Texture2D texture = _assetManager.LoadTexture($"Assets/Sprites/Actors/{def.Id}.png");
		e.Set(new RoomId(roomId));
		e.Set(new Health(def.Health));
		e.Set(new Sprite(texture));
		e.Set(new Position(spawnData.X, spawnData.Y));
		e.Set(new Collider(def.Width, def.Height));
		e.Set(new Components.Animation(
			def.FrameCount, def.Width, def.Height, def.FrameTime, true, false
		));
		e.Set(new Velocity());
		e.Set(new AIBehavior(def.Behavior));
		e.Set(new InteractionZone(def.Id));
		// TODO: set all that
		//e.Set(new CombatStats {
		//	AttackCooldown = def.AttackCooldown,
		//	AttackDamage = def.AttackDamage,
		//	MovementSpeed = def.Speed,
		//	AttackRange = 12,
		//	CritChance = 0,
		//	CritMultiplier = 1,
		//	Defense = def.Defense,
		//	DodgeChance = 0,
		//	HealthRegen = 0,
		//	AttackAngle = (float)(Math.PI / 4d)
		//});
		e.Set(new Faction(FactionName.NPC));

		return e;
	}

	private class NPCCatalogData {
		public List<NPCDefinition> NPCs { get; set; }
	}
}
