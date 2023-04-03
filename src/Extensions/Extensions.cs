using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc.ModelBinding;

public static class Extensions
{
    public static void AddToModelState(this ValidationResult result, ModelStateDictionary modelState)
    {
        foreach (var error in result.Errors)
        {
            modelState.AddModelError(error.PropertyName.ToCamelCase(), error.ErrorMessage);
        }
    }

    public static string ToCamelCase(this string str) => char.ToLowerInvariant(str[0]) + str[1..];
}