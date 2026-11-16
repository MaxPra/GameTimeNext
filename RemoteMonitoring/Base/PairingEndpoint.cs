using System.Net;

namespace RemoteMonitoring.Base
{
    /// <summary>
    /// Only paired phones may use the web app: /pair?code=... (the link of the QR code in arcora's settings) stores the
    /// code in a cookie, every other request without a valid cookie gets a page that asks to scan the QR code. Only the
    /// files that page needs (stylesheet, fonts, logo, favicon) are free.
    /// </summary>
    public static class PairingEndpoint
    {
        private static readonly string[] _publicPrefixes = ["/branding/", "/fonts/", "/favicon.ico"];

        public static void UsePairing(this WebApplication app)
        {
            app.Use(async (context, next) =>
            {
                string path = context.Request.Path.Value ?? "/";
                PairingService pairing = context.RequestServices.GetRequiredService<PairingService>();

                if (path.Equals("/pair", StringComparison.OrdinalIgnoreCase))
                {
                    string? code = context.Request.Query["code"];

                    if (await pairing.IsValidAsync(code))
                    {
                        context.Response.Cookies.Append(PairingService.CookieName, code!, new CookieOptions
                        {
                            HttpOnly = true,
                            SameSite = SameSiteMode.Lax,
                            Expires = DateTimeOffset.Now.AddYears(1),
                            IsEssential = true
                        });

                        context.Response.Redirect("/");
                        return;
                    }

                    await WriteUnpairedPageAsync(context, $"This QR code is no longer valid. Scan the current one in {AppInfo.Name}'s settings.");
                    return;
                }

                if (IsPublic(path) || await pairing.IsValidAsync(context.Request.Cookies[PairingService.CookieName]))
                {
                    await next();
                    return;
                }

                await WriteUnpairedPageAsync(context, $"Scan the QR code in {AppInfo.Name}'s settings (Monitoring, Remote Monitoring) to use this device.");
            });
        }

        private static bool IsPublic(string path)
        {
            // The stylesheet is served fingerprinted too (app.<hash>.css)
            return path.EndsWith(".css", StringComparison.OrdinalIgnoreCase)
                || _publicPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Plain page without Blazor (its scripts and connection need pairing), in the look of the web app.
        /// </summary>
        private static async Task WriteUnpairedPageAsync(HttpContext context, string message)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "text/html; charset=utf-8";

            await context.Response.WriteAsync($$"""
                <!DOCTYPE html>
                <html lang="en">
                <head>
                    <meta charset="utf-8" />
                    <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no, viewport-fit=cover" />
                    <meta name="theme-color" content="#101014" />
                    <title>{{AppInfo.Name}}</title>
                    <link rel="icon" type="image/svg+xml" href="/branding/icon.svg" />
                    <link rel="stylesheet" href="/app.css" />
                </head>
                <body>
                    <div class="arc-glow min-h-dvh">
                        <main class="mx-auto flex max-w-md flex-col gap-6 px-4 pt-16 pb-8">
                            <img class="h-[22px] w-auto self-start" src="/branding/wordmark.svg" alt="{{AppInfo.Name}}" />
                            <div class="arc-card">
                                <p class="text-base font-semibold text-white">Connect this device</p>
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
