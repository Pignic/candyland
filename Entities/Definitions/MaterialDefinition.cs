using System.Text.Json.Serialization;

namespace EldmeresTale.Entities.Definitions;

public class MaterialDefinition {

	[JsonPropertyName("id")]
	public string Id { get; set; }

	[JsonPropertyName("name")]
	public string Name { get; set; }

	[JsonPropertyName("description")]
	public string Description { get; set; }

	[JsonPropertyName("price")]
	public int Price { get; set; }

	[JsonPropertyName("weight")]
	public float Weight { get; set; }
}
