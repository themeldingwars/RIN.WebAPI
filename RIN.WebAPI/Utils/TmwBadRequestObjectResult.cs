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

        // todo try to set code from req classes, use there built-in codes e.g. ERR_INVALID_TAG, ERR_INVALID_REGION
        Value = new Error
        {
            code    = "TMW_BAD_REQUEST",
            message = allErrors
        };
    }
}
