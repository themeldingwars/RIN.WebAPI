using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using Newtonsoft.Json;
using RIN.Core;
using Serilog;

namespace RIN.WebAPI.Utils
{
    public static class ExceptionHandler
    {
        public static void UseTmwExceptionHandler(this IApplicationBuilder app)
        {
            app.UseExceptionHandler(appError =>
            {
                appError.Run(async context =>
                {
                    context.Response.StatusCode  = (int)HttpStatusCode.InternalServerError;
                    context.Response.ContentType = "application/json";
                    var contextFeature = context.Features.Get<IExceptionHandlerFeature>();
                    if(contextFeature != null) {
                        var   logger = Log.ForContext<Exception>();
                        Error err    = null;
                        
                        // Swap on the exception type
                        if (contextFeature.Error is TmwException tmwEx) {
                            err = tmwEx.Error;
                        }
                        else {
                            err = new Error
                            {
                                code    = Error.Codes.ERR_UNKNOWN,
                                message = $"Unknown Error: {contextFeature.Error.GetType()}"
                            };
                            
                            logger.Error(contextFeature.Error, $"Exception in {contextFeature.Error.Source}");
                        }

                        var errorStr = JsonConvert.SerializeObject(err);
                        await context.Response.WriteAsync(errorStr);
                    }
                });
            });
        }
    }
}