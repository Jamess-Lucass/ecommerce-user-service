using System.Text.Json.Serialization;

public record UpdateUserRequest
{
    public UpdateUserRequest(
        string email,
        string firstName,
        string lastName,
        UserStatus status,
        Role role)
    {
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        Status = status;
        Role = role;
    }

    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public UserStatus Status { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Role Role { get; set; }
};