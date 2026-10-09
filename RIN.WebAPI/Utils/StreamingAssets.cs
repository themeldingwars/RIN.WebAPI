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
    // ingame/ holds the in-game web host's static files: the panel manager page and assets/, served at /ingame/assets
    public static class StreamingAssets
    {
        public const string RequestPath = "/dl";

        private const string InGameFolder = "ingame";

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
                var files = new PhysicalFileProvider(root);
                dl.Use(async (context, next) =>
                {
                    if (!IsCovered(context, files))
                    {
                        context.Response.StatusCode = StatusCodes.Status404NotFound;
                        return;
                    }

                    await next();
                });

                ServeFiles(dl, files);
            });

            // The pages use root-relative urls, the copies in ingame/ are rewritten to /ingame
            var inGameAssets = Path.Combine(root, InGameFolder, "assets");
            Directory.CreateDirectory(inGameAssets);
            app.Map("/ingame/assets", assets => ServeFiles(assets, new PhysicalFileProvider(inGameAssets)));
        }

        public static string? InGameFile(HttpContext context, string name)
        {
            var root = context.RequestServices.GetRequiredService<IOptions<WebApiConfigSettings>>().Value.StreamingAssetsPath;
            if (string.IsNullOrWhiteSpace(root))
            {
                return null;
            }

            var contentRoot = context.RequestServices.GetRequiredService<IWebHostEnvironment>().ContentRootPath;
            var path        = Path.GetFullPath(Path.Combine(root, InGameFolder, name), contentRoot);
            return File.Exists(path) ? path : null;
        }

        private static void ServeFiles(IApplicationBuilder branch, IFileProvider files)
        {
            branch.UseStaticFiles(new StaticFileOptions
            {
                FileProvider          = files,
                ServeUnknownFileTypes = true,
                DefaultContentType    = "application/octet-stream"
            });

            branch.Run(context =>
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return Task.CompletedTask;
            });
        }

        // A 404 makes the client skip the clip, the gaps of a partial bank would play as zeros
        private static bool IsCovered(HttpContext context, IFileProvider files)
        {
            var path = context.Request.Path.Value;
            if (path == null || !path.EndsWith(".bnk", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var coverage = files.GetFileInfo(path + "_ac");
            var bank     = files.GetFileInfo(path);
            if (!coverage.Exists || !bank.Exists)
            {
                return true;
            }

            var range = context.Request.GetTypedHeaders().Range;
            if (range == null || range.Ranges.Count != 1)
            {
                return false;
            }

            var requested = range.Ranges.First();
            if (requested.From == null)
            {
                return false;
            }

            var first = requested.From.Value;
            var last  = Math.Min(requested.To ?? long.MaxValue, bank.Length - 1);

            // Same layout as the client's cache: u32 version, u32 count, count x { u32 first, u32 last } inclusive
            using var reader = new BinaryReader(coverage.CreateReadStream());
            reader.ReadUInt32();
            var count = reader.ReadUInt32();
            for (var i = 0; i < count; i++)
            {
                var start = reader.ReadUInt32();
                var end   = reader.ReadUInt32();
                if (start <= first && last <= end)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
