using System.ComponentModel.DataAnnotations.Schema;

public record User : Base
{
    [Column(TypeName = "varchar(128)")]
    public string Firstname { get; set; } = string.Empty;

    [Column(TypeName = "varchar(128)")]
    public string Lastname { get; set; } = string.Empty;

    [Column(TypeName = "varchar(320)")]
    public string Email { get; set; } = string.Empty;

    [Column(TypeName = "varchar(1024)")]
    public string AvatarUrl { get; set; } = string.Empty;

    public UserStatus Status { get; set; } = UserStatus.None;

    public Role Role { get; set; } = Role.None;
}