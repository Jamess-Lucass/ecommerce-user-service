using Microsoft.AspNetCore.Mvc;

public sealed class RoleAuthorizeAttribute : TypeFilterAttribute
{
    public RoleAuthorizeAttribute(params Role[] roles) : base(typeof(RoleAuthorizeFilter))
    {
        Arguments = new object[] { roles };
    }
}