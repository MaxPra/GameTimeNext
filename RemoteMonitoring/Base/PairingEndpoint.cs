using System.Net;

namespace RemoteMonitoring.Base
{
    /// <summary>
    /// Only paired phones may use the web app: /pair?code=... (the link of the QR code in arcora's settings) stores the
    /// code in a cookie, every other request without a valid cookie gets a page that asks to scan the QR code. While the
    /// current code is unknown (arcora not reachable), a page that loads itself again is shown instead, so a valid cookie
    /// is never sent to pair again. Only the files these pages need (stylesheet, fonts, logo, favicon) are free.
    /// </summary>
    public static class PairingEndpoint
    {
        private static readonly string[] _publicPrefixes = ["/branding/", "/fonts/", "/favicon.ico"];

        // Interval in which the page for an unreachable arcora loads itself again
        private const int UnreachableReloadSeconds = 5;

        public static void UsePairing(this WebApplication app)
        {
            app.Use(async (context, next) =>
            {
                string path = context.Request.Path.Value ?? "/";
                PairingService pairing = context.RequestServices.GetRequiredService<PairingService>();

                if (path.Equals("/pair", StringComparison.OrdinalIgnoreCase))
                {
                    string? code = context.Request.Query["code"];

                    switch (await pairing.CheckAsync(code))
                    {
                        case PairingCheckResult.Paired:
                            context.Response.Cookies.Append(PairingService.CookieName, code!, new CookieOptions
                            {
                                HttpOnly = true,
                                SameSite = SameSiteMode.Lax,
                                Expires = DateTimeOffset.Now.AddYears(1),
                                IsEssential = true
                            });

                            context.Response.Redirect("/");
                            return;

                        // The reload asks again with the same code, so the pairing completes once arcora answers
                        case PairingCheckResult.Unreachable:
                            await WriteUnreachablePageAsync(context);
                            return;

                        default:
                            await WriteUnpairedPageAsync(context, $"This QR code is no longer valid. Scan the current one in {AppInfo.Name}'s settings.");
                            return;
                    }
                }

                if (IsPublic(path))
                {
                    await next();
                    return;
                }

                switch (await pairing.CheckAsync(context.Request.Cookies[PairingService.CookieName]))
                {
                    case PairingCheckResult.Paired:
                        await next();
                        return;

                    // Not a reason to pair again: the cookie may well be valid
                    case PairingCheckResult.Unreachable:
                        await WriteUnreachablePageAsync(context);
                        return;

                    default:
                        await WriteUnpairedPageAsync(context, $"Scan the QR code in {AppInfo.Name}'s settings (Monitoring, Remote Monitoring) to use this device.");
                        return;
                }
            });
        }

        private static bool IsPublic(string path)
        {
            // The stylesheet is served fingerprinted too (app.<hash>.css)
            return path.EndsWith(".css", StringComparison.OrdinalIgnoreCase)
                || _publicPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        }

        private static Task WriteUnpairedPageAsync(HttpContext context, string message)
        {
            return WritePageAsync(context, StatusCodes.Status401Unauthorized, "Connect this device", message, reloadSeconds: null);
        }

        /// <summary>
        /// The current pairing code is unknown (arcora not reachable, or the web app has just started): the page loads
        /// itself again until arcora answers.
        /// </summary>
        private static Task WriteUnreachablePageAsync(HttpContext context)
        {
            return WritePageAsync(context, StatusCodes.Status503ServiceUnavailable, $"Can't reach {AppInfo.Name}",
                $"{AppInfo.Name} on your PC can't be reached right now. Make sure it is running, this page tries again by itself.",
                reloadSeconds: UnreachableReloadSeconds);
        }

        /// <summary>
        /// Plain page without Blazor (its scripts and connection need pairing), in the look of the web app.
        /// </summary>
        private static async Task WritePageAsync(HttpContext context, int statusCode, string title, string message, int? reloadSeconds)
        {
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "text/html; charset=utf-8";

            string reload = reloadSeconds is null ? string.Empty : $"""<meta http-equiv="refresh" content="{reloadSeconds}" />""";

            await context.Response.WriteAsync($$"""
                <!DOCTYPE html>
                <html lang="en">
                <head>
                    <meta charset="utf-8" />
                    <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no, viewport-fit=cover" />
                    <meta name="theme-color" content="#101014" />
                    {{reload}}
                    <title>{{AppInfo.Name}}</title>
                    <link rel="icon" type="image/svg+xml" href="/branding/icon.svg" />
                    <link rel="stylesheet" href="/app.css" />
                </head>
                <body>
                    <div class="arc-glow min-h-dvh">
                        <main class="mx-auto flex max-w-md flex-col gap-6 px-4 pt-16 pb-8">
                            <img class="h-[22px] w-auto self-start" src="/branding/wordmark.svg" alt="{{AppInfo.Name}}" />
                            <div class="arc-card">
                                <p class="text-base font-semibold text-white">{{WebUtility.HtmlEncode(title)}}</p>
                                <p class="text-[13px] text-arc-muted">{{WebUtility.HtmlEncode(message)}}</p>
                            </div>
                        </main>
                    </div>
                </body>
                </html>
                """);
        }
    }
}
