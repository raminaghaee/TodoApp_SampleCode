using System.Text.Json.Serialization;

namespace TodoApp.Business.Admin;

public sealed class SetRoleRequest
{
	[JsonPropertyName("role")]
	public string Role { get; set; } = "user";
}
