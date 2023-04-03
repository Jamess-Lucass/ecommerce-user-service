public record CreateUserRequest
{
    public CreateUserRequest(
        string email,
        string firstName,
        string lastName,
        string avatarUrl,
        Role role)
    {
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        AvatarUrl = avatarUrl;
        Role = role;
    }

    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string AvatarUrl { get; set; }
    public Role Role { get; set; }
};