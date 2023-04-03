using FluentValidation;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Email).NotNull().EmailAddress().MaximumLength(320);
        RuleFor(x => x.FirstName).NotNull().MinimumLength(2).MaximumLength(128);
        RuleFor(x => x.LastName).MaximumLength(128);
        RuleFor(x => x.AvatarUrl).MaximumLength(1024);
        RuleFor(x => x.Role).IsInEnum();
    }
}