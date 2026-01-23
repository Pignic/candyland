using DefaultEcs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EldmeresTale.ECS;

public static class EntityInspector {
	private static readonly JsonSerializerOptions JsonOptions = new() {
		WriteIndented = true,
		IncludeFields = true,
		DefaultIgnoreCondition = JsonIgnoreCondition.Never,
		Converters = {
			new Vector2Converter(),
			new RectangleConverter(),
			new ColorConverter(),
			new Texture2DConverter()
		}
	};

	public static void DumpEntity(Entity entity, string label, List<Type> excludes) {
		if (!entity.IsAlive) {
			System.Diagnostics.Debug.WriteLine($"[ENTITY DUMP] {label ?? "Entity"} is DEAD");
			return;
		}

		Dictionary<string, object> components = [];

		// Use reflection to get all component types
		Type entityType = entity.GetType();
		MethodInfo getMethod = entityType.GetMethod("Get", Type.EmptyTypes);
		MethodInfo hasMethod = entityType.GetMethod("Has", Type.EmptyTypes);

		// Get all component types from the world
		foreach (Type componentType in GetAllComponentTypes(entity)) {
			if (excludes.Contains(componentType)) {
				continue;
			}
			try {
				// Check if entity has this component
				MethodInfo genericHas = hasMethod.MakeGenericMethod(componentType);
				bool hasComponent = (bool)genericHas.Invoke(entity, null);

				if (hasComponent) {
					// Get the component value
					MethodInfo genericGet = getMethod.MakeGenericMethod(componentType);
					object component = genericGet.Invoke(entity, null);

					components[componentType.Name] = ComponentToDict(component);
				}
			} catch {
				// Skip components we can't access
			}
		}

		string json = JsonSerializer.Serialize(new {
			EntityId = entity.ToString(),
			Label = label,
			ComponentCount = components.Count,
			Components = components
		}, JsonOptions);

		System.Diagnostics.Debug.WriteLine($"\n[ENTITY DUMP] {label ?? "Entity"}");
		System.Diagnostics.Debug.WriteLine(json);
	}

	private static object ComponentToDict(object component) {
		if (component == null) {
			return null;
		}

		Type type = component.GetType();

		// Handle primitives
		if (type.IsPrimitive || type == typeof(string)) {
			return component;
		}

		// Handle structs/classes - convert to dictionary
		Dictionary<string, object> dict = [];

		// Get all fields
		foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance)) {
			try {
				object value = field.GetValue(component);
				dict[field.Name] = ConvertValue(value);
			} catch {
				dict[field.Name] = "<error>";
			}
		}

		// Get all properties
		foreach (PropertyInfo prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
			if (prop.CanRead) {
				try {
					object value = prop.GetValue(component);
					dict[prop.Name] = ConvertValue(value);
				} catch {
					dict[prop.Name] = "<error>";
				}
			}
		}

		return dict.Count > 0 ? dict : component.ToString();
	}

	private static object ConvertValue(object value) {
		if (value == null) {
			return null;
		}

		Type type = value.GetType();

		if (type.IsPrimitive || type == typeof(string)) {
			return value;
		}

		if (type.IsEnum) {
			return value.ToString();
		}

		// Handle common types
		if (type.Name.Contains("Vector2")) {
			return value.ToString();
		}
		if (type.Name.Contains("Rectangle")) {
			return value.ToString();
		}
		if (type.Name.Contains("Color")) {
			return value.ToString();
		}
		if (type.Name.Contains("Texture2D")) {
			return ((Microsoft.Xna.Framework.Graphics.Texture2D)value)?.Name ?? "null";
		}
		if (type.Name.Contains("Entity")) {
			return value.ToString();
		}

		return ComponentToDict(value);
	}

	private static IEnumerable<Type> GetAllComponentTypes(Entity entity) {
		// Get all types that could be components from loaded assemblies
		Assembly gameAssembly = Assembly.GetExecutingAssembly();

		return gameAssembly.GetTypes()
			.Where(t => t.Namespace?.Contains("ECS.Components") == true)
			.Where(t => t.IsValueType || t.IsClass);
	}

	// JSON Converters for MonoGame types
	private class Vector2Converter : JsonConverter<Microsoft.Xna.Framework.Vector2> {
		public override Microsoft.Xna.Framework.Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
			=> throw new NotImplementedException();
		public override void Write(Utf8JsonWriter writer, Microsoft.Xna.Framework.Vector2 value, JsonSerializerOptions options) {
			writer.WriteStringValue($"({value.X:F2}, {value.Y:F2})");
		}
	}

	private class RectangleConverter : JsonConverter<Microsoft.Xna.Framework.Rectangle> {
		public override Microsoft.Xna.Framework.Rectangle Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
			=> throw new NotImplementedException();
		public override void Write(Utf8JsonWriter writer, Microsoft.Xna.Framework.Rectangle value, JsonSerializerOptions options) {
			writer.WriteStringValue($"X:{value.X} Y:{value.Y} W:{value.Width} H:{value.Height}");
		}
	}

	private class ColorConverter : JsonConverter<Microsoft.Xna.Framework.Color> {
		public override Microsoft.Xna.Framework.Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
			=> throw new NotImplementedException();
		public override void Write(Utf8JsonWriter writer, Microsoft.Xna.Framework.Color value, JsonSerializerOptions options) {
			writer.WriteStringValue($"R:{value.R} G:{value.G} B:{value.B} A:{value.A}");
		}
	}

	private class Texture2DConverter : JsonConverter<Microsoft.Xna.Framework.Graphics.Texture2D> {
		public override Microsoft.Xna.Framework.Graphics.Texture2D Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
			=> throw new NotImplementedException();
		public override void Write(Utf8JsonWriter writer, Microsoft.Xna.Framework.Graphics.Texture2D value, JsonSerializerOptions options) {
			writer.WriteStringValue(value?.Name ?? "null");
		}
	}
}