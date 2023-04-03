using Asp.Versioning;
using Asp.Versioning.OData;
using Microsoft.OData.ModelBuilder;

public class UserDtoConfiguration : IModelConfiguration
{
    public void Apply(ODataModelBuilder builder, ApiVersion apiVersion, string? routePrefix)
    {
        var order = builder.EntitySet<UserDto>("Users");
    }
}