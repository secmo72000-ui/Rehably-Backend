using Microsoft.AspNetCore.Mvc.Filters;

namespace Rehably.API.Extensions;

/// <summary>
/// Clears model binding errors for optional nullable DateTime/int fields
/// when the submitted form value is an empty string.
/// Swagger sends "" for unfilled optional fields which the model binder
/// cannot parse into DateTime? — this filter suppresses those errors.
/// </summary>
public class NullableFormFieldFilter : IActionFilter
{
    private static readonly HashSet<string> OptionalNullableFields =
    [
        "SubscriptionStartDate",
        "SubscriptionEndDate",
        "CustomTrialDays"
    ];

    public void OnActionExecuting(ActionExecutingContext context)
    {
        foreach (var key in OptionalNullableFields)
        {
            if (!context.ModelState.ContainsKey(key))
                continue;

            var rawValue = context.HttpContext.Request.Form[key].ToString();
            if (string.IsNullOrWhiteSpace(rawValue))
                context.ModelState.Remove(key);
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
