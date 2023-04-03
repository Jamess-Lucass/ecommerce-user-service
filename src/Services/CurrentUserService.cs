public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid Id => _httpContextAccessor.HttpContext?.User.Claims.Where(x => x.Type == "sub").Select(x => Guid.Parse(x.Value)).FirstOrDefault() ?? Guid.Empty;
    public string? FirstName => _httpContextAccessor.HttpContext?.User.Claims.Where(x => x.Type == "firstName").Select(x => x.Value).FirstOrDefault();
    public string? Email => _httpContextAccessor.HttpContext?.User.Claims.Where(x => x.Type == "email").Select(x => x.Value).FirstOrDefault();
    public Role Role => _httpContextAccessor.HttpContext?.User.Claims.Where(x => x.Type == "role").Select(x => (Role)Enum.Parse(typeof(Role), x.Value)).FirstOrDefault() ?? Role.None;
}