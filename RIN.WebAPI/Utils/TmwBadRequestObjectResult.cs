using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using RIN.Core;

namespace RIN.WebAPI.Utils;

public class TmwBadRequestObjectResult : BadRequestObjectResult
{
    public TmwBadRequestObjectResult(ModelStateDictionary modelState) : base(modelState)
    {
        var allErrors = modelState
            .SelectMany(kvp => kvp.Value.Errors.Select(e => e.ErrorMessage))
            .Aggregate((current, next) => $"{current}\n{next}");

        Value = new Error
        {
            code    = Error.Codes.TMW_MSG,
            message = allErrors
        };
    }
}
