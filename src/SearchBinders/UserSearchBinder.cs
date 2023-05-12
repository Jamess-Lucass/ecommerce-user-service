using System.Linq.Expressions;
using Microsoft.AspNetCore.OData.Query.Expressions;
using Microsoft.OData.UriParser;

public class UserSearchBinder : QueryBinder, ISearchBinder
{
    public Expression BindSearch(SearchClause searchClause, QueryBinderContext context)
    {
        SearchTermNode? node = searchClause.Expression as SearchTermNode;

        if (node is null)
        {
            Expression<Func<UserDto, bool>> expression = p => true;

            return expression;
        }

        var searchTerm = node.Text.ToLower();

        Expression<Func<UserDto, bool>> exp = p =>
            p.Email.ToLower().Contains(searchTerm) ||
            p.FirstName.ToLower().Contains(searchTerm) ||
            p.LastName.ToLower().Contains(searchTerm);

        return exp;
    }
}
