using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Net.Http.Headers;
using RemoteMonitoring.ApiServices;

namespace RemoteMonitoring.Base
{
    /// <summary>
    /// Serves the cover of a profile under /cover/{pfid}. The file path comes from arcora (ProfileDto.PPFN), never from
    /// the request, so no other file of the PC can be read. A missing cover falls back to arcora's placeholder.
    /// </summary>
    public static class CoverEndpoint
    {
        private const string _placeholder = "images/cover-placeholder.png";

        private static readonly FileExtensionContentTypeProvider _contentTypes = new();

        public static void MapCovers(this WebApplication app)
        {
            app.MapGet("/cover/{pfid:long}", async (long pfid, HttpContext context, ProfilesApiService profilesApiService, IWebHostEnvironment environment) =>
            {
                string? path = await profilesApiService.GetCoverPath(pfid);

                if (path is null || !File.Exists(path) || !_contentTypes.TryGetContentType(path, out string? contentType) || !contentType.StartsWith("image/"))
                {
                    path = Path.Combine(environment.WebRootPath, _placeholder);
                    contentType = "image/png";
                }

                // Revalidated by date and tag, so a cover changed in arcora shows up after a minute at the latest
                FileInfo file = new FileInfo(path);
                EntityTagHeaderValue tag = new EntityTagHeaderValue($"\"{file.LastWriteTimeUtc.Ticks:x}-{file.Length:x}\"");
                context.Response.Headers.CacheControl = "public, max-age=60";

                return Results.File(path, contentType, lastModified: file.LastWriteTimeUtc, entityTag: tag);
            });
        }
    }
}
