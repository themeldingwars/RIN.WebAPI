using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using RIN.WebAPI.Models;
using Serilog;

namespace RIN.WebAPI.Utils
{
    // Stands in for dl.firefall.com, which the client streamed assets from that the installer didn't ship:
    //   vtex/static.vtex<N>               the virtual texture levels, firefall.ini [FilePaths] VTRemotePath
    //   AssetStream/<block>/<id>.bnk      the dialogue media banks, firefall.ini [FilePaths] AssetStreamPath
    // The client only asks for byte ranges and rejects anything but 206 for the banks
    public static class StreamingAssets
    {
        public const string RequestPath = "/dl";

        public static void MapStreamingAssets(this WebApplication app)
        {
            var root = app.Services.GetRequiredService<IOptions<WebApiConfigSettings>>().Value.StreamingAssetsPath;
            if (string.IsNullOrWhiteSpace(root))
            {
                return;
            }

            root = Path.GetFullPath(root, app.Environment.ContentRootPath);
            Directory.CreateDirectory(root);
            Log.Information("Serving streaming assets from {Path} at {RequestPath}", root, RequestPath);

            // Its own branch, so a missing file is a 404 and not a request for the catch-all route
            app.Map(RequestPath, dl =>
            {
                dl.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider          = new PhysicalFileProvider(root),
                    ServeUnknownFileTypes = true,
                    DefaultContentType    = "application/octet-stream"
                });

                dl.Run(context =>
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return Task.CompletedTask;
                });
            });
        }
    }
}
