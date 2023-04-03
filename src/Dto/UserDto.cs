using System.Text.Json.Serialization;

public record UserDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Role Role { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public UserStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid UpdatedBy { get; set; }
}