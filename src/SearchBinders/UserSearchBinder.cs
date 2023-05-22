using System.Linq.Expressions;

public class UserSearchBinder : ISearchBinder<UserDto>
{
    public Expression<Func<UserDto, bool>> Bind(string searchTerm)
    {
        var text = searchTerm.ToLower();

        Expression<Func<UserDto, bool>> exp = p =>
            p.Email.ToLower().Contains(text) ||
            p.FirstName.ToLower().Contains(text) ||
            p.LastName.ToLower().Contains(text);

        return exp;
    }
}
