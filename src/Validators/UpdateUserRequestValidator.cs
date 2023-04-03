using FluentValidation;

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.Email).NotNull().EmailAddress().MaximumLength(320);
        RuleFor(x => x.FirstName).NotNull().MinimumLength(2).MaximumLength(128);
        RuleFor(x => x.LastName).NotNull().MinimumLength(2).MaximumLength(128);
        RuleFor(x => x.Role).NotNull().IsInEnum().NotEqual(Role.None);
        RuleFor(x => x.Status).NotNull().IsInEnum().NotEqual(UserStatus.None);
    }
}