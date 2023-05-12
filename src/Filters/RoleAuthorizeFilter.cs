using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RoleAuthorizeFilter : Attribute, IAuthorizationFilter
{
    private readonly ICurrentUserService _currentUser;
    private readonly Role[] _roles;

    public RoleAuthorizeFilter(
        ICurrentUserService currentUser,
        params Role[] roles
        )
    {
        _currentUser = currentUser;
        _roles = roles;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (!_roles.Contains(_currentUser.Role))
        {
            context.Result = new ForbidResult();
            return;
        }
    }
}