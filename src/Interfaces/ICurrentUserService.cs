public interface ICurrentUserService
{
    Guid Id { get; }
    string? FirstName { get; }
    string? Email { get; }
    Role Role { get; }
}