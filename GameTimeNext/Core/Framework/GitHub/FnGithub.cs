using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace GameTimeNext.Core.Framework.GitHub
{
    public class FnGithub
    {
        public static async Task<UpdateCheckResult> CheckForUpdateAsync(
            string currentVersion,
            string owner,
            string repo)
        {
            try
            {
                Version? current = ExtractVersion(currentVersion);

                if (current == null)
                {
                    return new UpdateCheckResult
                    {
                        UpdateAvailable = false,
                        CurrentVersion = null,
                        LatestVersion = null,
                        ReleaseUrl = string.Empty
                    };
                }

                using HttpClient client = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(10)
                };

                client.DefaultRequestHeaders.UserAgent.ParseAdd("GameTimeNext Update Checker");
                client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");

                string url = $"https://api.github.com/repos/{owner}/{repo}/releases";

                using HttpResponseMessage response = await client.GetAsync(url).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    return new UpdateCheckResult
                    {
                        UpdateAvailable = false,
                        CurrentVersion = current,
                        LatestVersion = null,
                        ReleaseUrl = string.Empty
                    };
                }

                string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                List<GitHubReleaseDto>? releases = JsonSerializer.Deserialize<List<GitHubReleaseDto>>(json);

                if (releases == null || releases.Count == 0)
                {
                    return new UpdateCheckResult
                    {
                        UpdateAvailable = false,
                        CurrentVersion = current,
                        LatestVersion = null,
                        ReleaseUrl = string.Empty
                    };
                }

                GitHubReleaseDto? latestRelease = releases
                    .Where(r => !r.draft)
                    .Select(r => new
                    {
                        Release = r,
                        Version = ExtractVersion(r.tag_name)
                    })
                    .Where(x => x.Version != null)
                    .OrderByDescending(x => x.Version)
                    .Select(x => x.Release)
                    .FirstOrDefault();

                if (latestRelease == null)
                {
                    return new UpdateCheckResult
                    {
                        UpdateAvailable = false,
                        CurrentVersion = current,
                        LatestVersion = null,
                        ReleaseUrl = string.Empty
                    };
                }

                Version? latest = ExtractVersion(latestRelease.tag_name);

                return new UpdateCheckResult
                {
                    UpdateAvailable = latest != null && latest > current,
                    CurrentVersion = current,
                    LatestVersion = latest,
                    ReleaseUrl = latestRelease.html_url ?? string.Empty
                };
            }
            catch (TaskCanceledException)
            {
                return new UpdateCheckResult
                {
                    UpdateAvailable = false,
                    CurrentVersion = null,
                    LatestVersion = null,
                    ReleaseUrl = string.Empty
                };
            }
            catch (HttpRequestException)
            {
                return new UpdateCheckResult
                {
                    UpdateAvailable = false,
                    CurrentVersion = null,
                    LatestVersion = null,
                    ReleaseUrl = string.Empty
                };
            }
            catch
            {
                return new UpdateCheckResult
                {
                    UpdateAvailable = false,
                    CurrentVersion = null,
                    LatestVersion = null,
                    ReleaseUrl = string.Empty
                };
            }
        }

        private static Version? ExtractVersion(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            Match match = Regex.Match(input, @"\d+(\.\d+){1,3}");

            if (!match.Success)
                return null;

            return Version.TryParse(match.Value, out Version? version)
                ? version
                : null;
        }

        private class GitHubReleaseDto
        {
            public string tag_name { get; set; } = string.Empty;

            public string html_url { get; set; } = string.Empty;

            public bool draft { get; set; }

            public bool prerelease { get; set; }
        }
    }
}